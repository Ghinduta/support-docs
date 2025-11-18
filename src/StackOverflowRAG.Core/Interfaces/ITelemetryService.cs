using StackOverflowRAG.Core.Models;

namespace StackOverflowRAG.Core.Interfaces;

/// <summary>
/// Service for logging telemetry and metrics
/// </summary>
public interface ITelemetryService
{
    /// <summary>
    /// Log metrics for query/ask endpoint
    /// </summary>
    void LogQueryMetrics(QueryMetadata metadata);

    /// <summary>
    /// Log metrics for ingestion endpoint
    /// </summary>
    void LogIngestionMetrics(IngestionMetadata metadata);

    /// <summary>
    /// Log metrics for tag suggestion endpoint
    /// </summary>
    void LogTagMetrics(TagMetadata metadata);
}
