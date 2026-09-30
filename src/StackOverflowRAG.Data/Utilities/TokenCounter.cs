using System.Text.RegularExpressions;
using Microsoft.ML.Tokenizers;

namespace StackOverflowRAG.Data.Utilities;

/// <summary>
/// Counts tokens exactly the way OpenAI does, using the tiktoken encodings of the models this app calls.
/// </summary>
public static class TokenCounter
{
    // text-embedding-3-small uses cl100k_base; gpt-4o-mini uses o200k_base.
    private static readonly Tokenizer EmbeddingTokenizer = TiktokenTokenizer.CreateForEncoding("cl100k_base");
    private static readonly Tokenizer ChatTokenizer = TiktokenTokenizer.CreateForEncoding("o200k_base");

    /// <summary>
    /// Counts tokens as the embedding model sees them. Used to size chunks.
    /// </summary>
    public static int CountTokens(string text)
    {
        return string.IsNullOrEmpty(text) ? 0 : EmbeddingTokenizer.CountTokens(text);
    }

    /// <summary>
    /// Counts tokens as the chat model sees them. Used for cost estimates.
    /// </summary>
    public static int CountChatTokens(string text)
    {
        return string.IsNullOrEmpty(text) ? 0 : ChatTokenizer.CountTokens(text);
    }

    /// <summary>
    /// Returns the last <paramref name="tokenCount"/> embedding-model tokens of the text.
    /// </summary>
    public static string TakeLastTokens(string text, int tokenCount)
    {
        if (tokenCount <= 0 || string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        var startIndex = EmbeddingTokenizer.GetIndexByTokenCountFromEnd(text, tokenCount, out _, out _);
        return text[startIndex..];
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
