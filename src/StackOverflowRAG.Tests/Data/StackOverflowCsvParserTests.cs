using Microsoft.Extensions.Logging;
using Moq;
using StackOverflowRAG.Data.Models;
using StackOverflowRAG.Data.Parsers;

namespace StackOverflowRAG.Tests.Data;

public class StackOverflowCsvParserTests : IDisposable
{
    private readonly Mock<ILogger<StackOverflowCsvParser>> _mockLogger;
    private readonly StackOverflowCsvParser _parser;
    private readonly List<string> _tempFiles = new();

    public StackOverflowCsvParserTests()
    {
        _mockLogger = new Mock<ILogger<StackOverflowCsvParser>>();
        _parser = new StackOverflowCsvParser(_mockLogger.Object);
    }

    [Fact]
    public async Task ParseAsync_WithValidCsv_ReturnsDocuments()
    {
        // Arrange
        var csvPath = CreateTempCsvFile(@"Id,Title,Body,Tags
123,""How to use async"",""<p>Question about async/await</p>"",""c#|async|.net""
456,""What is LINQ"",""<p>Explain LINQ</p>"",""c#|linq""");

        // Act
        var result = await _parser.ParseAsync(csvPath, maxRows: 10);

        // Assert
        Assert.Equal(2, result.Count);

        var firstDoc = result[0];
        Assert.Equal(123, firstDoc.PostId);
        Assert.Equal("How to use async", firstDoc.QuestionTitle);
        Assert.Equal("Question about async/await", firstDoc.QuestionBody);
        Assert.Equal(3, firstDoc.Tags.Length);
        Assert.Contains("c#", firstDoc.Tags);
        Assert.Contains("async", firstDoc.Tags);
        Assert.Contains(".net", firstDoc.Tags);
    }

    [Fact]
    public async Task ParseAsync_WithMaxRows_LimitsResults()
    {
        // Arrange
        var csvPath = CreateTempCsvFile(@"Id,Title,Body,Tags
1,""Title 1"",""Body 1"",""tag1""
2,""Title 2"",""Body 2"",""tag2""
3,""Title 3"",""Body 3"",""tag3""
4,""Title 4"",""Body 4"",""tag4""
5,""Title 5"",""Body 5"",""tag5""");

        // Act
        var result = await _parser.ParseAsync(csvPath, maxRows: 3);

        // Assert
        Assert.Equal(3, result.Count);
    }

    [Fact]
    public void CleanHtml_RemovesTags()
    {
        // Arrange
        var html = "<p>This is <strong>bold</strong> text</p>";

        // Act
        var result = _parser.CleanHtml(html);

        // Assert
        Assert.Equal("This is bold text", result);
    }

    [Fact]
    public void CleanHtml_DecodesHtmlEntities()
    {
        // Arrange
        var html = "&lt;script&gt; &amp; &quot;quotes&quot;";

        // Act
        var result = _parser.CleanHtml(html);

        // Assert
        Assert.Equal("<script> & \"quotes\"", result);
    }

    [Fact]
    public void CleanHtml_RemovesScriptAndStyleTags()
    {
        // Arrange
        var html = "<p>Text</p><script>alert('test')</script><style>body{}</style>";

        // Act
        var result = _parser.CleanHtml(html);

        // Assert
        Assert.Equal("Text", result);
    }

    [Fact]
    public void CleanHtml_NormalizesWhitespace()
    {
        // Arrange
        var html = "<p>Text   with    \n\n  multiple   spaces</p>";

        // Act
        var result = _parser.CleanHtml(html);

        // Assert
        Assert.Equal("Text with multiple spaces", result);
    }

    [Fact]
    public void CleanHtml_HandlesEmptyOrNull()
    {
        // Assert
        Assert.Equal(string.Empty, _parser.CleanHtml(null!));
        Assert.Equal(string.Empty, _parser.CleanHtml(""));
        Assert.Equal(string.Empty, _parser.CleanHtml("   "));
    }

    [Fact]
    public async Task ParseAsync_SkipsInvalidRows()
    {
        // Arrange
        var csvPath = CreateTempCsvFile(@"Id,Title,Body,Tags
123,""Valid Title"",""Valid Body"",""tag1""
,""Missing ID"",""Body"",""tag2""
456,"""",""Body with no title"",""tag3""
789,""Valid Title 2"",""Valid Body 2"",""tag4""");

        // Act
        var result = await _parser.ParseAsync(csvPath);

        // Assert
        Assert.Equal(2, result.Count); // Only 2 valid rows
        Assert.Equal(123, result[0].PostId);
        Assert.Equal(789, result[1].PostId);
    }

    [Fact]
    public async Task ParseAsync_SkipsRowsWithoutTags()
    {
        // Arrange
        var csvPath = CreateTempCsvFile(@"Id,Title,Body,Tags
123,""Title"",""Body"",""""
456,""Title 2"",""Body 2"",""tag1""");

        // Act
        var result = await _parser.ParseAsync(csvPath);

        // Assert
        Assert.Single(result);
        Assert.Equal(456, result[0].PostId);
    }

    [Fact]
    public async Task ParseAsync_ParsesMultipleTagDelimiters()
    {
        // Arrange
        var csvPath = CreateTempCsvFile(@"Id,Title,Body,Tags
123,""Title"",""Body"",""tag1|tag2|tag3""
456,""Title 2"",""Body 2"",""tag4,tag5,tag6""");

        // Act
        var result = await _parser.ParseAsync(csvPath);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal(3, result[0].Tags.Length);
        Assert.Equal(3, result[1].Tags.Length);
    }

    [Fact]
    public async Task ParseAsync_WithAnswerBody_IncludesAnswer()
    {
        // Arrange
        var csvPath = CreateTempCsvFile(@"Id,Title,Body,AnswerBody,Tags
123,""Question"",""<p>Question text</p>"",""<p>Answer text</p>"",""tag1""");

        // Act
        var result = await _parser.ParseAsync(csvPath);

        // Assert
        Assert.Single(result);
        Assert.Equal("Answer text", result[0].AnswerBody);
    }

    [Fact]
    public async Task ParseAsync_WithoutAnswerBody_HasNullAnswer()
    {
        // Arrange
        var csvPath = CreateTempCsvFile(@"Id,Title,Body,Tags
123,""Question"",""<p>Question text</p>"",""tag1""");

        // Act
        var result = await _parser.ParseAsync(csvPath);

        // Assert
        Assert.Single(result);
        Assert.Null(result[0].AnswerBody);
    }

    [Fact]
    public async Task ParseAsync_FileNotFound_ThrowsException()
    {
        // Arrange
        var nonExistentPath = Path.Combine(Path.GetTempPath(), $"nonexistent_{Guid.NewGuid()}.csv");

        // Act & Assert
        await Assert.ThrowsAsync<FileNotFoundException>(
            async () => await _parser.ParseAsync(nonExistentPath));
    }

    [Fact]
    public void StackOverflowDocument_GetFullText_CombinesAllFields()
    {
        // Arrange
        var doc = new StackOverflowDocument
        {
            PostId = 123,
            QuestionTitle = "Test Title",
            QuestionBody = "Test Question",
            AnswerBody = "Test Answer",
            Tags = new[] { "tag1", "tag2" }
        };

        // Act
        var fullText = doc.GetFullText();

        // Assert
        Assert.Contains("Test Title", fullText);
        Assert.Contains("Test Question", fullText);
        Assert.Contains("Test Answer", fullText);
    }

    [Fact]
    public void StackOverflowDocument_IsValid_ReturnsTrueForValidDocument()
    {
        // Arrange
        var doc = new StackOverflowDocument
        {
            PostId = 123,
            QuestionTitle = "Title",
            QuestionBody = "Body",
            Tags = new[] { "tag1" }
        };

        // Act & Assert
        Assert.True(doc.IsValid());
    }

    [Fact]
    public void StackOverflowDocument_IsValid_ReturnsFalseForInvalidDocument()
    {
        // Assert - missing tags
        var doc1 = new StackOverflowDocument
        {
            PostId = 123,
            QuestionTitle = "Title",
            QuestionBody = "Body",
            Tags = Array.Empty<string>()
        };
        Assert.False(doc1.IsValid());

        // Assert - empty title
        var doc2 = new StackOverflowDocument
        {
            PostId = 123,
            QuestionTitle = "",
            QuestionBody = "Body",
            Tags = new[] { "tag1" }
        };
        Assert.False(doc2.IsValid());

        // Assert - zero post ID
        var doc3 = new StackOverflowDocument
        {
            PostId = 0,
            QuestionTitle = "Title",
            QuestionBody = "Body",
            Tags = new[] { "tag1" }
        };
        Assert.False(doc3.IsValid());
    }

    private string CreateTempCsvFile(string content)
    {
        var tempPath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.csv");
        File.WriteAllText(tempPath, content);
        _tempFiles.Add(tempPath);
        return tempPath;
    }

    public void Dispose()
    {
        foreach (var file in _tempFiles)
        {
            try
            {
                if (File.Exists(file))
                {
                    File.Delete(file);
                }
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }
}
