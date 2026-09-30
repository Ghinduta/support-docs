using StackOverflowRAG.Data.Repositories;

namespace StackOverflowRAG.Tests.Data;

public class QdrantVectorStoreRepositoryTests
{
    [Fact]
    public void PointIdFor_SameChunkId_ReturnsSameId()
    {
        Assert.Equal(
            QdrantVectorStoreRepository.PointIdFor("123_0"),
            QdrantVectorStoreRepository.PointIdFor("123_0"));
    }

    [Fact]
    public void PointIdFor_DifferentChunkIds_ReturnDifferentIds()
    {
        Assert.NotEqual(
            QdrantVectorStoreRepository.PointIdFor("123_0"),
            QdrantVectorStoreRepository.PointIdFor("123_1"));
    }
}
