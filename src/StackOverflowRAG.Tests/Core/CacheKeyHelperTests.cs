using StackOverflowRAG.Core.Helpers;

namespace StackOverflowRAG.Tests.Core;

public class CacheKeyHelperTests
{
    [Fact]
    public void GenerateEmbeddingKey_WithSameQuery_ReturnsSameKey()
    {
        // Arrange
        var query = "How to use async/await?";

        // Act
        var key1 = CacheKeyHelper.GenerateEmbeddingKey(query);
        var key2 = CacheKeyHelper.GenerateEmbeddingKey(query);

        // Assert
        Assert.Equal(key1, key2);
        Assert.StartsWith("emb:", key1);
    }

    [Fact]
    public void GenerateEmbeddingKey_WithDifferentQueries_ReturnsDifferentKeys()
    {
        // Arrange
        var query1 = "How to use async/await?";
        var query2 = "What is LINQ?";

        // Act
        var key1 = CacheKeyHelper.GenerateEmbeddingKey(query1);
        var key2 = CacheKeyHelper.GenerateEmbeddingKey(query2);

        // Assert
        Assert.NotEqual(key1, key2);
    }

    [Fact]
    public void GenerateEmbeddingKey_FormatsCorrectly()
    {
        // Arrange
        var query = "test query";

        // Act
        var key = CacheKeyHelper.GenerateEmbeddingKey(query);

        // Assert
        Assert.StartsWith("emb:", key);
        Assert.Matches(@"^emb:[a-f0-9]{32}$", key); // MD5 hash is 32 hex chars
    }

    [Fact]
    public void GenerateResponseKey_WithSameParameters_ReturnsSameKey()
    {
        // Arrange
        var query = "How to use async/await?";
        int topK = 5;
        bool useHybrid = true;

        // Act
        var key1 = CacheKeyHelper.GenerateResponseKey(query, topK, useHybrid);
        var key2 = CacheKeyHelper.GenerateResponseKey(query, topK, useHybrid);

        // Assert
        Assert.Equal(key1, key2);
        Assert.StartsWith("resp:", key1);
    }

    [Fact]
    public void GenerateResponseKey_WithDifferentQuery_ReturnsDifferentKey()
    {
        // Arrange
        var query1 = "How to use async/await?";
        var query2 = "What is LINQ?";
        int topK = 5;
        bool useHybrid = true;

        // Act
        var key1 = CacheKeyHelper.GenerateResponseKey(query1, topK, useHybrid);
        var key2 = CacheKeyHelper.GenerateResponseKey(query2, topK, useHybrid);

        // Assert
        Assert.NotEqual(key1, key2);
    }

    [Fact]
    public void GenerateResponseKey_WithDifferentTopK_ReturnsDifferentKey()
    {
        // Arrange
        var query = "How to use async/await?";
        int topK1 = 5;
        int topK2 = 10;
        bool useHybrid = true;

        // Act
        var key1 = CacheKeyHelper.GenerateResponseKey(query, topK1, useHybrid);
        var key2 = CacheKeyHelper.GenerateResponseKey(query, topK2, useHybrid);

        // Assert
        Assert.NotEqual(key1, key2);
    }

    [Fact]
    public void GenerateResponseKey_WithDifferentUseHybrid_ReturnsDifferentKey()
    {
        // Arrange
        var query = "How to use async/await?";
        int topK = 5;

        // Act
        var key1 = CacheKeyHelper.GenerateResponseKey(query, topK, useHybrid: true);
        var key2 = CacheKeyHelper.GenerateResponseKey(query, topK, useHybrid: false);

        // Assert
        Assert.NotEqual(key1, key2);
    }

    [Fact]
    public void GenerateResponseKey_FormatsCorrectly()
    {
        // Arrange
        var query = "test query";
        int topK = 5;
        bool useHybrid = true;

        // Act
        var key = CacheKeyHelper.GenerateResponseKey(query, topK, useHybrid);

        // Assert
        Assert.StartsWith("resp:", key);
        Assert.Matches(@"^resp:[a-f0-9]{32}$", key); // MD5 hash is 32 hex chars
    }

    [Fact]
    public void GenerateResponseKey_IsDeterministic()
    {
        // Arrange
        var query = "How to use async/await in C#?";
        int topK = 10;
        bool useHybrid = false;

        // Act - generate key multiple times
        var keys = Enumerable.Range(0, 100)
            .Select(_ => CacheKeyHelper.GenerateResponseKey(query, topK, useHybrid))
            .ToList();

        // Assert - all keys should be identical
        Assert.True(keys.All(k => k == keys[0]));
    }
}
