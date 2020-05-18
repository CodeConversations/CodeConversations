using System;
using System.Linq;
using Microsoft.Bot.Schema;

namespace CodeConversations.Bots
{
    internal class ChatUser
    {
        public string Id { get; }
        private readonly Mention _userMention;
        private string[] _userTemplates;
        private Random random;
        public int CodeSubmissionCount { get; private set; }

        public ChatUser(Mention userMention, string userId,   params string[] userTemplates)
        {
            random = new Random();
            Id = userId;
            _userMention = userMention;
            _userTemplates = (userTemplates ?? Enumerable.Empty<string>()).ToArray();
        }


        public void IncrementCodeSubmissionCount()
        {
            CodeSubmissionCount++;
        }

        public bool HasCustomTemplates => _userTemplates?.Length > 0;

        public  string GetMessageTemplate()
        {
            var messageIndex = random.Next(0, _userTemplates.Length);

            messageIndex += messageIndex + random.Next(0, _userTemplates.Length) + random.Next(0, _userTemplates.Length * 2);
            messageIndex = messageIndex % _userTemplates.Length;

            return _userTemplates[messageIndex];
        }

    }
}