namespace StackOverflowRAG.Data.Models;

/// <summary>
/// Represents a chunk of a Stack Overflow document for embedding and retrieval.
/// Documents are split into ~500 token chunks with overlap for better context.
/// </summary>
public class DocumentChunk
{
    /// <summary>
    /// Unique identifier for this chunk (PostId_ChunkIndex)
    /// </summary>
    public string ChunkId { get; set; } = string.Empty;

    /// <summary>
    /// Original Stack Overflow post ID
    /// </summary>
    public int PostId { get; set; }

    /// <summary>
    /// Question title (preserved for context)
    /// </summary>
    public string QuestionTitle { get; set; } = string.Empty;

    /// <summary>
    /// The text content of this chunk
    /// </summary>
    public string ChunkText { get; set; } = string.Empty;

    /// <summary>
    /// Zero-based index of this chunk within the document
    /// </summary>
    public int ChunkIndex { get; set; }

    /// <summary>
    /// Embedding vector for this chunk (populated during embedding generation)
    /// </summary>
    public float[]? Embedding { get; set; }

    /// <summary>
    /// Similarity score (populated during search, 0.0 to 1.0)
    /// </summary>
    public float Score { get; set; }

    /// <summary>
    /// Generates ChunkId from PostId and ChunkIndex
    /// </summary>
    public static string GenerateChunkId(int postId, int chunkIndex)
    {
        return $"{postId}_{chunkIndex}";
    }

    /// <summary>
    /// Sets the ChunkId based on current PostId and ChunkIndex
    /// </summary>
    public void SetChunkId()
    {
        ChunkId = GenerateChunkId(PostId, ChunkIndex);
    }
}
