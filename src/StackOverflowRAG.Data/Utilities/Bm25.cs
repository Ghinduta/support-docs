using System.Text.RegularExpressions;

namespace StackOverflowRAG.Data.Utilities;

/// <summary>
/// Turns text into BM25 sparse vectors for Qdrant. The term-frequency half of BM25 is computed here;
/// Qdrant applies the IDF half (rare words weigh more) itself via the sparse vector's IDF modifier.
/// </summary>
public static partial class Bm25
{
    // Standard BM25 parameters: k1 caps how much repeating a word helps, b how much long chunks are penalised.
    private const float K1 = 1.2f;
    private const float B = 0.75f;

    // Typical chunk length in words (chunks are ~500 tokens).
    private const float AverageChunkWords = 250f;

    private static readonly HashSet<string> StopWords = new(StringComparer.Ordinal)
    {
        "a", "an", "and", "are", "as", "at", "be", "been", "but", "by", "can", "could", "did", "do", "does",
        "for", "from", "had", "has", "have", "how", "i", "if", "in", "into", "is", "it", "its", "just", "me",
        "my", "no", "not", "of", "on", "or", "our", "should", "so", "some", "such", "than", "that", "the",
        "their", "them", "then", "there", "these", "they", "this", "to", "too", "was", "we", "were", "what",
        "when", "where", "which", "while", "who", "why", "will", "with", "would", "you", "your"
    };

    /// <summary>
    /// Sparse vector for a stored chunk: each word weighted by BM25 term frequency.
    /// </summary>
    public static (uint[] Indices, float[] Values) EncodeDocument(string text)
    {
        var words = Tokenize(text);
        var lengthNorm = 1 - B + B * (words.Count / AverageChunkWords);

        var entries = words
            .GroupBy(IndexFor)
            .Select(g => (Index: g.Key, Value: g.Count() * (K1 + 1) / (g.Count() + K1 * lengthNorm)))
            .ToArray();

        return (entries.Select(e => e.Index).ToArray(), entries.Select(e => e.Value).ToArray());
    }

    /// <summary>
    /// Sparse vector for a search query: each distinct word counts once.
    /// </summary>
    public static (uint[] Indices, float[] Values) EncodeQuery(string text)
    {
        var indices = Tokenize(text).Select(IndexFor).Distinct().ToArray();
        return (indices, Enumerable.Repeat(1f, indices.Length).ToArray());
    }

    /// <summary>
    /// Lowercases and splits into words, keeping programming terms like "c#", "c++" and "asp.net" whole.
    /// </summary>
    public static List<string> Tokenize(string text)
    {
        return WordPattern().Matches(text.ToLowerInvariant())
            .Select(m => m.Value)
            .Where(w => !StopWords.Contains(w))
            .ToList();
    }

    /// <summary>
    /// Stable word → dimension mapping (FNV-1a hash), so documents and queries agree without a shared vocabulary.
    /// </summary>
    private static uint IndexFor(string word)
    {
        var hash = 2166136261u;
        foreach (var c in word)
        {
            hash = (hash ^ c) * 16777619u;
        }
        return hash;
    }

    // A word starts with a letter or digit; inner '.', '#', '+', '_' are kept, trailing '.' is not.
    [GeneratedRegex(@"[a-z0-9](?:[a-z0-9_#+.]*[a-z0-9_#+])?")]
    private static partial Regex WordPattern();
}
