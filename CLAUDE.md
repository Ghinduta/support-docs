# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

Stack Overflow RAG assistant: a .NET 8 minimal API that answers questions by retrieving Stack Overflow posts from Qdrant and streaming an OpenAI answer with citations, plus an ML.NET tag suggester and a React chat front end in `web/`. It is a learning project that runs locally only.

## Commands

```bash
docker compose up -d                        # API (:5000 -> container 8080), Qdrant (:6333 REST, :6334 gRPC), Redis (:6379)
docker compose up -d qdrant redis           # infra only, then run the API from source:
dotnet run --project src/StackOverflowRAG.Api
dotnet build
dotnet test
dotnet test --filter "FullyQualifiedName~ChunkingServiceTests"            # one class
dotnet test --filter "FullyQualifiedName~ChunkingServiceTests.SomeMethod" # one test
dotnet script scripts/TrainTagModel.csx <csv-dir> [maxRows] [maxTags]     # offline tag-model training

cd web && npm install && npm run dev        # http://localhost:5173
npm run build                               # tsc -b && vite build
npm run lint                                # oxlint
```

Swagger is at `http://localhost:5000/swagger`. Data must be ingested (`POST /ingest`) before `/ask` or `/search` return anything. `src/StackOverflowRAG.Api/StackOverflowRAG.Api.http` has sample requests.

## Configuration

- Settings bind with .NET's `Section__Property` environment variable names (`OpenAI__ApiKey`, `Qdrant__Host`, `Redis__Enabled`, `Ingestion__CsvPath`, and so on); see `.env.example`. The chat model is `Llm__ModelName` (`OpenAI:ChatModel` exists on `OpenAIOptions` but nothing reads it).
- When the API runs from source, `Program.cs` loads `.env` from two directories above the working directory (the repo root when you start it from `src/StackOverflowRAG.Api`). `docker compose` injects the same file through `env_file` and overrides `Qdrant__Host` and `Ingestion__CsvPath` for the container network (`./data` is mounted at `/data`).
- **Services are registered conditionally** (`Program.cs`, around lines 60–190). Without an OpenAI key, the embedding service, `IIngestionService` and `ILlmService` are not registered. Without a Qdrant host, the vector store is not registered. With Redis disabled, `ICacheService` is not registered. Endpoints therefore resolve these services as optional, so a missing service shows up as a runtime error, not a failure at startup.
- `Qdrant__Host` is the REST URL (port 6333), but the client uses gRPC: port 6333 is rewritten to 6334.
- The tag model loads from `TagSuggestion:ModelPath` (default `models/tag-classifier.zip`), which `POST /tags/train` or the `.csx` script produces.

## Architecture

The projects reference each other in this order: `Api` → `Core` → `Data` + `ML`. `Tests` references all of them.

- **Api**: everything is in `Program.cs`, which holds the DI wiring and every endpoint as an inline lambda (`/health`, `/ingest`, `/search`, `/search/compare`, `GET`/`POST /ask`, `/tags/suggest`, `/tags/train`). There are no controllers.
- **Data**: infrastructure. It contains the CSV parser (it joins the Kaggle StackSample `Questions.csv` + `Answers.csv` + `Tags.csv` and strips HTML with HtmlAgilityPack), token-based chunking, the OpenAI embedding service (Semantic Kernel's `IEmbeddingGenerator`, with Polly retries) and `QdrantVectorStoreRepository`.
- **Core**: orchestration. It contains `IngestionService` (parse → chunk → embed → upsert), `RetrievalService` (vector search, or hybrid vector + keyword search weighted by `RetrievalOptions`), `LlmService` (Semantic Kernel chat completion, streaming), `RedisCacheService`, `TelemetryService` and helpers for cache keys, citations and cost estimation.
- **ML**: `TagClassifier`, an ML.NET TF-IDF + multi-label classifier, wrapped by `Core`'s `TagSuggestionService`.

**The `/ask` streaming contract** matters to the front end. `POST /ask` returns Server-Sent Events: `data: {"type": ...}` lines with the types `metadata`, `text`, `citations`, `final_metadata` and `done`. A cache hit replays the cached answer through the same stream. When no chunks match, the API returns plain JSON instead of a stream. Citations are serialized in PascalCase. `web/src/api.ts` (`streamAsk`) parses all of this, so change both sides together.

**Web**: Vite + React 19 + TypeScript. The dev server proxies `/api/*` to `API_URL` (default `http://localhost:5000`) with the `/api` prefix stripped, so the API needs no CORS setup.

## Tests

xUnit + Moq, organized into folders that mirror the projects (`Core/`, `Data/`, `ML/`). `Validation/GoldenTestSetValidation.cs` measures Hit@K retrieval quality against `TestData/golden-test-set.json`. It is `Skip`ped by default because it needs live infrastructure and ingested data.

## Docs

`docs/` (PRD, architecture, stories) is gitignored and exists only locally, if at all.
