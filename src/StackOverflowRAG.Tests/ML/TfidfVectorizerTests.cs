using StackOverflowRAG.ML.Models;
using StackOverflowRAG.ML.Services;
using Xunit;

namespace StackOverflowRAG.Tests.ML;

public class TfidfVectorizerTests : IDisposable
{
    private readonly string _testDataPath;
    private readonly string _testModelPath;
    private readonly TfidfVectorizer _vectorizer;

    public TfidfVectorizerTests()
    {
        _vectorizer = new TfidfVectorizer();
        _testDataPath = Path.Combine(Path.GetTempPath(), $"test_training_data_{Guid.NewGuid()}.csv");
        _testModelPath = Path.Combine(Path.GetTempPath(), $"test_model_{Guid.NewGuid()}.zip");

        // Create sample training data
        CreateSampleTrainingData();
    }

    public void Dispose()
    {
        // Clean up test files
        if (File.Exists(_testDataPath))
            File.Delete(_testDataPath);
        if (File.Exists(_testModelPath))
            File.Delete(_testModelPath);
    }

    private void CreateSampleTrainingData()
    {
        var trainingData = new[]
        {
            "Text,Tags",
            "\"How to use async await in C#\",\"c#,async-await,.net\"",
            "\"Python list comprehension examples\",\"python,list,list-comprehension\"",
            "\"JavaScript promises vs async await\",\"javascript,async-await,promise\"",
            "\"SQL join query optimization\",\"sql,join,performance\"",
            "\"React hooks useState tutorial\",\"reactjs,hooks,state\"",
            "\"Docker container networking\",\"docker,networking,containers\"",
            "\"Git merge vs rebase\",\"git,merge,rebase\"",
            "\"Linux file permissions chmod\",\"linux,permissions,chmod\"",
            "\"CSS flexbox layout guide\",\"css,flexbox,layout\"",
            "\"MongoDB aggregation pipeline\",\"mongodb,aggregation,nosql\""
        };

        File.WriteAllLines(_testDataPath, trainingData);
    }

    [Fact]
    public void Train_WithValidData_TrainsSuccessfully()
    {
        // Act
        _vectorizer.Train(_testDataPath);

        // Assert
        Assert.True(_vectorizer.VocabularySize > 0, "Vocabulary size should be greater than 0 after training");
    }

    [Fact]
    public void Train_WithNonexistentFile_ThrowsFileNotFoundException()
    {
        // Arrange
        var nonexistentPath = "nonexistent_file.csv";

        // Act & Assert
        Assert.Throws<FileNotFoundException>(() => _vectorizer.Train(nonexistentPath));
    }

    [Fact]
    public void ExtractFeatures_WithValidInput_ReturnsFeatureVector()
    {
        // Arrange
        _vectorizer.Train(_testDataPath);
        var title = "How to use async await";
        var body = "I am trying to understand async programming in C#";

        // Act
        var features = _vectorizer.ExtractFeatures(title, body);

        // Assert
        Assert.NotNull(features);
        Assert.NotEmpty(features);
        Assert.Equal(_vectorizer.VocabularySize, features.Length);
    }

    [Fact]
    public void ExtractFeatures_WithEmptyInput_ThrowsArgumentException()
    {
        // Arrange
        _vectorizer.Train(_testDataPath);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => _vectorizer.ExtractFeatures("", ""));
    }

    [Fact]
    public void ExtractFeatures_BeforeTraining_ThrowsInvalidOperationException()
    {
        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => _vectorizer.ExtractFeatures("test", "test"));
    }

    [Fact]
    public void VocabularySize_IsReasonable()
    {
        // Arrange
        _vectorizer.Train(_testDataPath);

        // Act
        var vocabSize = _vectorizer.VocabularySize;

        // Assert
        Assert.True(vocabSize > 10, "Vocabulary should have at least 10 features");
        Assert.True(vocabSize < 10000, "Vocabulary should not exceed 10000 features for this small dataset");
    }

    [Fact]
    public void SaveModel_AfterTraining_SavesSuccessfully()
    {
        // Arrange
        _vectorizer.Train(_testDataPath);

        // Act
        _vectorizer.SaveModel(_testModelPath);

        // Assert
        Assert.True(File.Exists(_testModelPath), "Model file should exist after saving");
    }

    [Fact]
    public void SaveModel_BeforeTraining_ThrowsInvalidOperationException()
    {
        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => _vectorizer.SaveModel(_testModelPath));
    }

    [Fact]
    public void LoadModel_WithValidModel_LoadsSuccessfully()
    {
        // Arrange
        _vectorizer.Train(_testDataPath);
        _vectorizer.SaveModel(_testModelPath);

        var newVectorizer = new TfidfVectorizer();

        // Act
        newVectorizer.LoadModel(_testModelPath);

        // Assert
        Assert.Equal(_vectorizer.VocabularySize, newVectorizer.VocabularySize);
    }

    [Fact]
    public void LoadModel_WithNonexistentFile_ThrowsFileNotFoundException()
    {
        // Arrange
        var nonexistentPath = "nonexistent_model.zip";

        // Act & Assert
        Assert.Throws<FileNotFoundException>(() => _vectorizer.LoadModel(nonexistentPath));
    }

    [Fact]
    public void ExtractFeatures_AfterLoadingModel_ProducesSameResults()
    {
        // Arrange
        _vectorizer.Train(_testDataPath);
        var features1 = _vectorizer.ExtractFeatures("async await", "C# programming");
        _vectorizer.SaveModel(_testModelPath);

        var newVectorizer = new TfidfVectorizer();
        newVectorizer.LoadModel(_testModelPath);

        // Act
        var features2 = newVectorizer.ExtractFeatures("async await", "C# programming");

        // Assert
        Assert.Equal(features1.Length, features2.Length);
        for (int i = 0; i < features1.Length; i++)
        {
            Assert.Equal(features1[i], features2[i], precision: 5);
        }
    }

    [Fact]
    public void ExtractFeatures_SimilarQuestions_ProduceSimilarVectors()
    {
        // Arrange
        _vectorizer.Train(_testDataPath);
        var features1 = _vectorizer.ExtractFeatures("async await C#", "How to use async programming");
        var features2 = _vectorizer.ExtractFeatures("C# async await", "async programming tutorial");

        // Act - Calculate cosine similarity
        var dotProduct = 0.0;
        var magnitude1 = 0.0;
        var magnitude2 = 0.0;

        for (int i = 0; i < features1.Length; i++)
        {
            dotProduct += features1[i] * features2[i];
            magnitude1 += features1[i] * features1[i];
            magnitude2 += features2[i] * features2[i];
        }

        var similarity = dotProduct / (Math.Sqrt(magnitude1) * Math.Sqrt(magnitude2));

        // Assert - Similar questions should have positive similarity
        Assert.True(similarity > 0.1, $"Similar questions should have similarity > 0.1, got {similarity}");
    }

    [Fact]
    public void ExtractFeatures_DifferentTopics_ProduceDifferentVectors()
    {
        // Arrange
        _vectorizer.Train(_testDataPath);
        var features1 = _vectorizer.ExtractFeatures("async await C#", "asynchronous programming");
        var features2 = _vectorizer.ExtractFeatures("Docker networking", "container communication");

        // Act - Calculate cosine similarity
        var dotProduct = 0.0;
        var magnitude1 = 0.0;
        var magnitude2 = 0.0;

        for (int i = 0; i < features1.Length; i++)
        {
            dotProduct += features1[i] * features2[i];
            magnitude1 += features1[i] * features1[i];
            magnitude2 += features2[i] * features2[i];
        }

        var similarity = dotProduct / (Math.Sqrt(magnitude1) * Math.Sqrt(magnitude2));

        // Assert - Different topics should have lower similarity than similar ones
        var similarFeatures1 = _vectorizer.ExtractFeatures("async await C#", "async programming tutorial");
        var dotProductSimilar = 0.0;
        var magnitude1Similar = 0.0;
        var magnitude2Similar = 0.0;

        for (int i = 0; i < features1.Length; i++)
        {
            dotProductSimilar += features1[i] * similarFeatures1[i];
            magnitude1Similar += features1[i] * features1[i];
            magnitude2Similar += similarFeatures1[i] * similarFeatures1[i];
        }

        var similaritySimilar = dotProductSimilar / (Math.Sqrt(magnitude1Similar) * Math.Sqrt(magnitude2Similar));

        Assert.True(similarity < similaritySimilar,
            $"Different topics similarity ({similarity}) should be less than similar topics ({similaritySimilar})");
    }
}
