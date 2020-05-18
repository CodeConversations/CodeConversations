namespace CodeConversations.Models
{
    public class CodeSnippet
    {
        public string Language { get; set; }
        public string Snippet { get; set; }
        public int? NextCardToSend { get; set; }
    }
}