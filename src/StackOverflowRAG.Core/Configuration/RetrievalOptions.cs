using System.ComponentModel.DataAnnotations;

namespace StackOverflowRAG.Core.Configuration;

/// <summary>
/// Configuration options for retrieval and search.
/// </summary>
public class RetrievalOptions
{
    public const string SectionName = "Retrieval";

    /// <summary>
    /// Default number of chunks to retrieve
    /// </summary>
    [Range(1, 100)]
    public int DefaultTopK { get; set; } = 10;

    /// <summary>
    /// Weight for vector similarity in hybrid search (0.0 to 1.0)
    /// </summary>
    [Range(0.0, 1.0)]
    public double VectorWeight { get; set; } = 0.5;

    /// <summary>
    /// Weight for keyword matching in hybrid search (0.0 to 1.0)
    /// </summary>
    [Range(0.0, 1.0)]
    public double KeywordWeight { get; set; } = 0.5;

    /// <summary>
    /// Validates configuration values
    /// </summary>
    public void Validate()
    {
        if (DefaultTopK < 1 || DefaultTopK > 100)
        {
            throw new InvalidOperationException("DefaultTopK must be between 1 and 100");
        }

        if (VectorWeight < 0 || VectorWeight > 1)
        {
            throw new InvalidOperationException("VectorWeight must be between 0.0 and 1.0");
        }

        if (KeywordWeight < 0 || KeywordWeight > 1)
        {
            throw new InvalidOperationException("KeywordWeight must be between 0.0 and 1.0");
        }
    }
}
