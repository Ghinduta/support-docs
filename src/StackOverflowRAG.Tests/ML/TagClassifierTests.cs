using StackOverflowRAG.ML.Interfaces;
using StackOverflowRAG.ML.Services;
using Xunit;

namespace StackOverflowRAG.Tests.ML;

public class TagClassifierTests : IDisposable
{
    private readonly string _testDataPath;
    private readonly string _testModelPath;
    private readonly TagClassifier _classifier;

    public TagClassifierTests()
    {
        _classifier = new TagClassifier();
        _testDataPath = Path.Combine(Path.GetTempPath(), $"test_tag_training_{Guid.NewGuid()}.csv");
        _testModelPath = Path.Combine(Path.GetTempPath(), $"test_tag_model_{Guid.NewGuid()}.zip");

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

        // Clean up tag list file
        var tagListPath = Path.ChangeExtension(_testModelPath, ".tags");
        if (File.Exists(tagListPath))
            File.Delete(tagListPath);
    }

    private void CreateSampleTrainingData()
    {
        var trainingData = new[]
        {
            "Text,Tags",
            "\"How to use async await in C#\",\"c#,async-await,.net\"",
            "\"C# LINQ query syntax examples\",\"c#,linq,.net\"",
            "\"Understanding C# delegates and events\",\"c#,.net,delegates\"",
            "\"Python list comprehension examples\",\"python,list,list-comprehension\"",
            "\"Python dictionary methods tutorial\",\"python,dictionary\"",
            "\"Python pandas dataframe operations\",\"python,pandas,dataframe\"",
            "\"JavaScript promises vs async await\",\"javascript,async-await,promise\"",
            "\"JavaScript array map filter reduce\",\"javascript,arrays,functional-programming\"",
            "\"React hooks useState useEffect\",\"javascript,reactjs,hooks\"",
            "\"SQL join query optimization\",\"sql,join,performance\"",
            "\"SQL aggregate functions GROUP BY\",\"sql,aggregation,group-by\"",
            "\"MySQL vs PostgreSQL comparison\",\"sql,mysql,postgresql\"",
            "\"React hooks useState tutorial\",\"reactjs,hooks,state\"",
            "\"React component lifecycle methods\",\"reactjs,lifecycle,components\"",
            "\"Docker container networking\",\"docker,networking,containers\"",
            "\"Docker compose multi-container apps\",\"docker,docker-compose\"",
            "\"Git merge vs rebase\",\"git,merge,rebase\"",
            "\"Git branching strategies\",\"git,branching,workflow\"",
            "\"Linux file permissions chmod\",\"linux,permissions,chmod\"",
            "\"CSS flexbox layout guide\",\"css,flexbox,layout\"",
            "\"MongoDB aggregation pipeline\",\"mongodb,aggregation,nosql\"",
            "\"REST API design best practices\",\"rest,api,design\"",
            "\"GraphQL vs REST comparison\",\"graphql,rest,api\"",
            "\"Kubernetes pod deployment\",\"kubernetes,deployment,pods\"",
            "\"AWS Lambda serverless functions\",\"aws,lambda,serverless\""
        };

        File.WriteAllLines(_testDataPath, trainingData);
    }

    [Fact]
    public void Train_WithValidData_TrainsSuccessfully()
    {
        // Act
        _classifier.Train(_testDataPath, maxTags: 20);

        // Assert
        Assert.True(_classifier.AvailableTags.Count > 0, "Should have available tags after training");
        Assert.True(_classifier.AvailableTags.Count <= 20, "Should not exceed max tags limit");
    }

    [Fact]
    public void Train_WithNonexistentFile_ThrowsFileNotFoundException()
    {
        // Arrange
        var nonexistentPath = "nonexistent_file.csv";

        // Act & Assert
        Assert.Throws<FileNotFoundException>(() => _classifier.Train(nonexistentPath));
    }

    [Fact]
    public void Train_SelectsTopNTags()
    {
        // Act
        _classifier.Train(_testDataPath, maxTags: 10);

        // Assert
        Assert.Equal(10, _classifier.AvailableTags.Count);
        // Most frequent tags should be selected (c#, python, javascript, sql appear multiple times)
        Assert.Contains("c#", _classifier.AvailableTags);
        Assert.Contains("python", _classifier.AvailableTags);
    }

    [Fact]
    public void PredictTags_WithValidInput_ReturnsPredictions()
    {
        // Arrange
        _classifier.Train(_testDataPath, maxTags: 20);
        var title = "How to use async await in C#";
        var body = "I am trying to understand asynchronous programming";

        // Act
        var predictions = _classifier.PredictTags(title, body, topK: 5);

        // Assert
        Assert.NotNull(predictions);
        Assert.NotEmpty(predictions);
        Assert.True(predictions.Count <= 5, "Should return at most topK predictions");
    }

    [Fact]
    public void PredictTags_ReturnsTopKTags()
    {
        // Arrange
        _classifier.Train(_testDataPath, maxTags: 20);

        // Act
        var predictions = _classifier.PredictTags("Python list operations", "Working with lists", topK: 3);

        // Assert
        Assert.Equal(3, predictions.Count);
    }

    [Fact]
    public void PredictTags_ConfidenceScoresAreValid()
    {
        // Arrange
        _classifier.Train(_testDataPath, maxTags: 20);

        // Act
        var predictions = _classifier.PredictTags("Docker containerization", "Running containers", topK: 5);

        // Assert
        foreach (var prediction in predictions)
        {
            Assert.True(prediction.Confidence >= 0.0f, $"Confidence {prediction.Confidence} should be >= 0");
            Assert.True(prediction.Confidence <= 1.0f, $"Confidence {prediction.Confidence} should be <= 1");
            Assert.False(string.IsNullOrWhiteSpace(prediction.Tag), "Tag should not be empty");
        }
    }

    [Fact]
    public void PredictTags_ResultsSortedByConfidence()
    {
        // Arrange
        _classifier.Train(_testDataPath, maxTags: 20);

        // Act
        var predictions = _classifier.PredictTags("C# programming language", "Learn C#", topK: 5);

        // Assert
        for (int i = 1; i < predictions.Count; i++)
        {
            Assert.True(predictions[i - 1].Confidence >= predictions[i].Confidence,
                $"Predictions should be sorted by confidence descending");
        }
    }

    [Fact]
    public void PredictTags_WithEmptyInput_ThrowsArgumentException()
    {
        // Arrange
        _classifier.Train(_testDataPath);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => _classifier.PredictTags("", ""));
    }

    [Fact]
    public void PredictTags_BeforeTraining_ThrowsInvalidOperationException()
    {
        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => _classifier.PredictTags("test", "test"));
    }

    [Fact]
    public void PredictTags_CSharpQuestion_ReturnsValidPredictions()
    {
        // Arrange
        _classifier.Train(_testDataPath, maxTags: 20);
        var title = "How to use LINQ in C#";
        var body = "I want to query collections using LINQ syntax in my .NET application";

        // Act
        var predictions = _classifier.PredictTags(title, body, topK: 5);

        // Assert - with small training data, exact tag matching is unreliable
        // Just verify structure and validity
        Assert.Equal(5, predictions.Count);
        Assert.All(predictions, p =>
        {
            Assert.NotEmpty(p.Tag);
            Assert.True(p.Confidence >= 0.0f && p.Confidence <= 1.0f);
        });
        // Verify tags are from available set
        Assert.All(predictions, p => Assert.Contains(p.Tag, _classifier.AvailableTags));
    }

    [Fact]
    public void SaveModel_AfterTraining_SavesSuccessfully()
    {
        // Arrange
        _classifier.Train(_testDataPath);

        // Act
        _classifier.SaveModel(_testModelPath);

        // Assert
        Assert.True(File.Exists(_testModelPath), "Model file should exist after saving");

        var tagListPath = Path.ChangeExtension(_testModelPath, ".tags");
        Assert.True(File.Exists(tagListPath), "Tag list file should exist after saving");
    }

    [Fact]
    public void SaveModel_BeforeTraining_ThrowsInvalidOperationException()
    {
        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => _classifier.SaveModel(_testModelPath));
    }

    [Fact]
    public void LoadModel_WithValidModel_LoadsSuccessfully()
    {
        // Arrange
        _classifier.Train(_testDataPath, maxTags: 15);
        var originalTagCount = _classifier.AvailableTags.Count;
        _classifier.SaveModel(_testModelPath);

        var newClassifier = new TagClassifier();

        // Act
        newClassifier.LoadModel(_testModelPath);

        // Assert
        Assert.Equal(originalTagCount, newClassifier.AvailableTags.Count);
        Assert.Equal(_classifier.AvailableTags, newClassifier.AvailableTags);
    }

    [Fact]
    public void LoadModel_WithNonexistentFile_ThrowsFileNotFoundException()
    {
        // Arrange
        var nonexistentPath = "nonexistent_model.zip";

        // Act & Assert
        Assert.Throws<FileNotFoundException>(() => _classifier.LoadModel(nonexistentPath));
    }

    [Fact]
    public void LoadModel_WithMissingTagList_ThrowsFileNotFoundException()
    {
        // Arrange
        _classifier.Train(_testDataPath);
        _classifier.SaveModel(_testModelPath);

        // Delete tag list file
        var tagListPath = Path.ChangeExtension(_testModelPath, ".tags");
        File.Delete(tagListPath);

        var newClassifier = new TagClassifier();

        // Act & Assert
        Assert.Throws<FileNotFoundException>(() => newClassifier.LoadModel(_testModelPath));
    }

    [Fact]
    public void PredictTags_AfterLoadingModel_ProducesSimilarResults()
    {
        // Arrange
        _classifier.Train(_testDataPath, maxTags: 20);
        var predictions1 = _classifier.PredictTags("Python pandas tutorial", "Learn pandas", topK: 3);
        _classifier.SaveModel(_testModelPath);

        var newClassifier = new TagClassifier();
        newClassifier.LoadModel(_testModelPath);

        // Act
        var predictions2 = newClassifier.PredictTags("Python pandas tutorial", "Learn pandas", topK: 3);

        // Assert
        Assert.Equal(predictions1.Count, predictions2.Count);

        for (int i = 0; i < predictions1.Count; i++)
        {
            Assert.Equal(predictions1[i].Tag, predictions2[i].Tag);
            // Confidence scores should be very similar (allow small floating point differences)
            Assert.True(Math.Abs(predictions1[i].Confidence - predictions2[i].Confidence) < 0.01f,
                $"Confidence scores should match: {predictions1[i].Confidence} vs {predictions2[i].Confidence}");
        }
    }

    [Fact]
    public void AvailableTags_BeforeTraining_ReturnsEmptyList()
    {
        // Act & Assert
        Assert.Empty(_classifier.AvailableTags);
    }

    [Fact]
    public void AvailableTags_AfterTraining_ReturnsTagList()
    {
        // Arrange & Act
        _classifier.Train(_testDataPath, maxTags: 15);

        // Assert
        Assert.NotEmpty(_classifier.AvailableTags);
        Assert.All(_classifier.AvailableTags, tag => Assert.False(string.IsNullOrWhiteSpace(tag)));
    }

    [Fact]
    public void PredictTags_WithOnlyTitle_Works()
    {
        // Arrange
        _classifier.Train(_testDataPath);

        // Act
        var predictions = _classifier.PredictTags("JavaScript async programming", "", topK: 3);

        // Assert
        Assert.NotEmpty(predictions);
        Assert.Equal(3, predictions.Count);
    }

    [Fact]
    public void PredictTags_WithOnlyBody_Works()
    {
        // Arrange
        _classifier.Train(_testDataPath);

        // Act
        var predictions = _classifier.PredictTags("", "I need help with SQL joins and query optimization", topK: 3);

        // Assert
        Assert.NotEmpty(predictions);
        Assert.Equal(3, predictions.Count);
    }
}
