using StackOverflowRAG.Core.Models;

namespace StackOverflowRAG.Core.Interfaces;

/// <summary>
/// Service for ingesting Stack Overflow data from CSV files.
/// Orchestrates the full pipeline: CSV → Parse → Chunk → Embed → Upsert.
/// </summary>
public interface IIngestionService
{
    /// <summary>
    /// Snapshot of the current (or last) ingestion's progress.
    /// </summary>
    IngestionProgress GetProgress();

    /// <summary>
    /// Executes full ingestion pipeline. Throws <see cref="InvalidOperationException"/> if one is already running.
    /// </summary>
    /// <param name="csvPath">Path to CSV file (optional, uses config if not provided)</param>
    /// <param name="maxRows">Maximum rows to process (optional, uses config if not provided)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Ingestion result summary</returns>
    Task<IngestionResult> IngestAsync(string? csvPath = null, int? maxRows = null, CancellationToken cancellationToken = default);
}
