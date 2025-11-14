using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Transforms.Text;
using StackOverflowRAG.ML.Interfaces;
using StackOverflowRAG.ML.Models;

namespace StackOverflowRAG.ML.Services;

/// <summary>
/// TF-IDF vectorizer implementation using ML.NET
/// </summary>
public class TfidfVectorizer : ITfidfVectorizer
{
    private readonly MLContext _mlContext;
    private ITransformer? _model;
    private int _vocabularySize;

    public int VocabularySize => _vocabularySize;

    public TfidfVectorizer()
    {
        _mlContext = new MLContext(seed: 42);
    }

    /// <summary>
    /// Train TF-IDF model on Stack Overflow questions
    /// </summary>
    public void Train(string trainingDataPath)
    {
        if (!File.Exists(trainingDataPath))
        {
            throw new FileNotFoundException($"Training data file not found: {trainingDataPath}");
        }

        // Load training data from CSV
        var dataView = _mlContext.Data.LoadFromTextFile<TagTrainingData>(
            path: trainingDataPath,
            hasHeader: true,
            separatorChar: ',',
            allowQuoting: true
        );

        // Build TF-IDF pipeline
        var textPipeline = _mlContext.Transforms.Text.NormalizeText("NormalizedText", "Text")
            .Append(_mlContext.Transforms.Text.TokenizeIntoWords("Tokens", "NormalizedText"))
            .Append(_mlContext.Transforms.Text.RemoveDefaultStopWords("TokensNoStopWords", "Tokens"))
            .Append(_mlContext.Transforms.Conversion.MapValueToKey("TokensKey", "TokensNoStopWords"))
            .Append(_mlContext.Transforms.Text.ProduceNgrams("Features", "TokensKey",
                ngramLength: 2,
                useAllLengths: true,
                weighting: NgramExtractingEstimator.WeightingCriteria.TfIdf));

        var pipeline = textPipeline;

        // Train the model
        _model = pipeline.Fit(dataView);

        // Calculate vocabulary size from the trained model
        var transformedData = _model.Transform(dataView);
        var features = _mlContext.Data.CreateEnumerable<FeatureVector>(transformedData, reuseRowObject: false);
        _vocabularySize = features.FirstOrDefault()?.Features?.Length ?? 0;
    }

    /// <summary>
    /// Extract TF-IDF features from question text
    /// </summary>
    public float[] ExtractFeatures(string title, string body)
    {
        if (_model == null)
        {
            throw new InvalidOperationException("Model must be trained or loaded before extracting features");
        }

        if (string.IsNullOrWhiteSpace(title) && string.IsNullOrWhiteSpace(body))
        {
            throw new ArgumentException("Title and body cannot both be empty");
        }

        // Combine title and body
        var combinedText = $"{title} {body}".Trim();

        // Create input data
        var input = new TagTrainingData { Text = combinedText };
        var inputData = _mlContext.Data.LoadFromEnumerable(new[] { input });

        // Transform to features
        var transformedData = _model.Transform(inputData);

        // Extract feature vector
        var features = _mlContext.Data.CreateEnumerable<FeatureVector>(transformedData, reuseRowObject: false)
            .FirstOrDefault();

        return features?.Features ?? Array.Empty<float>();
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

        _mlContext.Model.Save(_model, null, modelPath);
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

        _model = _mlContext.Model.Load(modelPath, out var _);

        // Determine vocabulary size from loaded model
        // Create a dummy input to get feature dimensions
        var dummyInput = new TagTrainingData { Text = "test" };
        var dummyData = _mlContext.Data.LoadFromEnumerable(new[] { dummyInput });
        var transformed = _model.Transform(dummyData);
        var features = _mlContext.Data.CreateEnumerable<FeatureVector>(transformed, reuseRowObject: false);
        _vocabularySize = features.FirstOrDefault()?.Features?.Length ?? 0;
    }

    /// <summary>
    /// Internal class for extracting feature vectors from transformed data
    /// </summary>
    private class FeatureVector
    {
        [VectorType]
        public float[]? Features { get; set; }
    }
}
