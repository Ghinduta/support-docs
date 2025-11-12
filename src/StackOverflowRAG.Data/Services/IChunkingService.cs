using StackOverflowRAG.Data.Models;

namespace StackOverflowRAG.Data.Services;

/// <summary>
/// Service for splitting documents into chunks for embedding and retrieval.
/// </summary>
public interface IChunkingService
{
    /// <summary>
    /// Splits a Stack Overflow document into chunks with configurable size and overlap.
    /// </summary>
    /// <param name="document">The document to chunk</param>
    /// <param name="chunkSize">Target chunk size in tokens</param>
    /// <param name="chunkOverlap">Overlap size in tokens between consecutive chunks</param>
    /// <returns>List of document chunks</returns>
    List<DocumentChunk> ChunkDocument(StackOverflowDocument document, int chunkSize, int chunkOverlap);

    /// <summary>
    /// Chunks multiple documents.
    /// </summary>
    /// <param name="documents">Documents to chunk</param>
    /// <param name="chunkSize">Target chunk size in tokens</param>
    /// <param name="chunkOverlap">Overlap size in tokens between consecutive chunks</param>
    /// <returns>List of all document chunks</returns>
    List<DocumentChunk> ChunkDocuments(List<StackOverflowDocument> documents, int chunkSize, int chunkOverlap);
}
