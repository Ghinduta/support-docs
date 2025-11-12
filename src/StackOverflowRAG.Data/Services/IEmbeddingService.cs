using StackOverflowRAG.Data.Models;

namespace StackOverflowRAG.Data.Services;

/// <summary>
/// Service for generating embeddings using OpenAI text-embedding models.
/// </summary>
public interface IEmbeddingService
{
    /// <summary>
    /// Generates an embedding for a single text.
    /// </summary>
    /// <param name="text">Text to generate embedding for</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Embedding vector</returns>
    Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates embeddings for multiple texts in batch.
    /// </summary>
    /// <param name="texts">Texts to generate embeddings for</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of embedding vectors in same order as input</returns>
    Task<List<float[]>> GenerateBatchEmbeddingsAsync(List<string> texts, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates embeddings for document chunks and populates their Embedding property.
    /// </summary>
    /// <param name="chunks">Chunks to generate embeddings for</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task GenerateChunkEmbeddingsAsync(List<DocumentChunk> chunks, CancellationToken cancellationToken = default);
}
