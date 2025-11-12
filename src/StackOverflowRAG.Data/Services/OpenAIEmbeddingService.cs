using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using StackOverflowRAG.Data.Models;

namespace StackOverflowRAG.Data.Services;

/// <summary>
/// Embedding service using OpenAI text-embedding-3-small via Microsoft.Extensions.AI.
/// </summary>
public class OpenAIEmbeddingService : IEmbeddingService
{
    private readonly IEmbeddingGenerator<string, Embedding<float>> _embeddingGenerator;
    private readonly ILogger<OpenAIEmbeddingService> _logger;
    private readonly AsyncRetryPolicy _retryPolicy;

    public OpenAIEmbeddingService(
        IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator,
        ILogger<OpenAIEmbeddingService> logger)
    {
        _embeddingGenerator = embeddingGenerator;
        _logger = logger;

        // Configure retry policy: 3 retries with exponential backoff
        _retryPolicy = Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)),
                onRetry: (exception, timeSpan, retryCount, context) =>
                {
                    _logger.LogWarning(
                        exception,
                        "Embedding API call failed (attempt {RetryCount}). Retrying in {RetryDelay}s",
                        retryCount,
                        timeSpan.TotalSeconds);
                });
    }

    /// <inheritdoc />
    public async Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new ArgumentException("Text cannot be null or empty", nameof(text));
        }

        return await _retryPolicy.ExecuteAsync(async () =>
        {
            var result = await _embeddingGenerator.GenerateAsync(text, cancellationToken: cancellationToken);
            return result.Vector.ToArray();
        });
    }

    /// <inheritdoc />
    public async Task<List<float[]>> GenerateBatchEmbeddingsAsync(List<string> texts, CancellationToken cancellationToken = default)
    {
        if (texts == null || texts.Count == 0)
        {
            return new List<float[]>();
        }

        _logger.LogInformation("Generating embeddings for {Count} texts", texts.Count);

        return await _retryPolicy.ExecuteAsync(async () =>
        {
            var results = await _embeddingGenerator.GenerateAsync(texts, cancellationToken: cancellationToken);
            return results.Select(e => e.Vector.ToArray()).ToList();
        });
    }

    /// <inheritdoc />
    public async Task GenerateChunkEmbeddingsAsync(List<DocumentChunk> chunks, CancellationToken cancellationToken = default)
    {
        if (chunks == null || chunks.Count == 0)
        {
            _logger.LogWarning("No chunks provided for embedding generation");
            return;
        }

        _logger.LogInformation("Generating embeddings for {ChunkCount} chunks", chunks.Count);

        // Process in batches of 100 (OpenAI batch limit)
        const int batchSize = 100;
        var totalBatches = (int)Math.Ceiling((double)chunks.Count / batchSize);

        for (int batchIndex = 0; batchIndex < totalBatches; batchIndex++)
        {
            var batch = chunks
                .Skip(batchIndex * batchSize)
                .Take(batchSize)
                .ToList();

            var texts = batch.Select(c => c.ChunkText).ToList();

            try
            {
                var embeddings = await GenerateBatchEmbeddingsAsync(texts, cancellationToken);

                // Assign embeddings to chunks
                for (int i = 0; i < batch.Count; i++)
                {
                    batch[i].Embedding = embeddings[i];
                }

                _logger.LogInformation(
                    "Batch {BatchIndex}/{TotalBatches} completed ({Count} embeddings)",
                    batchIndex + 1,
                    totalBatches,
                    batch.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to generate embeddings for batch {BatchIndex}/{TotalBatches}",
                    batchIndex + 1,
                    totalBatches);
                throw;
            }
        }

        _logger.LogInformation("Successfully generated embeddings for all {ChunkCount} chunks", chunks.Count);
    }
}
