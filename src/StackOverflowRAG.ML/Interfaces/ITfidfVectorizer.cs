namespace StackOverflowRAG.ML.Interfaces;

/// <summary>
/// Interface for TF-IDF feature extraction from question text
/// </summary>
public interface ITfidfVectorizer
{
    /// <summary>
    /// Extract TF-IDF feature vector from question title and body
    /// </summary>
    /// <param name="title">Question title</param>
    /// <param name="body">Question body</param>
    /// <returns>Feature vector as float array</returns>
    float[] ExtractFeatures(string title, string body);

    /// <summary>
    /// Train the TF-IDF vectorizer on a collection of training data
    /// </summary>
    /// <param name="trainingDataPath">Path to CSV file containing training data</param>
    void Train(string trainingDataPath);

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
    /// Get the size of the TF-IDF vocabulary
    /// </summary>
    int VocabularySize { get; }
}
