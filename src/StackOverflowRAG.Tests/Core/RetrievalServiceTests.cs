using Microsoft.Extensions.Logging;
using Moq;
using StackOverflowRAG.Core.Services;
using StackOverflowRAG.Data.Models;
using StackOverflowRAG.Data.Repositories;
using StackOverflowRAG.Data.Services;

namespace StackOverflowRAG.Tests.Core;

public class RetrievalServiceTests
{
    private readonly Mock<IEmbeddingService> _mockEmbeddingService;
    private readonly Mock<IVectorStoreRepository> _mockVectorStore;
    private readonly Mock<ILogger<RetrievalService>> _mockLogger;
    private readonly RetrievalService _service;

    public RetrievalServiceTests()
    {
        _mockEmbeddingService = new Mock<IEmbeddingService>();
        _mockVectorStore = new Mock<IVectorStoreRepository>();
        _mockLogger = new Mock<ILogger<RetrievalService>>();

        _service = new RetrievalService(
            _mockEmbeddingService.Object,
            _mockVectorStore.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task SearchAsync_ValidQuery_ReturnsResults()
    {
        // Arrange
        var query = "How to use async/await in C#?";
        var topK = 10;
        var queryEmbedding = new float[1536]; // Mock embedding
        Array.Fill(queryEmbedding, 0.5f);

        var mockResults = new List<(DocumentChunk Chunk, float Score)>
        {
            (new DocumentChunk
            {
                ChunkId = "1_chunk_0",
                PostId = 1,
                QuestionTitle = "Async/Await in C#",
                ChunkText = "Async/await is a feature...",
                ChunkIndex = 0
            }, 0.95f),
            (new DocumentChunk
            {
                ChunkId = "2_chunk_0",
                PostId = 2,
                QuestionTitle = "Understanding async programming",
                ChunkText = "When using async...",
                ChunkIndex = 0
            }, 0.87f)
        };

        _mockEmbeddingService
            .Setup(x => x.GenerateEmbeddingAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(queryEmbedding);

        _mockVectorStore
            .Setup(x => x.SearchAsync(queryEmbedding, topK, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockResults);

        // Act
        var results = await _service.SearchAsync(query, topK);

        // Assert
        Assert.NotNull(results);
        Assert.Equal(2, results.Count);
        Assert.Equal(0.95f, results[0].Score);
        Assert.Equal(0.87f, results[1].Score);
        Assert.Equal("Async/Await in C#", results[0].QuestionTitle);
    }

    [Fact]
    public async Task SearchAsync_EmptyQuery_ThrowsArgumentException()
    {
        // Arrange
        var query = "";
        var topK = 10;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.SearchAsync(query, topK));
    }

    [Fact]
    public async Task SearchAsync_NullQuery_ThrowsArgumentException()
    {
        // Arrange
        string? query = null;
        var topK = 10;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.SearchAsync(query!, topK));
    }

    [Fact]
    public async Task SearchAsync_InvalidTopK_ThrowsArgumentException()
    {
        // Arrange
        var query = "test query";
        var topK = 0;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.SearchAsync(query, topK));
    }

    [Fact]
    public async Task SearchAsync_NoResults_ReturnsEmptyList()
    {
        // Arrange
        var query = "nonexistent query";
        var topK = 10;
        var queryEmbedding = new float[1536];
        Array.Fill(queryEmbedding, 0.5f);

        _mockEmbeddingService
            .Setup(x => x.GenerateEmbeddingAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(queryEmbedding);

        _mockVectorStore
            .Setup(x => x.SearchAsync(queryEmbedding, topK, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<(DocumentChunk, float)>());

        // Act
        var results = await _service.SearchAsync(query, topK);

        // Assert
        Assert.NotNull(results);
        Assert.Empty(results);
    }

    [Fact]
    public async Task SearchAsync_ResultsAreSortedByScore()
    {
        // Arrange
        var query = "test query";
        var topK = 10;
        var queryEmbedding = new float[1536];
        Array.Fill(queryEmbedding, 0.5f);

        var mockResults = new List<(DocumentChunk Chunk, float Score)>
        {
            (new DocumentChunk { ChunkId = "1", PostId = 1, QuestionTitle = "Title 1", ChunkText = "Text 1", ChunkIndex = 0 }, 0.95f),
            (new DocumentChunk { ChunkId = "2", PostId = 2, QuestionTitle = "Title 2", ChunkText = "Text 2", ChunkIndex = 0 }, 0.87f),
            (new DocumentChunk { ChunkId = "3", PostId = 3, QuestionTitle = "Title 3", ChunkText = "Text 3", ChunkIndex = 0 }, 0.75f)
        };

        _mockEmbeddingService
            .Setup(x => x.GenerateEmbeddingAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(queryEmbedding);

        _mockVectorStore
            .Setup(x => x.SearchAsync(queryEmbedding, topK, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockResults);

        // Act
        var results = await _service.SearchAsync(query, topK);

        // Assert
        Assert.Equal(3, results.Count);
        Assert.True(results[0].Score >= results[1].Score);
        Assert.True(results[1].Score >= results[2].Score);
    }

    [Fact]
    public async Task HybridSearchAsync_WithHybridEnabled_UsesHybridSearch()
    {
        // Arrange
        var query = "async await C#";
        var topK = 10;
        var queryEmbedding = new float[1536];
        Array.Fill(queryEmbedding, 0.5f);

        var mockResults = new List<(DocumentChunk Chunk, float Score)>
        {
            (new DocumentChunk
            {
                ChunkId = "1_chunk_0",
                PostId = 1,
                QuestionTitle = "Async/Await in C#",
                ChunkText = "Using async and await...",
                ChunkIndex = 0
            }, 0.95f)
        };

        _mockEmbeddingService
            .Setup(x => x.GenerateEmbeddingAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(queryEmbedding);

        _mockVectorStore
            .Setup(x => x.HybridSearchAsync(
                queryEmbedding,
                query,
                topK,
                It.IsAny<double>(),
                It.IsAny<double>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockResults);

        // Act
        var results = await _service.HybridSearchAsync(query, topK, useHybrid: true);

        // Assert
        Assert.NotNull(results);
        Assert.Single(results);
        Assert.Equal(0.95f, results[0].Score);
        _mockVectorStore.Verify(
            x => x.HybridSearchAsync(queryEmbedding, query, topK, It.IsAny<double>(), It.IsAny<double>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HybridSearchAsync_WithHybridDisabled_UsesVectorSearch()
    {
        // Arrange
        var query = "test query";
        var topK = 10;
        var queryEmbedding = new float[1536];
        Array.Fill(queryEmbedding, 0.5f);

        var mockResults = new List<(DocumentChunk Chunk, float Score)>
        {
            (new DocumentChunk { ChunkId = "1", PostId = 1, QuestionTitle = "Title", ChunkText = "Text", ChunkIndex = 0 }, 0.95f)
        };

        _mockEmbeddingService
            .Setup(x => x.GenerateEmbeddingAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(queryEmbedding);

        _mockVectorStore
            .Setup(x => x.SearchAsync(queryEmbedding, topK, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockResults);

        // Act
        var results = await _service.HybridSearchAsync(query, topK, useHybrid: false);

        // Assert
        Assert.NotNull(results);
        Assert.Single(results);
        _mockVectorStore.Verify(
            x => x.SearchAsync(queryEmbedding, topK, It.IsAny<CancellationToken>()),
            Times.Once);
        _mockVectorStore.Verify(
            x => x.HybridSearchAsync(It.IsAny<float[]>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HybridSearchAsync_EmptyQuery_ThrowsArgumentException()
    {
        // Arrange
        var query = "";
        var topK = 10;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.HybridSearchAsync(query, topK));
    }
}
