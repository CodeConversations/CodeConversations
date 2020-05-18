using System;
using System.Collections.Generic;
using Microsoft.Bot.Schema;

namespace CodeConversations.Bots
{
    internal static class UserGame
    {
        private static readonly Dictionary<string, ChatUser> Users = new Dictionary<string, ChatUser>();
        private static readonly List<string> Level1MessageTemplates;
        private static readonly List<string> Level2MessageTemplates;
        private static readonly Random Random;

        public static ChatUser CurrentChatUser { get; set; }

        static UserGame()
        {
            Random = new Random();
            Level1MessageTemplates = new List<string>
            {
                "Coding is fun with friends. 👋",
                "Happy to run this one too. 🎉",
                "Happy to run this one too. 🎉🎉",
                "I got this. 👌",
                "Coding is fun with friends. 👋",
                "I got this.",
                "Ok, I see you got some more to run. Let me do it.",
                "Let me run this one too. Drums roll please! 🥁",
                "Coding is fun with friends. 👋",
                "Let me run this one too.",
                "I got this. 👌",
                "Let me run this one too. Drums roll please! 🥁🥁",
                "Happy this one too.",
                "Let me run this one too.",
                "Let me run this one too.",
                "Happy to run this one too. 🎉",
                "I got this. 👌",
                "Coding is fun with friends. 👋",
                "I got this",
                "Coding is fun with friends. 👋",
                "I got this. 👌",
                "Happy to run this one too. 🎉🎉"
            };

            Level2MessageTemplates = new List<string>
            {
                "Coding is fun with friends. 👋",
                "Happy to run this one too. 🎉🎉🎉",
                @"{0}, we are on a coding spree I see. Let me run it. 😋 😋 😋",
                "I got this. 👌",
                "Ok, I see you got some more to run. Let me do it.",
                "Coding is fun with friends. 👋",
                "Let me run this one too. Drums roll please! 🥁🥁",
                "Happy this one too.",
                @"{0}, we are on a coding spree I see. Let me run it. 😋",
                "This keyboard is on fire. 🔥💻🔥",
                "Let me run this one too.",
                "This keyboard is on fire. 🔥💻🔥",
                @"{}, we are on a coding spree I see. Let me run it. 😋 😋",
                "Let me run this one too. Drums roll please! 🥁🥁🥁",
                "Let me run this one too.",
                "This keyboard is on fire. 🔥💻🔥",
                @"{0}, we are on a coding spree I see. Let me run it. 😋 😋",
                "Happy to run this one too. 🎉",
                "This keyboard is on fire. 🔥💻🔥",
                "This keyboard is on fire. 🔥💻🔥",
                "I got this. 👌",
                "Coding is fun with friends. 👋",
                "I got this",
                @"{0}, we are on a coding spree I see. Let me run it. 😋 😋 😋",
                "Let me run this one too. Drums roll please! 🥁🥁",
                "Coding is fun with friends. 👋",
                "I got this. 👌",
                "Happy to run this one too. 🎉🎉"
            };
        }

        

        private static string GenerateMessage(Mention mention, string template)
        {
            return string.Format(template, mention.Text);
        }

        internal static string GetMessageForUser(Mention userMention)
        {
            var user = Users[userMention.Text];

            var template = @"Hey {0} It looks like you're typing some code. Let me run it for you! 😊";
            
            if (user.CodeSubmissionCount < 3)
            {
                template = GetMessageTemplate(Level1MessageTemplates);
            }
            else
            {
                var split = Random.NextDouble();

                if (split > 5)
                {
                    split = Random.NextDouble();
                    if (user.HasCustomTemplates && split > 7)
                    {
                        template = user.GetMessageTemplate();
                    }
                    else
                    {

                        template = GetMessageTemplate(Level2MessageTemplates);
                    }
                }
                else
                {
                    template = GetMessageTemplate(Level1MessageTemplates);
                }
            }

            return GenerateMessage(userMention, template);
        }

        private static string[] CreateUserMessages(Mention userMention, string fromId)
        {
            var messages = Array.Empty<string>();
            return messages;
        }

        private static string GetMessageTemplate(List<string> messages)
        {
            var messageIndex = Random.Next(0, messages.Count);

            messageIndex += messageIndex + Random.Next(0, messages.Count) + Random.Next(0, messages.Count * 2);
            messageIndex = messageIndex % messages.Count;

            return messages[messageIndex];
        }

        public static ChatUser GetOrCreateUser(Mention userMention, ChannelAccount from)
        {
            if (!Users.TryGetValue(userMention.Text, out var user))
            {
                var additionalMessages = CreateUserMessages(userMention, from.Id);
                var newUser = new ChatUser(userMention, from.Id,  additionalMessages);
                Users.Add(userMention.Text, newUser);
                return newUser;
            }

            return user;
        }
    }
}