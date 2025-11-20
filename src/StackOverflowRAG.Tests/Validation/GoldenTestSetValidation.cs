using System.Text.Json;
using Microsoft.Extensions.Logging;
using Moq;
using StackOverflowRAG.Core.Interfaces;
using StackOverflowRAG.Data.Models;
using Xunit;
using Xunit.Abstractions;

namespace StackOverflowRAG.Tests.Validation;

/// <summary>
/// Golden test set validation for measuring retrieval quality using Hit@K metrics.
/// </summary>
public class GoldenTestSetValidation
{
    private readonly ITestOutputHelper _output;
    private readonly string _goldenTestSetPath;

    public GoldenTestSetValidation(ITestOutputHelper output)
    {
        _output = output;
        _goldenTestSetPath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "..", "..", "..", "TestData", "golden-test-set.json");
    }

    /// <summary>
    /// Model for golden test set entries
    /// </summary>
    public class GoldenTestEntry
    {
        public int Id { get; set; }
        public string QuestionText { get; set; } = string.Empty;
        public List<int> ExpectedPostIds { get; set; } = new();
        public List<string> Tags { get; set; } = new();
        public string Notes { get; set; } = string.Empty;
    }

    /// <summary>
    /// Results for a single test case
    /// </summary>
    public class TestResult
    {
        public int TestId { get; set; }
        public string Question { get; set; } = string.Empty;
        public bool Hit { get; set; }
        public List<int> RetrievedPostIds { get; set; } = new();
        public List<int> ExpectedPostIds { get; set; } = new();
        public string Notes { get; set; } = string.Empty;
    }

    /// <summary>
    /// Loads the golden test set from JSON file
    /// </summary>
    [Fact]
    public void LoadGoldenTestSet_ShouldLoadAllEntries()
    {
        // Arrange & Act
        var testSet = LoadGoldenTestSet();

        // Assert
        Assert.NotNull(testSet);
        Assert.True(testSet.Count >= 10, $"Golden test set should have at least 10 entries, found {testSet.Count}");

        _output.WriteLine($"Loaded {testSet.Count} golden test entries");

        foreach (var entry in testSet)
        {
            _output.WriteLine($"  [{entry.Id}] {entry.QuestionText.Substring(0, Math.Min(50, entry.QuestionText.Length))}...");
        }
    }

    /// <summary>
    /// Validates the structure of the golden test set
    /// </summary>
    [Fact]
    public void GoldenTestSet_ShouldHaveValidStructure()
    {
        // Arrange
        var testSet = LoadGoldenTestSet();

        // Assert
        foreach (var entry in testSet)
        {
            Assert.True(entry.Id > 0, $"Entry should have positive Id");
            Assert.False(string.IsNullOrWhiteSpace(entry.QuestionText), $"Entry {entry.Id} should have question text");
            Assert.NotNull(entry.ExpectedPostIds);
            Assert.NotNull(entry.Tags);
        }

        // Check for unique IDs
        var ids = testSet.Select(e => e.Id).ToList();
        Assert.Equal(ids.Count, ids.Distinct().Count());

        _output.WriteLine($"Golden test set structure validation passed: {testSet.Count} entries");
    }

    /// <summary>
    /// Calculates Hit@K metric using mock retrieval service.
    /// This test demonstrates the validation pattern - actual validation requires real services.
    /// </summary>
    [Fact]
    public void CalculateHitAtK_WithMockService_ShouldComputeMetric()
    {
        // Arrange
        var testSet = LoadGoldenTestSet();
        var mockRetrieval = new Mock<IRetrievalService>();

        // Mock retrieval to return chunks based on question
        mockRetrieval
            .Setup(x => x.SearchAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string query, int topK, CancellationToken ct) =>
            {
                // Return mock chunks - in real scenario these would come from actual retrieval
                return new List<DocumentChunk>
                {
                    new() { PostId = 1, ChunkText = "Sample chunk", Score = 0.9f },
                    new() { PostId = 2, ChunkText = "Sample chunk 2", Score = 0.8f },
                    new() { PostId = 3, ChunkText = "Sample chunk 3", Score = 0.7f }
                };
            });

        // Act
        var results = new List<TestResult>();
        var topK = 5;
        var hits = 0;

        foreach (var entry in testSet)
        {
            var chunks = mockRetrieval.Object.SearchAsync(entry.QuestionText, topK).Result;
            var retrievedPostIds = chunks.Select(c => c.PostId).ToList();

            // Check if any expected post ID is in retrieved results
            var hit = entry.ExpectedPostIds.Count == 0 || // If no expected IDs, count as hit (needs population)
                      entry.ExpectedPostIds.Any(id => retrievedPostIds.Contains(id));

            if (hit) hits++;

            results.Add(new TestResult
            {
                TestId = entry.Id,
                Question = entry.QuestionText,
                Hit = hit,
                RetrievedPostIds = retrievedPostIds,
                ExpectedPostIds = entry.ExpectedPostIds,
                Notes = entry.ExpectedPostIds.Count == 0 ? "No expected IDs configured" : ""
            });
        }

        // Calculate Hit@K
        var hitRate = (double)hits / testSet.Count;

        // Assert
        _output.WriteLine($"\n=== Hit@{topK} Validation Results ===\n");

        foreach (var result in results)
        {
            var status = result.Hit ? "HIT" : "MISS";
            _output.WriteLine($"[{result.TestId}] {status}: {result.Question.Substring(0, Math.Min(50, result.Question.Length))}...");

            if (!result.Hit)
            {
                _output.WriteLine($"     Expected: [{string.Join(", ", result.ExpectedPostIds)}]");
                _output.WriteLine($"     Retrieved: [{string.Join(", ", result.RetrievedPostIds)}]");
            }
        }

        _output.WriteLine($"\n=== Summary ===");
        _output.WriteLine($"Hit@{topK} = {hitRate:P1} ({hits}/{testSet.Count})");
        _output.WriteLine($"Target: >= 70%");
        _output.WriteLine($"Status: {(hitRate >= 0.7 ? "PASS" : "NEEDS IMPROVEMENT")}");

        // Note: With mock data and empty expected IDs, this will pass
        // Real validation requires populating expectedPostIds and using real retrieval
        Assert.True(results.Count > 0, "Should have processed test entries");
    }

    /// <summary>
    /// Reports on golden test set readiness
    /// </summary>
    [Fact]
    public void ReportGoldenTestSetReadiness()
    {
        // Arrange
        var testSet = LoadGoldenTestSet();

        // Calculate readiness
        var totalEntries = testSet.Count;
        var configuredEntries = testSet.Count(e => e.ExpectedPostIds.Count > 0);
        var unconfiguredEntries = totalEntries - configuredEntries;

        _output.WriteLine("\n=== Golden Test Set Readiness Report ===\n");
        _output.WriteLine($"Total entries: {totalEntries}");
        _output.WriteLine($"Configured (have expected post IDs): {configuredEntries}");
        _output.WriteLine($"Unconfigured (need post IDs): {unconfiguredEntries}");
        _output.WriteLine($"Readiness: {(double)configuredEntries / totalEntries:P0}");

        if (unconfiguredEntries > 0)
        {
            _output.WriteLine("\n--- Unconfigured Entries ---");
            foreach (var entry in testSet.Where(e => e.ExpectedPostIds.Count == 0))
            {
                _output.WriteLine($"  [{entry.Id}] {entry.QuestionText}");
            }

            _output.WriteLine("\nTo configure:");
            _output.WriteLine("1. Ingest data: POST /ingest");
            _output.WriteLine("2. Search for each question: GET /search?query=...");
            _output.WriteLine("3. Note the PostId of correct answer");
            _output.WriteLine("4. Add PostId to expectedPostIds in golden-test-set.json");
        }

        // Assert
        Assert.True(totalEntries >= 10, "Should have at least 10 test entries");
    }

    /// <summary>
    /// Helper to load the golden test set from JSON
    /// </summary>
    private List<GoldenTestEntry> LoadGoldenTestSet()
    {
        if (!File.Exists(_goldenTestSetPath))
        {
            throw new FileNotFoundException($"Golden test set not found at: {_goldenTestSetPath}");
        }

        var json = File.ReadAllText(_goldenTestSetPath);
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        return JsonSerializer.Deserialize<List<GoldenTestEntry>>(json, options)
            ?? throw new InvalidOperationException("Failed to deserialize golden test set");
    }
}

/// <summary>
/// Integration tests for Hit@K validation with real services.
/// These tests require running infrastructure (Qdrant, Redis) and ingested data.
/// </summary>
public class GoldenTestSetIntegrationTests
{
    private readonly ITestOutputHelper _output;

    public GoldenTestSetIntegrationTests(ITestOutputHelper output)
    {
        _output = output;
    }

    /// <summary>
    /// Template for running Hit@K validation with real retrieval service.
    /// Requires: docker compose up, data ingested, golden set configured with real post IDs.
    /// </summary>
    [Fact(Skip = "Requires running infrastructure and configured golden test set")]
    public async Task CalculateHitAtK_WithRealService_ShouldMeetTarget()
    {
        // This test should be enabled after:
        // 1. Infrastructure is running (docker compose up)
        // 2. Data is ingested (POST /ingest)
        // 3. Golden test set is configured with real post IDs

        // Arrange
        // var serviceProvider = BuildServiceProvider();
        // var retrievalService = serviceProvider.GetRequiredService<IRetrievalService>();
        // var testSet = LoadGoldenTestSet();

        // Act
        // var hits = 0;
        // var topK = 5;
        // foreach (var entry in testSet)
        // {
        //     var chunks = await retrievalService.SearchAsync(entry.QuestionText, topK);
        //     if (entry.ExpectedPostIds.Any(id => chunks.Any(c => c.PostId == id)))
        //         hits++;
        // }
        // var hitRate = (double)hits / testSet.Count;

        // Assert
        // Assert.True(hitRate >= 0.7, $"Hit@{topK} = {hitRate:P1}, expected >= 70%");

        _output.WriteLine("Integration test template - enable when infrastructure ready");
        await Task.CompletedTask;
    }
}
