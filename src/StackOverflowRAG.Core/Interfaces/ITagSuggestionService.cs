using StackOverflowRAG.Core.Models;

namespace StackOverflowRAG.Core.Interfaces;

/// <summary>
/// Service for suggesting Stack Overflow tags using ML.NET
/// </summary>
public interface ITagSuggestionService
{
    /// <summary>
    /// Suggest tags for a Stack Overflow question
    /// </summary>
    /// <param name="title">Question title</param>
    /// <param name="body">Question body</param>
    /// <param name="topK">Number of tags to return</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Tag suggestions with confidence scores</returns>
    Task<TagSuggestionResponse> SuggestTagsAsync(string title, string body, int topK = 5, CancellationToken cancellationToken = default);

    /// <summary>
    /// Initialize the tag suggestion service (load model, etc.)
    /// </summary>
    Task InitializeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Train a new tag classifier model from Stack Overflow data
    /// </summary>
    /// <param name="request">Training parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Training results</returns>
    Task<TagTrainingResponse> TrainModelAsync(TagTrainingRequest request, CancellationToken cancellationToken = default);
}
