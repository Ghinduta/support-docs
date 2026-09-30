using StackOverflowRAG.Data.Utilities;

namespace StackOverflowRAG.Tests.Data;

public class Bm25Tests
{
    [Fact]
    public void Tokenize_KeepsProgrammingTermsAndDropsStopWords()
    {
        var words = Bm25.Tokenize("How do I use C# and C++ with ASP.NET in the end.");

        Assert.Equal(new[] { "use", "c#", "c++", "asp.net", "end" }, words);
    }

    [Fact]
    public void EncodeDocument_RepeatedWordWeighsMoreButWithDiminishingReturns()
    {
        var once = Bm25.EncodeDocument("parse").Values.Single();
        var twice = Bm25.EncodeDocument("parse parse").Values.Single();
        var tenTimes = Bm25.EncodeDocument(string.Join(' ', Enumerable.Repeat("parse", 10))).Values.Single();

        Assert.True(twice > once);
        Assert.True((tenTimes - twice) / 8 < twice - once); // each extra repeat adds less
        Assert.True(tenTimes < 2.2f); // never exceeds k1 + 1
    }

    [Fact]
    public void EncodeQuery_SameWordMapsToSameIndexAsDocument()
    {
        var document = Bm25.EncodeDocument("DateTime ParseExact example");
        var query = Bm25.EncodeQuery("parseexact");

        Assert.Contains(query.Indices.Single(), document.Indices);
        Assert.Equal(1f, query.Values.Single());
    }

    [Fact]
    public void EncodeQuery_CountsEachWordOnce()
    {
        var query = Bm25.EncodeQuery("sort sort list");

        Assert.Equal(2, query.Indices.Length);
    }
}
