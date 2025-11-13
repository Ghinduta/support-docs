namespace StackOverflowRAG.Core.Models;

/// <summary>
/// Response model for RAG query with answer and citations
/// </summary>
public class QueryResponse
{
    /// <summary>
    /// The generated answer from the LLM
    /// </summary>
    public string Answer { get; set; } = string.Empty;

    /// <summary>
    /// Citations to source Stack Overflow posts (3-5 typically)
    /// </summary>
    public List<Citation> Citations { get; set; } = new();

    /// <summary>
    /// Metadata about the query execution
    /// </summary>
    public QueryMetadata Metadata { get; set; } = new();
}

/// <summary>
/// Metadata about query execution
/// </summary>
public class QueryMetadata
{
    /// <summary>
    /// Total latency in milliseconds
    /// </summary>
    public long LatencyMs { get; set; }

    /// <summary>
    /// Estimated token count
    /// </summary>
    public int EstimatedTokens { get; set; }

    /// <summary>
    /// Number of chunks retrieved
    /// </summary>
    public int ChunkCount { get; set; }

    /// <summary>
    /// Search type used (hybrid or vector-only)
    /// </summary>
    public string SearchType { get; set; } = string.Empty;
}
