 using System.ComponentModel.DataAnnotations;

namespace StackOverflowRAG.Data.Models;

/// <summary>
/// Represents a parsed Stack Overflow document from CSV data.
/// Combines question and accepted answer into a single document for RAG.
/// </summary>
public class StackOverflowDocument
{
    /// <summary>
    /// Stack Overflow post ID (question ID)
    /// </summary>
    [Required]
    public int PostId { get; set; }

    /// <summary>
    /// Question title
    /// </summary>
    [Required]
    [MaxLength(500)]
    public string QuestionTitle { get; set; } = string.Empty;

    /// <summary>
    /// Question body content (HTML cleaned)
    /// </summary>
    [Required]
    public string QuestionBody { get; set; } = string.Empty;

    /// <summary>
    /// Accepted answer body content (HTML cleaned)
    /// Can be null if no accepted answer exists
    /// </summary>
    public string? AnswerBody { get; set; }

    /// <summary>
    /// Array of tags associated with the question
    /// </summary>
    [Required]
    [MinLength(1)]
    public string[] Tags { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Returns full text representation for chunking
    /// </summary>
    public string GetFullText()
    {
        var text = $"Title: {QuestionTitle}\n\nQuestion: {QuestionBody}";

        if (!string.IsNullOrEmpty(AnswerBody))
        {
            text += $"\n\nAnswer: {AnswerBody}";
        }

        return text;
    }

    /// <summary>
    /// Validates that document has minimum required content
    /// </summary>
    public bool IsValid()
    {
        return PostId > 0
            && !string.IsNullOrWhiteSpace(QuestionTitle)
            && !string.IsNullOrWhiteSpace(QuestionBody)
            && Tags.Length > 0;
    }
}
