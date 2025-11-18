using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackOverflowRAG.Core.Configuration;
using StackOverflowRAG.Core.Interfaces;
using StackOverflowRAG.Core.Models;
using StackOverflowRAG.Data.Parsers;
using StackOverflowRAG.ML.Interfaces;
using StackOverflowRAG.ML.Services;

namespace StackOverflowRAG.Core.Services;

/// <summary>
/// Service for suggesting Stack Overflow tags using ML.NET classifier
/// </summary>
public class TagSuggestionService : ITagSuggestionService
{
    private readonly ITagClassifier _classifier;
    private readonly ILogger<TagSuggestionService> _logger;
    private readonly TagSuggestionOptions _options;
    private readonly IStackOverflowCsvParser _csvParser;
    private readonly IngestionOptions _ingestionOptions;
    private bool _initialized = false;

    public TagSuggestionService(
        ILogger<TagSuggestionService> logger,
        IOptions<TagSuggestionOptions> options,
        IStackOverflowCsvParser csvParser,
        IOptions<IngestionOptions> ingestionOptions)
    {
        _logger = logger;
        _options = options.Value;
        _csvParser = csvParser;
        _ingestionOptions = ingestionOptions.Value;
        _classifier = new TagClassifier();
    }

    /// <summary>
    /// Initialize the service by loading the trained model
    /// </summary>
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (_initialized)
        {
            _logger.LogInformation("Tag suggestion service already initialized");
            return;
        }

        try
        {
            _logger.LogInformation("Initializing tag suggestion service...");

            // Check if model file exists
            if (!File.Exists(_options.ModelPath))
            {
                _logger.LogWarning("Tag classifier model not found at: {ModelPath}. Tag suggestion will not be available.", _options.ModelPath);
                return;
            }

            // Load the trained model
            await Task.Run(() => _classifier.LoadModel(_options.ModelPath), cancellationToken);

            _initialized = true;
            _logger.LogInformation("Tag suggestion service initialized successfully. Available tags: {TagCount}", _classifier.AvailableTags.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize tag suggestion service");
            throw;
        }
    }

    /// <summary>
    /// Suggest tags for a question
    /// </summary>
    public async Task<TagSuggestionResponse> SuggestTagsAsync(
        string title,
        string body,
        int topK = 5,
        CancellationToken cancellationToken = default)
    {
        if (!_initialized)
        {
            throw new InvalidOperationException("Tag suggestion service is not initialized. Call InitializeAsync first.");
        }

        if (string.IsNullOrWhiteSpace(title) && string.IsNullOrWhiteSpace(body))
        {
            throw new ArgumentException("Title and body cannot both be empty");
        }

        try
        {
            _logger.LogInformation("Suggesting tags for question. Title length: {TitleLength}, Body length: {BodyLength}, TopK: {TopK}",
                title?.Length ?? 0, body?.Length ?? 0, topK);

            // Run prediction on background thread
            var predictions = await Task.Run(
                () => _classifier.PredictTags(title ?? string.Empty, body ?? string.Empty, topK),
                cancellationToken);

            var response = new TagSuggestionResponse
            {
                Tags = predictions.Select(p => p.Tag).ToList(),
                Confidence = predictions.Select(p => p.Confidence).ToList(),
                AvailableTagCount = _classifier.AvailableTags.Count
            };

            _logger.LogInformation("Tag suggestion completed. Suggested tags: {Tags}",
                string.Join(", ", response.Tags));

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error suggesting tags");
            throw;
        }
    }

    /// <summary>
    /// Train a new tag classifier model from Stack Overflow CSV data
    /// </summary>
    public async Task<TagTrainingResponse> TrainModelAsync(TagTrainingRequest request, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var response = new TagTrainingResponse();

        try
        {
            // Determine CSV path
            var csvPath = request.CsvPath ?? _ingestionOptions.CsvPath;
            if (string.IsNullOrWhiteSpace(csvPath))
            {
                response.Errors.Add("CSV path not provided and not configured in settings");
                return response;
            }

            _logger.LogInformation("Starting tag classifier training. CSV Path: {CsvPath}, MaxRows: {MaxRows}, MaxTags: {MaxTags}",
                csvPath, request.MaxRows, request.MaxTags);

            // Parse CSV files
            _logger.LogInformation("Loading Stack Overflow data from CSV...");
            var documents = await _csvParser.ParseAsync(csvPath, request.MaxRows, cancellationToken);

            if (documents == null || documents.Count == 0)
            {
                response.Errors.Add("No data loaded from CSV files");
                return response;
            }

            _logger.LogInformation("Loaded {Count} Stack Overflow questions", documents.Count);

            // Create temporary training data file in the format: Text,Tags
            var tempTrainingFile = Path.Combine(Path.GetTempPath(), $"tag_training_{Guid.NewGuid()}.csv");

            try
            {
                _logger.LogInformation("Creating training data file...");
                var questionsWithTags = documents.Where(d => d.Tags != null && d.Tags.Length > 0).ToList();

                using (var writer = new StreamWriter(tempTrainingFile, false, Encoding.UTF8))
                {
                    // Write header
                    await writer.WriteLineAsync("Text,Tags");

                    // Write training examples
                    foreach (var doc in questionsWithTags)
                    {
                        var combinedText = $"{doc.QuestionTitle} {doc.QuestionBody}".Replace("\"", "\"\"").Trim();
                        var tags = string.Join(",", doc.Tags).Replace("\"", "\"\"");

                        if (!string.IsNullOrWhiteSpace(combinedText) && !string.IsNullOrWhiteSpace(tags))
                        {
                            await writer.WriteLineAsync($"\"{combinedText}\",\"{tags}\"");
                        }
                    }
                }

                response.QuestionsProcessed = questionsWithTags.Count;
                _logger.LogInformation("Created training data with {Count} examples", questionsWithTags.Count);

                // Train the classifier
                _logger.LogInformation("Training tag classifier model...");
                var classifier = new TagClassifier();

                await Task.Run(() => classifier.Train(tempTrainingFile, request.MaxTags), cancellationToken);

                // Create output directory if needed
                var outputDir = Path.GetDirectoryName(request.OutputPath);
                if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
                {
                    Directory.CreateDirectory(outputDir);
                }

                // Save the model
                _logger.LogInformation("Saving trained model to {OutputPath}...", request.OutputPath);
                await Task.Run(() => classifier.SaveModel(request.OutputPath), cancellationToken);

                // Populate response
                response.Success = true;
                response.ModelPath = request.OutputPath;
                response.TagCount = classifier.AvailableTags.Count;
                response.TopTags = classifier.AvailableTags.Take(20).ToList();
                response.Metrics = $"Trained on {questionsWithTags.Count} questions with {classifier.AvailableTags.Count} unique tags";

                stopwatch.Stop();
                response.TrainingDurationSeconds = stopwatch.Elapsed.TotalSeconds;

                _logger.LogInformation("Tag classifier training completed successfully in {Duration:F2}s. Model saved to {ModelPath}",
                    response.TrainingDurationSeconds, response.ModelPath);

                // Reload the model if this is the default path
                if (request.OutputPath == _options.ModelPath)
                {
                    _logger.LogInformation("Reloading tag suggestion service with new model...");
                    _initialized = false;
                    await InitializeAsync(cancellationToken);
                }

                return response;
            }
            finally
            {
                // Clean up temporary file
                if (File.Exists(tempTrainingFile))
                {
                    File.Delete(tempTrainingFile);
                }
            }
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            response.TrainingDurationSeconds = stopwatch.Elapsed.TotalSeconds;
            response.Errors.Add($"Training failed: {ex.Message}");
            _logger.LogError(ex, "Tag classifier training failed");
            return response;
        }
    }
}
