namespace StackOverflowRAG.Core.Models;

/// <summary>
/// Telemetry data for query/ask endpoint logging
/// </summary>
public class QueryTelemetry
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
