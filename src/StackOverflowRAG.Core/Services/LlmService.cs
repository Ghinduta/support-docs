using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using StackOverflowRAG.Core.Configuration;
using StackOverflowRAG.Core.Interfaces;
using StackOverflowRAG.Core.Models;
using StackOverflowRAG.Data.Models;
using StackOverflowRAG.Data.Utilities;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;

namespace StackOverflowRAG.Core.Services;

/// <summary>
/// Service for generating LLM responses using Semantic Kernel
/// </summary>
public class LlmService : ILlmService
{
    private readonly Kernel _kernel;
    private readonly LlmOptions _options;
    private readonly ILogger<LlmService> _logger;

    public LlmService(
        Kernel kernel,
        IOptions<LlmOptions> options,
        ILogger<LlmService> logger)
    {
        _kernel = kernel ?? throw new ArgumentNullException(nameof(kernel));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<QueryExpansion> ExpandQueryAsync(string question, int count = 3, CancellationToken cancellationToken = default)
    {
        var system =
            $"You help a search engine over Stack Overflow posts. Rewrite the user's question into {count} different " +
            "search queries that could find the answer: use the technical terms, library or API names an expert would " +
            "use, and vary the wording between queries. Keep each under 15 words. " +
            "Reply with JSON only: {\"queries\": [\"...\"]}";

        var history = new ChatHistory();
        history.AddSystemMessage(system);
        history.AddUserMessage(question);

        var chatService = _kernel.GetRequiredService<IChatCompletionService>();
        var reply = await chatService.GetChatMessageContentAsync(
            history,
            new OpenAIPromptExecutionSettings { Temperature = 0, ResponseFormat = "json_object" },
            _kernel,
            cancellationToken);

        var content = reply.Content ?? "{}";
        using var json = System.Text.Json.JsonDocument.Parse(content);
        var queries = json.RootElement.TryGetProperty("queries", out var list)
            ? list.EnumerateArray()
                .Select(q => q.GetString()?.Trim())
                .Where(q => !string.IsNullOrEmpty(q))
                .Select(q => q!)
                .Take(count)
                .ToList()
            : new List<string>();

        _logger.LogInformation("Expanded question into {Count} queries: {Queries}", queries.Count, string.Join(" | ", queries));

        return new QueryExpansion
        {
            Queries = queries,
            PromptTokens = TokenCounter.CountChatTokens(system + question),
            CompletionTokens = TokenCounter.CountChatTokens(content)
        };
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<string> StreamAnswerAsync(
        string question,
        List<DocumentChunk> retrievedChunks,
        double? temperature = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(question))
        {
            throw new ArgumentException("Question cannot be null or empty", nameof(question));
        }

        if (retrievedChunks == null)
        {
            throw new ArgumentNullException(nameof(retrievedChunks));
        }

        var stopwatch = Stopwatch.StartNew();

        _logger.LogInformation(
            "Starting LLM streaming: Question length={QuestionLength}, Context chunks={ChunkCount}",
            question.Length,
            retrievedChunks.Count);

        // Build the prompt with context
        var prompt = BuildPrompt(question, retrievedChunks);

        _logger.LogDebug("Prompt built: {PromptLength} characters", prompt.Length);

        // Get chat completion service
        IChatCompletionService chatService;
        try
        {
            chatService = _kernel.GetRequiredService<IChatCompletionService>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get chat completion service");
            throw;
        }

        // Create chat history
        var chatHistory = new ChatHistory();
        chatHistory.AddSystemMessage(_options.SystemPrompt);
        chatHistory.AddUserMessage(prompt);

        _logger.LogDebug("Sending request to {ModelName}", _options.ModelName);

        // Stream the response
        var responseBuilder = new StringBuilder();
        var firstTokenReceived = false;
        var firstTokenLatency = 0L;

        var executionSettings = new OpenAIPromptExecutionSettings
        {
            Temperature = temperature ?? _options.Temperature,
            MaxTokens = _options.MaxTokens
        };

        IAsyncEnumerable<StreamingChatMessageContent> streamingResponse;
        try
        {
            streamingResponse = chatService.GetStreamingChatMessageContentsAsync(
                chatHistory,
                executionSettings: executionSettings,
                kernel: _kernel,
                cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error initiating LLM streaming for question: '{Question}' (trimmed)",
                question.Substring(0, Math.Min(100, question.Length)));
            throw;
        }

        await foreach (var chunk in streamingResponse.ConfigureAwait(false))
        {
            if (!firstTokenReceived)
            {
                firstTokenLatency = stopwatch.ElapsedMilliseconds;
                firstTokenReceived = true;
                _logger.LogDebug("First token received after {Latency}ms", firstTokenLatency);
            }

            var content = chunk.Content;
            if (!string.IsNullOrEmpty(content))
            {
                responseBuilder.Append(content);
                yield return content;
            }
        }

        stopwatch.Stop();

        var totalTokens = TokenCounter.CountChatTokens(prompt) + TokenCounter.CountChatTokens(responseBuilder.ToString());

        _logger.LogInformation(
            "LLM streaming completed: Duration={Duration}ms, FirstToken={FirstToken}ms, Tokens={Tokens}, ResponseLength={ResponseLength}",
            stopwatch.ElapsedMilliseconds,
            firstTokenLatency,
            totalTokens,
            responseBuilder.Length);
    }

    /// <summary>
    /// Builds the prompt with retrieved context chunks
    /// </summary>
    private string BuildPrompt(string question, List<DocumentChunk> chunks)
    {
        var promptBuilder = new StringBuilder();

        // Add context section
        promptBuilder.AppendLine("Context from Stack Overflow:");
        promptBuilder.AppendLine();

        for (int i = 0; i < chunks.Count; i++)
        {
            var chunk = chunks[i];
            promptBuilder.AppendLine($"[{i + 1}] Question: {chunk.QuestionTitle} (Post ID: {chunk.PostId})");
            promptBuilder.AppendLine($"Content: {chunk.ChunkText}");
            promptBuilder.AppendLine($"Relevance Score: {chunk.Score:F3}");
            promptBuilder.AppendLine();
        }

        // Add user question
        promptBuilder.AppendLine("---");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine($"User Question: {question}");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("Please provide a helpful answer based on the context above. Cite sources using [Title](Post ID) format.");

        return promptBuilder.ToString();
    }
}
