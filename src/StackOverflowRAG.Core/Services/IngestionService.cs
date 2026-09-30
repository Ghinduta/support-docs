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

    // The service is a singleton, so this is the one shared progress record.
    private readonly object _progressLock = new();
    private IngestionProgress _progress = new();

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
    public IngestionProgress GetProgress()
    {
        lock (_progressLock)
        {
            return new IngestionProgress
            {
                Running = _progress.Running,
                Step = _progress.Step,
                StepNumber = _progress.StepNumber,
                TotalSteps = _progress.TotalSteps,
                Done = _progress.Done,
                Total = _progress.Total,
                MaxRows = _progress.MaxRows,
                StartedAt = _progress.StartedAt,
                FinishedAt = _progress.FinishedAt,
                Error = _progress.Error
            };
        }
    }

    /// <inheritdoc />
    public async Task<IngestionResult> IngestAsync(
        string? csvPath = null,
        int? maxRows = null,
        CancellationToken cancellationToken = default)
    {
        lock (_progressLock)
        {
            if (_progress.Running)
            {
                throw new InvalidOperationException("An ingestion is already running.");
            }

            _progress = new IngestionProgress
            {
                Running = true,
                MaxRows = maxRows ?? _options.MaxRows,
                StartedAt = DateTime.UtcNow
            };
        }

        IngestionResult? result = null;
        try
        {
            result = await RunIngestionAsync(csvPath, maxRows, cancellationToken);
            return result;
        }
        finally
        {
            lock (_progressLock)
            {
                _progress.Running = false;
                _progress.FinishedAt = DateTime.UtcNow;
                _progress.Error = cancellationToken.IsCancellationRequested
                    ? "Cancelled."
                    : result?.ErrorMessages.FirstOrDefault();
            }
        }
    }

    private void SetStep(int stepNumber, string step, int total = 0)
    {
        lock (_progressLock)
        {
            _progress.StepNumber = stepNumber;
            _progress.Step = step;
            _progress.Done = 0;
            _progress.Total = total;
        }
    }

    /// <summary>
    /// Reports on the calling thread; Progress&lt;T&gt; would post to the thread pool and could apply updates out of order.
    /// </summary>
    private sealed class InlineProgress(Action<int> report) : IProgress<int>
    {
        public void Report(int value) => report(value);
    }

    private async Task<IngestionResult> RunIngestionAsync(
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
            SetStep(1, "Preparing database");
            await _vectorStore.EnsureCollectionExistsAsync(cancellationToken);

            // Step 2: Parse CSV
            _logger.LogInformation("Step 2/5: Parsing CSV");
            SetStep(2, "Reading CSV files");
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
            SetStep(3, "Splitting into chunks");
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
            SetStep(4, "Generating embeddings", chunks.Count);
            await _embeddingService.GenerateChunkEmbeddingsAsync(
                chunks,
                new InlineProgress(done => { lock (_progressLock) _progress.Done = done; }),
                cancellationToken);
            result.EmbeddingsGenerated = chunks.Count(c => c.Embedding != null);
            _logger.LogInformation("Generated {Count} embeddings", result.EmbeddingsGenerated);

            // Step 5: Upsert to Qdrant
            _logger.LogInformation("Step 5/5: Upserting to Qdrant");
            SetStep(5, "Saving to Qdrant");
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
