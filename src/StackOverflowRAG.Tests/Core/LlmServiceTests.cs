using StackOverflowRAG.Core.Configuration;

namespace StackOverflowRAG.Tests.Core;

public class LlmServiceTests
{
    // Note: Testing actual LLM streaming requires real OpenAI API calls
    // These tests focus on configuration validation which can be tested in isolation

    [Fact]
    public void LlmOptions_Validate_ValidOptions_DoesNotThrow()
    {
        // Arrange
        var options = new LlmOptions
        {
            ModelName = "gpt-4o-mini",
            Temperature = 0.7,
            MaxTokens = 1000,
            SystemPrompt = "Test prompt"
        };

        // Act & Assert
        options.Validate(); // Should not throw
    }

    [Fact]
    public void LlmOptions_Validate_NullModelName_ThrowsInvalidOperationException()
    {
        // Arrange
        var options = new LlmOptions
        {
            ModelName = null!,
            Temperature = 0.7,
            MaxTokens = 1000,
            SystemPrompt = "Test prompt"
        };

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => options.Validate());
    }

    [Fact]
    public void LlmOptions_Validate_InvalidTemperature_ThrowsInvalidOperationException()
    {
        // Arrange
        var options = new LlmOptions
        {
            ModelName = "gpt-4o-mini",
            Temperature = 3.0, // Invalid: > 2.0
            MaxTokens = 1000,
            SystemPrompt = "Test prompt"
        };

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => options.Validate());
    }

    [Fact]
    public void LlmOptions_Validate_InvalidMaxTokens_ThrowsInvalidOperationException()
    {
        // Arrange
        var options = new LlmOptions
        {
            ModelName = "gpt-4o-mini",
            Temperature = 0.7,
            MaxTokens = 20000, // Invalid: > 16000
            SystemPrompt = "Test prompt"
        };

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => options.Validate());
    }

    [Fact]
    public void LlmOptions_Validate_NullSystemPrompt_ThrowsInvalidOperationException()
    {
        // Arrange
        var options = new LlmOptions
        {
            ModelName = "gpt-4o-mini",
            Temperature = 0.7,
            MaxTokens = 1000,
            SystemPrompt = null!
        };

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => options.Validate());
    }
}
