namespace StackOverflowRAG.Core.Models;

/// <summary>
/// Request model for ingestion endpoint.
/// </summary>
public class IngestionRequest
{
    /// <summary>
    /// Optional path to CSV file. If not provided, uses configured default.
    /// </summary>
    public string? CsvPath { get; set; }

    /// <summary>
    /// Maximum rows to ingest. If not provided, uses configured default.
    /// </summary>
    public int? MaxRows { get; set; }
}
