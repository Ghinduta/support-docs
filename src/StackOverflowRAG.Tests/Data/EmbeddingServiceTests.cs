using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Moq;
using StackOverflowRAG.Data.Models;
using StackOverflowRAG.Data.Services;

namespace StackOverflowRAG.Tests.Data;

public class EmbeddingServiceTests
{
    private readonly Mock<IEmbeddingGenerator<string, Embedding<float>>> _mockEmbeddingGenerator;
    private readonly Mock<ILogger<OpenAIEmbeddingService>> _mockLogger;
    private readonly OpenAIEmbeddingService _service;

    public EmbeddingServiceTests()
    {
        _mockEmbeddingGenerator = new Mock<IEmbeddingGenerator<string, Embedding<float>>>();
        _mockLogger = new Mock<ILogger<OpenAIEmbeddingService>>();
        _service = new OpenAIEmbeddingService(_mockEmbeddingGenerator.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task GenerateEmbeddingAsync_WithEmptyText_ThrowsException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            async () => await _service.GenerateEmbeddingAsync(""));
    }

    [Fact]
    public async Task GenerateBatchEmbeddingsAsync_WithMultipleTexts_ReturnsMultipleEmbeddings()
    {
        // Arrange
        var texts = new List<string> { "Text 1", "Text 2" };

        _mockEmbeddingGenerator
            .Setup(x => x.GenerateAsync(
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<EmbeddingGenerationOptions?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((IEnumerable<string> input, EmbeddingGenerationOptions? options, CancellationToken ct) =>
            {
                var embeddings = new[]
                {
                    new Embedding<float>(new float[] { 0.1f, 0.2f }),
                    new Embedding<float>(new float[] { 0.3f, 0.4f })
                };
                return new GeneratedEmbeddings<Embedding<float>>(embeddings);
            });

        // Act
        var result = await _service.GenerateBatchEmbeddingsAsync(texts);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal(2, result[0].Length);
        Assert.Equal(2, result[1].Length);
    }

    [Fact]
    public async Task GenerateChunkEmbeddingsAsync_AssignsEmbeddingsToChunks()
    {
        // Arrange
        var chunks = new List<DocumentChunk>
        {
            new DocumentChunk { ChunkId = "1_0", PostId = 1, ChunkText = "Text 1", ChunkIndex = 0 },
            new DocumentChunk { ChunkId = "2_0", PostId = 2, ChunkText = "Text 2", ChunkIndex = 0 }
        };

        _mockEmbeddingGenerator
            .Setup(x => x.GenerateAsync(
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<EmbeddingGenerationOptions?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((IEnumerable<string> input, EmbeddingGenerationOptions? options, CancellationToken ct) =>
            {
                var embeddings = new[]
                {
                    new Embedding<float>(new float[] { 0.1f, 0.2f }),
                    new Embedding<float>(new float[] { 0.3f, 0.4f })
                };
                return new GeneratedEmbeddings<Embedding<float>>(embeddings);
            });

        // Act
        await _service.GenerateChunkEmbeddingsAsync(chunks);

        // Assert
        Assert.NotNull(chunks[0].Embedding);
        Assert.NotNull(chunks[1].Embedding);
        Assert.Equal(2, chunks[0].Embedding!.Length);
        Assert.Equal(2, chunks[1].Embedding!.Length);
    }
}
