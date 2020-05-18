// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Teams;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Schema.Teams;
using Microsoft.DotNet.Interactive;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Server;
using Newtonsoft.Json.Linq;
using CodeConversations.Infrastructure;
using CodeConversations.Models;
using CodeConversations.Workers;
using Microsoft.DotNet.Interactive.Formatting;
using Microsoft.Extensions.Configuration;

namespace CodeConversations.Bots
{
    public class CodeConversationsBot : TeamsActivityHandler
    {
        private UserState _userState;

        readonly string regularExpression = @"(\r(.*?)\r)";

        private readonly IConfiguration _configuration;
        private string _botId;

        public CodeConversationsBot(UserState userState,
            IConfiguration configuration)
        {
            _configuration = configuration;
            _userState = userState;
            _botId = configuration["MicrosoftAppId"];
        }


        public override async Task OnTurnAsync(ITurnContext turnContext,
            CancellationToken cancellationToken)
        {
            await base.OnTurnAsync(turnContext, cancellationToken);
            await _userState.SaveChangesAsync(turnContext, false, cancellationToken);
        }

        protected override async Task OnTeamsMembersAddedAsync(IList<TeamsChannelAccount> teamsMembersAdded,
            TeamInfo teamInfo,
            ITurnContext<IConversationUpdateActivity> turnContext,
            CancellationToken cancellationToken)
        {
            await base.OnTeamsMembersAddedAsync(teamsMembersAdded, teamInfo, turnContext, cancellationToken);
            DotNetInteractiveProcessRunner.Instance.SessionLanguage = null;
            var card = CardUtilities.CreateAdaptiveCardAttachment(CardJsonFiles.SelectLanguage);
            var attach = MessageFactory.Attachment(card);
            await turnContext.SendActivityAsync(attach);
        }

#pragma warning disable CS1998
        protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext,
            CancellationToken cancellationToken)
        {
            var value = turnContext.Activity;
            var attachments = turnContext.Activity.Attachments;
            if (turnContext.Activity.Value == null) // someone typed in something, it isn't a card
            {

                var content = turnContext.Activity.Text;
                var code = CheckForCode(content);

                var conversationReference = turnContext.Activity.GetConversationReference();
                var mention = new Mention
                {
                    Mentioned = turnContext.Activity.From,
                    Text = $"<at>{turnContext.Activity.From.Name}</at>",
                };

                if (!string.IsNullOrEmpty(code))
                {
                    if (DotNetInteractiveProcessRunner.Instance.CanExecuteCode)
                    {
                        var submissionToken = Guid.NewGuid().ToString("N");
                        var messageText = string.Empty;
                        var user = UserGame.GetOrCreateUser(mention, turnContext.Activity.From);
                        if (UserGame.CurrentChatUser?.Id != user.Id)
                        {
                            UserGame.CurrentChatUser = user;
                            messageText = $"Hey {mention.Text} It looks like you're typing some code. Let me run it for you! 😊";
                        }
                        else
                        {
                            messageText = UserGame.GetMessageForUser( mention);
                        }

                        await turnContext.Adapter.ContinueConversationAsync(_botId, conversationReference, async (context, token) =>
                        {
                            var message = MessageFactory.Text(messageText);
                            if (messageText.Contains(mention.Text))
                            {
                                message.Entities.Add(mention);
                            }
                            await context.SendActivityAsync(message, token);
                        }, cancellationToken);

                        // build the envelope
                        var submitCode = new SubmitCode(code);
                        submitCode.SetToken(submissionToken);
                        var envelope = KernelCommandEnvelope.Create(submitCode);
                        var channel = ContentSubjectHelper.GetOrCreateChannel(submissionToken);
                        EnvelopeHelper.StoreEnvelope(submissionToken, envelope);
                        var cardSent = false;
                        channel
                            .Timeout(DateTimeOffset.UtcNow.Add(TimeSpan.FromMinutes(1)))
                            .Buffer(TimeSpan.FromSeconds(1))
                            .Subscribe(
                         onNext: async formattedValues =>
                         {
                             turnContext.Adapter.ContinueConversationAsync(_botId, conversationReference,
                                 (context, token) =>
                                 {
                                     if (formattedValues.Count > 0)
                                     {
                                         var hasHtml = formattedValues.Any(f => f.MimeType == HtmlFormatter.MimeType);

                                         if (hasHtml)
                                         {
                                             if (!cardSent)
                                             {
                                                 cardSent = true;
                                                 var card = new HeroCard
                                                 {
                                                     Title = "Your output is too awesome 😎",
                                                     Subtitle = "Use the viewer to see it.",
                                                     Buttons = new List<CardAction>
                                                     {
                                                        new TaskModuleAction("Open Viewer",
                                                            new {data = submissionToken})
                                                     },
                                                 }.ToAttachment();
                                                 var message = MessageFactory.Attachment(card);
                                                 context.SendActivityAsync(message, token).Wait();
                                             }
                                         }
                                         else
                                         {
                                             var content = string.Join("\n", formattedValues.Select(f => f.Value));
                                             var message = MessageFactory.Text($"```\n{content.HtmlEncode()}");
                                             context.SendActivityAsync(message, token).Wait();
                                         }
                                     }

                                     return Task.CompletedTask;
                                 }, cancellationToken).Wait();
                         }, onCompleted: async () =>
                         {
                             await turnContext.Adapter.ContinueConversationAsync(_botId, conversationReference, async (context, token) =>
                             {
                                 await Task.Delay(1000);
                                 var message = MessageFactory.Text($"{mention.Text} all done here 👍");
                                 message.Entities.Add(mention);
                                 await context.SendActivityAsync(message, token);
                             }, cancellationToken);
                         },
                           onError: async error =>
                           {
                               await turnContext.Adapter.ContinueConversationAsync(_botId, conversationReference, async (context, token) =>
                               {
                                   await Task.Delay(1000);
                                   var message = MessageFactory.Text($@"{mention.Text} there were some issues 👎 :\n {error.Message}");
                                   message.Entities.Add(mention);
                                   await context.SendActivityAsync(message, token);
                               }, cancellationToken);
                           });

                        user.IncrementCodeSubmissionCount();
                        await DotNetInteractiveProcessRunner.Instance.ExecuteEnvelope(submissionToken);
                    }
                    else
                    {
                        await turnContext.Adapter.ContinueConversationAsync(_botId, conversationReference, async (context, token) =>
                        {
                            var message = MessageFactory.Text($"Sorry {mention.Text} cannot execute your code now. 😓");
                            message.Entities.Add(mention);
                            await context.SendActivityAsync(message, token);
                        }, cancellationToken);
                    }
                }
                else if (string.IsNullOrWhiteSpace(DotNetInteractiveProcessRunner.Instance.SessionLanguage))
                {
                    var card = CardUtilities.CreateAdaptiveCardAttachment(CardJsonFiles.SelectLanguage);
                    var attach = MessageFactory.Attachment(card);
                    await turnContext.SendActivityAsync(attach, cancellationToken);
                }
                else if (content.Contains("👊"))
                {
                    var mentioned = turnContext.Activity.GetMentions()?.FirstOrDefault(m => m.Mentioned.Id.EndsWith(_botId));
                    if (mentioned != null)
                    {
                        await turnContext.Adapter.ContinueConversationAsync(_botId, conversationReference,
                            async (context, token) =>
                            {
                                var message = MessageFactory.Text($"{mention.Text} back at you my friend! 👊");
                                message.Entities.Add(mention);
                                await context.SendActivityAsync(message, token);
                            }, cancellationToken);
                    }
                }
            }
            else
            {
                var userAction = turnContext.Activity.Value;

                if (((JObject)userAction).Value<string>("userAction").Equals("SelectLanguage"))
                {
                    if (string.IsNullOrWhiteSpace(DotNetInteractiveProcessRunner.Instance.SessionLanguage))
                    {
                        var language = ((JObject)userAction).Value<string>("language");
                        DotNetInteractiveProcessRunner.Instance.SessionLanguage = language;
                        var languageLabel = ((JObject)userAction).Value<string>("languageLabel");
                        var message = MessageFactory.Text($"All set. Let's write some {DotNetInteractiveProcessRunner.Instance.SessionLanguage} code together! 🤘🏻");
                        await turnContext.SendActivityAsync(message, cancellationToken);
                    }
                }
            }
        }

        protected override async Task<TaskModuleResponse> OnTeamsTaskModuleFetchAsync(
            ITurnContext<IInvokeActivity> turnContext,
            TaskModuleRequest taskModuleRequest,
            CancellationToken cancellationToken)
        {

            var token = ((JObject)taskModuleRequest.Data)["data"].Value<string>();

            var url = $"https://{ _configuration["CodeConversationsDomain"]}/executor?Token={token}";

            return new TaskModuleResponse
            {
                Task = new TaskModuleContinueResponse
                {
                    Type = "continue",
                    Value = new TaskModuleTaskInfo
                    {
                        Height = "large",
                        Width = "large",
                        Title = "Powered by .NET interactive aka.ms/codeconversations",
                        Url = url,
                        FallbackUrl = url
                    }
                }
            };
        }
#pragma warning restore CS1998

        string CheckForCode(string content)
        {
            var isMatch = Regex.IsMatch(content, regularExpression);
            var code = string.Empty;
            if (DoesMessageContainCode(content))
            {
                code = GetCodeFromMessage(content);
                code = HttpUtility.HtmlDecode(code);
            }
            return code;
        }

        private bool DoesMessageContainCode(string messageText)
        {
            var matches = Regex.Matches(messageText, regularExpression, RegexOptions.Singleline);
            return matches.Any();
        }

        private string GetCodeFromMessage(string messageText)
        {
            var matches = Regex.Matches(messageText, regularExpression, RegexOptions.Singleline);
            var result = matches.First().Groups[2].Value;
            return result;
        }
    }
}
