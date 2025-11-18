using Microsoft.ML;
using Microsoft.ML.Data;
using StackOverflowRAG.ML.Interfaces;
using StackOverflowRAG.ML.Models;

namespace StackOverflowRAG.ML.Services;

/// <summary>
/// Multi-label tag classifier using ML.NET logistic regression
/// </summary>
public class TagClassifier : ITagClassifier
{
    private readonly MLContext _mlContext;
    private ITransformer? _model;
    private PredictionEngine<TagClassifierInput, TagClassifierOutput>? _predictionEngine;
    private List<string> _availableTags = new();

    public IReadOnlyList<string> AvailableTags => _availableTags.AsReadOnly();

    public TagClassifier()
    {
        _mlContext = new MLContext(seed: 42);
    }

    /// <summary>
    /// Train multi-class classifier on Stack Overflow tags
    /// </summary>
    public void Train(string trainingDataPath, int maxTags = 50)
    {
        if (!File.Exists(trainingDataPath))
        {
            throw new FileNotFoundException($"Training data file not found: {trainingDataPath}");
        }

        // Load raw training data
        var rawData = LoadAndPreprocessData(trainingDataPath, maxTags);

        // Split into train/test (80/20)
        var trainTestSplit = _mlContext.Data.TrainTestSplit(rawData, testFraction: 0.2, seed: 42);

        // Build classification pipeline
        var pipeline = _mlContext.Transforms.Text.FeaturizeText("Features", "Text")
            .Append(_mlContext.Transforms.Conversion.MapValueToKey("Label", "Label"))
            .Append(_mlContext.MulticlassClassification.Trainers.LbfgsMaximumEntropy())
            .Append(_mlContext.Transforms.Conversion.MapKeyToValue("PredictedLabel"));

        // Train the model
        Console.WriteLine("Training tag classifier...");
        _model = pipeline.Fit(trainTestSplit.TrainSet);

        // Evaluate on test set
        var predictions = _model.Transform(trainTestSplit.TestSet);
        var metrics = _mlContext.MulticlassClassification.Evaluate(predictions);

        Console.WriteLine($"Model trained successfully!");
        Console.WriteLine($"  Macro Accuracy: {metrics.MacroAccuracy:F4}");
        Console.WriteLine($"  Micro Accuracy: {metrics.MicroAccuracy:F4}");
        Console.WriteLine($"  Log Loss: {metrics.LogLoss:F4}");
        Console.WriteLine($"  Available Tags: {_availableTags.Count}");

        // Create prediction engine
        _predictionEngine = _mlContext.Model.CreatePredictionEngine<TagClassifierInput, TagClassifierOutput>(_model);
    }

    /// <summary>
    /// Predict top K tags for a question
    /// </summary>
    public List<TagPrediction> PredictTags(string title, string body, int topK = 5)
    {
        if (_predictionEngine == null || _model == null)
        {
            throw new InvalidOperationException("Model must be trained or loaded before making predictions");
        }

        if (string.IsNullOrWhiteSpace(title) && string.IsNullOrWhiteSpace(body))
        {
            throw new ArgumentException("Title and body cannot both be empty");
        }

        // Combine title and body
        var combinedText = $"{title} {body}".Trim();
        var input = new TagClassifierInput { Text = combinedText };

        // Get prediction with probability scores
        var prediction = _predictionEngine.Predict(input);

        // Convert scores to tag predictions
        var tagPredictions = new List<TagPrediction>();

        for (int i = 0; i < _availableTags.Count && i < prediction.Score.Length; i++)
        {
            tagPredictions.Add(new TagPrediction(_availableTags[i], prediction.Score[i]));
        }

        // Return top K tags sorted by confidence
        return tagPredictions
            .OrderByDescending(tp => tp.Confidence)
            .Take(topK)
            .ToList();
    }

    /// <summary>
    /// Save trained model to disk
    /// </summary>
    public void SaveModel(string modelPath)
    {
        if (_model == null)
        {
            throw new InvalidOperationException("Model must be trained before saving");
        }

        var directory = Path.GetDirectoryName(modelPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        // Save model
        _mlContext.Model.Save(_model, null, modelPath);

        // Save tag list alongside model
        var tagListPath = Path.ChangeExtension(modelPath, ".tags");
        File.WriteAllLines(tagListPath, _availableTags);

        Console.WriteLine($"Model saved to: {modelPath}");
        Console.WriteLine($"Tag list saved to: {tagListPath}");
    }

    /// <summary>
    /// Load previously trained model from disk
    /// </summary>
    public void LoadModel(string modelPath)
    {
        if (!File.Exists(modelPath))
        {
            throw new FileNotFoundException($"Model file not found: {modelPath}");
        }

        // Load model
        _model = _mlContext.Model.Load(modelPath, out var modelInputSchema);

        // Load tag list
        var tagListPath = Path.ChangeExtension(modelPath, ".tags");
        if (!File.Exists(tagListPath))
        {
            throw new FileNotFoundException($"Tag list file not found: {tagListPath}");
        }

        _availableTags = File.ReadAllLines(tagListPath).ToList();

        // Create prediction engine
        _predictionEngine = _mlContext.Model.CreatePredictionEngine<TagClassifierInput, TagClassifierOutput>(_model);

        Console.WriteLine($"Model loaded from: {modelPath}");
        Console.WriteLine($"Available tags: {_availableTags.Count}");
    }

    /// <summary>
    /// Load and preprocess training data, filtering to top N tags
    /// </summary>
    private IDataView LoadAndPreprocessData(string trainingDataPath, int maxTags)
    {
        // Load raw data
        var rawData = _mlContext.Data.LoadFromTextFile<TagTrainingData>(
            path: trainingDataPath,
            hasHeader: true,
            separatorChar: ',',
            allowQuoting: true
        );

        // Convert to enumerable to process
        var dataList = _mlContext.Data.CreateEnumerable<TagTrainingData>(rawData, reuseRowObject: false).ToList();

        // Count tag frequencies
        var tagFrequency = new Dictionary<string, int>();
        foreach (var row in dataList)
        {
            if (string.IsNullOrWhiteSpace(row.Tags)) continue;

            var tags = row.Tags.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(t => t.Trim())
                .Where(t => !string.IsNullOrWhiteSpace(t));

            foreach (var tag in tags)
            {
                tagFrequency[tag] = tagFrequency.GetValueOrDefault(tag, 0) + 1;
            }
        }

        // Get top N most frequent tags
        _availableTags = tagFrequency
            .OrderByDescending(kvp => kvp.Value)
            .Take(maxTags)
            .Select(kvp => kvp.Key)
            .ToList();

        Console.WriteLine($"Selected top {_availableTags.Count} tags from {tagFrequency.Count} unique tags");
        Console.WriteLine($"Top 10 tags: {string.Join(", ", _availableTags.Take(10))}");

        // Create training examples: one row per (text, tag) pair
        var trainingExamples = new List<TagClassifierInput>();
        var topTagsSet = new HashSet<string>(_availableTags);

        foreach (var row in dataList)
        {
            if (string.IsNullOrWhiteSpace(row.Tags)) continue;

            var questionTags = row.Tags.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(t => t.Trim())
                .Where(t => topTagsSet.Contains(t))
                .ToList();

            // For each tag this question has, create a training example
            foreach (var tag in questionTags)
            {
                trainingExamples.Add(new TagClassifierInput
                {
                    Text = row.Text,
                    Label = tag
                });
            }
        }

        Console.WriteLine($"Created {trainingExamples.Count} training examples from {dataList.Count} questions");

        return _mlContext.Data.LoadFromEnumerable(trainingExamples);
    }
}
