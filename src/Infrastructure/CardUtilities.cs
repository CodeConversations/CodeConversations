using System.IO;
using Microsoft.Bot.Schema;
using AdaptiveCards.Templating;
using Newtonsoft.Json;

namespace CodeConversations.Infrastructure
{
    public class CardUtilities
    {
        public static Attachment CreateAdaptiveCardAttachment(string filePath)
        {
            var adaptiveCardJson = File.ReadAllText(filePath);
            var adaptiveCardAttachment = new Attachment()
            {
                ContentType = "application/vnd.microsoft.card.adaptive",
                Content = JsonConvert.DeserializeObject(adaptiveCardJson),
            };
            return adaptiveCardAttachment;
        }

        public static Attachment CreateAdaptiveCardFromTemplateAndDataAttachment<T>(string filePath, T data)
        {
            var transformer = new AdaptiveTransformer();
            var templateJson = File.ReadAllText(filePath);
            var dataJson = JsonConvert.SerializeObject(data);
            var cardJson = transformer.Transform(templateJson, dataJson);

            var adaptiveCardAttachment = new Attachment()
            {
                ContentType = "application/vnd.microsoft.card.adaptive",
                Content = JsonConvert.DeserializeObject(cardJson),
            };
            
            return adaptiveCardAttachment;
        }
    }
}