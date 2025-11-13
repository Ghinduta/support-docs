using StackOverflowRAG.Core.Helpers;
using StackOverflowRAG.Data.Models;

namespace StackOverflowRAG.Tests.Core;

public class CitationHelperTests
{
    [Fact]
    public void ExtractCitations_WithValidChunks_ReturnsTopCitations()
    {
        // Arrange
        var chunks = new List<DocumentChunk>
        {
            new() { PostId = 123, QuestionTitle = "How to use async?", Score = 0.95f, ChunkIndex = 0 },
            new() { PostId = 123, QuestionTitle = "How to use async?", Score = 0.90f, ChunkIndex = 1 },
            new() { PostId = 456, QuestionTitle = "What is LINQ?", Score = 0.85f, ChunkIndex = 0 },
            new() { PostId = 789, QuestionTitle = "Entity Framework tips", Score = 0.80f, ChunkIndex = 0 },
        };

        // Act
        var citations = CitationHelper.ExtractCitations(chunks, maxCitations: 3);

        // Assert
        Assert.Equal(3, citations.Count);
        Assert.Equal(123, citations[0].PostId); // Highest score
        Assert.Equal("How to use async?", citations[0].Title);
        Assert.Equal(0.95f, citations[0].RelevanceScore); // Max score for PostId 123
        Assert.Equal(456, citations[1].PostId);
        Assert.Equal(789, citations[2].PostId);
    }

    [Fact]
    public void ExtractCitations_RemovesDuplicatePosts_TakesHighestScore()
    {
        // Arrange
        var chunks = new List<DocumentChunk>
        {
            new() { PostId = 123, QuestionTitle = "Test", Score = 0.80f, ChunkIndex = 0 },
            new() { PostId = 123, QuestionTitle = "Test", Score = 0.95f, ChunkIndex = 1 },
            new() { PostId = 123, QuestionTitle = "Test", Score = 0.70f, ChunkIndex = 2 },
        };

        // Act
        var citations = CitationHelper.ExtractCitations(chunks);

        // Assert
        Assert.Single(citations);
        Assert.Equal(123, citations[0].PostId);
        Assert.Equal(0.95f, citations[0].RelevanceScore); // Should take max score
    }

    [Fact]
    public void ExtractCitations_SortsByRelevanceScore_Descending()
    {
        // Arrange
        var chunks = new List<DocumentChunk>
        {
            new() { PostId = 111, QuestionTitle = "Low", Score = 0.60f, ChunkIndex = 0 },
            new() { PostId = 222, QuestionTitle = "High", Score = 0.95f, ChunkIndex = 0 },
            new() { PostId = 333, QuestionTitle = "Medium", Score = 0.75f, ChunkIndex = 0 },
        };

        // Act
        var citations = CitationHelper.ExtractCitations(chunks);

        // Assert
        Assert.Equal(3, citations.Count);
        Assert.Equal(222, citations[0].PostId); // Highest score first
        Assert.Equal(333, citations[1].PostId);
        Assert.Equal(111, citations[2].PostId); // Lowest score last
    }

    [Fact]
    public void ExtractCitations_WithEmptyChunks_ReturnsEmptyList()
    {
        // Arrange
        var chunks = new List<DocumentChunk>();

        // Act
        var citations = CitationHelper.ExtractCitations(chunks);

        // Assert
        Assert.Empty(citations);
    }

    [Fact]
    public void ExtractCitations_WithNullChunks_ReturnsEmptyList()
    {
        // Act
        var citations = CitationHelper.ExtractCitations(null!);

        // Assert
        Assert.Empty(citations);
    }

    [Fact]
    public void ExtractCitations_RespectsMaxCitations_Limit()
    {
        // Arrange
        var chunks = new List<DocumentChunk>
        {
            new() { PostId = 111, QuestionTitle = "Q1", Score = 0.90f, ChunkIndex = 0 },
            new() { PostId = 222, QuestionTitle = "Q2", Score = 0.85f, ChunkIndex = 0 },
            new() { PostId = 333, QuestionTitle = "Q3", Score = 0.80f, ChunkIndex = 0 },
            new() { PostId = 444, QuestionTitle = "Q4", Score = 0.75f, ChunkIndex = 0 },
            new() { PostId = 555, QuestionTitle = "Q5", Score = 0.70f, ChunkIndex = 0 },
            new() { PostId = 666, QuestionTitle = "Q6", Score = 0.65f, ChunkIndex = 0 },
        };

        // Act
        var citations = CitationHelper.ExtractCitations(chunks, maxCitations: 3);

        // Assert
        Assert.Equal(3, citations.Count);
        Assert.Equal(111, citations[0].PostId); // Top 3 only
        Assert.Equal(222, citations[1].PostId);
        Assert.Equal(333, citations[2].PostId);
    }

    [Fact]
    public void GenerateStackOverflowUrl_CreatesCorrectUrl()
    {
        // Arrange
        int postId = 123456;

        // Act
        var url = CitationHelper.GenerateStackOverflowUrl(postId);

        // Assert
        Assert.Equal("https://stackoverflow.com/questions/123456", url);
    }

    [Fact]
    public void ExtractCitations_VerifiesUrlsAreGenerated()
    {
        // Arrange
        var chunks = new List<DocumentChunk>
        {
            new() { PostId = 123, QuestionTitle = "Test", Score = 0.90f, ChunkIndex = 0 },
        };

        // Act
        var citations = CitationHelper.ExtractCitations(chunks);

        // Assert
        Assert.Single(citations);
        Assert.Equal("https://stackoverflow.com/questions/123", citations[0].Url);
    }
}
