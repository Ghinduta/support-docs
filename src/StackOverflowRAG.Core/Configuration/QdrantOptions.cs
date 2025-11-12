using System.ComponentModel.DataAnnotations;

namespace StackOverflowRAG.Core.Configuration;

/// <summary>
/// Configuration options for Qdrant vector database.
/// </summary>
public class QdrantOptions
{
    public const string SectionName = "Qdrant";

    /// <summary>
    /// Qdrant server host (e.g., http://localhost:6333)
    /// </summary>
    [Required]
    public string Host { get; set; } = "http://localhost:6333";

    /// <summary>
    /// Collection name for storing chunks
    /// </summary>
    [Required]
    public string CollectionName { get; set; } = "stackoverflow_chunks";

    /// <summary>
    /// API key for Qdrant Cloud (optional, not needed for local)
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// Validates configuration values
    /// </summary>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Host))
        {
            throw new InvalidOperationException("Qdrant Host must be configured");
        }

        if (string.IsNullOrWhiteSpace(CollectionName))
        {
            throw new InvalidOperationException("Qdrant CollectionName must be configured");
        }
    }
}
