using System.Globalization;
using System.Text.RegularExpressions;
using CsvHelper;
using CsvHelper.Configuration;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using StackOverflowRAG.Data.Models;

namespace StackOverflowRAG.Data.Parsers;

/// <summary>
/// Parser for Stack Overflow CSV data with HTML cleaning.
/// </summary>
public class StackOverflowCsvParser : IStackOverflowCsvParser
{
    private readonly ILogger<StackOverflowCsvParser> _logger;

    public StackOverflowCsvParser(ILogger<StackOverflowCsvParser> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<List<StackOverflowDocument>> ParseAsync(
        string csvPath,
        int maxRows = 10000,
        CancellationToken cancellationToken = default)
    {
        // Check if path is a directory (three-CSV structure) or single file (legacy)
        if (Directory.Exists(csvPath))
        {
            _logger.LogInformation("Detected directory path, loading three-CSV structure from {CsvPath}", csvPath);
            return await ParseThreeCsvStructureAsync(csvPath, maxRows, cancellationToken);
        }

        if (!File.Exists(csvPath))
        {
            _logger.LogError("CSV file not found: {CsvPath}", csvPath);
            throw new FileNotFoundException($"CSV file not found: {csvPath}");
        }

        _logger.LogInformation("Starting CSV parsing from {CsvPath} (max rows: {MaxRows})", csvPath, maxRows);

        var documents = new List<StackOverflowDocument>();
        var skippedCount = 0;

        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            MissingFieldFound = null, // Don't throw on missing fields
            BadDataFound = null // Don't throw on bad data
        };

        using var reader = new StreamReader(csvPath);
        using var csv = new CsvReader(reader, config);

        await csv.ReadAsync();
        csv.ReadHeader();

        var rowNumber = 1;

        while (await csv.ReadAsync() && documents.Count < maxRows)
        {
            rowNumber++;
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var document = ParseRow(csv);

                if (document != null && document.IsValid())
                {
                    documents.Add(document);
                }
                else
                {
                    skippedCount++;
                    _logger.LogWarning("Skipped invalid document at row {RowNumber}", rowNumber);
                }
            }
            catch (Exception ex)
            {
                skippedCount++;
                _logger.LogWarning(ex, "Error parsing row {RowNumber}, skipping", rowNumber);
            }
        }

        _logger.LogInformation(
            "CSV parsing completed: {ParsedCount} documents, {SkippedCount} skipped",
            documents.Count,
            skippedCount);

        return documents;
    }

    private async Task<List<StackOverflowDocument>> ParseThreeCsvStructureAsync(
        string directoryPath,
        int maxRows,
        CancellationToken cancellationToken)
    {
        var questionsPath = Path.Combine(directoryPath, "Questions.csv");
        var answersPath = Path.Combine(directoryPath, "Answers.csv");
        var tagsPath = Path.Combine(directoryPath, "Tags.csv");

        if (!File.Exists(questionsPath))
            throw new FileNotFoundException($"Questions.csv not found in {directoryPath}");
        if (!File.Exists(answersPath))
            throw new FileNotFoundException($"Answers.csv not found in {directoryPath}");
        if (!File.Exists(tagsPath))
            throw new FileNotFoundException($"Tags.csv not found in {directoryPath}");

        _logger.LogInformation("Loading Questions.csv (max {MaxRows} rows)", maxRows);
        var questions = await LoadQuestionsAsync(questionsPath, maxRows, cancellationToken);
        _logger.LogInformation("Loaded {Count} questions", questions.Count);

        _logger.LogInformation("Loading Answers.csv");
        var answers = await LoadAnswersAsync(answersPath, cancellationToken);
        _logger.LogInformation("Loaded {Count} answers", answers.Count);

        _logger.LogInformation("Loading Tags.csv");
        var tags = await LoadTagsAsync(tagsPath, cancellationToken);
        _logger.LogInformation("Loaded {Count} tag entries", tags.Count);

        _logger.LogInformation("Joining data: {QuestionCount} questions, {AnswerCount} answers, {TagCount} tag entries",
            questions.Count, answers.Count, tags.Count);

        if (questions.Count == 0)
        {
            _logger.LogWarning("No questions loaded from Questions.csv");
            return new List<StackOverflowDocument>();
        }

        // Join data: Questions ← Answers (top score) ← Tags (aggregated)
        var documents = new List<StackOverflowDocument>();

        foreach (var question in questions)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Get top-scoring answer for this question
            var topAnswer = answers
                .Where(a => a.ParentId == question.Id)
                .OrderByDescending(a => a.Score)
                .FirstOrDefault();

            // Get all tags for this question
            var questionTags = tags
                .Where(t => t.Id == question.Id)
                .Select(t => t.Tag)
                .Distinct()
                .ToArray();

            if (questionTags.Length == 0)
            {
                _logger.LogDebug("Skipping question {Id} - no tags", question.Id);
                continue;
            }

            var document = new StackOverflowDocument
            {
                PostId = question.Id,
                QuestionTitle = CleanHtml(question.Title),
                QuestionBody = CleanHtml(question.Body),
                AnswerBody = topAnswer != null ? CleanHtml(topAnswer.Body) : null,
                Tags = questionTags
            };

            if (document.IsValid())
            {
                documents.Add(document);
            }
        }

        _logger.LogInformation("Successfully joined {DocumentCount} documents", documents.Count);
        return documents;
    }

    private async Task<List<QuestionRow>> LoadQuestionsAsync(string path, int maxRows, CancellationToken cancellationToken)
    {
        var questions = new List<QuestionRow>();
        var skippedCount = 0;
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            MissingFieldFound = null,
            BadDataFound = null
        };

        using var reader = new StreamReader(path);
        using var csv = new CsvReader(reader, config);

        await csv.ReadAsync();
        csv.ReadHeader();
        _logger.LogInformation("CSV headers: {Headers}", string.Join(", ", csv.HeaderRecord));

        var rowCount = 0;
        while (await csv.ReadAsync() && questions.Count < maxRows)
        {
            rowCount++;
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var id = csv.GetField<int>("Id");
                var title = csv.GetField<string>("Title");
                var body = csv.GetField<string>("Body");
                var score = csv.GetField<int>("Score");

                if (!string.IsNullOrWhiteSpace(title) && !string.IsNullOrWhiteSpace(body))
                {
                    questions.Add(new QuestionRow { Id = id, Title = title, Body = body, Score = score });

                    if (questions.Count == 1)
                    {
                        _logger.LogInformation("First question: Id={Id}, Title={Title} (trimmed), Score={Score}",
                            id, title.Substring(0, Math.Min(50, title.Length)), score);
                    }
                }
                else
                {
                    skippedCount++;
                }
            }
            catch (Exception ex)
            {
                skippedCount++;
                _logger.LogWarning(ex, "Error parsing question row {RowNumber}, skipping", rowCount);
            }
        }

        _logger.LogInformation("Questions loaded: {Count} valid, {Skipped} skipped out of {Total} rows",
            questions.Count, skippedCount, rowCount);
        return questions;
    }

    private async Task<List<AnswerRow>> LoadAnswersAsync(string path, CancellationToken cancellationToken)
    {
        var answers = new List<AnswerRow>();
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            MissingFieldFound = null,
            BadDataFound = null
        };

        using var reader = new StreamReader(path);
        using var csv = new CsvReader(reader, config);

        await csv.ReadAsync();
        csv.ReadHeader();

        while (await csv.ReadAsync())
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var id = csv.GetField<int>("Id");
                var parentId = csv.GetField<int>("ParentId");
                var body = csv.GetField<string>("Body");
                var score = csv.GetField<int>("Score");

                if (!string.IsNullOrWhiteSpace(body))
                {
                    answers.Add(new AnswerRow { Id = id, ParentId = parentId, Body = body, Score = score });
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error parsing answer row, skipping");
            }
        }

        return answers;
    }

    private async Task<List<TagRow>> LoadTagsAsync(string path, CancellationToken cancellationToken)
    {
        var tags = new List<TagRow>();
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            MissingFieldFound = null,
            BadDataFound = null
        };

        using var reader = new StreamReader(path);
        using var csv = new CsvReader(reader, config);

        await csv.ReadAsync();
        csv.ReadHeader();

        while (await csv.ReadAsync())
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var id = csv.GetField<int>("Id");
                var tag = csv.GetField<string>("Tag");

                if (!string.IsNullOrWhiteSpace(tag))
                {
                    tags.Add(new TagRow { Id = id, Tag = tag });
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error parsing tag row, skipping");
            }
        }

        return tags;
    }

    /// <inheritdoc />
    public string CleanHtml(string html)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            return string.Empty;
        }

        try
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            // Remove script and style nodes
            doc.DocumentNode.Descendants()
                .Where(n => n.Name == "script" || n.Name == "style")
                .ToList()
                .ForEach(n => n.Remove());

            // Get inner text and decode HTML entities
            var text = System.Net.WebUtility.HtmlDecode(doc.DocumentNode.InnerText);

            // Clean up whitespace
            text = Regex.Replace(text, @"\s+", " ");
            text = text.Trim();

            return text;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error cleaning HTML, returning raw text");
            // Fallback: basic tag removal with regex
            var text = Regex.Replace(html, "<.*?>", string.Empty);
            return System.Net.WebUtility.HtmlDecode(text).Trim();
        }
    }

    private StackOverflowDocument? ParseRow(CsvReader csv)
    {
        // Expected CSV columns: Id, Title, Body, AcceptedAnswerId, Tags
        // Note: We may not have all columns in every CSV format

        var postIdStr = csv.GetField<string>("Id") ?? csv.GetField<string>("PostId");
        var title = csv.GetField<string>("Title");
        var body = csv.GetField<string>("Body") ?? csv.GetField<string>("QuestionBody");
        var tagsStr = csv.GetField<string>("Tags");

        // Try to get answer - may be in different columns depending on CSV format
        string? answerBody = null;
        try
        {
            answerBody = csv.GetField<string>("AnswerBody") ?? csv.GetField<string>("AcceptedAnswerBody");
        }
        catch
        {
            // Answer column may not exist
        }

        // Validate required fields
        if (string.IsNullOrWhiteSpace(postIdStr) ||
            string.IsNullOrWhiteSpace(title) ||
            string.IsNullOrWhiteSpace(body) ||
            string.IsNullOrWhiteSpace(tagsStr))
        {
            return null;
        }

        if (!int.TryParse(postIdStr, out var postId))
        {
            return null;
        }

        // Parse tags (pipe-delimited: "c#|async|.net")
        var tags = tagsStr
            .Split(new[] { '|', ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(t => t.Trim())
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .ToArray();

        if (tags.Length == 0)
        {
            return null;
        }

        // Clean HTML from title, body, and answer
        var cleanTitle = CleanHtml(title);
        var cleanBody = CleanHtml(body);
        var cleanAnswer = !string.IsNullOrWhiteSpace(answerBody) ? CleanHtml(answerBody) : null;

        return new StackOverflowDocument
        {
            PostId = postId,
            QuestionTitle = cleanTitle,
            QuestionBody = cleanBody,
            AnswerBody = cleanAnswer,
            Tags = tags
        };
    }

    // Helper classes for three-CSV structure
    private record QuestionRow
    {
        public int Id { get; init; }
        public string Title { get; init; } = string.Empty;
        public string Body { get; init; } = string.Empty;
        public int Score { get; init; }
    }

    private record AnswerRow
    {
        public int Id { get; init; }
        public int ParentId { get; init; }
        public string Body { get; init; } = string.Empty;
        public int Score { get; init; }
    }

    private record TagRow
    {
        public int Id { get; init; }
        public string Tag { get; init; } = string.Empty;
    }
}
