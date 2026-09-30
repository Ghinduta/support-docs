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
    /// Validates configuration values
    /// </summary>
    public void Validate()
    {
        if (DefaultTopK < 1 || DefaultTopK > 100)
        {
            throw new InvalidOperationException("DefaultTopK must be between 1 and 100");
        }
    }
}
