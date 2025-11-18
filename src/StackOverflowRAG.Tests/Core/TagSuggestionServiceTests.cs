using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using StackOverflowRAG.Core.Configuration;
using StackOverflowRAG.Core.Services;
using StackOverflowRAG.Data.Parsers;
using Xunit;

namespace StackOverflowRAG.Tests.Core;

public class TagSuggestionServiceTests : IDisposable
{
    private readonly string _testModelPath;
    private readonly string _testDataPath;
    private readonly Mock<ILogger<TagSuggestionService>> _mockLogger;
    private readonly Mock<IStackOverflowCsvParser> _mockCsvParser;
    private readonly Mock<IOptions<IngestionOptions>> _mockIngestionOptions;

    public TagSuggestionServiceTests()
    {
        _testModelPath = Path.Combine(Path.GetTempPath(), $"test_tag_model_{Guid.NewGuid()}.zip");
        _testDataPath = Path.Combine(Path.GetTempPath(), $"test_training_data_{Guid.NewGuid()}.csv");
        _mockLogger = new Mock<ILogger<TagSuggestionService>>();
        _mockCsvParser = new Mock<IStackOverflowCsvParser>();
        _mockIngestionOptions = new Mock<IOptions<IngestionOptions>>();
        _mockIngestionOptions.Setup(x => x.Value).Returns(new IngestionOptions());

        // Create sample training data and train a model for testing
        CreateSampleTrainingData();
        TrainTestModel();
    }

    public void Dispose()
    {
        // Clean up test files
        if (File.Exists(_testModelPath))
            File.Delete(_testModelPath);
        if (File.Exists(_testDataPath))
            File.Delete(_testDataPath);

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
            "\"C# LINQ query syntax\",\"c#,linq,.net\"",
            "\"Python list comprehension\",\"python,list\"",
            "\"Python pandas dataframe\",\"python,pandas\"",
            "\"JavaScript promises\",\"javascript,promise\"",
            "\"React hooks useState\",\"javascript,reactjs,hooks\"",
            "\"SQL join optimization\",\"sql,join\"",
            "\"Docker networking\",\"docker,networking\"",
            "\"Git merge vs rebase\",\"git,merge\"",
            "\"MongoDB aggregation\",\"mongodb,aggregation\""
        };

        File.WriteAllLines(_testDataPath, trainingData);
    }

    private void TrainTestModel()
    {
        var classifier = new StackOverflowRAG.ML.Services.TagClassifier();
        classifier.Train(_testDataPath, maxTags: 20);
        classifier.SaveModel(_testModelPath);
    }

    [Fact]
    public void TagSuggestionOptions_Validate_ValidOptions_DoesNotThrow()
    {
        // Arrange
        var options = new TagSuggestionOptions
        {
            ModelPath = "models/tag-classifier.zip",
            DefaultTopK = 5,
            Enabled = true
        };

        // Act & Assert
        options.Validate(); // Should not throw
    }

    [Fact]
    public void TagSuggestionOptions_Validate_NullModelPath_ThrowsInvalidOperationException()
    {
        // Arrange
        var options = new TagSuggestionOptions
        {
            ModelPath = null!,
            DefaultTopK = 5
        };

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => options.Validate());
    }

    [Fact]
    public void TagSuggestionOptions_Validate_InvalidDefaultTopK_ThrowsInvalidOperationException()
    {
        // Arrange
        var options = new TagSuggestionOptions
        {
            ModelPath = "models/tag-classifier.zip",
            DefaultTopK = 15 // Invalid: > 10
        };

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => options.Validate());
    }

    [Fact]
    public async Task InitializeAsync_WithValidModel_InitializesSuccessfully()
    {
        // Arrange
        var options = Options.Create(new TagSuggestionOptions
        {
            ModelPath = _testModelPath,
            Enabled = true
        });

        var service = new TagSuggestionService(_mockLogger.Object, options, _mockCsvParser.Object, _mockIngestionOptions.Object);

        // Act
        await service.InitializeAsync();

        // Assert - calling Initialize again should log that it's already initialized
        await service.InitializeAsync();
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("already initialized")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task InitializeAsync_WithMissingModel_LogsWarning()
    {
        // Arrange
        var options = Options.Create(new TagSuggestionOptions
        {
            ModelPath = "nonexistent-model.zip",
            Enabled = true
        });

        var service = new TagSuggestionService(_mockLogger.Object, options, _mockCsvParser.Object, _mockIngestionOptions.Object);

        // Act
        await service.InitializeAsync();

        // Assert - should log warning about missing model
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("not found")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task SuggestTagsAsync_WithValidInput_ReturnsTagSuggestions()
    {
        // Arrange
        var options = Options.Create(new TagSuggestionOptions
        {
            ModelPath = _testModelPath,
            Enabled = true
        });

        var service = new TagSuggestionService(_mockLogger.Object, options, _mockCsvParser.Object, _mockIngestionOptions.Object);
        await service.InitializeAsync();

        // Act
        var result = await service.SuggestTagsAsync("Python programming", "Learn pandas", topK: 3);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Tags.Count);
        Assert.Equal(3, result.Confidence.Count);
        Assert.True(result.AvailableTagCount > 0);
        Assert.All(result.Confidence, score => Assert.InRange(score, 0.0f, 1.0f));
    }

    [Fact]
    public async Task SuggestTagsAsync_BeforeInitialization_ThrowsInvalidOperationException()
    {
        // Arrange
        var options = Options.Create(new TagSuggestionOptions
        {
            ModelPath = _testModelPath,
            Enabled = true
        });

        var service = new TagSuggestionService(_mockLogger.Object, options, _mockCsvParser.Object, _mockIngestionOptions.Object);
        // Note: NOT calling InitializeAsync

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.SuggestTagsAsync("Test title", "Test body"));
    }

    [Fact]
    public async Task SuggestTagsAsync_WithEmptyTitleAndBody_ThrowsArgumentException()
    {
        // Arrange
        var options = Options.Create(new TagSuggestionOptions
        {
            ModelPath = _testModelPath,
            Enabled = true
        });

        var service = new TagSuggestionService(_mockLogger.Object, options, _mockCsvParser.Object, _mockIngestionOptions.Object);
        await service.InitializeAsync();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => service.SuggestTagsAsync("", ""));
    }

    [Fact]
    public async Task SuggestTagsAsync_ConfidenceScoresInDescendingOrder()
    {
        // Arrange
        var options = Options.Create(new TagSuggestionOptions
        {
            ModelPath = _testModelPath,
            Enabled = true
        });

        var service = new TagSuggestionService(_mockLogger.Object, options, _mockCsvParser.Object, _mockIngestionOptions.Object);
        await service.InitializeAsync();

        // Act
        var result = await service.SuggestTagsAsync("C# async programming", "Using async await", topK: 5);

        // Assert
        for (int i = 1; i < result.Confidence.Count; i++)
        {
            Assert.True(result.Confidence[i - 1] >= result.Confidence[i],
                "Confidence scores should be in descending order");
        }
    }

    [Fact]
    public async Task SuggestTagsAsync_WithOnlyTitle_Works()
    {
        // Arrange
        var options = Options.Create(new TagSuggestionOptions
        {
            ModelPath = _testModelPath,
            Enabled = true
        });

        var service = new TagSuggestionService(_mockLogger.Object, options, _mockCsvParser.Object, _mockIngestionOptions.Object);
        await service.InitializeAsync();

        // Act
        var result = await service.SuggestTagsAsync("JavaScript promises", "", topK: 3);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Tags.Count);
    }

    [Fact]
    public async Task SuggestTagsAsync_WithOnlyBody_Works()
    {
        // Arrange
        var options = Options.Create(new TagSuggestionOptions
        {
            ModelPath = _testModelPath,
            Enabled = true
        });

        var service = new TagSuggestionService(_mockLogger.Object, options, _mockCsvParser.Object, _mockIngestionOptions.Object);
        await service.InitializeAsync();

        // Act
        var result = await service.SuggestTagsAsync("", "I need help with SQL joins", topK: 3);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Tags.Count);
    }
}
