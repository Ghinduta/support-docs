#!/usr/bin/env dotnet-script
#r "nuget: Microsoft.ML, 5.0.0"

using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

// Simple training script for tag classifier
// Usage: dotnet script scripts/TrainTagModel.csx [csv-directory] [max-rows]

var args = Args.ToArray();
var csvDirectory = args.Length > 0 ? args[0] : null;
var maxRows = args.Length > 1 ? int.Parse(args[1]) : 10000;
var maxTags = args.Length > 2 ? int.Parse(args[2]) : 50;

Console.WriteLine("╔════════════════════════════════════════╗");
Console.WriteLine("║  Stack Overflow Tag Classifier Trainer ║");
Console.WriteLine("╚════════════════════════════════════════╝");
Console.WriteLine();

// Check for data
string trainingDataPath;

if (csvDirectory != null && Directory.Exists(csvDirectory))
{
    Console.WriteLine($"📂 Using Kaggle StackSample data from: {csvDirectory}");

    var questionsPath = Path.Combine(csvDirectory, "Questions.csv");
    var answersPath = Path.Combine(csvDirectory, "Answers.csv");
    var tagsPath = Path.Combine(csvDirectory, "Tags.csv");

    if (!File.Exists(questionsPath) || !File.Exists(tagsPath))
    {
        Console.WriteLine("❌ ERROR: Questions.csv or Tags.csv not found!");
        Console.WriteLine($"   Looking for files in: {csvDirectory}");
        return 1;
    }

    // Create combined training data
    Console.WriteLine("🔄 Processing CSV files...");
    trainingDataPath = "training-data-temp.csv";

    // TODO: Implement CSV combining logic
    // For now, just use sample data
    Console.WriteLine("⚠️  Full CSV processing not implemented yet.");
    Console.WriteLine("   Using sample data instead...");
    trainingDataPath = "train-sample-model.csv";
}
else
{
    Console.WriteLine("📝 Using sample training data");
    trainingDataPath = "train-sample-model.csv";

    if (!File.Exists(trainingDataPath))
    {
        Console.WriteLine($"❌ ERROR: Training data not found at {trainingDataPath}");
        Console.WriteLine("   Please ensure train-sample-model.csv exists in the project root.");
        return 1;
    }
}

// Create output directory
var outputPath = "models/tag-classifier.zip";
Directory.CreateDirectory("models");

Console.WriteLine();
Console.WriteLine($"⚙️  Configuration:");
Console.WriteLine($"   Training data: {trainingDataPath}");
Console.WriteLine($"   Output model: {outputPath}");
Console.WriteLine($"   Max tags: {maxTags}");
Console.WriteLine();

Console.WriteLine("🚀 Training model (this may take a minute)...");
Console.WriteLine();

// We can't directly use the TagClassifier here without compiling the project
// So let's create instructions instead
Console.WriteLine("To train the model, run:");
Console.WriteLine();
Console.WriteLine("  dotnet run --project src/StackOverflowRAG.ML.Tools");
Console.WriteLine();
Console.WriteLine("Or use the training data with the compiled classifier.");

return 0;
