namespace StackOverflowRAG.Core.Configuration;

/// <summary>
/// Configuration for the evaluation runner.
/// </summary>
public class EvalOptions
{
    public const string SectionName = "Evals";

    /// <summary>
    /// Folder holding dataset.json and runs/*.json
    /// </summary>
    public string Directory { get; set; } = "evals";

    /// <summary>
    /// How many test questions to generate
    /// </summary>
    public int DatasetSize { get; set; } = 100;

    /// <summary>
    /// How many questions are evaluated in parallel
    /// </summary>
    public int MaxParallelism { get; set; } = 5;
}
