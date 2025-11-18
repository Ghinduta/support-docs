namespace StackOverflowRAG.Core.Models;

/// <summary>
/// Response model for tag suggestion
/// </summary>
public class TagSuggestionResponse
{
    /// <summary>
    /// Suggested tags ordered by confidence
    /// </summary>
    public List<string> Tags { get; set; } = new();

    /// <summary>
    /// Confidence scores for each tag (0-1)
    /// </summary>
    public List<float> Confidence { get; set; } = new();

    /// <summary>
    /// Total number of available tags in the model
    /// </summary>
    public int AvailableTagCount { get; set; }
}
