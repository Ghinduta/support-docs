using System.Text.RegularExpressions;

namespace StackOverflowRAG.Data.Utilities;

/// <summary>
/// Utility for estimating token counts in text.
/// Uses approximation: ~4 characters per token (OpenAI standard).
/// </summary>
public static class TokenCounter
{
    /// <summary>
    /// Average characters per token for OpenAI models
    /// </summary>
    private const double CharsPerToken = 4.0;

    /// <summary>
    /// Estimates the number of tokens in a text string.
    /// Uses approximation: tokenCount ≈ text.Length / 4
    /// </summary>
    /// <param name="text">Text to count tokens in</param>
    /// <returns>Estimated token count</returns>
    public static int EstimateTokenCount(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return 0;
        }

        // Simple approximation: 1 token ≈ 4 characters
        return (int)Math.Ceiling(text.Length / CharsPerToken);
    }

    /// <summary>
    /// Splits text into sentences using common sentence delimiters.
    /// </summary>
    /// <param name="text">Text to split</param>
    /// <returns>Array of sentences</returns>
    public static string[] SplitIntoSentences(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return Array.Empty<string>();
        }

        // Split on sentence boundaries: . ! ? followed by space or newline
        // Keep the punctuation with the sentence
        var sentences = Regex.Split(text, @"(?<=[.!?])\s+")
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .ToArray();

        return sentences;
    }
}
