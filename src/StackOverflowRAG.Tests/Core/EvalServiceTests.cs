using StackOverflowRAG.Core.Services;
using StackOverflowRAG.Data.Models;

namespace StackOverflowRAG.Tests.Core;

public class EvalServiceTests
{
    [Fact]
    public void JoinChunks_DropsOverlapRepeatedFromPreviousChunk()
    {
        var chunks = new[]
        {
            new DocumentChunk { ChunkIndex = 0, ChunkText = "Title: T Question: How do I parse dates in C#? I tried many things." },
            new DocumentChunk { ChunkIndex = 1, ChunkText = "I tried many things. Answer: Use DateTime.ParseExact." }
        };

        var text = EvalService.JoinChunks(chunks);

        Assert.Equal("Title: T Question: How do I parse dates in C#? I tried many things. Answer: Use DateTime.ParseExact.", text);
    }

    [Fact]
    public void JoinChunks_WithoutOverlap_SeparatesWithSpace()
    {
        var chunks = new[]
        {
            new DocumentChunk { ChunkIndex = 0, ChunkText = "First part." },
            new DocumentChunk { ChunkIndex = 1, ChunkText = "Second part." }
        };

        Assert.Equal("First part. Second part.", EvalService.JoinChunks(chunks));
    }

    [Fact]
    public void SplitQuestionAndAnswer_SplitsOnAnswerMarker()
    {
        var (question, answer) = EvalService.SplitQuestionAndAnswer("Title: T Question: Q? Answer: Use X.");

        Assert.Equal("Title: T Question: Q?", question);
        Assert.Equal("Use X.", answer);
    }

    [Fact]
    public void SplitQuestionAndAnswer_WithoutAnswer_ReturnsEmptyAnswer()
    {
        var (question, answer) = EvalService.SplitQuestionAndAnswer("Title: T Question: Q?");

        Assert.Equal("Title: T Question: Q?", question);
        Assert.Equal(string.Empty, answer);
    }
}
