using System.ComponentModel.DataAnnotations;

namespace StackOverflowRAG.Core.Models;

/// <summary>
/// Request model for querying the RAG system
/// </summary>
public class QueryRequest
{
    /// <summary>
    /// The user's question
    /// </summary>
    [Required(ErrorMessage = "Question is required")]
    [MinLength(3, ErrorMessage = "Question must be at least 3 characters")]
    public string Question { get; set; } = string.Empty;

    /// <summary>
    /// Number of chunks to retrieve (default: 5)
    /// </summary>
    [Range(1, 20, ErrorMessage = "TopK must be between 1 and 20")]
    public int TopK { get; set; } = 5;

    /// <summary>
    /// Whether to use hybrid search (default: true)
    /// </summary>
    public bool UseHybrid { get; set; } = true;
}
