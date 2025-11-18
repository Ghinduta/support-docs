using System.ComponentModel.DataAnnotations;

namespace StackOverflowRAG.Core.Models;

/// <summary>
/// Request model for training the tag classifier
/// </summary>
public class TagTrainingRequest
{
    /// <summary>
    /// Path to the directory containing Questions.csv, Answers.csv, Tags.csv
    /// If not provided, uses the path from configuration
    /// </summary>
    public string? CsvPath { get; set; }

    /// <summary>
    /// Maximum number of questions to use for training (default: 10000)
    /// </summary>
    [Range(100, 100000, ErrorMessage = "MaxRows must be between 100 and 100,000")]
    public int MaxRows { get; set; } = 10000;

    /// <summary>
    /// Maximum number of unique tags to include in the model (default: 50)
    /// </summary>
    [Range(10, 200, ErrorMessage = "MaxTags must be between 10 and 200")]
    public int MaxTags { get; set; } = 50;

    /// <summary>
    /// Output path for the trained model (default: models/tag-classifier.zip)
    /// </summary>
    public string OutputPath { get; set; } = "models/tag-classifier.zip";
}
