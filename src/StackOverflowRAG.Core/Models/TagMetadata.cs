namespace StackOverflowRAG.Core.Models;

/// <summary>
/// Metadata for tag suggestion endpoint telemetry
/// </summary>
public class TagMetadata
{
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public int InputLength { get; set; }
    public required List<string> PredictedTags { get; set; }
    public long LatencyMs { get; set; }
    public int TopK { get; set; }
    public bool ModelLoaded { get; set; }
}
