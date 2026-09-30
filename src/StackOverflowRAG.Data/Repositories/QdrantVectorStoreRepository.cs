using System.Security.Cryptography;
using System.Text;
using Google.Protobuf.Collections;
using Microsoft.Extensions.Logging;
using Qdrant.Client;
using Qdrant.Client.Grpc;
using StackOverflowRAG.Data.Models;
using StackOverflowRAG.Data.Utilities;

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
    private const string DenseVectorName = "dense";
    private const string Bm25VectorName = "bm25";

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

                // Two vectors per chunk: the OpenAI embedding (meaning) and a BM25 sparse vector (keywords).
                // The IDF modifier makes Qdrant weight rare words higher, which completes BM25 scoring.
                await _client.CreateCollectionAsync(
                    collectionName: _collectionName,
                    vectorsConfig: new VectorParamsMap
                    {
                        Map = { [DenseVectorName] = new VectorParams { Size = VectorSize, Distance = Distance.Cosine } }
                    },
                    sparseVectorsConfig: new SparseVectorConfig
                    {
                        Map = { [Bm25VectorName] = new SparseVectorParams { Modifier = Modifier.Idf } }
                    },
                    cancellationToken: cancellationToken);

                _logger.LogInformation("Collection {CollectionName} created with dense and BM25 vectors", _collectionName);
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
            var points = validChunks.Select(chunk =>
            {
                var (indices, values) = Bm25.EncodeDocument(chunk.ChunkText);
                return new PointStruct
            {
                Id = new PointId { Uuid = PointIdFor(chunk.ChunkId).ToString() },
                Vectors = new Dictionary<string, Vector>
                {
                    [DenseVectorName] = chunk.Embedding!,
                    [Bm25VectorName] = (values, indices)
                },
                Payload =
                {
                    ["chunk_id"] = chunk.ChunkId,
                    ["post_id"] = chunk.PostId,
                    ["question_title"] = chunk.QuestionTitle,
                    ["chunk_text"] = chunk.ChunkText,
                    ["chunk_index"] = chunk.ChunkIndex
                }
            };
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
            var searchResult = await _client.QueryAsync(
                collectionName: _collectionName,
                query: queryEmbedding,
                usingVector: DenseVectorName,
                limit: (ulong)limit,
                payloadSelector: true,
                cancellationToken: cancellationToken);

            var results = searchResult
                .Select(point => (Chunk: ToChunk(point.Payload), Score: point.Score))
                .ToList();

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
    public async Task<List<(DocumentChunk Chunk, float Score)>> HybridSearchAsync(
        float[] queryEmbedding,
        string queryText,
        int limit = 10,
        CancellationToken cancellationToken = default)
    {
        if (queryEmbedding == null || queryEmbedding.Length == 0)
        {
            throw new ArgumentException("Query embedding cannot be null or empty", nameof(queryEmbedding));
        }

        if (string.IsNullOrWhiteSpace(queryText))
        {
            throw new ArgumentException("Query text cannot be null or empty", nameof(queryText));
        }

        // Each search fetches a deeper candidate list than the final limit, so RRF has overlap to work with.
        var candidates = (ulong)Math.Max(limit * 4, 20);
        var prefetch = new List<PrefetchQuery>
        {
            new() { Query = queryEmbedding, Using = DenseVectorName, Limit = candidates }
        };

        var (indices, values) = Bm25.EncodeQuery(queryText);
        if (indices.Length > 0)
        {
            prefetch.Add(new() { Query = (values, indices), Using = Bm25VectorName, Limit = candidates });
        }

        _logger.LogInformation(
            "Performing hybrid search (vector + BM25, fused with RRF): Limit={Limit}, QueryWords={WordCount}",
            limit, indices.Length);

        try
        {
            var points = await _client.QueryAsync(
                collectionName: _collectionName,
                query: Fusion.Rrf,
                prefetch: prefetch,
                limit: (ulong)limit,
                payloadSelector: true,
                cancellationToken: cancellationToken);

            var results = points.Select(point => (Chunk: ToChunk(point.Payload), Score: point.Score)).ToList();

            _logger.LogInformation("Hybrid search completed: {Count} results", results.Count);
            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to perform hybrid search");
            throw;
        }
    }

    /// <inheritdoc />
    /// <inheritdoc />
    public async Task<List<DocumentChunk>> GetFirstChunksAsync(int limit, CancellationToken cancellationToken = default)
    {
        var response = await _client.ScrollAsync(
            collectionName: _collectionName,
            filter: MatchInteger("chunk_index", 0),
            limit: (uint)limit,
            cancellationToken: cancellationToken);

        return response.Result.Select(point => ToChunk(point.Payload)).ToList();
    }

    /// <inheritdoc />
    public async Task<List<DocumentChunk>> GetChunksForPostAsync(int postId, CancellationToken cancellationToken = default)
    {
        var response = await _client.ScrollAsync(
            collectionName: _collectionName,
            filter: MatchInteger("post_id", postId),
            limit: 100,
            cancellationToken: cancellationToken);

        return response.Result
            .Select(point => ToChunk(point.Payload))
            .OrderBy(chunk => chunk.ChunkIndex)
            .ToList();
    }

    private static Filter MatchInteger(string key, long value) => new()
    {
        Must = { new Condition { Field = new FieldCondition { Key = key, Match = new Match { Integer = value } } } }
    };

    private static DocumentChunk ToChunk(MapField<string, Value> payload) => new()
    {
        ChunkId = payload["chunk_id"].StringValue,
        PostId = (int)payload["post_id"].IntegerValue,
        QuestionTitle = payload["question_title"].StringValue,
        ChunkText = payload["chunk_text"].StringValue,
        ChunkIndex = (int)payload["chunk_index"].IntegerValue
    };

    /// <summary>
    /// Qdrant point IDs must be UUIDs or integers, so the ChunkId is hashed into a stable UUID.
    /// The same chunk always maps to the same point, which makes re-ingestion overwrite instead of duplicate.
    /// </summary>
    public static Guid PointIdFor(string chunkId) => new(MD5.HashData(Encoding.UTF8.GetBytes(chunkId)));

    public async Task<long> GetCountAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Before the first ingestion the collection doesn't exist yet, which means zero chunks.
            var collections = await _client.ListCollectionsAsync(cancellationToken);
            if (!collections.Contains(_collectionName))
            {
                return 0;
            }

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
