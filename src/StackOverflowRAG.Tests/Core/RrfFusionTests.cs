using StackOverflowRAG.Core.Services;
using StackOverflowRAG.Data.Models;

namespace StackOverflowRAG.Tests.Core;

public class RrfFusionTests
{
    private static List<DocumentChunk> Ranked(params string[] ids) =>
        ids.Select(id => new DocumentChunk { ChunkId = id }).ToList();

    [Fact]
    public void FuseWithRrf_ChunkRankedWellInBothListsWins()
    {
        // A is 1st and 2nd, C is 3rd and 1st, so A edges out C; B and D appear in one list each.
        var fused = RetrievalService.FuseWithRrf(new[] { Ranked("A", "B", "C"), Ranked("C", "A", "D") });

        Assert.Equal(new[] { "A", "C", "B", "D" }, fused.Select(c => c.ChunkId));
    }

    [Fact]
    public void FuseWithRrf_ReturnsEachChunkOnceWithFusedScore()
    {
        var fused = RetrievalService.FuseWithRrf(new[] { Ranked("A"), Ranked("A") });

        var chunk = Assert.Single(fused);
        Assert.Equal(2.0 / 61, chunk.Score, precision: 5);
    }
}
