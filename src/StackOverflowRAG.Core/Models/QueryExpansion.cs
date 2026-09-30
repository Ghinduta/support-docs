namespace StackOverflowRAG.Core.Models;

/// <summary>
/// Alternative phrasings of a question (multi-query) and the tokens it cost to produce them.
/// </summary>
public class QueryExpansion
{
    public List<string> Queries { get; set; } = new();
    public int PromptTokens { get; set; }
    public int CompletionTokens { get; set; }
}
