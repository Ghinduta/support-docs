using DotNetEnv;
using Microsoft.Extensions.AI;
using Microsoft.SemanticKernel;
using Qdrant.Client;
using Serilog;
using StackExchange.Redis;
using StackOverflowRAG.Core.Configuration;
using StackOverflowRAG.Core.Helpers;
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

// Configure and register retrieval service
builder.Services.Configure<RetrievalOptions>(
    builder.Configuration.GetSection(RetrievalOptions.SectionName));
builder.Services.AddSingleton<IRetrievalService, RetrievalService>();

// Configure and register LLM service
builder.Services.Configure<LlmOptions>(
    builder.Configuration.GetSection(LlmOptions.SectionName));

if (openAiOptions != null && !string.IsNullOrWhiteSpace(openAiOptions.ApiKey))
{
    // Create a Kernel for chat completion (LLM service)
    var llmKernelBuilder = Kernel.CreateBuilder();
    llmKernelBuilder.AddOpenAIChatCompletion(
        modelId: builder.Configuration.GetValue<string>("Llm:ModelName") ?? "gpt-4o-mini",
        apiKey: openAiOptions.ApiKey);
    var llmKernel = llmKernelBuilder.Build();

    builder.Services.AddSingleton(llmKernel);
    builder.Services.AddSingleton<ILlmService, LlmService>();
}

// Configure and register Redis cache service
builder.Services.Configure<RedisOptions>(
    builder.Configuration.GetSection(RedisOptions.SectionName));

var redisOptions = builder.Configuration.GetSection(RedisOptions.SectionName).Get<RedisOptions>();
if (redisOptions != null && redisOptions.Enabled && !string.IsNullOrWhiteSpace(redisOptions.ConnectionString))
{
    Console.WriteLine($"Registering Redis with connection: {redisOptions.ConnectionString}");
    builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
    {
        var configuration = ConfigurationOptions.Parse(redisOptions.ConnectionString);
        configuration.AbortOnConnectFail = false; // Don't fail if Redis is down
        return ConnectionMultiplexer.Connect(configuration);
    });
    builder.Services.AddSingleton<ICacheService, RedisCacheService>();
}
else
{
    Console.WriteLine("WARNING: Redis is disabled or not configured. Caching features will not be available.");
}

// TODO: Add remaining service registrations
// builder.Services.AddSingleton<ITagSuggestionService, TagSuggestionService>();
// builder.Services.AddSingleton<ITelemetryService, TelemetryService>();

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

// Compare endpoint - shows hybrid vs vector-only side-by-side
app.MapGet("/search/compare", async (
    IRetrievalService retrievalService,
    string query,
    int topK = 5,
    CancellationToken cancellationToken = default) =>
{
    try
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return Results.BadRequest(new { error = "Query parameter is required" });
        }

        // Run both searches in parallel
        var hybridTask = retrievalService.HybridSearchAsync(query, topK, useHybrid: true, cancellationToken);
        var vectorTask = retrievalService.SearchAsync(query, topK, cancellationToken);

        await Task.WhenAll(hybridTask, vectorTask);

        var hybridResults = hybridTask.Result;
        var vectorResults = vectorTask.Result;

        return Results.Ok(new
        {
            query,
            topK,
            comparison = new
            {
                hybrid = new
                {
                    resultCount = hybridResults.Count,
                    avgScore = hybridResults.Count > 0 ? hybridResults.Average(r => r.Score) : 0,
                    topScore = hybridResults.Count > 0 ? hybridResults.Max(r => r.Score) : 0,
                    results = hybridResults.Select(r => new
                    {
                        r.ChunkId,
                        r.PostId,
                        r.QuestionTitle,
                        r.Score,
                        chunkPreview = r.ChunkText.Length > 200 ? r.ChunkText.Substring(0, 200) + "..." : r.ChunkText
                    })
                },
                vectorOnly = new
                {
                    resultCount = vectorResults.Count,
                    avgScore = vectorResults.Count > 0 ? vectorResults.Average(r => r.Score) : 0,
                    topScore = vectorResults.Count > 0 ? vectorResults.Max(r => r.Score) : 0,
                    results = vectorResults.Select(r => new
                    {
                        r.ChunkId,
                        r.PostId,
                        r.QuestionTitle,
                        r.Score,
                        chunkPreview = r.ChunkText.Length > 200 ? r.ChunkText.Substring(0, 200) + "..." : r.ChunkText
                    })
                }
            },
            analysis = new
            {
                uniqueToHybrid = hybridResults.Count(h => !vectorResults.Any(v => v.ChunkId == h.ChunkId)),
                uniqueToVector = vectorResults.Count(v => !hybridResults.Any(h => h.ChunkId == v.ChunkId)),
                sharedResults = hybridResults.Count(h => vectorResults.Any(v => v.ChunkId == h.ChunkId)),
                rankingDifference = hybridResults.Count > 0 && vectorResults.Count > 0
                    ? Math.Abs(hybridResults[0].Score - vectorResults[0].Score)
                    : 0
            }
        });
    }
    catch (Exception ex)
    {
        return Results.Problem(
            title: "Comparison failed",
            detail: ex.Message,
            statusCode: 500);
    }
})
.WithName("CompareSearch")
.WithDescription("Compare hybrid vs vector-only search side-by-side")
.WithOpenApi();

// Search endpoint (temporary for testing Story 2.2)
app.MapGet("/search", async (
    IRetrievalService retrievalService,
    string query,
    int topK = 5,
    bool useHybrid = true,
    CancellationToken cancellationToken = default) =>
{
    try
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return Results.BadRequest(new { error = "Query parameter is required" });
        }

        var results = useHybrid
            ? await retrievalService.HybridSearchAsync(query, topK, useHybrid, cancellationToken)
            : await retrievalService.SearchAsync(query, topK, cancellationToken);

        return Results.Ok(new
        {
            query,
            topK,
            searchType = useHybrid ? "hybrid" : "vector-only",
            resultCount = results.Count,
            results = results.Select(r => new
            {
                r.ChunkId,
                r.PostId,
                r.QuestionTitle,
                r.Score,
                chunkPreview = r.ChunkText.Length > 200 ? r.ChunkText.Substring(0, 200) + "..." : r.ChunkText
            })
        });
    }
    catch (Exception ex)
    {
        return Results.Problem(
            title: "Search failed",
            detail: ex.Message,
            statusCode: 500);
    }
})
.WithName("Search")
.WithDescription("Test search endpoint - supports both vector-only and hybrid search")
.WithOpenApi();

// Ask endpoint - full RAG pipeline with streaming, caching, and citations (Story 2.5)
app.MapGet("/ask", async (
    IRetrievalService retrievalService,
    ILlmService llmService,
    ICacheService? cacheService,
    string question,
    int topK = 5,
    bool useHybrid = true,
    CancellationToken cancellationToken = default) =>
{
    if (string.IsNullOrWhiteSpace(question))
    {
        return Results.BadRequest(new { error = "Question parameter is required" });
    }

    try
    {
        // Generate cache key for response
        var cacheKey = CacheKeyHelper.GenerateResponseKey(question, topK, useHybrid);

        // Check cache first
        string? cachedResponse = null;
        if (cacheService != null)
        {
            cachedResponse = await cacheService.GetAsync(cacheKey, cancellationToken);
        }

        if (cachedResponse != null)
        {
            // Cache hit - stream from cache
            return Results.Stream(async responseStream =>
            {
                await using var writer = new StreamWriter(responseStream, leaveOpen: true);
                await writer.WriteAsync(cachedResponse);
                await writer.FlushAsync();
            }, "text/event-stream");
        }

        // Cache miss - retrieve chunks and generate response
        var chunks = useHybrid
            ? await retrievalService.HybridSearchAsync(question, topK, useHybrid, cancellationToken)
            : await retrievalService.SearchAsync(question, topK, cancellationToken);

        if (chunks.Count == 0)
        {
            return Results.Ok(new
            {
                question,
                answer = "I couldn't find any relevant information in the Stack Overflow database to answer your question.",
                citations = Array.Empty<object>()
            });
        }

        var citations = CitationHelper.ExtractCitations(chunks, maxCitations: 5);

        // Stream response and accumulate for caching
        return Results.Stream(async responseStream =>
        {
            await using var writer = new StreamWriter(responseStream, leaveOpen: true);
            var responseBuilder = new System.Text.StringBuilder();

            // Write metadata
            var metadataLine = $"data: {{\"type\":\"metadata\",\"question\":\"{question}\",\"chunkCount\":{chunks.Count},\"searchType\":\"{(useHybrid ? "hybrid" : "vector-only")}\",\"cached\":false}}\n";
            await writer.WriteLineAsync(metadataLine);
            await writer.FlushAsync();
            responseBuilder.AppendLine(metadataLine);

            // Stream LLM response
            await foreach (var chunk in llmService.StreamAnswerAsync(question, chunks, cancellationToken))
            {
                var textLine = $"data: {{\"type\":\"text\",\"content\":\"{chunk.Replace("\"", "\\\"").Replace("\n", "\\n")}\"}}\n\n";
                await writer.WriteAsync(textLine);
                await writer.FlushAsync();
                responseBuilder.Append(textLine);
            }

            // Send citations
            var citationsJson = System.Text.Json.JsonSerializer.Serialize(citations);
            var citationsLine = $"data: {{\"type\":\"citations\",\"sources\":{citationsJson}}}\n";
            await writer.WriteLineAsync(citationsLine);
            await writer.FlushAsync();
            responseBuilder.AppendLine(citationsLine);

            // Send completion marker
            var doneLine = "data: {\"type\":\"done\"}\n";
            await writer.WriteLineAsync(doneLine);
            await writer.FlushAsync();
            responseBuilder.AppendLine(doneLine);

            // Cache the complete response
            if (cacheService != null)
            {
                var redisOptions = builder.Configuration.GetSection(RedisOptions.SectionName).Get<RedisOptions>();
                var ttl = redisOptions?.GetTtl() ?? TimeSpan.FromHours(24);
                await cacheService.SetAsync(cacheKey, responseBuilder.ToString(), ttl, cancellationToken);
            }
        }, "text/event-stream");
    }
    catch (Exception ex)
    {
        return Results.Problem(
            title: "Ask failed",
            detail: ex.Message,
            statusCode: 500);
    }
})
.WithName("AskGet")
.WithDescription("Full RAG pipeline: retrieves context, streams LLM answer with Redis caching, and provides citations (GET version)")
.WithOpenApi();

// POST /ask - Main RAG endpoint with JSON body (Story 2.6)
app.MapPost("/ask", async (
    IRetrievalService retrievalService,
    ILlmService llmService,
    ICacheService? cacheService,
    QueryRequest request,
    CancellationToken cancellationToken) =>
{
    var startTime = System.Diagnostics.Stopwatch.StartNew();

    if (string.IsNullOrWhiteSpace(request.Question))
    {
        return Results.BadRequest(new { error = "Question is required" });
    }

    try
    {
        // Generate cache key
        var cacheKey = CacheKeyHelper.GenerateResponseKey(request.Question, request.TopK, request.UseHybrid);

        // Check cache
        string? cachedResponse = null;
        if (cacheService != null)
        {
            cachedResponse = await cacheService.GetAsync(cacheKey, cancellationToken);
        }

        if (cachedResponse != null)
        {
            startTime.Stop();
            // Cache hit - return cached response with metadata update
            return Results.Stream(async responseStream =>
            {
                await using var writer = new StreamWriter(responseStream, leaveOpen: true);

                // Add cache hit metadata marker
                await writer.WriteLineAsync($"data: {{\"type\":\"metadata\",\"question\":\"{request.Question}\",\"cacheHit\":true,\"latencyMs\":{startTime.ElapsedMilliseconds}}}\n");
                await writer.FlushAsync();

                // Stream cached content (skip first metadata line from cache)
                var lines = cachedResponse.Split('\n').Skip(1);
                foreach (var line in lines)
                {
                    if (!string.IsNullOrWhiteSpace(line))
                    {
                        await writer.WriteLineAsync(line);
                        await writer.FlushAsync();
                    }
                }
            }, "text/event-stream");
        }

        // Cache miss - full pipeline
        var chunks = request.UseHybrid
            ? await retrievalService.HybridSearchAsync(request.Question, request.TopK, request.UseHybrid, cancellationToken)
            : await retrievalService.SearchAsync(request.Question, request.TopK, cancellationToken);

        if (chunks.Count == 0)
        {
            return Results.Ok(new
            {
                question = request.Question,
                answer = "I couldn't find any relevant information in the Stack Overflow database to answer your question.",
                citations = Array.Empty<object>()
            });
        }

        var citations = CitationHelper.ExtractCitations(chunks, maxCitations: 5);

        // Stream response with enhanced metadata
        return Results.Stream(async responseStream =>
        {
            await using var writer = new StreamWriter(responseStream, leaveOpen: true);
            var responseBuilder = new System.Text.StringBuilder();
            var answerBuilder = new System.Text.StringBuilder();

            // Build prompt for token estimation
            var promptText = $"Context: {string.Join(" ", chunks.Select(c => c.ChunkText))}\nQuestion: {request.Question}";
            var promptTokens = CostEstimator.EstimateTokens(promptText);

            // Write initial metadata
            var metadataLine = $"data: {{\"type\":\"metadata\",\"question\":\"{request.Question}\",\"chunkCount\":{chunks.Count},\"searchType\":\"{(request.UseHybrid ? "hybrid" : "vector-only")}\",\"cacheHit\":false}}\n";
            await writer.WriteLineAsync(metadataLine);
            await writer.FlushAsync();
            responseBuilder.AppendLine(metadataLine);

            // Stream LLM response
            await foreach (var chunk in llmService.StreamAnswerAsync(request.Question, chunks, cancellationToken))
            {
                answerBuilder.Append(chunk);
                var textLine = $"data: {{\"type\":\"text\",\"content\":\"{chunk.Replace("\"", "\\\"").Replace("\n", "\\n")}\"}}\n\n";
                await writer.WriteAsync(textLine);
                await writer.FlushAsync();
                responseBuilder.Append(textLine);
            }

            // Send citations
            var citationsJson = System.Text.Json.JsonSerializer.Serialize(citations);
            var citationsLine = $"data: {{\"type\":\"citations\",\"sources\":{citationsJson}}}\n";
            await writer.WriteLineAsync(citationsLine);
            await writer.FlushAsync();
            responseBuilder.AppendLine(citationsLine);

            // Calculate final metadata
            startTime.Stop();
            var completionTokens = CostEstimator.EstimateTokens(answerBuilder.ToString());
            var totalTokens = promptTokens + completionTokens;
            var estimatedCost = CostEstimator.EstimateLlmCost(promptTokens, completionTokens);

            // Send final metadata
            var finalMetadata = new
            {
                type = "final_metadata",
                latencyMs = startTime.ElapsedMilliseconds,
                tokensUsed = totalTokens,
                estimatedCost = Math.Round(estimatedCost, 6),
                cacheHit = false,
                retrievedChunks = chunks.Count
            };
            var finalMetadataJson = System.Text.Json.JsonSerializer.Serialize(finalMetadata);
            var finalMetadataLine = $"data: {finalMetadataJson}\n";
            await writer.WriteLineAsync(finalMetadataLine);
            await writer.FlushAsync();
            responseBuilder.AppendLine(finalMetadataLine);

            // Send completion marker
            var doneLine = "data: {\"type\":\"done\"}\n";
            await writer.WriteLineAsync(doneLine);
            await writer.FlushAsync();
            responseBuilder.AppendLine(doneLine);

            // Cache the response
            if (cacheService != null)
            {
                var redisOptions = builder.Configuration.GetSection(RedisOptions.SectionName).Get<RedisOptions>();
                var ttl = redisOptions?.GetTtl() ?? TimeSpan.FromHours(24);
                await cacheService.SetAsync(cacheKey, responseBuilder.ToString(), ttl, cancellationToken);
            }
        }, "text/event-stream");
    }
    catch (Exception ex)
    {
        return Results.Problem(
            title: "Ask failed",
            detail: ex.Message,
            statusCode: 500);
    }
})
.WithName("Ask")
.WithDescription("Full RAG pipeline with POST JSON body: retrieves context, streams LLM answer with caching, citations, and enhanced metadata (Story 2.6)")
.WithOpenApi();

app.Run();
