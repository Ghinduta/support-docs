using System.ComponentModel.DataAnnotations;

namespace StackOverflowRAG.Core.Configuration;

/// <summary>
/// Configuration options for data ingestion pipeline.
/// </summary>
public class IngestionOptions
{
    public const string SectionName = "Ingestion";

    /// <summary>
    /// Path to the Stack Overflow CSV file
    /// </summary>
    [Required]
    public string CsvPath { get; set; } = "./data/stacksample.csv";

    /// <summary>
    /// Maximum number of rows to ingest from CSV
    /// </summary>
    [Range(1, 1000000)]
    public int MaxRows { get; set; } = 10000;

    /// <summary>
    /// Chunk size in tokens for document splitting
    /// </summary>
    [Range(100, 2000)]
    public int ChunkSize { get; set; } = 500;

    /// <summary>
    /// Overlap size in tokens between chunks
    /// </summary>
    [Range(0, 500)]
    public int ChunkOverlap { get; set; } = 50;

    /// <summary>
    /// Validates configuration values
    /// </summary>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(CsvPath))
        {
            throw new InvalidOperationException("CsvPath must be configured");
        }

        if (ChunkOverlap >= ChunkSize)
        {
            throw new InvalidOperationException("ChunkOverlap must be less than ChunkSize");
        }
    }
}
