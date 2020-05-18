using System.IO;

namespace CodeConversations.Models
{
    public class CardJsonFiles
    {
        public static string SelectLanguage { get; } = Path.Combine(".", "Cards", $"{nameof(SelectLanguage)}.json");
    }
}