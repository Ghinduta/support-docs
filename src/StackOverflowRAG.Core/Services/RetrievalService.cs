using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackOverflowRAG.Core.Configuration;
using StackOverflowRAG.Core.Interfaces;
using StackOverflowRAG.Data.Models;
using StackOverflowRAG.Data.Repositories;
using StackOverflowRAG.Data.Services;

namespace StackOverflowRAG.Core.Services;

/// <summary>
/// Service for retrieving relevant chunks using vector search
/// </summary>
public class RetrievalService : IRetrievalService
{
    private readonly IEmbeddingService _embeddingService;
    private readonly IVectorStoreRepository _vectorStore;
    private readonly RetrievalOptions _options;
    private readonly ILogger<RetrievalService> _logger;

    public RetrievalService(
        IEmbeddingService embeddingService,
        IVectorStoreRepository vectorStore,
        IOptions<RetrievalOptions> options,
        ILogger<RetrievalService> logger)
    {
        _embeddingService = embeddingService ?? throw new ArgumentNullException(nameof(embeddingService));
        _vectorStore = vectorStore ?? throw new ArgumentNullException(nameof(vectorStore));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    // Constructor for testing (without IOptions wrapper)
    public RetrievalService(
        IEmbeddingService embeddingService,
        IVectorStoreRepository vectorStore,
        ILogger<RetrievalService> logger)
    {
        _embeddingService = embeddingService ?? throw new ArgumentNullException(nameof(embeddingService));
        _vectorStore = vectorStore ?? throw new ArgumentNullException(nameof(vectorStore));
        _options = new RetrievalOptions(); // Use defaults
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<List<DocumentChunk>> SearchAsync(string query, int topK, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            throw new ArgumentException("Query cannot be null or empty", nameof(query));
        }

        if (topK <= 0)
        {
            throw new ArgumentException("TopK must be greater than 0", nameof(topK));
        }

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        _logger.LogInformation("Starting vector search: Query length={QueryLength}, TopK={TopK}",
            query.Length, topK);

        try
        {
            // Step 1: Embed the query using the same model as ingestion
            _logger.LogDebug("Generating query embedding");
            var queryEmbedding = await _embeddingService.GenerateEmbeddingAsync(query, cancellationToken);

            _logger.LogDebug("Query embedding generated: {Dimensions} dimensions", queryEmbedding.Length);

            // Step 2: Perform vector similarity search in Qdrant
            _logger.LogDebug("Searching Qdrant for similar chunks");
            var searchResults = await _vectorStore.SearchAsync(queryEmbedding, topK, cancellationToken);

            stopwatch.Stop();

            // Map results and populate Score field
            var results = searchResults.Select(r =>
            {
                r.Chunk.Score = r.Score;
                return r.Chunk;
            }).ToList();

            _logger.LogInformation(
                "Vector search completed: Query='{Query}' (trimmed), Results={ResultCount}, TopK={TopK}, Duration={Duration}ms",
                query.Substring(0, Math.Min(100, query.Length)),
                results.Count,
                topK,
                stopwatch.ElapsedMilliseconds);

            if (results.Count == 0)
            {
                _logger.LogWarning("No results found for query: '{Query}' (trimmed)",
                    query.Substring(0, Math.Min(100, query.Length)));
            }
            else
            {
                _logger.LogDebug("Top result score: {Score}, Bottom result score: {BottomScore}",
                    results.First().Score,
                    results.Last().Score);
            }

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during vector search for query: '{Query}' (trimmed)",
                query.Substring(0, Math.Min(100, query.Length)));
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<List<DocumentChunk>> HybridSearchAsync(string query, int topK, bool useHybrid = true, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            throw new ArgumentException("Query cannot be null or empty", nameof(query));
        }

        if (topK <= 0)
        {
            throw new ArgumentException("TopK must be greater than 0", nameof(topK));
        }

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        _logger.LogInformation("Starting hybrid search: Query length={QueryLength}, TopK={TopK}, UseHybrid={UseHybrid}",
            query.Length, topK, useHybrid);

        try
        {
            // Step 1: Embed the query
            _logger.LogDebug("Generating query embedding");
            var queryEmbedding = await _embeddingService.GenerateEmbeddingAsync(query, cancellationToken);

            // Step 2: Perform hybrid or vector-only search
            List<(DocumentChunk Chunk, float Score)> searchResults;

            if (useHybrid)
            {
                _logger.LogDebug("Performing hybrid search with weights: Vector={VectorWeight}, Keyword={KeywordWeight}",
                    _options.VectorWeight, _options.KeywordWeight);

                searchResults = await _vectorStore.HybridSearchAsync(
                    queryEmbedding,
                    query,
                    topK,
                    _options.VectorWeight,
                    _options.KeywordWeight,
                    cancellationToken);
            }
            else
            {
                _logger.LogDebug("Performing vector-only search");
                searchResults = await _vectorStore.SearchAsync(queryEmbedding, topK, cancellationToken);
            }

            stopwatch.Stop();

            // Map results and populate Score field
            var results = searchResults.Select(r =>
            {
                r.Chunk.Score = r.Score;
                return r.Chunk;
            }).ToList();

            _logger.LogInformation(
                "Hybrid search completed: Query='{Query}' (trimmed), Results={ResultCount}, TopK={TopK}, Duration={Duration}ms, SearchType={SearchType}",
                query.Substring(0, Math.Min(100, query.Length)),
                results.Count,
                topK,
                stopwatch.ElapsedMilliseconds,
                useHybrid ? "Hybrid" : "Vector-only");

            if (results.Count == 0)
            {
                _logger.LogWarning("No results found for query: '{Query}' (trimmed)",
                    query.Substring(0, Math.Min(100, query.Length)));
            }
            else
            {
                _logger.LogDebug("Top result score: {Score:F4}, Bottom result score: {BottomScore:F4}",
                    results.First().Score,
                    results.Last().Score);
            }

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during hybrid search for query: '{Query}' (trimmed)",
                query.Substring(0, Math.Min(100, query.Length)));
            throw;
        }
    }
}
