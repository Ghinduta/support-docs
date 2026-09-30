namespace StackOverflowRAG.Core.Models;

/// <summary>
/// Live state of the current (or last) ingestion, so any client can show progress.
/// </summary>
public class IngestionProgress
{
    public bool Running { get; set; }

    /// <summary>
    /// Current step, e.g. "Parsing CSV" or "Generating embeddings"
    /// </summary>
    public string Step { get; set; } = string.Empty;

    /// <summary>
    /// 1-based index of the current step out of <see cref="TotalSteps"/>
    /// </summary>
    public int StepNumber { get; set; }

    public int TotalSteps { get; set; } = 5;

    /// <summary>
    /// Items finished in the current step (chunks embedded); 0 when the step has no countable items
    /// </summary>
    public int Done { get; set; }

    /// <summary>
    /// Items in the current step; 0 when the step has no countable items
    /// </summary>
    public int Total { get; set; }

    public int MaxRows { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? FinishedAt { get; set; }
    public string? Error { get; set; }
}
