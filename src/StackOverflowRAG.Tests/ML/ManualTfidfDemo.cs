using StackOverflowRAG.ML.Models;
using StackOverflowRAG.ML.Services;
using Xunit;
using Xunit.Abstractions;

namespace StackOverflowRAG.Tests.ML;

/// <summary>
/// Manual demonstration test - run this to see TF-IDF in action
/// Run: dotnet test --filter "ManualTfidfDemo"
/// </summary>
public class ManualTfidfDemo
{
    private readonly ITestOutputHelper _output;

    public ManualTfidfDemo(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void DemonstrateFullWorkflow()
    {
        _output.WriteLine("=== TF-IDF Vectorizer Demo ===\n");

        // Step 1: Create training data
        var trainingDataPath = Path.Combine(Path.GetTempPath(), $"demo_training_{Guid.NewGuid()}.csv");
        _output.WriteLine($"1. Creating training data at: {trainingDataPath}");

        var trainingData = new[]
        {
            "Text,Tags",
            "\"How to use async await in C#\",\"c#,async-await,.net\"",
            "\"Python list comprehension examples\",\"python,list\"",
            "\"JavaScript promises tutorial\",\"javascript,promise\"",
            "\"SQL join query optimization\",\"sql,join\"",
            "\"React hooks useState\",\"reactjs,hooks\"",
            "\"Docker container networking\",\"docker,networking\"",
            "\"Entity Framework migrations\",\"c#,entity-framework\"",
            "\"Linux bash scripting\",\"linux,bash\"",
            "\"CSS grid layout\",\"css,grid\"",
            "\"MongoDB aggregation\",\"mongodb,nosql\""
        };

        File.WriteAllLines(trainingDataPath, trainingData);
        _output.WriteLine($"   Created {trainingData.Length - 1} training samples\n");

        // Step 2: Train vectorizer
        _output.WriteLine("2. Training TF-IDF vectorizer...");
        var vectorizer = new TfidfVectorizer();
        vectorizer.Train(trainingDataPath);
        _output.WriteLine($"   ✓ Training complete!");
        _output.WriteLine($"   Vocabulary size: {vectorizer.VocabularySize}\n");

        // Step 3: Extract features from different questions
        _output.WriteLine("3. Extracting features from test questions:\n");

        var testCases = new[]
        {
            ("C# async programming", "How to use async await in C#"),
            ("Docker networking", "Connect multiple Docker containers"),
            ("CSS layout", "Grid vs flexbox in CSS"),
            ("Python lists", "List comprehension in Python")
        };

        foreach (var (title, body) in testCases)
        {
            var features = vectorizer.ExtractFeatures(title, body);
            var nonZeroCount = features.Count(f => f > 0);
            var topFeatures = features.OrderByDescending(f => f).Take(5).ToArray();

            _output.WriteLine($"   Question: \"{title}\"");
            _output.WriteLine($"   Body: \"{body}\"");
            _output.WriteLine($"   → Feature vector: {features.Length} dimensions, {nonZeroCount} non-zero");
            _output.WriteLine($"   → Top 5 values: [{string.Join(", ", topFeatures.Select(f => f.ToString("F4")))}]");
            _output.WriteLine("");
        }

        // Step 4: Compare similar vs different questions
        _output.WriteLine("4. Comparing similarity between questions:\n");

        var q1Features = vectorizer.ExtractFeatures("C# async await", "asynchronous programming");
        var q2Features = vectorizer.ExtractFeatures("C# async", "async await tutorial");
        var q3Features = vectorizer.ExtractFeatures("Docker networking", "container communication");

        var similaritySame = CosineSimilarity(q1Features, q2Features);
        var similarityDifferent = CosineSimilarity(q1Features, q3Features);

        _output.WriteLine($"   Similarity (C# async vs C# async): {similaritySame:F4}");
        _output.WriteLine($"   Similarity (C# async vs Docker): {similarityDifferent:F4}");
        _output.WriteLine($"   → Similar topics have higher similarity: {(similaritySame > similarityDifferent ? "✓ YES" : "✗ NO")}\n");

        // Step 5: Test model persistence
        _output.WriteLine("5. Testing model save/load:\n");

        var modelPath = Path.Combine(Path.GetTempPath(), $"demo_model_{Guid.NewGuid()}.zip");
        vectorizer.SaveModel(modelPath);
        _output.WriteLine($"   Saved model to: {modelPath}");
        _output.WriteLine($"   File size: {new FileInfo(modelPath).Length / 1024.0:F2} KB");

        var loadedVectorizer = new TfidfVectorizer();
        loadedVectorizer.LoadModel(modelPath);
        _output.WriteLine($"   Loaded model. Vocabulary size: {loadedVectorizer.VocabularySize}");

        var originalFeatures = vectorizer.ExtractFeatures("test", "question");
        var loadedFeatures = loadedVectorizer.ExtractFeatures("test", "question");
        var areEqual = originalFeatures.SequenceEqual(loadedFeatures);

        _output.WriteLine($"   → Features match after reload: {(areEqual ? "✓ YES" : "✗ NO")}\n");

        // Cleanup
        File.Delete(trainingDataPath);
        File.Delete(modelPath);
        _output.WriteLine("=== Demo Complete ===");
    }

    private double CosineSimilarity(float[] a, float[] b)
    {
        var dotProduct = 0.0;
        var magnitudeA = 0.0;
        var magnitudeB = 0.0;

        for (int i = 0; i < a.Length; i++)
        {
            dotProduct += a[i] * b[i];
            magnitudeA += a[i] * a[i];
            magnitudeB += b[i] * b[i];
        }

        if (magnitudeA == 0 || magnitudeB == 0)
            return 0;

        return dotProduct / (Math.Sqrt(magnitudeA) * Math.Sqrt(magnitudeB));
    }
}
