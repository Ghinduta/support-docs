using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using StackOverflowRAG.Core.Configuration;
using StackOverflowRAG.Core.Helpers;
using StackOverflowRAG.Core.Interfaces;
using StackOverflowRAG.Core.Models;
using StackOverflowRAG.Data.Models;
using StackOverflowRAG.Data.Repositories;
using StackOverflowRAG.Data.Utilities;

namespace StackOverflowRAG.Core.Services;

/// <summary>
/// Evaluates the RAG pipeline against a fixed test set: retrieval (Hit@K, MRR) and answers (LLM judge, 1-5).
/// Test set and runs are JSON files so runs stay comparable and can be versioned.
/// </summary>
public class EvalService : IEvalService
{
    // Real users ask in three ways, so the test set has one question style for each (in equal shares):
    // specific (own words), keyword (exact technical terms) and vague (doesn't know the right terms).
    private static readonly Dictionary<string, string> RewritePrompts = new()
    {
        ["specific"] =
            "Below is a Stack Overflow question. Write the question a developer would type into a chat assistant " +
            "about the same problem. Rules: 5 to 15 words; describe the symptom or goal rather than naming the exact " +
            "technology when you can; use synonyms instead of the post's distinctive words and never reuse the title's " +
            "wording; no code, no error messages copied verbatim. Reply with the question only.",
        ["keyword"] =
            "Below is a Stack Overflow question. Write the question a developer would type into a chat assistant " +
            "after copying the exact technical terms from their code or screen: 5 to 15 words that include the most " +
            "specific identifiers from the post, such as a class, method, function, library, command, config key or " +
            "error message, spelled exactly as in the post. Do not copy the title word for word. Reply with the question only.",
        ["vague"] =
            "Below is a Stack Overflow question. Write a short, vague question (5 to 10 words) that a developer who " +
            "barely knows the right terms might type about the same problem. Mention at most one technology, avoid the " +
            "post's distinctive words and the title's wording, no code. Reply with the question only."
    };

    private static readonly string[] Styles = ["specific", "keyword", "vague"];

    private const string JudgePrompt =
        "You are a strict, impartial evaluator of answers produced by a retrieval-augmented assistant. " +
        "First list every claim in the ANSWER that the CONTEXT does not support. Then score each criterion from 1 to 5 " +
        "using these anchors; do not give 5 unless the anchor for 5 is fully met.\n\n" +
        "faithfulness (ANSWER vs CONTEXT):\n" +
        "  5 = every claim is supported by the context; 4 = one minor unsupported detail; " +
        "3 = several unsupported details or one unsupported key claim; 2 = mostly unsupported; " +
        "1 = contradicts the context or ignores it.\n" +
        "relevance (ANSWER vs QUESTION):\n" +
        "  5 = directly solves what was asked, nothing off-topic; 4 = solves it with some padding; " +
        "3 = partially addresses it; 2 = mostly about something else; 1 = does not address the question.\n" +
        "correctness (ANSWER vs REFERENCE):\n" +
        "  5 = same solution as the reference or an equivalent one; 4 = right approach, a detail missing or off; " +
        "3 = partly right, or a different approach that only sometimes works; 2 = mostly wrong; " +
        "1 = wrong, or says it cannot answer when the reference does.\n\n" +
        "Reply with JSON only: {\"unsupported_claims\": [\"...\"], \"faithfulness\": n, \"relevance\": n, " +
        "\"correctness\": n, \"reason\": \"one sentence on the main weakness\"}";

    // Repeatable answers, so a score change comes from the setting being tested rather than sampling noise.
    private const double AnswerTemperature = 0;

    // Keeps judge prompts bounded; long accepted answers are mostly code and quotes.
    private const int MaxReferenceChars = 6000;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    private readonly IVectorStoreRepository _vectorStore;
    private readonly IRetrievalService _retrievalService;
    private readonly ILlmService _llmService;
    private readonly IChatCompletionService _chat;
    private readonly EvalOptions _options;
    private readonly ILogger<EvalService> _logger;

    public EvalService(
        IVectorStoreRepository vectorStore,
        IRetrievalService retrievalService,
        ILlmService llmService,
        Kernel kernel,
        IOptions<EvalOptions> options,
        ILogger<EvalService> logger)
    {
        _vectorStore = vectorStore;
        _retrievalService = retrievalService;
        _llmService = llmService;
        _chat = kernel.GetRequiredService<IChatCompletionService>();
        _options = options.Value;
        _logger = logger;
    }

    private string DatasetPath => Path.Combine(_options.Directory, "dataset.json");
    private string RunsDirectory => Path.Combine(_options.Directory, "runs");

    /// <inheritdoc />
    public async Task<EvalDataset> GetDatasetAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(DatasetPath))
        {
            return new EvalDataset();
        }

        var bytes = await File.ReadAllBytesAsync(DatasetPath, cancellationToken);
        return new EvalDataset
        {
            Id = DatasetIdFor(bytes),
            Cases = JsonSerializer.Deserialize<List<EvalCase>>(bytes, JsonOptions) ?? new()
        };
    }

    /// <inheritdoc />
    public async Task<EvalDataset> GenerateDatasetAsync(CancellationToken cancellationToken = default)
    {
        var firstChunks = await _vectorStore.GetFirstChunksAsync(_options.DatasetSize, cancellationToken);
        if (firstChunks.Count == 0)
        {
            throw new InvalidOperationException("No ingested data found. Run an ingestion first.");
        }

        _logger.LogInformation("Generating eval dataset from {Count} ingested posts", firstChunks.Count);

        // Stable order (by post) so "first N" means the same questions every time, with styles spread evenly.
        var posts = firstChunks
            .OrderBy(c => c.PostId)
            .Select((chunk, index) => (First: chunk, Style: Styles[index % Styles.Length]))
            .ToList();

        var cases = new ConcurrentBag<EvalCase>();
        await Parallel.ForEachAsync(
            posts,
            new ParallelOptions { MaxDegreeOfParallelism = _options.MaxParallelism, CancellationToken = cancellationToken },
            async (post, ct) =>
            {
                var (first, style) = post;
                var chunks = await _vectorStore.GetChunksForPostAsync(first.PostId, ct);
                var (questionText, referenceAnswer) = SplitQuestionAndAnswer(JoinChunks(chunks));

                // Without a reference answer, correctness can't be judged.
                if (string.IsNullOrWhiteSpace(referenceAnswer))
                {
                    return;
                }

                var prompt = RewritePrompts[style];
                var question = await CompleteAsync(prompt, questionText, 0.3, jsonOutput: false, ct);
                cases.Add(new EvalCase
                {
                    ExpectedPostId = first.PostId,
                    OriginalTitle = first.QuestionTitle,
                    Question = question.Trim(),
                    Style = style,
                    ReferenceAnswer = referenceAnswer.Length > MaxReferenceChars
                        ? referenceAnswer[..MaxReferenceChars]
                        : referenceAnswer
                });
            });

        var dataset = cases.OrderBy(c => c.ExpectedPostId).ToList();
        for (var i = 0; i < dataset.Count; i++)
        {
            dataset[i].Id = i + 1;
        }

        Directory.CreateDirectory(_options.Directory);
        var bytes = JsonSerializer.SerializeToUtf8Bytes(dataset, JsonOptions);
        await File.WriteAllBytesAsync(DatasetPath, bytes, cancellationToken);

        _logger.LogInformation("Saved eval dataset with {Count} cases to {Path}", dataset.Count, DatasetPath);
        return new EvalDataset { Id = DatasetIdFor(bytes), Cases = dataset };
    }

    /// <inheritdoc />
    public async Task<EvalRun> RunAsync(EvalRunRequest request, CancellationToken cancellationToken = default)
    {
        var dataset = await GetDatasetAsync(cancellationToken);
        if (dataset.Cases.Count == 0)
        {
            throw new InvalidOperationException("No test set yet. Generate one first.");
        }

        var cases = dataset.Cases.Take(request.Size).ToList();
        _logger.LogInformation(
            "Starting eval run: {Count} cases, TopK={TopK}, UseHybrid={UseHybrid}, UseMultiQuery={UseMultiQuery}",
            cases.Count, request.TopK, request.UseHybrid, request.UseMultiQuery);

        var results = new ConcurrentBag<EvalCaseResult>();
        await Parallel.ForEachAsync(
            cases,
            new ParallelOptions { MaxDegreeOfParallelism = _options.MaxParallelism, CancellationToken = cancellationToken },
            async (evalCase, ct) => results.Add(await EvaluateCaseAsync(evalCase, request, ct)));

        var ordered = results.OrderBy(r => r.CaseId).ToList();
        var scored = ordered.Where(r => r.Error == null).ToList();

        var run = new EvalRun
        {
            Id = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss"),
            CreatedAt = DateTime.UtcNow,
            Settings = request,
            DatasetId = dataset.Id,
            AnswerTemperature = AnswerTemperature,
            CaseCount = ordered.Count,
            HitRate = Average(scored, r => r.Rank.HasValue ? 1 : 0),
            Mrr = Average(scored, r => r.Rank.HasValue ? 1.0 / r.Rank.Value : 0),
            AvgFaithfulness = Average(scored, r => r.Faithfulness),
            AvgRelevance = Average(scored, r => r.Relevance),
            AvgCorrectness = Average(scored, r => r.Correctness),
            AvgLatencyMs = Average(scored, r => r.LatencyMs),
            TotalCost = Math.Round(ordered.Sum(r => r.Cost), 6),
            Errors = ordered.Count - scored.Count,
            ByStyle = scored
                .GroupBy(r => r.Style)
                .ToDictionary(g => g.Key, g =>
                {
                    var group = g.ToList();
                    return new EvalStyleScores
                    {
                        Count = group.Count,
                        HitRate = Average(group, r => r.Rank.HasValue ? 1 : 0),
                        Mrr = Average(group, r => r.Rank.HasValue ? 1.0 / r.Rank.Value : 0),
                        AvgFaithfulness = Average(group, r => r.Faithfulness),
                        AvgCorrectness = Average(group, r => r.Correctness)
                    };
                }),
            Results = ordered
        };

        Directory.CreateDirectory(RunsDirectory);
        await File.WriteAllTextAsync(
            Path.Combine(RunsDirectory, $"{run.Id}.json"),
            JsonSerializer.Serialize(run, JsonOptions),
            cancellationToken);

        _logger.LogInformation(
            "Eval run {Id} finished: HitRate={HitRate:F2}, MRR={Mrr:F2}, Faithfulness={F:F2}, Relevance={R:F2}, Correctness={C:F2}",
            run.Id, run.HitRate, run.Mrr, run.AvgFaithfulness, run.AvgRelevance, run.AvgCorrectness);

        return run;
    }

    /// <inheritdoc />
    public async Task<List<EvalRun>> ListRunsAsync(CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(RunsDirectory))
        {
            return new List<EvalRun>();
        }

        var runs = new List<EvalRun>();
        foreach (var file in Directory.GetFiles(RunsDirectory, "*.json"))
        {
            var run = await ReadRunAsync(file, cancellationToken);
            if (run != null)
            {
                run.Results = null;
                runs.Add(run);
            }
        }

        return runs.OrderByDescending(r => r.CreatedAt).ToList();
    }

    /// <inheritdoc />
    public Task<EvalRun?> GetRunAsync(string id, CancellationToken cancellationToken = default)
    {
        // The ID becomes a file name, so only accept the format RunAsync produces.
        if (!System.Text.RegularExpressions.Regex.IsMatch(id, @"^\d{8}-\d{6}$"))
        {
            return Task.FromResult<EvalRun?>(null);
        }

        var path = Path.Combine(RunsDirectory, $"{id}.json");
        return File.Exists(path) ? ReadRunAsync(path, cancellationToken) : Task.FromResult<EvalRun?>(null);
    }

    private async Task<EvalCaseResult> EvaluateCaseAsync(EvalCase evalCase, EvalRunRequest request, CancellationToken ct)
    {
        var result = new EvalCaseResult
        {
            CaseId = evalCase.Id,
            Question = evalCase.Question,
            Style = evalCase.Style,
            ExpectedPostId = evalCase.ExpectedPostId
        };

        try
        {
            var stopwatch = Stopwatch.StartNew();

            // Same retrieval and answering path as POST /ask.
            var expansion = request.UseMultiQuery
                ? await _llmService.ExpandQueryAsync(evalCase.Question, cancellationToken: ct)
                : null;
            var chunks = expansion != null
                ? await _retrievalService.MultiQuerySearchAsync(
                    [evalCase.Question, .. expansion.Queries], request.TopK, request.UseHybrid, ct)
                : await _retrievalService.HybridSearchAsync(evalCase.Question, request.TopK, request.UseHybrid, ct);
            result.SearchedQueries = expansion?.Queries;
            result.RetrievedPostIds = chunks.Select(c => c.PostId).Distinct().ToList();
            var position = result.RetrievedPostIds.IndexOf(evalCase.ExpectedPostId);
            result.Rank = position >= 0 ? position + 1 : null;

            var answer = new StringBuilder();
            await foreach (var token in _llmService.StreamAnswerAsync(evalCase.Question, chunks, AnswerTemperature, ct))
            {
                answer.Append(token);
            }

            stopwatch.Stop();
            result.Answer = answer.ToString();
            result.LatencyMs = stopwatch.ElapsedMilliseconds;

            var context = string.Join("\n\n", chunks.Select(c => c.ChunkText));
            var promptTokens = TokenCounter.CountChatTokens($"Context: {context}\nQuestion: {evalCase.Question}")
                + (expansion?.PromptTokens ?? 0);
            var completionTokens = TokenCounter.CountChatTokens(result.Answer) + (expansion?.CompletionTokens ?? 0);
            result.Tokens = promptTokens + completionTokens;
            result.Cost = CostEstimator.EstimateLlmCost(promptTokens, completionTokens);

            await JudgeAsync(result, evalCase, context, ct);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Eval case {CaseId} failed", evalCase.Id);
            result.Error = ex.Message;
        }

        return result;
    }

    private async Task JudgeAsync(EvalCaseResult result, EvalCase evalCase, string context, CancellationToken ct)
    {
        var input =
            $"QUESTION:\n{evalCase.Question}\n\n" +
            $"CONTEXT:\n{context}\n\n" +
            $"ANSWER:\n{result.Answer}\n\n" +
            $"REFERENCE:\n{evalCase.ReferenceAnswer}";

        var json = await CompleteAsync(JudgePrompt, input, 0, jsonOutput: true, ct);
        using var verdict = JsonDocument.Parse(json);
        var root = verdict.RootElement;

        result.Faithfulness = root.GetProperty("faithfulness").GetInt32();
        result.Relevance = root.GetProperty("relevance").GetInt32();
        result.Correctness = root.GetProperty("correctness").GetInt32();
        result.JudgeReason = root.TryGetProperty("reason", out var reason) ? reason.GetString() ?? "" : "";
    }

    private async Task<string> CompleteAsync(string system, string user, double temperature, bool jsonOutput, CancellationToken ct)
    {
        var history = new ChatHistory();
        history.AddSystemMessage(system);
        history.AddUserMessage(user);

        var settings = new OpenAIPromptExecutionSettings { Temperature = temperature };
        if (jsonOutput)
        {
            settings.ResponseFormat = "json_object";
        }

        var reply = await _chat.GetChatMessageContentAsync(history, settings, cancellationToken: ct);
        return reply.Content ?? string.Empty;
    }

    /// <summary>
    /// Rebuilds a post's text from its chunks, dropping the overlap each chunk repeats from the previous one.
    /// </summary>
    public static string JoinChunks(IEnumerable<DocumentChunk> chunks)
    {
        var text = new StringBuilder();
        foreach (var chunk in chunks)
        {
            var chunkText = chunk.ChunkText;
            var current = text.ToString();
            var overlap = 0;
            for (var length = Math.Min(chunkText.Length, current.Length); length >= 20; length--)
            {
                if (current.EndsWith(chunkText[..length], StringComparison.Ordinal))
                {
                    overlap = length;
                    break;
                }
            }

            if (text.Length > 0 && overlap == 0)
            {
                text.Append(' ');
            }

            text.Append(chunkText[overlap..]);
        }

        return text.ToString();
    }

    /// <summary>
    /// Splits "Title: … Question: … Answer: …" (see StackOverflowDocument.GetFullText) into question and answer.
    /// </summary>
    public static (string Question, string Answer) SplitQuestionAndAnswer(string fullText)
    {
        var marker = fullText.IndexOf("Answer: ", StringComparison.Ordinal);
        return marker < 0
            ? (fullText, string.Empty)
            : (fullText[..marker].Trim(), fullText[(marker + "Answer: ".Length)..].Trim());
    }

    /// <summary>
    /// Short fingerprint of the test set file, so runs record exactly which questions they used.
    /// </summary>
    private static string DatasetIdFor(byte[] datasetFile) =>
        Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(datasetFile))[..8].ToLowerInvariant();

    private static double Average(List<EvalCaseResult> results, Func<EvalCaseResult, double> selector) =>
        results.Count == 0 ? 0 : Math.Round(results.Average(selector), 3);

    private static async Task<EvalRun?> ReadRunAsync(string path, CancellationToken ct)
    {
        await using var stream = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<EvalRun>(stream, JsonOptions, ct);
    }
}
