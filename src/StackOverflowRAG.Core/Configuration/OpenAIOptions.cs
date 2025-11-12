using System.ComponentModel.DataAnnotations;

namespace StackOverflowRAG.Core.Configuration;

/// <summary>
/// Configuration options for OpenAI API.
/// </summary>
public class OpenAIOptions
{
    public const string SectionName = "OpenAI";

    /// <summary>
    /// OpenAI API key
    /// </summary>
    [Required]
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Embedding model name (e.g., text-embedding-3-small)
    /// </summary>
    [Required]
    public string EmbeddingModel { get; set; } = "text-embedding-3-small";

    /// <summary>
    /// Chat completion model name (e.g., gpt-4o-mini)
    /// </summary>
    public string ChatModel { get; set; } = "gpt-4o-mini";

    /// <summary>
    /// Validates configuration values
    /// </summary>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(ApiKey))
        {
            throw new InvalidOperationException("OpenAI ApiKey must be configured");
        }

        if (string.IsNullOrWhiteSpace(EmbeddingModel))
        {
            throw new InvalidOperationException("OpenAI EmbeddingModel must be configured");
        }
    }
}
