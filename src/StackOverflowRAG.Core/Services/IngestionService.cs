using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackOverflowRAG.Core.Configuration;
using StackOverflowRAG.Core.Interfaces;
using StackOverflowRAG.Core.Models;
using StackOverflowRAG.Data.Parsers;
using StackOverflowRAG.Data.Repositories;
using StackOverflowRAG.Data.Services;

namespace StackOverflowRAG.Core.Services;

/// <summary>
/// Orchestrates the full ingestion pipeline: CSV → Parse → Chunk → Embed → Upsert.
/// </summary>
public class IngestionService : IIngestionService
{
    private readonly IStackOverflowCsvParser _csvParser;
    private readonly IChunkingService _chunkingService;
    private readonly IEmbeddingService _embeddingService;
    private readonly IVectorStoreRepository _vectorStore;
    private readonly IngestionOptions _options;
    private readonly ILogger<IngestionService> _logger;

    public IngestionService(
        IStackOverflowCsvParser csvParser,
        IChunkingService chunkingService,
        IEmbeddingService embeddingService,
        IVectorStoreRepository vectorStore,
        IOptions<IngestionOptions> options,
        ILogger<IngestionService> logger)
    {
        _csvParser = csvParser;
        _chunkingService = chunkingService;
        _embeddingService = embeddingService;
        _vectorStore = vectorStore;
        _options = options.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<IngestionResult> IngestAsync(
        string? csvPath = null,
        int? maxRows = null,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = new IngestionResult();

        try
        {
            // Use provided values or fall back to configuration
            var actualCsvPath = csvPath ?? _options.CsvPath;
            // If maxRows is null or <= 0, use config default
            var actualMaxRows = (maxRows.HasValue && maxRows.Value > 0) ? maxRows.Value : _options.MaxRows;

            _logger.LogInformation(
                "Starting ingestion: CsvPath={CsvPath}, MaxRows={MaxRows}",
                actualCsvPath,
                actualMaxRows);

            // Step 1: Ensure Qdrant collection exists
            _logger.LogInformation("Step 1/5: Ensuring Qdrant collection exists");
            await _vectorStore.EnsureCollectionExistsAsync(cancellationToken);

            // Step 2: Parse CSV
            _logger.LogInformation("Step 2/5: Parsing CSV");
            var documents = await _csvParser.ParseAsync(actualCsvPath, actualMaxRows, cancellationToken);
            result.DocumentsLoaded = documents.Count;
            _logger.LogInformation("Parsed {Count} documents from CSV", documents.Count);

            if (documents.Count == 0)
            {
                result.ErrorMessages.Add("No valid documents found in CSV");
                result.ValidationPassed = false;
                return result;
            }

            // Step 3: Chunk documents
            _logger.LogInformation("Step 3/5: Chunking documents");
            var chunks = _chunkingService.ChunkDocuments(
                documents,
                _options.ChunkSize,
                _options.ChunkOverlap);
            result.ChunksCreated = chunks.Count;
            _logger.LogInformation("Created {Count} chunks", chunks.Count);

            if (chunks.Count == 0)
            {
                result.ErrorMessages.Add("No chunks created from documents");
                result.ValidationPassed = false;
                return result;
            }

            // Step 4: Generate embeddings
            _logger.LogInformation("Step 4/5: Generating embeddings");
            await _embeddingService.GenerateChunkEmbeddingsAsync(chunks, cancellationToken);
            result.EmbeddingsGenerated = chunks.Count(c => c.Embedding != null);
            _logger.LogInformation("Generated {Count} embeddings", result.EmbeddingsGenerated);

            // Step 5: Upsert to Qdrant
            _logger.LogInformation("Step 5/5: Upserting to Qdrant");
            await _vectorStore.UpsertChunksAsync(chunks, cancellationToken);
            result.QdrantUpserts = chunks.Count(c => c.Embedding != null);
            _logger.LogInformation("Upserted {Count} chunks to Qdrant", result.QdrantUpserts);

            // Validation: Check Qdrant count
            result.TotalChunksInQdrant = await _vectorStore.GetCountAsync(cancellationToken);
            result.ValidationPassed = result.TotalChunksInQdrant >= result.QdrantUpserts;

            if (!result.ValidationPassed)
            {
                result.ErrorMessages.Add(
                    $"Validation failed: Expected at least {result.QdrantUpserts} chunks in Qdrant, but found {result.TotalChunksInQdrant}");
            }

            stopwatch.Stop();
            result.DurationSeconds = stopwatch.Elapsed.TotalSeconds;

            _logger.LogInformation(
                "Ingestion completed: {Documents} docs → {Chunks} chunks → {Embeddings} embeddings → {Upserts} upserts in {Duration:F2}s",
                result.DocumentsLoaded,
                result.ChunksCreated,
                result.EmbeddingsGenerated,
                result.QdrantUpserts,
                result.DurationSeconds);

            return result;
        }
        catch (FileNotFoundException ex)
        {
            _logger.LogError(ex, "CSV file not found: {Message}", ex.Message);
            result.ErrorMessages.Add($"CSV file not found: {ex.Message}");
            result.ValidationPassed = false;
            stopwatch.Stop();
            result.DurationSeconds = stopwatch.Elapsed.TotalSeconds;
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ingestion failed: {Message}", ex.Message);
            result.ErrorMessages.Add($"Ingestion failed: {ex.Message}");
            result.ValidationPassed = false;
            stopwatch.Stop();
            result.DurationSeconds = stopwatch.Elapsed.TotalSeconds;
            return result;
        }
    }
}
