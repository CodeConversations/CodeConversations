using Microsoft.Bot.Schema;
using Newtonsoft.Json.Linq;

namespace CodeConversations.Models
{
    public static class CodeSnippetExtensions
    {
        public static CodeSnippet GetCodeSnippet(this Activity activity)
        {
            if((activity.Value as JObject) != null)
            {
                return ((JObject)activity.Value).ToObject<CodeSnippet>();
            }

            return null;
        }
    }
}