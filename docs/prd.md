# Stack Overflow RAG Assistant - Product Requirements Document (PRD)

## Goals and Background Context

### Goals

- Gain hands-on understanding of RAG pipeline: chunking → embeddings → hybrid search → LLM synthesis with citations
- Learn .NET 8 patterns for LLM streaming, structured outputs, and resilient API integration
- Implement basic MLOps: Docker orchestration, telemetry, caching, cost tracking
- Build measurable system with Hit@5 ≥ 0.7, p50 latency <2s, demonstrable citations
- Document trade-offs (chunk size, top-k, caching strategy) to inform future architecture discussions

### Background Context

This is a learning project to master RAG fundamentals using a realistic Stack Overflow dataset. Most RAG tutorials use toy data or skip operational concerns (caching, metrics, cost tracking). This project implements the full pipeline end-to-end in .NET 8, demonstrating practical patterns for LLM integration, vector search, and lightweight MLOps.

The system answers technical questions with cited Stack Overflow snippets and suggests relevant tags. Built for local execution with `docker compose up`, it prioritizes speed and measurability over production polish. Success means understanding how retrieval quality impacts answers, latency/cost trade-offs, and architectural choices for RAG systems.

### Change Log

| Date | Version | Description | Author |
|------|---------|-------------|--------|
| 2025-11-08 | 1.0 | Initial PRD | John (PM Agent) |

---

## Requirements

### Functional

- **FR1:** System shall provide `/ingest` endpoint that loads Kaggle StackSample CSV, cleans text, chunks documents into ~500 token segments with configurable overlap, generates embeddings, and upserts to vector store
- **FR2:** System shall provide `/ask` endpoint that accepts natural language question, embeds query, performs hybrid search (keyword + vector similarity), retrieves top-k chunks, and streams LLM response
- **FR3:** `/ask` responses shall include 3-5 formatted citations referencing source Stack Overflow posts with titles and links
- **FR4:** System shall provide `/tags/suggest` endpoint that accepts question title and body, and returns top 3-5 predicted Stack Overflow tags using TF-IDF + logistic regression baseline
- **FR5:** System shall cache repeated queries (embeddings and LLM responses) in Redis with configurable TTL
- **FR6:** System shall log telemetry for each request: latency (p50/p95), token counts, estimated cost per request, cache hit/miss
- **FR7:** System shall include simple UI (Blazor or static HTML) or CLI (Spectre.Console) to interact with `/ask` and `/tags/suggest` endpoints
- **FR8:** System shall include 10-20 question/answer pairs as golden test set to validate retrieval quality with Hit@5 metric

### Non-Functional

- **NFR1:** Answer latency p50 shall be <2 seconds (local, uncached queries)
- **NFR2:** Answer latency p95 shall be <5 seconds (local, uncached queries)
- **NFR3:** Retrieval Hit@5 on golden test set shall be ≥0.7
- **NFR4:** System shall run entirely via `docker compose up` with no additional manual setup beyond environment variables
- **NFR5:** Data ingestion for 10k-50k documents shall complete in <10 minutes
- **NFR6:** System shall support 10+ concurrent queries without performance degradation
- **NFR7:** Average tokens per request shall be logged and remain <4k context
- **NFR8:** Cache hit rate on repeated queries shall be >50% (validates caching effectiveness)
- **NFR9:** All API keys shall be loaded from environment variables, never committed to repository
- **NFR10:** System shall favor small models and short contexts to optimize for local execution speed

---

## User Interface Design Goals

### Overall UX Vision

Learning-focused simplicity: functional interface for testing RAG pipeline, not production polish. Primary goal is rapid iteration and validation, not aesthetic refinement. Interface should make it easy to ask questions, see streaming responses with citations, and validate tag suggestions.

### Key Interaction Paradigms

- **Question Entry:** Simple text input field for natural language questions
- **Streaming Responses:** Real-time display of LLM answer as it generates, with citations appearing inline or at the end
- **Tag Suggestions:** Display top 3-5 tags with confidence scores or simply as labels
- **Metrics Visibility:** Optional display of latency, token count, cache hit for each query (useful for learning)

### Core Screens and Views

1. **Main Query Interface:** Text input for question, streaming answer display, citation list
2. **Tag Suggestion View:** Text input for question title/body, tag output display
3. **Ingestion Status:** (Optional) Simple status page showing ingestion progress and document count
4. **Metrics Dashboard:** (Optional, Phase 2) Display p50/p95 latency trends, cache hit rates, cost tracking

### Accessibility

None - learning project, no accessibility requirements for MVP.

### Branding

None - minimal styling, focus on functionality. Use default Bootstrap or basic CSS for readability.

### Target Device and Platforms

Web Responsive (any browser) OR CLI (cross-platform terminal). Choose whichever is faster to implement.

---

## Technical Assumptions

### Repository Structure

**Monorepo** - single repository with all components.

Suggested project structure:
- `src/StackOverflowRAG.Api/` - .NET 8 minimal API
- `src/StackOverflowRAG.Core/` - Business logic, services
- `src/StackOverflowRAG.Data/` - Data ingestion, CSV parsing
- `src/StackOverflowRAG.ML/` - Tag suggestion (TF-IDF + sklearn)
- `docker-compose.yml` - Orchestration for API, Qdrant, Redis, (optional) Ollama

### Service Architecture

**Monolith** within the minimal API, using dependency injection for services:
- `IngestionService` - Handles CSV loading, chunking, embedding, vector store upsert
- `RetrievalService` - Embeds queries, performs hybrid search, retrieves top-k chunks
- `LlmService` - Manages LLM API calls, streaming responses, citation formatting
- `TagSuggestionService` - TF-IDF feature extraction + logistic regression inference
- `TelemetryService` - Logs latency, tokens, cost, cache metrics
- `CacheService` - Redis integration for query/embedding/response caching

Docker Compose orchestrates:
- .NET 8 minimal API container
- Qdrant vector store container
- Redis cache container
- (Optional) Ollama container for local LLM

### Testing Requirements

**Unit + Integration** - Focus on critical paths:
- Unit tests for chunking logic, embedding generation, cache key generation
- Integration tests for vector store operations, LLM streaming, tag suggestion
- Manual golden test set validation (Hit@5 metric) - no automated E2E for MVP
- No comprehensive test pyramid; prioritize testing retrieval and LLM integration

### Additional Technical Assumptions and Requests

- **Backend Stack:** .NET 8 minimal API with dependency injection
- **LLM Integration:** Evaluate both Semantic Kernel and raw HttpClient; choose based on streaming simplicity
  - Local option: Ollama with Llama 3.2 or Phi-3
  - Cloud option: OpenAI GPT-4o-mini or Azure OpenAI
  - Prefer streaming for better UX
- **Embedding Model:**
  - OpenAI `text-embedding-3-small` OR
  - Local `all-MiniLM-L6-v2` via Ollama
  - Prioritize speed over ultimate quality
- **Vector Store:** Qdrant (preferred for simple Docker deployment and good .NET client)
  - Alternative: Weaviate or Azure AI Search (architect to decide based on .NET SDK quality)
- **Caching:** Redis with TTL-based expiration
  - Cache embeddings (query → vector)
  - Cache LLM responses (query + top-k chunks → answer)
  - Configurable TTL (start with 24 hours)
- **Tag Suggestion:** TF-IDF + logistic regression baseline
  - Options: ML.NET (if viable) OR Python microservice with sklearn (if ML.NET too complex)
  - Architect to decide based on implementation complexity
- **Data Source:** Kaggle StackSample (10% Stack Overflow sample)
  - Ingest subset: 10k-50k rows initially
  - CSV parsing with robust error handling
- **Chunking Strategy:**
  - Default: ~500 tokens per chunk
  - Configurable overlap (e.g., 50-100 tokens)
  - Document trade-offs in README
- **Hybrid Search:**
  - Combine keyword search (BM25/full-text) with vector similarity
  - Start with equal weighting; make configurable for experimentation
- **Environment Configuration:**
  - API keys in `.env` file (never committed)
  - Environment-based config for: LLM endpoint, embedding model, vector store connection, Redis connection, chunk size, top-k, cache TTL
- **Telemetry:**
  - Structured logging (JSON format)
  - Log: timestamp, query, latency, token count, cost estimate, cache hit/miss, retrieval count
  - Simple console output for MVP; optionally file-based logs
- **Security:**
  - No authentication for MVP (local execution only)
  - API keys in environment variables
  - No PII in logs
  - Stack Overflow data usage per Kaggle terms of service

---

## Epic List

**Epic 1: Foundation & Data Ingestion**
Build project foundation with Docker orchestration and implement data ingestion pipeline to load, chunk, embed, and store Stack Overflow data.

**Epic 2: RAG Query Pipeline**
Implement hybrid search retrieval and LLM integration to answer questions with streaming responses and citations.

**Epic 3: Tag Suggestion & Observability**
Add tag suggestion capability and comprehensive telemetry to make system measurable and production-ready for learning validation.

---

## Epic 1: Foundation & Data Ingestion

**Goal:** Establish .NET 8 minimal API project with Docker Compose orchestration (API, Qdrant, Redis). Implement data ingestion pipeline that loads Kaggle StackSample CSV, chunks documents, generates embeddings, and stores in vector store. Validate end-to-end ingestion with test data and confirm vector store contains searchable content.

### Story 1.1: Project Setup & Docker Orchestration

As a **developer**,
I want **a .NET 8 minimal API project with Docker Compose configuration for all services**,
so that **I can run the entire system with `docker compose up` and have a foundation for building features**.

**Acceptance Criteria:**
1. .NET 8 minimal API project created with solution structure (API, Core, Data, ML projects)
2. Docker Compose file defines services: API, Qdrant, Redis (and optionally Ollama)
3. API has `/health` endpoint returning 200 OK
4. `docker compose up` starts all services successfully
5. Environment variable configuration setup (`.env.example` provided)
6. Basic dependency injection configured for services (placeholders for IngestionService, RetrievalService, etc.)
7. README documents how to run the project and required environment variables

### Story 1.2: CSV Data Loading & Parsing

As a **developer**,
I want **the system to load and parse Kaggle StackSample CSV files**,
so that **I can extract Stack Overflow questions, answers, and tags for ingestion**.

**Acceptance Criteria:**
1. Data ingestion service can load CSV files from configurable path
2. Parser extracts: question ID, question title, question body, answer body, tags
3. Basic data validation and cleaning (remove null/empty entries, sanitize HTML)
4. Configurable row limit (default: 10k rows for MVP)
5. Error handling for malformed CSV entries (log and skip)
6. Unit tests verify parsing logic on sample CSV data
7. Ingestion logs summary: total rows loaded, rows skipped, errors encountered

### Story 1.3: Document Chunking Strategy

As a **developer**,
I want **the system to chunk Stack Overflow posts into ~500 token segments with configurable overlap**,
so that **chunks fit within embedding model limits and retrieval returns relevant context**.

**Acceptance Criteria:**
1. Chunking service splits documents into segments of ~500 tokens (configurable)
2. Overlap between chunks is configurable (default: 50 tokens)
3. Each chunk retains metadata: source post ID, title, chunk index
4. Chunking preserves sentence boundaries where possible (don't split mid-sentence)
5. Unit tests verify chunk size, overlap, and metadata preservation
6. Logging shows: total documents, total chunks created, avg chunks per document

### Story 1.4: Embedding Generation & Vector Store Integration

As a **developer**,
I want **the system to generate embeddings for chunks and store them in Qdrant**,
so that **I can perform vector similarity search for retrieval**.

**Acceptance Criteria:**
1. Embedding service integrates with chosen embedding model (OpenAI `text-embedding-3-small` or local model via Ollama)
2. Batch embedding generation for efficiency (configurable batch size, e.g., 100 chunks)
3. Qdrant client configured and connected to Docker Compose Qdrant instance
4. Chunks with embeddings upserted to Qdrant collection with metadata (post ID, title, chunk text)
5. Error handling and retry logic for embedding API calls and Qdrant upserts
6. Integration test verifies: chunk → embedding → Qdrant upsert → retrieval by vector search
7. Logging shows: total embeddings generated, upsert success/failure count, time taken

### Story 1.5: Ingestion Endpoint & Validation

As a **developer**,
I want **a `/ingest` endpoint that triggers the full ingestion pipeline and validates results**,
so that **I can load data on demand and confirm the vector store is populated correctly**.

**Acceptance Criteria:**
1. `POST /ingest` endpoint accepts CSV file path (or uses default from config)
2. Endpoint triggers: CSV load → chunk → embed → upsert pipeline
3. Returns ingestion summary: documents loaded, chunks created, embeddings generated, Qdrant upserts completed, time taken
4. Validation query performs sample vector search in Qdrant and returns result count
5. Error responses include actionable messages (e.g., "CSV not found", "Qdrant connection failed")
6. Manual test confirms: run `/ingest`, verify Qdrant contains expected chunk count via API or Qdrant UI

---

## Epic 2: RAG Query Pipeline

**Goal:** Implement `/ask` endpoint that accepts questions, performs hybrid search (keyword + vector), retrieves top-k chunks, and streams LLM responses with 3-5 formatted citations. Add Redis caching for embeddings and responses to optimize repeat queries.

### Story 2.1: Query Embedding & Vector Search

As a **user**,
I want **the system to embed my question and perform vector similarity search**,
so that **the system retrieves relevant Stack Overflow chunks**.

**Acceptance Criteria:**
1. Query embedding service generates vector for user question using same model as ingestion
2. Qdrant vector search retrieves top-k chunks (configurable, default: 10) by cosine similarity
3. Retrieved chunks include metadata: post ID, title, chunk text, similarity score
4. Integration test verifies: query → embedding → Qdrant search → results returned
5. Logging shows: query text, top-k setting, number of results, search latency

### Story 2.2: Hybrid Search (Keyword + Vector)

As a **developer**,
I want **the system to combine keyword search with vector search**,
so that **retrieval benefits from both semantic similarity and exact keyword matches**.

**Acceptance Criteria:**
1. Keyword search implementation using Qdrant's full-text search or separate keyword index
2. Hybrid search combines vector similarity scores and keyword match scores with configurable weighting (default: 50/50)
3. Top-k results ranked by combined score
4. Unit tests verify: keyword-only query returns keyword matches, hybrid query balances both
5. Configuration allows tuning keyword vs. vector weight
6. Logging shows: hybrid score breakdown for top results

### Story 2.3: LLM Integration with Streaming Responses

As a **user**,
I want **the system to stream LLM responses in real-time**,
so that **I see answers generating immediately rather than waiting for full completion**.

**Acceptance Criteria:**
1. LLM service integrates with chosen LLM (Ollama local or OpenAI GPT-4o-mini)
2. Prompt construction: system message + retrieved chunks as context + user question
3. Streaming response via Server-Sent Events (SSE) or chunked transfer encoding
4. Error handling for LLM API failures with graceful fallback messages
5. Integration test verifies: question + chunks → streaming LLM response
6. Logging shows: prompt token count, completion token count, total tokens, LLM latency

### Story 2.4: Citation Formatting & Response Structure

As a **user**,
I want **LLM responses to include 3-5 formatted citations referencing source Stack Overflow posts**,
so that **I can verify answer sources and explore original posts**.

**Acceptance Criteria:**
1. System instructs LLM to cite sources using chunk metadata (post ID, title)
2. Citations formatted as: `[Title](https://stackoverflow.com/questions/{post_id})`
3. Responses include 3-5 citations minimum (or all if fewer chunks retrieved)
4. Citations appear inline in answer or as dedicated "Sources" section at end
5. Manual validation: sample queries return answers with properly formatted, clickable citations
6. Logging shows: number of citations included per response

### Story 2.5: Redis Caching for Queries & Responses

As a **developer**,
I want **the system to cache query embeddings and LLM responses in Redis**,
so that **repeat queries return instantly and reduce API costs**.

**Acceptance Criteria:**
1. Redis client configured and connected to Docker Compose Redis instance
2. Cache key generation based on query text (hash or normalized string)
3. Cache stores: query → embedding vector (TTL: configurable, default 24h)
4. Cache stores: query + top-k chunks → LLM response (TTL: configurable, default 24h)
5. Cache hit: return cached response immediately; cache miss: perform full pipeline
6. Integration test verifies: first query misses cache and stores result, second identical query hits cache
7. Logging shows: cache hit/miss status, cache key, TTL

### Story 2.6: `/ask` Endpoint Integration

As a **user**,
I want **a `/ask` endpoint that accepts my question and returns a streaming answer with citations**,
so that **I can get grounded responses to technical questions**.

**Acceptance Criteria:**
1. `POST /ask` endpoint accepts JSON: `{ "question": "..." }`
2. Endpoint orchestrates: cache check → embed query → hybrid search → LLM streaming response
3. Streaming response includes: answer text (streamed), citations (list), metadata (latency, tokens, cache hit)
4. Error responses with helpful messages (e.g., "No relevant results found", "LLM service unavailable")
5. Manual test: submit question, verify streaming answer with 3-5 citations, check metadata
6. p50 latency for uncached queries <2s (validate with 10 sample queries)

---

## Epic 3: Tag Suggestion & Observability

**Goal:** Implement `/tags/suggest` endpoint for multi-label tag prediction. Add comprehensive telemetry (latency, tokens, cost, cache metrics) and create golden test set to validate retrieval quality. Provide simple UI or CLI for interaction.

### Story 3.1: TF-IDF Feature Extraction for Tag Suggestion

As a **developer**,
I want **the system to extract TF-IDF features from question text**,
so that **tag prediction has meaningful input features**.

**Acceptance Criteria:**
1. TF-IDF feature extraction using ML.NET or Python sklearn (architect chooses based on complexity)
2. Training data: use Kaggle StackSample questions + tags to build TF-IDF vocabulary and model
3. Feature vectorization for input question (title + body)
4. Unit tests verify: sample question → TF-IDF feature vector
5. Logging shows: vocabulary size, feature vector dimensionality

### Story 3.2: Logistic Regression Multi-Label Classifier

As a **developer**,
I want **a trained logistic regression model that predicts Stack Overflow tags**,
so that **the system can suggest relevant tags for new questions**.

**Acceptance Criteria:**
1. Logistic regression multi-label classifier trained on StackSample dataset
2. Model serialized and loaded at API startup (no retraining on each request)
3. Prediction returns top 3-5 tags with confidence scores
4. Unit tests verify: sample question → predicted tags (validate with known examples)
5. Model performance logged: training accuracy, sample validation predictions
6. README documents: model training process, expected accuracy, trade-offs

### Story 3.3: `/tags/suggest` Endpoint

As a **user**,
I want **a `/tags/suggest` endpoint that predicts tags for my question**,
so that **I can see what Stack Overflow tags are relevant to my question**.

**Acceptance Criteria:**
1. `POST /tags/suggest` endpoint accepts JSON: `{ "title": "...", "body": "..." }`
2. Endpoint returns: `{ "tags": ["tag1", "tag2", ...], "confidence": [0.8, 0.6, ...] }`
3. Top 3-5 tags returned based on highest confidence scores
4. Error handling for malformed input or model inference failures
5. Manual test: submit sample questions, verify tags are plausible (subjective review of 10 examples)
6. Logging shows: input text length, predicted tags, inference latency

### Story 3.4: Telemetry & Metrics Logging

As a **developer**,
I want **comprehensive telemetry logged for all requests**,
so that **I can measure system performance, cost, and cache effectiveness**.

**Acceptance Criteria:**
1. Telemetry service logs for each `/ask` request: timestamp, query, latency (p50/p95), token count, estimated cost, cache hit/miss, retrieval count
2. Telemetry logs for each `/tags/suggest` request: timestamp, input length, predicted tags, inference latency
3. Ingestion telemetry: documents loaded, chunks created, embeddings generated, time taken
4. Logs output in structured JSON format to console (and optionally to file)
5. README documents: how to interpret logs, cost estimation formula
6. Manual review: run 20 queries, analyze logs, calculate p50/p95 latency and avg cost

### Story 3.5: Golden Test Set & Hit@5 Validation

As a **developer**,
I want **a golden test set of 10-20 question/answer pairs with Hit@5 metric validation**,
so that **I can objectively measure retrieval quality**.

**Acceptance Criteria:**
1. Golden test set created: 10-20 Stack Overflow questions with known correct answer post IDs
2. Validation script runs: for each question, retrieve top-5 chunks, check if correct answer is in top-5
3. Hit@5 metric calculated: (questions with answer in top-5) / (total questions)
4. Target: Hit@5 ≥ 0.7
5. Script outputs: Hit@5 score, per-question results (hit/miss), chunk rankings
6. README documents: golden set creation process, Hit@5 results, observations

### Story 3.6: Simple UI or CLI Interface

As a **user**,
I want **a simple UI or CLI to interact with `/ask` and `/tags/suggest` endpoints**,
so that **I can easily test the system without using curl or Postman**.

**Acceptance Criteria:**
1. UI option: Simple Blazor page or static HTML with text input and response display
2. CLI option: Spectre.Console-based CLI with question input and formatted output
3. Interface supports: asking questions (displays streaming answers + citations), suggesting tags (displays tag list)
4. Optional: display request metadata (latency, tokens, cache hit) for learning purposes
5. Manual test: use interface to ask 5 questions and suggest tags for 3 questions, verify functionality
6. README documents: how to access and use the interface

---

## Next Steps

### Architect Prompt

**Create an architecture document for the Stack Overflow RAG Assistant** based on this PRD. Focus on:
- Finalizing vector store choice (Qdrant vs. Weaviate vs. Azure AI Search) with .NET SDK quality and Docker simplicity as key criteria
- Deciding LLM integration approach: Semantic Kernel vs. raw HttpClient for streaming
- Tag suggestion implementation: ML.NET feasibility vs. Python sklearn microservice
- Chunking strategy parameters: default chunk size, overlap, and configuration approach
- Hybrid search weighting and configurability
- Cache TTL and invalidation strategy
- Detailed service architecture and data flow diagrams
- Document trade-offs and rationale for each choice

This is a learning project—prioritize simplicity and speed over scalability. Architect should provide clear justification for technology choices to support learning goals.
