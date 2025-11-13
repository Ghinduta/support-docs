using StackOverflowRAG.Core.Models;
using StackOverflowRAG.Data.Models;

namespace StackOverflowRAG.Core.Helpers;

/// <summary>
/// Helper methods for extracting citations from retrieved chunks
/// </summary>
public static class CitationHelper
{
    /// <summary>
    /// Extracts citations from retrieved chunks, returning top 3-5 unique sources
    /// </summary>
    /// <param name="chunks">Retrieved document chunks</param>
    /// <param name="maxCitations">Maximum number of citations to return (default 5)</param>
    /// <returns>List of citations sorted by relevance score</returns>
    public static List<Citation> ExtractCitations(List<DocumentChunk> chunks, int maxCitations = 5)
    {
        if (chunks == null || chunks.Count == 0)
        {
            return new List<Citation>();
        }

        // Group chunks by PostId to get unique posts, taking the highest score per post
        var uniquePosts = chunks
            .GroupBy(c => c.PostId)
            .Select(g => new
            {
                PostId = g.Key,
                Title = g.First().QuestionTitle,
                MaxScore = g.Max(c => c.Score)
            })
            .OrderByDescending(p => p.MaxScore)
            .Take(Math.Min(maxCitations, chunks.Count))
            .ToList();

        // Map to Citation objects with Stack Overflow URLs
        return uniquePosts.Select(p => new Citation
        {
            PostId = p.PostId,
            Title = p.Title,
            Url = GenerateStackOverflowUrl(p.PostId),
            RelevanceScore = p.MaxScore
        }).ToList();
    }

    /// <summary>
    /// Generates Stack Overflow URL from post ID
    /// </summary>
    /// <param name="postId">Stack Overflow post ID</param>
    /// <returns>Full Stack Overflow URL</returns>
    public static string GenerateStackOverflowUrl(int postId)
    {
        return $"https://stackoverflow.com/questions/{postId}";
    }
}
