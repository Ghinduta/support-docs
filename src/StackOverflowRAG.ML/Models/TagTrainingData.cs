using Microsoft.ML.Data;

namespace StackOverflowRAG.ML.Models;

/// <summary>
/// Training data model for tag prediction.
/// Combines question title and body into a single text field for TF-IDF feature extraction.
/// </summary>
public class TagTrainingData
{
    /// <summary>
    /// Combined text from question title and body
    /// </summary>
    [LoadColumn(0)]
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Comma-separated list of Stack Overflow tags
    /// </summary>
    [LoadColumn(1)]
    public string Tags { get; set; } = string.Empty;
}
