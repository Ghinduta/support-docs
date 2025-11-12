using StackOverflowRAG.Data.Models;

namespace StackOverflowRAG.Core.Interfaces;

/// <summary>
/// Service for retrieving relevant chunks using vector search
/// </summary>
public interface IRetrievalService
{
    /// <summary>
    /// Searches for relevant chunks using vector similarity only
    /// </summary>
    /// <param name="query">User's question text</param>
    /// <param name="topK">Number of chunks to retrieve</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of relevant chunks with similarity scores</returns>
    Task<List<DocumentChunk>> SearchAsync(string query, int topK, CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches for relevant chunks using hybrid search (vector + keyword)
    /// </summary>
    /// <param name="query">User's question text</param>
    /// <param name="topK">Number of chunks to retrieve</param>
    /// <param name="useHybrid">Whether to use hybrid search (default: true)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of relevant chunks with combined scores</returns>
    Task<List<DocumentChunk>> HybridSearchAsync(string query, int topK, bool useHybrid = true, CancellationToken cancellationToken = default);
}
