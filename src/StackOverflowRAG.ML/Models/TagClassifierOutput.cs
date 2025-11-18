using Microsoft.ML.Data;

namespace StackOverflowRAG.ML.Models;

/// <summary>
/// Output from tag classification model
/// </summary>
public class TagClassifierOutput
{
    /// <summary>
    /// Predicted tag label
    /// </summary>
    [ColumnName("PredictedLabel")]
    public string PredictedTag { get; set; } = string.Empty;

    /// <summary>
    /// Confidence score for the prediction (0-1)
    /// </summary>
    [ColumnName("Score")]
    public float[] Score { get; set; } = Array.Empty<float>();
}
