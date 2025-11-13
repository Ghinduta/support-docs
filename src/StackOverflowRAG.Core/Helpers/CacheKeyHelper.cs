using System.Security.Cryptography;
using System.Text;

namespace StackOverflowRAG.Core.Helpers;

/// <summary>
/// Helper methods for generating cache keys
/// </summary>
public static class CacheKeyHelper
{
    /// <summary>
    /// Generates a cache key for query embeddings
    /// </summary>
    /// <param name="query">The query text</param>
    /// <returns>Cache key in format: emb:{hash}</returns>
    public static string GenerateEmbeddingKey(string query)
    {
        var hash = ComputeMD5Hash(query);
        return $"emb:{hash}";
    }

    /// <summary>
    /// Generates a cache key for LLM responses
    /// </summary>
    /// <param name="query">The query text</param>
    /// <param name="topK">Number of chunks retrieved</param>
    /// <param name="useHybrid">Whether hybrid search was used</param>
    /// <returns>Cache key in format: resp:{hash}</returns>
    public static string GenerateResponseKey(string query, int topK, bool useHybrid)
    {
        var input = $"{query}|{topK}|{useHybrid}";
        var hash = ComputeMD5Hash(input);
        return $"resp:{hash}";
    }

    /// <summary>
    /// Computes MD5 hash of input string
    /// </summary>
    /// <param name="input">Input string</param>
    /// <returns>Hexadecimal hash string</returns>
    private static string ComputeMD5Hash(string input)
    {
        var inputBytes = Encoding.UTF8.GetBytes(input);
        var hashBytes = MD5.HashData(inputBytes);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
}
