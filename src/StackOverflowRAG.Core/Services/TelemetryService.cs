using Microsoft.Extensions.Logging;
using StackOverflowRAG.Core.Interfaces;
using StackOverflowRAG.Core.Models;
using System.Text.Json;

namespace StackOverflowRAG.Core.Services;

/// <summary>
/// Service for logging telemetry and metrics using structured logging
/// </summary>
public class TelemetryService : ITelemetryService
{
    private readonly ILogger<TelemetryService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public TelemetryService(ILogger<TelemetryService> logger)
    {
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    public void LogQueryMetrics(QueryTelemetry metadata)
    {
        _logger.LogInformation(
            "QueryCompleted: {EventType} | Question: {Question} | Latency: {LatencyMs}ms | Tokens: {TokensInput}/{TokensOutput} | Cost: ${EstimatedCost:F6} | Cache: {CacheHit} | Chunks: {RetrievedChunks} | Search: {SearchType}",
            "QueryCompleted",
            TruncateString(metadata.Question, 100),
            metadata.LatencyMs,
            metadata.TokensInput,
            metadata.TokensOutput,
            metadata.EstimatedCost,
            metadata.CacheHit,
            metadata.RetrievedChunks,
            metadata.SearchType
        );

        // Also log as JSON for structured querying
        var json = JsonSerializer.Serialize(new
        {
            eventType = "QueryCompleted",
            timestamp = metadata.Timestamp.ToString("o"),
            metadata.Question,
            metadata.LatencyMs,
            metadata.TokensInput,
            metadata.TokensOutput,
            metadata.EstimatedCost,
            metadata.CacheHit,
            metadata.RetrievedChunks,
            metadata.SearchType,
            metadata.TopK
        }, _jsonOptions);

        _logger.LogInformation("Telemetry: {TelemetryJson}", json);
    }

    public void LogIngestionMetrics(IngestionMetadata metadata)
    {
        _logger.LogInformation(
            "IngestionCompleted: {EventType} | Docs: {DocsLoaded} | Chunks: {ChunksCreated} | Embeddings: {EmbeddingsGenerated} | Duration: {DurationMs}ms | Cost: ${EstimatedCost:F6} | Success: {Success}",
            "IngestionCompleted",
            metadata.DocsLoaded,
            metadata.ChunksCreated,
            metadata.EmbeddingsGenerated,
            metadata.DurationMs,
            metadata.EstimatedCost,
            metadata.Success
        );

        // Also log as JSON
        var json = JsonSerializer.Serialize(new
        {
            eventType = "IngestionCompleted",
            timestamp = metadata.Timestamp.ToString("o"),
            metadata.DocsLoaded,
            metadata.ChunksCreated,
            metadata.EmbeddingsGenerated,
            metadata.DurationMs,
            metadata.EstimatedCost,
            metadata.Success,
            metadata.ErrorMessage
        }, _jsonOptions);

        _logger.LogInformation("Telemetry: {TelemetryJson}", json);
    }

    public void LogTagMetrics(TagMetadata metadata)
    {
        _logger.LogInformation(
            "TagSuggestionCompleted: {EventType} | InputLength: {InputLength} | Tags: {TagCount} | Latency: {LatencyMs}ms | TopK: {TopK}",
            "TagSuggestionCompleted",
            metadata.InputLength,
            metadata.PredictedTags.Count,
            metadata.LatencyMs,
            metadata.TopK
        );

        // Also log as JSON
        var json = JsonSerializer.Serialize(new
        {
            eventType = "TagSuggestionCompleted",
            timestamp = metadata.Timestamp.ToString("o"),
            metadata.InputLength,
            predictedTags = metadata.PredictedTags,
            metadata.LatencyMs,
            metadata.TopK,
            metadata.ModelLoaded
        }, _jsonOptions);

        _logger.LogInformation("Telemetry: {TelemetryJson}", json);
    }

    private static string TruncateString(string value, int maxLength)
    {
        if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
            return value;

        return value.Substring(0, maxLength) + "...";
    }
}
