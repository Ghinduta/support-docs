namespace StackOverflowRAG.Core.Models;

/// <summary>
/// Represents a citation to a Stack Overflow post used as a source
/// </summary>
public class Citation
{
    /// <summary>
    /// Stack Overflow post ID
    /// </summary>
    public int PostId { get; set; }

    /// <summary>
    /// Question title
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Full Stack Overflow URL
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// Relevance score from vector search (0.0 to 1.0)
    /// </summary>
    public float RelevanceScore { get; set; }
}
