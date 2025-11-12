using Microsoft.Extensions.Logging;
using Moq;
using StackOverflowRAG.Data.Models;
using StackOverflowRAG.Data.Services;
using StackOverflowRAG.Data.Utilities;

namespace StackOverflowRAG.Tests.Data;

public class ChunkingServiceTests
{
    private readonly Mock<ILogger<ChunkingService>> _mockLogger;
    private readonly ChunkingService _service;

    public ChunkingServiceTests()
    {
        _mockLogger = new Mock<ILogger<ChunkingService>>();
        _service = new ChunkingService(_mockLogger.Object);
    }

    [Fact]
    public void ChunkDocument_WithShortDocument_ReturnsSingleChunk()
    {
        // Arrange
        var document = new StackOverflowDocument
        {
            PostId = 123,
            QuestionTitle = "Short Question",
            QuestionBody = "This is a short question body.",
            Tags = new[] { "test" }
        };

        // Act
        var chunks = _service.ChunkDocument(document, chunkSize: 500, chunkOverlap: 50);

        // Assert
        Assert.Single(chunks);
        Assert.Equal(123, chunks[0].PostId);
        Assert.Equal("Short Question", chunks[0].QuestionTitle);
        Assert.Equal(0, chunks[0].ChunkIndex);
        Assert.Contains("This is a short question body", chunks[0].ChunkText);
    }

    [Fact]
    public void ChunkDocument_WithLongDocument_ReturnsMultipleChunks()
    {
        // Arrange - Create a document with ~1500 tokens (6000 chars)
        var longBody = string.Join(" ", Enumerable.Repeat("This is a sentence with some content.", 150));
        var document = new StackOverflowDocument
        {
            PostId = 456,
            QuestionTitle = "Long Question",
            QuestionBody = longBody,
            Tags = new[] { "test" }
        };

        // Act
        var chunks = _service.ChunkDocument(document, chunkSize: 500, chunkOverlap: 50);

        // Assert
        Assert.True(chunks.Count > 1, $"Expected multiple chunks but got {chunks.Count}");

        // Verify chunk metadata
        for (int i = 0; i < chunks.Count; i++)
        {
            Assert.Equal(456, chunks[i].PostId);
            Assert.Equal("Long Question", chunks[i].QuestionTitle);
            Assert.Equal(i, chunks[i].ChunkIndex);
            Assert.Equal($"456_{i}", chunks[i].ChunkId);
        }
    }

    [Fact]
    public void ChunkDocument_ChunkSizeIsReasonable()
    {
        // Arrange
        var longBody = string.Join(" ", Enumerable.Repeat("This is a test sentence.", 200));
        var document = new StackOverflowDocument
        {
            PostId = 789,
            QuestionTitle = "Test",
            QuestionBody = longBody,
            Tags = new[] { "test" }
        };

        // Act
        var chunks = _service.ChunkDocument(document, chunkSize: 500, chunkOverlap: 50);

        // Assert
        foreach (var chunk in chunks)
        {
            var tokenCount = TokenCounter.EstimateTokenCount(chunk.ChunkText);
            // Allow some flexibility: chunks should be roughly 400-600 tokens
            Assert.InRange(tokenCount, 200, 700);
        }
    }

    [Fact]
    public void ChunkDocument_WithInvalidDocument_ReturnsEmpty()
    {
        // Arrange
        var invalidDoc = new StackOverflowDocument
        {
            PostId = 0,
            QuestionTitle = "",
            QuestionBody = "",
            Tags = Array.Empty<string>()
        };

        // Act
        var chunks = _service.ChunkDocument(invalidDoc, chunkSize: 500, chunkOverlap: 50);

        // Assert
        Assert.Empty(chunks);
    }

    [Fact]
    public void ChunkDocument_GeneratesCorrectChunkIds()
    {
        // Arrange
        var document = new StackOverflowDocument
        {
            PostId = 999,
            QuestionTitle = "Test",
            QuestionBody = string.Join(" ", Enumerable.Repeat("Sentence.", 200)),
            Tags = new[] { "test" }
        };

        // Act
        var chunks = _service.ChunkDocument(document, chunkSize: 500, chunkOverlap: 50);

        // Assert
        for (int i = 0; i < chunks.Count; i++)
        {
            Assert.Equal($"999_{i}", chunks[i].ChunkId);
        }
    }

    [Fact]
    public void ChunkDocuments_ProcessesMultipleDocuments()
    {
        // Arrange
        var documents = new List<StackOverflowDocument>
        {
            new StackOverflowDocument
            {
                PostId = 1,
                QuestionTitle = "Q1",
                QuestionBody = "Body 1",
                Tags = new[] { "tag1" }
            },
            new StackOverflowDocument
            {
                PostId = 2,
                QuestionTitle = "Q2",
                QuestionBody = "Body 2",
                Tags = new[] { "tag2" }
            }
        };

        // Act
        var chunks = _service.ChunkDocuments(documents, chunkSize: 500, chunkOverlap: 50);

        // Assert
        Assert.NotEmpty(chunks);
        Assert.Contains(chunks, c => c.PostId == 1);
        Assert.Contains(chunks, c => c.PostId == 2);
    }

    [Fact]
    public void TokenCounter_EstimatesTokenCount()
    {
        // Arrange
        var text = "This is a test."; // ~15 chars = ~4 tokens

        // Act
        var count = TokenCounter.EstimateTokenCount(text);

        // Assert
        Assert.InRange(count, 3, 5);
    }

    [Fact]
    public void TokenCounter_SplitsIntoSentences()
    {
        // Arrange
        var text = "First sentence. Second sentence! Third sentence?";

        // Act
        var sentences = TokenCounter.SplitIntoSentences(text);

        // Assert
        Assert.Equal(3, sentences.Length);
        Assert.Contains("First sentence.", sentences[0]);
        Assert.Contains("Second sentence!", sentences[1]);
        Assert.Contains("Third sentence?", sentences[2]);
    }
}
