using Microsoft.Extensions.Logging;
using StackOverflowRAG.Data.Models;
using StackOverflowRAG.Data.Utilities;

namespace StackOverflowRAG.Data.Services;

/// <summary>
/// Service for splitting documents into fixed-size chunks with overlap.
/// Preserves sentence boundaries where possible.
/// </summary>
public class ChunkingService : IChunkingService
{
    private readonly ILogger<ChunkingService> _logger;

    public ChunkingService(ILogger<ChunkingService> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public List<DocumentChunk> ChunkDocument(StackOverflowDocument document, int chunkSize, int chunkOverlap)
    {
        if (document == null || !document.IsValid())
        {
            _logger.LogWarning("Invalid document provided for chunking: PostId={PostId}", document?.PostId ?? 0);
            return new List<DocumentChunk>();
        }

        if (chunkSize <= 0)
        {
            throw new ArgumentException("Chunk size must be greater than 0", nameof(chunkSize));
        }

        if (chunkOverlap < 0 || chunkOverlap >= chunkSize)
        {
            throw new ArgumentException("Chunk overlap must be >= 0 and < chunk size", nameof(chunkOverlap));
        }

        var fullText = document.GetFullText();
        var sentences = TokenCounter.SplitIntoSentences(fullText);

        if (sentences.Length == 0)
        {
            _logger.LogWarning("No sentences found in document PostId={PostId}", document.PostId);
            return new List<DocumentChunk>();
        }

        var chunks = new List<DocumentChunk>();
        var currentChunkSentences = new List<string>();
        var currentTokenCount = 0;
        var chunkIndex = 0;

        foreach (var sentence in sentences)
        {
            var sentenceTokens = TokenCounter.EstimateTokenCount(sentence);

            // If adding this sentence exceeds chunk size and we have content, create chunk
            if (currentTokenCount + sentenceTokens > chunkSize && currentChunkSentences.Count > 0)
            {
                // Create chunk from accumulated sentences
                var chunkText = string.Join(" ", currentChunkSentences);
                var chunk = CreateChunk(document, chunkText, chunkIndex);
                chunks.Add(chunk);
                chunkIndex++;

                // Start new chunk with overlap
                var overlapText = GetOverlapText(chunkText, chunkOverlap);
                if (!string.IsNullOrEmpty(overlapText))
                {
                    currentChunkSentences = new List<string> { overlapText };
                    currentTokenCount = TokenCounter.EstimateTokenCount(overlapText);
                }
                else
                {
                    currentChunkSentences.Clear();
                    currentTokenCount = 0;
                }
            }

            // Add sentence to current chunk
            currentChunkSentences.Add(sentence);
            currentTokenCount += sentenceTokens;
        }

        // Add final chunk if there's remaining content
        if (currentChunkSentences.Count > 0)
        {
            var chunkText = string.Join(" ", currentChunkSentences);
            var chunk = CreateChunk(document, chunkText, chunkIndex);
            chunks.Add(chunk);
        }

        _logger.LogInformation(
            "Chunked document PostId={PostId} into {ChunkCount} chunks (avg {AvgTokens} tokens/chunk)",
            document.PostId,
            chunks.Count,
            chunks.Count > 0 ? chunks.Average(c => TokenCounter.EstimateTokenCount(c.ChunkText)) : 0);

        return chunks;
    }

    /// <inheritdoc />
    public List<DocumentChunk> ChunkDocuments(List<StackOverflowDocument> documents, int chunkSize, int chunkOverlap)
    {
        if (documents == null || documents.Count == 0)
        {
            _logger.LogWarning("No documents provided for chunking");
            return new List<DocumentChunk>();
        }

        _logger.LogInformation("Starting chunking for {DocumentCount} documents", documents.Count);

        var allChunks = new List<DocumentChunk>();

        foreach (var document in documents)
        {
            var chunks = ChunkDocument(document, chunkSize, chunkOverlap);
            allChunks.AddRange(chunks);
        }

        _logger.LogInformation(
            "Chunking completed: {DocumentCount} documents → {ChunkCount} chunks (avg {AvgChunksPerDoc} chunks/doc)",
            documents.Count,
            allChunks.Count,
            allChunks.Count > 0 ? (double)allChunks.Count / documents.Count : 0);

        return allChunks;
    }

    /// <summary>
    /// Creates a DocumentChunk from text and metadata.
    /// </summary>
    private DocumentChunk CreateChunk(StackOverflowDocument document, string chunkText, int chunkIndex)
    {
        var chunk = new DocumentChunk
        {
            PostId = document.PostId,
            QuestionTitle = document.QuestionTitle,
            ChunkText = chunkText.Trim(),
            ChunkIndex = chunkIndex
        };

        chunk.SetChunkId();
        return chunk;
    }

    /// <summary>
    /// Extracts the last N tokens from text for overlap.
    /// </summary>
    private string GetOverlapText(string text, int overlapTokens)
    {
        if (overlapTokens <= 0 || string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        var estimatedChars = overlapTokens * 4; // ~4 chars per token
        if (text.Length <= estimatedChars)
        {
            return text;
        }

        // Take last N characters (approximate)
        var overlapText = text.Substring(text.Length - estimatedChars);

        // Try to start at a word boundary
        var firstSpace = overlapText.IndexOf(' ');
        if (firstSpace > 0 && firstSpace < overlapText.Length / 2)
        {
            overlapText = overlapText.Substring(firstSpace + 1);
        }

        return overlapText.Trim();
    }
}
