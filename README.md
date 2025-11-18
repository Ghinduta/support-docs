# Stack Overflow RAG Assistant

A learning-focused RAG (Retrieval Augmented Generation) system that answers technical questions with grounded responses and citations from Stack Overflow data. Built with .NET 8, demonstrating modern LLM integration, vector search, and mini MLOps patterns.

## Features

- **RAG Pipeline:** CSV ingestion → chunking → embeddings → vector storage → hybrid search → LLM synthesis
- **Streaming Responses:** Real-time answer generation with Server-Sent Events
- **Source Citations:** Every answer includes 3-5 Stack Overflow post citations
- **Tag Suggestion:** ML.NET-based multi-label classification for Stack Overflow tags
- **Hybrid Search:** Combines vector similarity and keyword matching for better retrieval
- **Redis Caching:** TTL-based caching for embeddings and responses
- **Comprehensive Telemetry:** Logs latency, token usage, cost estimates, and cache performance
- **Swagger UI:** Interactive API documentation and testing

## Prerequisites

- **Docker Desktop** 24+ with Docker Compose
- **.NET 8 SDK** (8.0.100 or later)
- **OpenAI API Key** (get one at [platform.openai.com](https://platform.openai.com/api-keys))

## Quick Start

### 1. Clone and Configure

```bash
# Clone the repository
git clone <repo-url>
cd support-docs

# Copy environment template
cp .env.example .env

# Edit .env and add your OpenAI API key
# OPENAI_API_KEY=sk-your-key-here
```

### 2. Start Services

```bash
# Start all services (API, Qdrant, Redis)
docker compose up -d

# Check all services are running
docker compose ps
```

### 3. Access the API

- **API:** http://localhost:5000
- **Swagger UI:** http://localhost:5000/swagger
- **Health Check:** http://localhost:5000/health
- **Qdrant Dashboard:** http://localhost:6333/dashboard

### 4. Ingest Data

The system expects the Kaggle StackSample dataset with three CSV files:
- `Questions.csv` - Stack Overflow questions
- `Answers.csv` - Answers to questions
- `Tags.csv` - Tags associated with questions

Download from [Kaggle StackSample](https://www.kaggle.com/datasets/stackoverflow/stacksample) and configure the directory path in `.env`.

Then POST to `/ingest` via Swagger UI at http://localhost:5000/swagger

## Environment Variables

Configure these in your `.env` file:

| Variable | Description | Default |
|----------|-------------|---------|
| `OPENAI_API_KEY` | OpenAI API key (required) | - |
| `OPENAI_EMBEDDING_MODEL` | Embedding model | `text-embedding-3-small` |
| `OPENAI_COMPLETION_MODEL` | LLM model | `gpt-4o-mini` |
| `QDRANT_HOST` | Qdrant endpoint | `http://localhost:6333` |
| `REDIS_CONNECTION_STRING` | Redis connection | `localhost:6379` |
| `REDIS_CACHE_TTL_HOURS` | Cache TTL | `24` |
| `CSV_BASE_PATH` | Directory containing Questions.csv, Answers.csv, Tags.csv | `C:\Users\Nicky\Documents\kaggle\stacksample` |
| `MAX_ROWS` | Max questions to ingest | `10000` |
| `CHUNK_SIZE` | Tokens per chunk | `500` |
| `CHUNK_OVERLAP` | Overlap between chunks | `50` |
| `DEFAULT_TOP_K` | Chunks to retrieve | `10` |
| `VECTOR_WEIGHT` | Vector search weight | `0.5` |
| `KEYWORD_WEIGHT` | Keyword search weight | `0.5` |

## API Endpoints

### Health Check
```
GET /health
```
Returns API status and timestamp.

### Ingest Data
```
POST /ingest
Content-Type: application/json

{
  "csvPath": "C:/Users/Nicky/Documents/kaggle/stacksample",
  "maxRows": 10000
}
```

Loads and joins Questions.csv, Answers.csv, and Tags.csv from the specified directory.

### Ask Question
```
POST /ask
Content-Type: application/json

{
  "question": "How to use async/await in C#?",
  "topK": 5,
  "useHybrid": true
}
```
Returns streaming response with LLM-generated answer, source citations, latency metrics, token usage, and cost estimates. Supports Redis caching for improved performance.

### Suggest Tags
```
POST /tags/suggest
Content-Type: application/json

{
  "title": "How to use async/await",
  "body": "I'm trying to understand async programming...",
  "topK": 5
}
```
Returns suggested Stack Overflow tags using ML.NET classifier (TF-IDF + multi-label classification).

### Train Tag Model
```
POST /tags/train
Content-Type: application/json

{
  "csvPath": "data/stacksample.csv",
  "maxRows": 10000,
  "testSplitRatio": 0.2
}
```
Trains a new tag classification model from Stack Overflow data.

## Project Structure

```
StackOverflowRAG/
├── src/
│   ├── StackOverflowRAG.Api/          # Minimal API (endpoints, Program.cs)
│   ├── StackOverflowRAG.Core/         # Business logic and service interfaces
│   ├── StackOverflowRAG.Data/         # Data models and repositories
│   ├── StackOverflowRAG.ML/           # ML.NET tag suggestion
│   └── StackOverflowRAG.Tests/        # Unit and integration tests
├── docs/                               # Documentation (PRD, architecture, stories)
├── docker-compose.yml                  # Service orchestration
├── Dockerfile                          # API container definition
├── .env.example                        # Environment variable template
└── README.md                           # This file
```

## Development

### Build Solution
```bash
dotnet build
```

### Run Tests
```bash
dotnet test
```

### Run API Locally (without Docker)
```bash
# Start Qdrant and Redis
docker compose up -d qdrant redis

# Run API
dotnet run --project src/StackOverflowRAG.Api
```

### Stop All Services
```bash
docker compose down
```

## Architecture

This is a **learning project** demonstrating:

- **RAG Pipeline:** Chunking, embeddings (OpenAI text-embedding-3-small), hybrid search (vector + keyword)
- **Vector Database:** Qdrant for storing and searching embeddings
- **LLM Integration:** Semantic Kernel with streaming responses (GPT-4o-mini)
- **Caching:** Redis for query and response caching
- **ML:** ML.NET for TF-IDF + logistic regression tag prediction
- **Observability:** Structured logging with Serilog, telemetry for latency/tokens/cost
- **Deployment:** Docker Compose for reproducible local environment

**Key Decisions:**
- Qdrant (simple Docker deployment, good .NET client)
- Semantic Kernel (official Microsoft LLM library)
- ML.NET (pure .NET, no microservice complexity)
- Swagger (zero-effort UI for testing)

See `docs/architecture/technical-decisions.md` for detailed rationale.

## Performance Metrics

Observed performance on local development environment (Windows 11, Docker Desktop):

**Query Latency:**
- Cache hit: ~10-50ms
- Cache miss (hybrid search + LLM): ~1-3 seconds
- Embeddings generation: ~200-500ms per batch
- Vector search: ~50-100ms (10K vectors)

**Cost Estimates (OpenAI):**
- Embedding (text-embedding-3-small): ~$0.00002 per 1K tokens
- LLM response (gpt-4o-mini): ~$0.00015 input + $0.0006 output per 1K tokens
- Typical question: ~$0.0003-0.0008 per query (without caching)

**Cache Performance:**
- Redis TTL: 24 hours (configurable)
- Cache hit rate: 60-80% for repeated questions
- Cost savings: ~90% reduction for cached queries

**Tag Suggestion:**
- Model training: ~10-30 seconds for 10K questions
- Inference latency: ~50-150ms per question
- Model accuracy: Varies by dataset quality and tag frequency

## Future Improvements

Potential enhancements for learning or production use:

**Retrieval Quality:**
- [ ] Implement reranking model (e.g., cross-encoder)
- [ ] Add query expansion and reformulation
- [ ] Semantic chunking (respect code blocks, paragraphs)
- [ ] Metadata filtering (tags, dates, scores)

**ML & Models:**
- [ ] Transformer-based tag classifier (BERT, RoBERTa)
- [ ] Fine-tune embedding model on Stack Overflow data
- [ ] Question similarity detection (avoid duplicates)

**Production Readiness:**
- [ ] Authentication and rate limiting
- [ ] CI/CD pipeline with automated tests
- [ ] Kubernetes deployment manifests
- [ ] Monitoring and alerting (Prometheus, Grafana)
- [ ] A/B testing framework for retrieval strategies

**User Experience:**
- [ ] Web UI frontend (React/Vue)
- [ ] Conversation history and follow-up questions
- [ ] User feedback collection (thumbs up/down)
- [ ] Export answers to markdown/PDF

## Learning Goals

This project demonstrates:

1. **RAG Fundamentals:** End-to-end retrieval augmented generation
2. **.NET 8 Patterns:** Minimal APIs, dependency injection, async/await
3. **LLM Integration:** Streaming, structured outputs, cost tracking
4. **Vector Search:** Embeddings, similarity search, hybrid retrieval
5. **ML Basics:** TF-IDF, multi-label classification, model training
6. **MLOps:** Docker orchestration, caching, telemetry, configuration management

## Known Limitations (Learning Project)

- **Local execution only** (not production-ready)
- **No authentication** (single user assumed)
- **Simple chunking** (fixed size, no semantic boundaries)
- **Basic error handling** (focused on happy path)
- **Manual deployment** (no CI/CD)

## Documentation

- **Project Brief:** `docs/brief.md`
- **PRD:** `docs/prd.md`
- **Architecture:** `docs/architecture.md`
- **Technical Decisions:** `docs/architecture/technical-decisions.md`
- **User Stories:** `docs/stories/README.md`

## License

This is a learning project. Use it to learn, experiment, and build upon!

---

**Current Status:** Architecture updated for three-CSV Kaggle StackSample structure

**Data Format:** Questions.csv + Answers.csv + Tags.csv (joined during ingestion)
