using DotNetEnv;
using Microsoft.Extensions.AI;
using Microsoft.SemanticKernel;
using Qdrant.Client;
using Serilog;
using StackOverflowRAG.Core.Configuration;
using StackOverflowRAG.Core.Interfaces;
using StackOverflowRAG.Core.Models;
using StackOverflowRAG.Core.Services;
using StackOverflowRAG.Data.Parsers;
using StackOverflowRAG.Data.Repositories;
using StackOverflowRAG.Data.Services;

#pragma warning disable SKEXP0001 // Type is for evaluation purposes only
#pragma warning disable SKEXP0010 // Type is for evaluation purposes only

// Load .env file from project root (two directories up from Api folder)
var currentDir = Directory.GetCurrentDirectory();
Console.WriteLine($"Current directory: {currentDir}");
var envPath = Path.Combine(currentDir, "..", "..", ".env");
var fullEnvPath = Path.GetFullPath(envPath);
Console.WriteLine($"Looking for .env at: {fullEnvPath}");
if (File.Exists(fullEnvPath))
{
    Console.WriteLine(".env file found, loading...");
    Env.Load(fullEnvPath);
}
else
{
    Console.WriteLine(".env file NOT found!");
}

var builder = WebApplication.CreateBuilder(args);

// Override configuration with environment variables
builder.Configuration.AddEnvironmentVariables();

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .WriteTo.Console(outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new()
    {
        Title = "Stack Overflow RAG Assistant API",
        Version = "v1.0",
        Description = "Learning-focused RAG system for answering technical questions with Stack Overflow citations"
    });
});

// Configure options
builder.Services.Configure<IngestionOptions>(builder.Configuration.GetSection(IngestionOptions.SectionName));
builder.Services.Configure<OpenAIOptions>(builder.Configuration.GetSection(OpenAIOptions.SectionName));
builder.Services.Configure<QdrantOptions>(builder.Configuration.GetSection(QdrantOptions.SectionName));

// Register CSV parser
builder.Services.AddSingleton<IStackOverflowCsvParser, StackOverflowCsvParser>();

// Register chunking service
builder.Services.AddSingleton<IChunkingService, ChunkingService>();

// Register embedding service using Microsoft.Extensions.AI
var openAiOptions = builder.Configuration.GetSection(OpenAIOptions.SectionName).Get<OpenAIOptions>();
if (openAiOptions != null && !string.IsNullOrWhiteSpace(openAiOptions.ApiKey))
{
    var kernelBuilder = Kernel.CreateBuilder();
    kernelBuilder.AddOpenAIEmbeddingGenerator(
        modelId: openAiOptions.EmbeddingModel,
        apiKey: openAiOptions.ApiKey);
    var kernel = kernelBuilder.Build();

    // Get the embedding generator from Semantic Kernel
    var embeddingGenerator = kernel.GetRequiredService<IEmbeddingGenerator<string, Embedding<float>>>();

    builder.Services.AddSingleton<IEmbeddingGenerator<string, Embedding<float>>>(embeddingGenerator);
    builder.Services.AddSingleton<IEmbeddingService, OpenAIEmbeddingService>();
}

// Register Qdrant client and repository
var qdrantOptions = builder.Configuration.GetSection(QdrantOptions.SectionName).Get<QdrantOptions>();
Console.WriteLine($"Qdrant options loaded: Host={qdrantOptions?.Host ?? "NULL"}, Collection={qdrantOptions?.CollectionName ?? "NULL"}");

if (qdrantOptions != null && !string.IsNullOrWhiteSpace(qdrantOptions.Host))
{
    Console.WriteLine($"Registering QdrantClient with host: {qdrantOptions.Host}");

    // Parse the host URI to extract components
    var hostUri = new Uri(qdrantOptions.Host);

    // QdrantClient uses gRPC which typically runs on port 6334 (not 6333 REST port)
    // If the configured port is 6333 (REST), use 6334 for gRPC instead
    var grpcPort = hostUri.Port == 6333 ? 6334 : hostUri.Port;

    Console.WriteLine($"Parsed URI - Host: {hostUri.Host}, gRPC Port: {grpcPort}, HTTPS: {hostUri.Scheme == "https"}");

    builder.Services.AddSingleton(sp =>
    {
        return new QdrantClient(
            host: hostUri.Host,
            port: grpcPort,
            https: hostUri.Scheme == "https",
            apiKey: qdrantOptions.ApiKey,
            grpcTimeout: TimeSpan.FromSeconds(30),
            loggerFactory: sp.GetRequiredService<ILoggerFactory>()
        );
    });

    builder.Services.AddSingleton<IVectorStoreRepository>(sp =>
    {
        var client = sp.GetRequiredService<QdrantClient>();
        var logger = sp.GetRequiredService<ILogger<QdrantVectorStoreRepository>>();
        return new QdrantVectorStoreRepository(client, qdrantOptions.CollectionName, logger);
    });
}
else
{
    Console.WriteLine("WARNING: Qdrant configuration is missing or invalid. Vector database features will not be available.");
}

// Register ingestion service
builder.Services.AddSingleton<IIngestionService, IngestionService>();

// TODO: Add remaining service registrations
// builder.Services.AddSingleton<IRetrievalService, RetrievalService>();
// builder.Services.AddSingleton<ILlmService, LlmService>();
// builder.Services.AddSingleton<ITagSuggestionService, TagSuggestionService>();
// builder.Services.AddSingleton<ITelemetryService, TelemetryService>();
// builder.Services.AddSingleton<ICacheService, CacheService>();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Stack Overflow RAG API v1.0");
        options.RoutePrefix = "swagger";
    });
}

// Health check endpoint
app.MapGet("/health", () =>
{
    var response = new
    {
        status = "healthy",
        timestamp = DateTime.UtcNow.ToString("o"),
        service = "Stack Overflow RAG Assistant"
    };
    return Results.Ok(response);
})
.WithName("HealthCheck")
.WithDescription("Health check endpoint to verify API is running")
.WithOpenApi();

// Ingestion endpoint
app.MapPost("/ingest", async (
    IIngestionService ingestionService,
    IngestionRequest? request,
    CancellationToken cancellationToken) =>
{
    try
    {
        var result = await ingestionService.IngestAsync(
            request?.CsvPath,
            request?.MaxRows,
            cancellationToken);

        if (!result.ValidationPassed || result.ErrorMessages.Count > 0)
        {
            return Results.BadRequest(result);
        }

        return Results.Ok(result);
    }
    catch (Exception ex)
    {
        return Results.Problem(
            title: "Ingestion failed",
            detail: ex.Message,
            statusCode: 500);
    }
})
.WithName("Ingest")
.WithDescription("Triggers full ingestion pipeline: CSV → Parse → Chunk → Embed → Upsert")
.WithOpenApi();

app.Run();
