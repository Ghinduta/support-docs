namespace StackOverflowRAG.ML.Interfaces;

/// <summary>
/// Interface for multi-label tag classification
/// </summary>
public interface ITagClassifier
{
    /// <summary>
    /// Predict Stack Overflow tags for a question
    /// </summary>
    /// <param name="title">Question title</param>
    /// <param name="body">Question body</param>
    /// <param name="topK">Number of top tags to return (default: 5)</param>
    /// <returns>List of predicted tags with confidence scores</returns>
    List<TagPrediction> PredictTags(string title, string body, int topK = 5);

    /// <summary>
    /// Train the tag classifier on Stack Overflow data
    /// </summary>
    /// <param name="trainingDataPath">Path to CSV file containing training data (Text, Tags)</param>
    /// <param name="maxTags">Maximum number of unique tags to include in the model (default: 50)</param>
    void Train(string trainingDataPath, int maxTags = 50);

    /// <summary>
    /// Save the trained model to disk
    /// </summary>
    /// <param name="modelPath">Path where model will be saved</param>
    void SaveModel(string modelPath);

    /// <summary>
    /// Load a previously trained model from disk
    /// </summary>
    /// <param name="modelPath">Path to the saved model</param>
    void LoadModel(string modelPath);

    /// <summary>
    /// Get the list of tags the model can predict
    /// </summary>
    IReadOnlyList<string> AvailableTags { get; }
}

/// <summary>
/// Represents a predicted tag with confidence score
/// </summary>
public record TagPrediction(string Tag, float Confidence);
