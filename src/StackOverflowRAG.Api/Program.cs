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

// Register ingestion service (only if embedding service is available)
if (openAiOptions != null && !string.IsNullOrWhiteSpace(openAiOptions.ApiKey))
{
    builder.Services.AddSingleton<IIngestionService, IngestionService>();
}
else
{
    Console.WriteLine("WARNING: OpenAI API key not configured. Ingestion and RAG features will not be available.");
}

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

// Register tag suggestion service
builder.Services.Configure<TagSuggestionOptions>(
    builder.Configuration.GetSection(TagSuggestionOptions.SectionName));
builder.Services.AddSingleton<ITagSuggestionService, TagSuggestionService>();

// Register telemetry service
builder.Services.AddSingleton<ITelemetryService, TelemetryService>();

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
.WithOpenApi(operation =>
{
    operation.Summary = "Check API health status";
    return operation;
});

// Ingestion endpoint
app.MapPost("/ingest", async (
    IIngestionService? ingestionService,
    IngestionRequest? request,
    CancellationToken cancellationToken) =>
{
    if (ingestionService == null)
    {
        return Results.Problem(
            title: "Ingestion not available",
            detail: "Ingestion service requires OpenAI API key. Please configure OpenAI__ApiKey in .env file.",
            statusCode: 503);
    }

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
.WithOpenApi(operation =>
{
    operation.Summary = "Ingest Stack Overflow data from CSV";
    operation.RequestBody.Description = "Ingestion configuration";

    var mediaType = operation.RequestBody.Content["application/json"];
    mediaType.Example = new Microsoft.OpenApi.Any.OpenApiObject
    {
        ["csvPath"] = new Microsoft.OpenApi.Any.OpenApiString("data/stacksample"),
        ["maxRows"] = new Microsoft.OpenApi.Any.OpenApiInteger(10000)
    };

    return operation;
});

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
.WithOpenApi(operation =>
{
    operation.Summary = "Compare hybrid and vector-only search results";

    // Add parameter descriptions
    operation.Parameters[0].Description = "Search query (e.g., 'How to sort a list in Python?')";
    operation.Parameters[0].Example = new Microsoft.OpenApi.Any.OpenApiString("How to sort a list in Python?");

    operation.Parameters[1].Description = "Number of results to return from each search method";
    operation.Parameters[1].Example = new Microsoft.OpenApi.Any.OpenApiInteger(5);

    return operation;
});

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
.WithOpenApi(operation =>
{
    operation.Summary = "Search Stack Overflow knowledge base";

    // Add parameter descriptions
    operation.Parameters[0].Description = "Search query (e.g., 'How to handle exceptions in C#?')";
    operation.Parameters[0].Example = new Microsoft.OpenApi.Any.OpenApiString("How to handle exceptions in C#?");

    operation.Parameters[1].Description = "Number of results to return (default: 5)";
    operation.Parameters[1].Example = new Microsoft.OpenApi.Any.OpenApiInteger(5);

    operation.Parameters[2].Description = "Use hybrid search (BM25 + vector) or vector-only (default: true)";
    operation.Parameters[2].Example = new Microsoft.OpenApi.Any.OpenApiBoolean(true);

    return operation;
});

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
.WithOpenApi(operation =>
{
    operation.Summary = "Ask a question (GET with streaming)";

    // Add parameter descriptions
    operation.Parameters[0].Description = "Question to ask (e.g., 'What is the best way to handle async/await in JavaScript?')";
    operation.Parameters[0].Example = new Microsoft.OpenApi.Any.OpenApiString("What is the best way to handle async/await in JavaScript?");

    operation.Parameters[1].Description = "Number of context chunks to retrieve (default: 5)";
    operation.Parameters[1].Example = new Microsoft.OpenApi.Any.OpenApiInteger(5);

    operation.Parameters[2].Description = "Use hybrid search for context retrieval (default: true)";
    operation.Parameters[2].Example = new Microsoft.OpenApi.Any.OpenApiBoolean(true);

    return operation;
});

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
            // Cache hit - return cached response with updated metadata
            return Results.Stream(async responseStream =>
            {
                await using var writer = new StreamWriter(responseStream, leaveOpen: true);

                // Add cache hit metadata marker
                await writer.WriteLineAsync($"data: {{\"type\":\"metadata\",\"question\":\"{request.Question}\",\"cacheHit\":true,\"latencyMs\":{startTime.ElapsedMilliseconds}}}\n");
                await writer.FlushAsync();

                // Stream cached content (skip first metadata line and final_metadata from cache)
                var lines = cachedResponse.Split('\n').Skip(1);
                foreach (var line in lines)
                {
                    if (!string.IsNullOrWhiteSpace(line) && !line.Contains("\"type\":\"final_metadata\""))
                    {
                        await writer.WriteLineAsync(line);
                        await writer.FlushAsync();
                    }
                }

                // Send updated final_metadata with cache hit
                var finalMetadata = new
                {
                    type = "final_metadata",
                    latencyMs = startTime.ElapsedMilliseconds,
                    tokensUsed = 0,
                    estimatedCost = 0.0,
                    cacheHit = true,
                    retrievedChunks = 0
                };
                var finalMetadataJson = System.Text.Json.JsonSerializer.Serialize(finalMetadata);
                await writer.WriteLineAsync($"data: {finalMetadataJson}\n");
                await writer.FlushAsync();
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
.WithOpenApi(operation =>
{
    operation.Summary = "Ask a question (POST with JSON body)";
    operation.RequestBody.Description = "Question and search configuration";

    var mediaType = operation.RequestBody.Content["application/json"];
    mediaType.Example = new Microsoft.OpenApi.Any.OpenApiObject
    {
        ["question"] = new Microsoft.OpenApi.Any.OpenApiString("What is the best way to handle async/await in JavaScript?"),
        ["topK"] = new Microsoft.OpenApi.Any.OpenApiInteger(5),
        ["useHybrid"] = new Microsoft.OpenApi.Any.OpenApiBoolean(true)
    };

    return operation;
});

// POST /tags/suggest - Tag suggestion endpoint (Story 3.3)
app.MapPost("/tags/suggest", async (
    ITagSuggestionService tagSuggestionService,
    ITelemetryService? telemetryService,
    TagSuggestionRequest request,
    CancellationToken cancellationToken) =>
{
    var startTime = System.Diagnostics.Stopwatch.StartNew();

    try
    {
        // Validate input
        if (string.IsNullOrWhiteSpace(request.Title) && string.IsNullOrWhiteSpace(request.Body))
        {
            return Results.BadRequest(new { error = "Either title or body must be provided" });
        }

        if (request.TopK < 1 || request.TopK > 10)
        {
            return Results.BadRequest(new { error = "TopK must be between 1 and 10" });
        }

        // Get tag suggestions
        var response = await tagSuggestionService.SuggestTagsAsync(
            request.Title,
            request.Body,
            request.TopK,
            cancellationToken);

        // Log telemetry
        startTime.Stop();
        if (telemetryService != null)
        {
            var inputText = $"{request.Title} {request.Body}";
            telemetryService.LogTagMetrics(new TagMetadata
            {
                InputLength = inputText.Length,
                PredictedTags = response.Tags,
                LatencyMs = startTime.ElapsedMilliseconds,
                TopK = request.TopK,
                ModelLoaded = true
            });
        }

        return Results.Ok(response);
    }
    catch (InvalidOperationException ex) when (ex.Message.Contains("not initialized"))
    {
        return Results.Problem(
            title: "Tag suggestion service not available",
            detail: "The tag classifier model is not loaded. Please ensure the model file exists and the service is initialized.",
            statusCode: 503);
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
    catch (Exception ex)
    {
        return Results.Problem(
            title: "Tag suggestion failed",
            detail: ex.Message,
            statusCode: 500);
    }
})
.WithName("SuggestTags")
.WithDescription("Suggests Stack Overflow tags for a question using ML.NET classifier (Story 3.3)")
.WithOpenApi(operation =>
{
    operation.Summary = "Suggest tags for a Stack Overflow question";
    operation.RequestBody.Description = "Question details for tag prediction";

    // Add request example
    var mediaType = operation.RequestBody.Content["application/json"];
    mediaType.Example = new Microsoft.OpenApi.Any.OpenApiObject
    {
        ["title"] = new Microsoft.OpenApi.Any.OpenApiString("How to sort a list in Python?"),
        ["body"] = new Microsoft.OpenApi.Any.OpenApiString("I have a list of numbers [3, 1, 4, 1, 5] and I want to sort them in ascending order. What's the best way to do this?"),
        ["topK"] = new Microsoft.OpenApi.Any.OpenApiInteger(5)
    };

    return operation;
});

// POST /tags/train - Train tag classifier model
app.MapPost("/tags/train", async (
    ITagSuggestionService tagSuggestionService,
    TagTrainingRequest request,
    CancellationToken cancellationToken) =>
{
    try
    {
        Log.Information("Starting tag classifier training via API...");

        var result = await tagSuggestionService.TrainModelAsync(request, cancellationToken);

        if (result.Success)
        {
            return Results.Ok(result);
        }
        else
        {
            return Results.BadRequest(result);
        }
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Tag classifier training failed");
        return Results.Problem(
            title: "Training failed",
            detail: ex.Message,
            statusCode: 500);
    }
})
.WithName("TrainTagClassifier")
.WithDescription("Train a new tag classifier model from Stack Overflow CSV data")
.WithOpenApi(operation =>
{
    operation.Summary = "Train tag classifier model";
    operation.RequestBody.Description = "Training configuration and data source";

    // Add request example
    var mediaType = operation.RequestBody.Content["application/json"];
    mediaType.Example = new Microsoft.OpenApi.Any.OpenApiObject
    {
        ["csvPath"] = new Microsoft.OpenApi.Any.OpenApiString("data/stacksamples"),
        ["maxRows"] = new Microsoft.OpenApi.Any.OpenApiInteger(10000),
        ["testSplitRatio"] = new Microsoft.OpenApi.Any.OpenApiDouble(0.2)
    };

    return operation;
});

// Initialize tag suggestion service on startup
var tagService = app.Services.GetService<ITagSuggestionService>();
if (tagService != null)
{
    try
    {
        await tagService.InitializeAsync();
    }
    catch (Exception ex)
    {
        Log.Warning(ex, "Failed to initialize tag suggestion service. Tag suggestion will not be available.");
    }
}

app.Run();
