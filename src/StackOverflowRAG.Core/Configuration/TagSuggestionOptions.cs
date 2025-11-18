using System.ComponentModel.DataAnnotations;

namespace StackOverflowRAG.Core.Configuration;

/// <summary>
/// Configuration options for tag suggestion service
/// </summary>
public class TagSuggestionOptions
{
    public const string SectionName = "TagSuggestion";

    /// <summary>
    /// Path to the trained tag classifier model (.zip file)
    /// </summary>
    [Required]
    public string ModelPath { get; set; } = "models/tag-classifier.zip";

    /// <summary>
    /// Default number of tags to suggest
    /// </summary>
    [Range(1, 10)]
    public int DefaultTopK { get; set; } = 5;

    /// <summary>
    /// Whether tag suggestion is enabled
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Validates configuration values
    /// </summary>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(ModelPath))
        {
            throw new InvalidOperationException("ModelPath cannot be null or empty");
        }

        if (DefaultTopK < 1 || DefaultTopK > 10)
        {
            throw new InvalidOperationException("DefaultTopK must be between 1 and 10");
        }
    }
}
