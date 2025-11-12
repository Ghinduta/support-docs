using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using StackOverflowRAG.Core.Configuration;
using StackOverflowRAG.Core.Services;
using StackOverflowRAG.Data.Models;
using StackOverflowRAG.Data.Parsers;
using StackOverflowRAG.Data.Repositories;
using StackOverflowRAG.Data.Services;

namespace StackOverflowRAG.Tests.Core;

public class IngestionServiceTests
{
    private readonly Mock<IStackOverflowCsvParser> _mockParser;
    private readonly Mock<IChunkingService> _mockChunkingService;
    private readonly Mock<IEmbeddingService> _mockEmbeddingService;
    private readonly Mock<IVectorStoreRepository> _mockVectorStore;
    private readonly Mock<ILogger<IngestionService>> _mockLogger;
    private readonly IngestionService _service;

    public IngestionServiceTests()
    {
        _mockParser = new Mock<IStackOverflowCsvParser>();
        _mockChunkingService = new Mock<IChunkingService>();
        _mockEmbeddingService = new Mock<IEmbeddingService>();
        _mockVectorStore = new Mock<IVectorStoreRepository>();
        _mockLogger = new Mock<ILogger<IngestionService>>();

        var options = Options.Create(new IngestionOptions
        {
            CsvPath = "./test.csv",
            MaxRows = 100,
            ChunkSize = 500,
            ChunkOverlap = 50
        });

        _service = new IngestionService(
            _mockParser.Object,
            _mockChunkingService.Object,
            _mockEmbeddingService.Object,
            _mockVectorStore.Object,
            options,
            _mockLogger.Object);
    }

    [Fact]
    public async Task IngestAsync_WithValidData_ReturnsSuccessResult()
    {
        // Arrange
        var documents = new List<StackOverflowDocument>
        {
            new StackOverflowDocument
            {
                PostId = 1,
                QuestionTitle = "Test",
                QuestionBody = "Body",
                Tags = new[] { "tag1" }
            }
        };

        var chunks = new List<DocumentChunk>
        {
            new DocumentChunk
            {
                PostId = 1,
                ChunkText = "Test chunk",
                ChunkIndex = 0,
                Embedding = new float[] { 0.1f, 0.2f }
            }
        };

        _mockVectorStore.Setup(x => x.EnsureCollectionExistsAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockParser.Setup(x => x.ParseAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(documents);

        _mockChunkingService.Setup(x => x.ChunkDocuments(It.IsAny<List<StackOverflowDocument>>(), It.IsAny<int>(), It.IsAny<int>()))
            .Returns(chunks);

        _mockEmbeddingService.Setup(x => x.GenerateChunkEmbeddingsAsync(It.IsAny<List<DocumentChunk>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockVectorStore.Setup(x => x.UpsertChunksAsync(It.IsAny<List<DocumentChunk>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockVectorStore.Setup(x => x.GetCountAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1L);

        // Act
        var result = await _service.IngestAsync();

        // Assert
        Assert.Equal(1, result.DocumentsLoaded);
        Assert.Equal(1, result.ChunksCreated);
        Assert.Equal(1, result.EmbeddingsGenerated);
        Assert.Equal(1, result.QdrantUpserts);
        Assert.True(result.ValidationPassed);
        Assert.Empty(result.ErrorMessages);
    }

    [Fact]
    public async Task IngestAsync_WithNoDocuments_ReturnsErrorResult()
    {
        // Arrange
        _mockVectorStore.Setup(x => x.EnsureCollectionExistsAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockParser.Setup(x => x.ParseAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<StackOverflowDocument>());

        // Act
        var result = await _service.IngestAsync();

        // Assert
        Assert.Equal(0, result.DocumentsLoaded);
        Assert.False(result.ValidationPassed);
        Assert.Contains("No valid documents found in CSV", result.ErrorMessages);
    }

    [Fact]
    public async Task IngestAsync_WithFileNotFound_ReturnsErrorResult()
    {
        // Arrange
        _mockVectorStore.Setup(x => x.EnsureCollectionExistsAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockParser.Setup(x => x.ParseAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new FileNotFoundException("test.csv not found"));

        // Act
        var result = await _service.IngestAsync();

        // Assert
        Assert.False(result.ValidationPassed);
        Assert.Contains("CSV file not found", result.ErrorMessages[0]);
    }
}
