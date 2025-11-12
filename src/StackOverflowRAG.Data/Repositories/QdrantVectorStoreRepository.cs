using Microsoft.Extensions.Logging;
using Qdrant.Client;
using Qdrant.Client.Grpc;
using StackOverflowRAG.Data.Models;

namespace StackOverflowRAG.Data.Repositories;

/// <summary>
/// Qdrant implementation of vector store repository.
/// </summary>
public class QdrantVectorStoreRepository : IVectorStoreRepository
{
    private readonly QdrantClient _client;
    private readonly string _collectionName;
    private readonly ILogger<QdrantVectorStoreRepository> _logger;
    private const int VectorSize = 1536; // text-embedding-3-small dimension

    public QdrantVectorStoreRepository(
        QdrantClient client,
        string collectionName,
        ILogger<QdrantVectorStoreRepository> logger)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _collectionName = collectionName ?? throw new ArgumentNullException(nameof(collectionName));
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task EnsureCollectionExistsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Check if collection exists
            var collections = await _client.ListCollectionsAsync(cancellationToken);
            var exists = collections.Any(c => c == _collectionName);

            if (!exists)
            {
                _logger.LogInformation("Creating Qdrant collection: {CollectionName}", _collectionName);

                await _client.CreateCollectionAsync(
                    collectionName: _collectionName,
                    vectorsConfig: new VectorParams
                    {
                        Size = VectorSize,
                        Distance = Distance.Cosine
                    },
                    cancellationToken: cancellationToken);

                _logger.LogInformation("Collection {CollectionName} created successfully", _collectionName);
            }
            else
            {
                _logger.LogInformation("Collection {CollectionName} already exists", _collectionName);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to ensure collection {CollectionName} exists", _collectionName);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task UpsertChunksAsync(List<DocumentChunk> chunks, CancellationToken cancellationToken = default)
    {
        if (chunks == null || chunks.Count == 0)
        {
            _logger.LogWarning("No chunks provided for upsertion");
            return;
        }

        // Filter out chunks without embeddings
        var validChunks = chunks.Where(c => c.Embedding != null && c.Embedding.Length > 0).ToList();

        if (validChunks.Count == 0)
        {
            _logger.LogWarning("No chunks with valid embeddings to upsert");
            return;
        }

        _logger.LogInformation("Upserting {Count} chunks to Qdrant", validChunks.Count);

        try
        {
            var points = validChunks.Select(chunk => new PointStruct
            {
                Id = new PointId { Uuid = Guid.NewGuid().ToString() },
                Vectors = chunk.Embedding!,
                Payload =
                {
                    ["chunk_id"] = chunk.ChunkId,
                    ["post_id"] = chunk.PostId,
                    ["question_title"] = chunk.QuestionTitle,
                    ["chunk_text"] = chunk.ChunkText,
                    ["chunk_index"] = chunk.ChunkIndex
                }
            }).ToList();

            await _client.UpsertAsync(
                collectionName: _collectionName,
                points: points,
                cancellationToken: cancellationToken);

            _logger.LogInformation("Successfully upserted {Count} chunks to Qdrant", validChunks.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upsert chunks to Qdrant");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<List<(DocumentChunk Chunk, float Score)>> SearchAsync(
        float[] queryEmbedding,
        int limit = 10,
        CancellationToken cancellationToken = default)
    {
        if (queryEmbedding == null || queryEmbedding.Length == 0)
        {
            throw new ArgumentException("Query embedding cannot be null or empty", nameof(queryEmbedding));
        }

        _logger.LogInformation("Searching Qdrant for top {Limit} similar chunks", limit);

        try
        {
            var searchResult = await _client.SearchAsync(
                collectionName: _collectionName,
                vector: queryEmbedding,
                limit: (ulong)limit,
                cancellationToken: cancellationToken);

            var results = searchResult.Select(point =>
            {
                var chunk = new DocumentChunk
                {
                    ChunkId = point.Payload["chunk_id"].StringValue,
                    PostId = (int)point.Payload["post_id"].IntegerValue,
                    QuestionTitle = point.Payload["question_title"].StringValue,
                    ChunkText = point.Payload["chunk_text"].StringValue,
                    ChunkIndex = (int)point.Payload["chunk_index"].IntegerValue
                };

                return (Chunk: chunk, Score: point.Score);
            }).ToList();

            _logger.LogInformation("Found {Count} matching chunks", results.Count);

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to search Qdrant");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<long> GetCountAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var info = await _client.GetCollectionInfoAsync(_collectionName, cancellationToken);
            return (long)info.PointsCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get collection count");
            throw;
        }
    }
}
