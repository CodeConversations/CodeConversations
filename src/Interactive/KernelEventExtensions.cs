using System.Linq;
using Microsoft.DotNet.Interactive;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Formatting;

namespace CodeConversations.Interactive
{
    internal static class KernelEventExtensions
    {
        public static FormattedValue GetHtmlOrPlainText(this DisplayEventBase displayEvent)
        {
            var formattedValue =
                displayEvent.FormattedValues.SingleOrDefault(d => d.MimeType == HtmlFormatter.MimeType)
                ?? displayEvent.FormattedValues
                    .SingleOrDefault(d => d.MimeType == PlainTextFormatter.MimeType);

            return formattedValue;
        }

        public static FormattedValue GetHtml(this DisplayEventBase displayEvent)
        {
            var formattedValue =
                displayEvent.FormattedValues.SingleOrDefault(d => d.MimeType == HtmlFormatter.MimeType);

            return formattedValue;
        }

        public static FormattedValue GetPlainText(this DisplayEventBase displayEvent)
        {
            var formattedValue =
                displayEvent.FormattedValues.SingleOrDefault(d => d.MimeType == PlainTextFormatter.MimeType);

            return formattedValue;
        }
    }
}