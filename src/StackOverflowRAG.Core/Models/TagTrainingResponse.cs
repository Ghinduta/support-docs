namespace StackOverflowRAG.Core.Models;

/// <summary>
/// Response model for tag classifier training
/// </summary>
public class TagTrainingResponse
{
    /// <summary>
    /// Whether training succeeded
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Path where the model was saved
    /// </summary>
    public string? ModelPath { get; set; }

    /// <summary>
    /// Number of questions used for training
    /// </summary>
    public int QuestionsProcessed { get; set; }

    /// <summary>
    /// Number of tags in the trained model
    /// </summary>
    public int TagCount { get; set; }

    /// <summary>
    /// List of top tags in the model
    /// </summary>
    public List<string> TopTags { get; set; } = new();

    /// <summary>
    /// Training metrics
    /// </summary>
    public string? Metrics { get; set; }

    /// <summary>
    /// Any error messages
    /// </summary>
    public List<string> Errors { get; set; } = new();

    /// <summary>
    /// Training duration in seconds
    /// </summary>
    public double TrainingDurationSeconds { get; set; }
}
