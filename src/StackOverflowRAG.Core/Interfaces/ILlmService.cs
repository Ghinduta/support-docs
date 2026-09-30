using StackOverflowRAG.Core.Models;
using StackOverflowRAG.Data.Models;

namespace StackOverflowRAG.Core.Interfaces;

/// <summary>
/// Service for LLM integration (streaming responses)
/// </summary>
public interface ILlmService
{
    /// <summary>
    /// Streams an answer to the user's question using retrieved chunks as context
    /// </summary>
    /// <param name="question">User's question</param>
    /// <param name="retrievedChunks">Context chunks from vector search</param>
    /// <param name="temperature">Overrides the configured temperature (evals use 0 for repeatable answers)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Async enumerable of response text chunks</returns>
    /// <summary>
    /// Rewrites a question into alternative phrasings for multi-query search.
    /// </summary>
    /// <param name="question">User's question</param>
    /// <param name="count">Number of phrasings to produce (the original is not included)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task<QueryExpansion> ExpandQueryAsync(string question, int count = 3, CancellationToken cancellationToken = default);

    IAsyncEnumerable<string> StreamAnswerAsync(
        string question,
        List<DocumentChunk> retrievedChunks,
        double? temperature = null,
        CancellationToken cancellationToken = default);
}
