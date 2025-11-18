using System.ComponentModel.DataAnnotations;

namespace StackOverflowRAG.Core.Models;

/// <summary>
/// Request model for tag suggestion
/// </summary>
public class TagSuggestionRequest
{
    /// <summary>
    /// Question title
    /// </summary>
    [Required(ErrorMessage = "Title is required")]
    [MinLength(3, ErrorMessage = "Title must be at least 3 characters")]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Question body/content
    /// </summary>
    public string Body { get; set; } = string.Empty;

    /// <summary>
    /// Number of tags to return (default: 5, max: 10)
    /// </summary>
    [Range(1, 10, ErrorMessage = "TopK must be between 1 and 10")]
    public int TopK { get; set; } = 5;
}
