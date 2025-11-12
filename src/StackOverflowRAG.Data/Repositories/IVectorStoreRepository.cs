using StackOverflowRAG.Data.Models;

namespace StackOverflowRAG.Data.Repositories;

/// <summary>
/// Repository for storing and retrieving document chunks in vector database.
/// </summary>
public interface IVectorStoreRepository
{
    /// <summary>
    /// Ensures the collection exists with proper configuration.
    /// </summary>
    Task EnsureCollectionExistsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Upserts document chunks to the vector store.
    /// </summary>
    /// <param name="chunks">Chunks with embeddings to store</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task UpsertChunksAsync(List<DocumentChunk> chunks, CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches for similar chunks using vector similarity.
    /// </summary>
    /// <param name="queryEmbedding">Query embedding vector</param>
    /// <param name="limit">Maximum number of results</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of matching chunks with similarity scores</returns>
    Task<List<(DocumentChunk Chunk, float Score)>> SearchAsync(
        float[] queryEmbedding,
        int limit = 10,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets count of chunks in collection.
    /// </summary>
    Task<long> GetCountAsync(CancellationToken cancellationToken = default);
}
