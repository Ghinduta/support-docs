using StackOverflowRAG.Core.Models;

namespace StackOverflowRAG.Core.Interfaces;

/// <summary>
/// Builds the test set and runs evaluations of retrieval and answer quality.
/// </summary>
public interface IEvalService
{
    /// <summary>
    /// Returns the saved test set (empty when none has been generated).
    /// </summary>
    Task<EvalDataset> GetDatasetAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Samples ingested posts, rewrites each into a user-style question and saves the test set, replacing any existing one.
    /// </summary>
    Task<EvalDataset> GenerateDatasetAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs the first <see cref="EvalRunRequest.Size"/> test questions through retrieval, answering and judging, and saves the run.
    /// </summary>
    Task<EvalRun> RunAsync(EvalRunRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists saved runs, newest first, without per-question results.
    /// </summary>
    Task<List<EvalRun>> ListRunsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns one saved run with per-question results, or null.
    /// </summary>
    Task<EvalRun?> GetRunAsync(string id, CancellationToken cancellationToken = default);
}
