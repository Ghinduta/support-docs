namespace StackOverflowRAG.Core.Models;

/// <summary>
/// Metadata for ingestion endpoint telemetry
/// </summary>
public class IngestionMetadata
{
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public int DocsLoaded { get; set; }
    public int ChunksCreated { get; set; }
    public int EmbeddingsGenerated { get; set; }
    public long DurationMs { get; set; }
    public double EstimatedCost { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
}
