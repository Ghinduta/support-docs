namespace StackOverflowRAG.Core.Models;

/// <summary>
/// Result summary from ingestion pipeline.
/// </summary>
public class IngestionResult
{
    /// <summary>
    /// Number of documents successfully loaded from CSV
    /// </summary>
    public int DocumentsLoaded { get; set; }

    /// <summary>
    /// Number of chunks created from documents
    /// </summary>
    public int ChunksCreated { get; set; }

    /// <summary>
    /// Number of embeddings generated
    /// </summary>
    public int EmbeddingsGenerated { get; set; }

    /// <summary>
    /// Number of chunks upserted to Qdrant
    /// </summary>
    public int QdrantUpserts { get; set; }

    /// <summary>
    /// Total duration of ingestion in seconds
    /// </summary>
    public double DurationSeconds { get; set; }

    /// <summary>
    /// Number of errors encountered (documents skipped)
    /// </summary>
    public int Errors { get; set; }

    /// <summary>
    /// Total chunks in Qdrant after ingestion (validation)
    /// </summary>
    public long TotalChunksInQdrant { get; set; }

    /// <summary>
    /// Whether validation passed (expected chunks match actual)
    /// </summary>
    public bool ValidationPassed { get; set; }

    /// <summary>
    /// Any error messages
    /// </summary>
    public List<string> ErrorMessages { get; set; } = new();
}
