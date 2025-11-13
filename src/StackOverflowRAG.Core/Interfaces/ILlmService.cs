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
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Async enumerable of response text chunks</returns>
    IAsyncEnumerable<string> StreamAnswerAsync(
        string question,
        List<DocumentChunk> retrievedChunks,
        CancellationToken cancellationToken = default);
}
