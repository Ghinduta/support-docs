namespace StackOverflowRAG.Core.Models;

/// <summary>
/// Metadata for query/ask endpoint telemetry
/// </summary>
public class QueryMetadata
{
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public required string Question { get; set; }
    public long LatencyMs { get; set; }
    public int TokensInput { get; set; }
    public int TokensOutput { get; set; }
    public double EstimatedCost { get; set; }
    public bool CacheHit { get; set; }
    public int RetrievedChunks { get; set; }
    public string SearchType { get; set; } = "hybrid";
    public int TopK { get; set; }
}
