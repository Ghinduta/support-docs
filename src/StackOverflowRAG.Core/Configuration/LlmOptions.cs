using System.ComponentModel.DataAnnotations;

namespace StackOverflowRAG.Core.Configuration;

/// <summary>
/// Configuration options for LLM service.
/// </summary>
public class LlmOptions
{
    public const string SectionName = "Llm";

    /// <summary>
    /// OpenAI model name (default: gpt-4o-mini)
    /// </summary>
    [Required]
    public string ModelName { get; set; } = "gpt-4o-mini";

    /// <summary>
    /// Temperature for response generation (0.0 to 2.0)
    /// </summary>
    [Range(0.0, 2.0)]
    public double Temperature { get; set; } = 0.7;

    /// <summary>
    /// Maximum tokens in response
    /// </summary>
    [Range(1, 16000)]
    public int MaxTokens { get; set; } = 1000;

    /// <summary>
    /// System prompt template for RAG
    /// </summary>
    [Required]
    public string SystemPrompt { get; set; } =
        "You are a helpful assistant that answers technical questions using provided Stack Overflow context. " +
        "Always cite your sources with [Question Title](Post ID) format. " +
        "Be concise and accurate. If the context doesn't contain relevant information, say so.";

    /// <summary>
    /// Validates configuration values
    /// </summary>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(ModelName))
        {
            throw new InvalidOperationException("ModelName cannot be null or empty");
        }

        if (Temperature < 0 || Temperature > 2)
        {
            throw new InvalidOperationException("Temperature must be between 0.0 and 2.0");
        }

        if (MaxTokens < 1 || MaxTokens > 16000)
        {
            throw new InvalidOperationException("MaxTokens must be between 1 and 16000");
        }

        if (string.IsNullOrWhiteSpace(SystemPrompt))
        {
            throw new InvalidOperationException("SystemPrompt cannot be null or empty");
        }
    }
}
