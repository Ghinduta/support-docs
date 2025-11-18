using Microsoft.ML.Data;

namespace StackOverflowRAG.ML.Models;

/// <summary>
/// Input data for tag classification
/// </summary>
public class TagClassifierInput
{
    /// <summary>
    /// Combined text from question title and body
    /// </summary>
    [LoadColumn(0)]
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Single tag label (for binary classification per tag)
    /// </summary>
    [LoadColumn(1)]
    public string Label { get; set; } = string.Empty;
}
