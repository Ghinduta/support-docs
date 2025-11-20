using StackOverflowRAG.Data.Models;

namespace StackOverflowRAG.Data.Parsers;

/// <summary>
/// Interface for parsing Stack Overflow CSV data.
/// </summary>
public interface IStackOverflowCsvParser
{
    /// <summary>
    /// Parses Stack Overflow CSV file and returns documents.
    /// </summary>
    /// <param name="csvPath">Path to data/stacksample.csvthe CSV file</param>
    /// <param name="maxRows">Maximum number of rows to parse (default: 10000)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of parsed Stack Overflow documents</returns>
    Task<List<StackOverflowDocument>> ParseAsync(
        string csvPath,
        int maxRows = 10000,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Cleans HTML content by removing tags and decoding entities.
    /// </summary>
    /// <param name="html">HTML content to clean</param>
    /// <returns>Clean text content</returns>
    string CleanHtml(string html);
}
