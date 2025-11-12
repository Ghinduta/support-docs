# Project Brief: Stack Overflow RAG Assistant

## Executive Summary

A lightweight, learning-focused Q&A assistant that demonstrates RAG (Retrieval Augmented Generation) fundamentals by answering technical questions with grounded responses and citations from a curated Stack Overflow dataset. The system will auto-suggest tags for new questions and serve as a practical exploration of LLM integration, vector search, and mini MLOps patterns in .NET 8. Built as a minimal API with speed and local execution as primary constraints, this project prioritizes hands-on learning over production polish.

**Primary Problem:** Understanding RAG architecture, LLM integration patterns, and basic MLOps requires hands-on implementation, but most tutorials lack realistic datasets and end-to-end workflows.

**Target Market:** Individual developer (you) learning modern AI/ML integration patterns in .NET ecosystem.

**Key Value Proposition:** A complete, runnable RAG system that demonstrates real-world patterns (chunking, embeddings, hybrid search, streaming, caching, telemetry) with measurable outcomes and documented trade-offs.

---

## Problem Statement

### Current State
- RAG tutorials often use toy datasets or skip critical implementation details (chunking strategies, caching, cost tracking)
- Understanding the full pipeline from data ingestion → vector storage → retrieval → LLM response requires building it end-to-end
- .NET ecosystem examples for LLM integration are less common than Python equivalents
- Multi-label classification (tag suggestion) often treated separately from RAG systems

### Impact
- Without hands-on implementation, concepts like "hybrid search," "chunk overlap," and "reranking" remain abstract
- Production readiness gaps (caching, telemetry, cost tracking) aren't obvious until you build the system
- Missing practical knowledge of trade-offs: chunk size vs. retrieval accuracy, local models vs. API latency, simple vs. complex search strategies

### Why Existing Solutions Fall Short
- Python RAG tutorials don't translate directly to .NET patterns
- Production RAG frameworks (LangChain, etc.) obscure the fundamentals with abstraction layers
- Most examples don't include realistic operational concerns (Docker deployment, metrics, caching)

### Urgency
- Learning window available now; want hands-on knowledge before discussing architecture choices with team
- Stack Overflow dataset is readily available and realistic for technical Q&A domain
- .NET 8 + modern LLM APIs provide good foundation for rapid iteration

---

## Proposed Solution

### Core Concept
Build a minimal but complete RAG system using Stack Overflow data that answers technical questions with cited sources and suggests relevant tags. The system will be instrumented to measure key metrics (latency, token usage, cost) and optimized for local development speed.

### Key Components
1. **Data Ingestion Pipeline:** CSV → cleaning → chunking → embedding → vector storage
2. **Hybrid Retrieval:** Combine keyword search (BM25/full-text) with vector similarity
3. **LLM Integration:** Streaming responses with structured citations and resilient HTTP handling
4. **Tag Suggestion:** TF-IDF + logistic regression baseline for multi-label classification
5. **Operational Layer:** Docker Compose orchestration, Redis caching, telemetry logging

### Key Differentiators
- **Learning-first design:** Every choice documented with rationale and trade-offs
- **Measurable outcomes:** Golden test set, retrieval Hit@5 metrics, latency tracking
- **One-command deployment:** `docker compose up` gets everything running locally
- **.NET native:** Demonstrates modern .NET patterns for LLM/vector integration

### Why This Will Succeed
- Scoped to realistic learning goals with clear non-goals
- Uses proven, available dataset (Kaggle StackSample)
- Favors simple baselines that work over complex optimizations
- Built-in metrics to validate each component independently

### High-Level Vision
A working system that demonstrates you understand:
- How RAG retrieval quality impacts answer quality
- Trade-offs in chunk size, embedding models, search strategies
- Cost and latency implications of different LLM choices
- Basic MLOps patterns (containerization, config management, observability)

---

## Target Users

### Primary User Segment: You (Developer Learning RAG + .NET AI Patterns)

**Profile:**
- Experienced developer learning RAG implementation details
- Familiar with .NET ecosystem, exploring LLM integration patterns
- Interested in practical MLOps basics (Docker, metrics, caching)
- Preparing to contribute to architecture decisions on AI features

**Current Behaviors:**
- Reading RAG tutorials and architecture blog posts
- Experimenting with LLM APIs and vector databases
- Evaluating technology choices for production systems

**Specific Needs:**
- Hands-on implementation to solidify conceptual understanding
- Realistic dataset that demonstrates real-world retrieval challenges
- Measurable outcomes to validate each component works
- Documentation of trade-offs to inform future architecture discussions

**Goals:**
- Deeply understand RAG pipeline: chunking → embedding → search → synthesis
- Learn .NET patterns for LLM streaming, JSON mode, error handling
- Experience basic MLOps: containerization, config, telemetry, caching
- Build intuition for performance/cost/quality trade-offs

---

## Goals & Success Metrics

### Business Objectives
- **Learning Outcome:** Can explain and justify chunk size, top-k, embedding model, and caching strategy choices
- **Technical Demonstration:** Working system demonstrating full RAG pipeline in .NET 8
- **Knowledge Transfer:** Documented trade-offs ready to inform production architecture discussions

### User Success Metrics
- **First Answer Latency:** <1-2 seconds for cached queries, <5 seconds for new queries (local execution)
- **Answer Quality:** Responses include 3-5 relevant Stack Overflow citations
- **Tag Accuracy:** Suggested tags are plausible (subjective evaluation on sample questions)
- **Retrieval Quality:** Hit@5 ≥ 0.7 on tiny golden test set

### Key Performance Indicators (KPIs)

- **Retrieval Hit@5:** ≥0.7 on golden set (top 5 chunks contain answer)
- **p50 Answer Latency:** <2 seconds (local, uncached)
- **p95 Answer Latency:** <5 seconds (local, uncached)
- **Cache Hit Rate:** >50% on repeated queries (validation caching works)
- **Token Efficiency:** Avg tokens/request logged and reasonable (<4k context)
- **Cost Per Request:** Logged and documented (baseline for optimization)
- **Tag Suggestion Plausibility:** Manual review of 10 sample outputs shows ≥3/5 relevant tags

---

## MVP Scope

### Core Features (Must Have)

- **`/ingest` Endpoint:** Load Kaggle StackSample CSV, clean text, chunk documents (~500 token chunks with configurable overlap), generate embeddings, upsert to vector store
  - *Rationale:* Foundation for all retrieval; demonstrates chunking strategy impact

- **`/ask` Endpoint:** Accept natural language question, embed query, perform hybrid search (keyword + vector), retrieve top-k chunks, stream LLM response with 3-5 formatted citations
  - *Rationale:* Core RAG workflow; streaming provides good UX even with slower models

- **`/tags/suggest` Endpoint:** Accept question title + body, return top 3-5 predicted Stack Overflow tags using TF-IDF + logistic regression baseline
  - *Rationale:* Demonstrates multi-label classification; complements RAG with structured prediction

- **Telemetry & Metrics:** Log p50/p95 latency, token counts, estimated cost per request, cache hit rates
  - *Rationale:* Makes performance and cost visible; essential for trade-off discussions

- **Redis Caching:** TTL-based cache for repeated queries (both embeddings and LLM responses)
  - *Rationale:* Demonstrates caching strategy; makes repeat queries snappy

- **Docker Compose Deployment:** One command (`docker compose up`) runs all services (API, vector DB, Redis, any needed model services)
  - *Rationale:* Makes system reproducible and easy to demo

- **Basic UI or CLI:** Simple interface to interact with `/ask` and `/tags/suggest` endpoints
  - *Rationale:* Functional testing; doesn't need to be polished

- **Golden Test Set:** 10-20 question/answer pairs to validate retrieval quality (Hit@5 metric)
  - *Rationale:* Objective measure that retrieval is working; avoids subjective-only evaluation

### Out of Scope for MVP

- Heavy model fine-tuning or training custom embeddings
- Authentication, authorization, multi-user support
- Full CI/CD pipeline or Kubernetes deployment
- Comprehensive evaluation suite or A/B testing framework
- Reranking models (may add post-MVP if needed)
- Production-grade error handling and retry logic (basic resilience only)
- Complex UI polish or mobile responsiveness
- Real-time data updates or incremental indexing

### MVP Success Criteria

System successfully demonstrates the full RAG pipeline with measurable quality, performance, and cost. Specifically:

1. **Functional:** All three endpoints work and produce sensible outputs
2. **Fast:** Answers stream in <1-2 seconds locally (uncached)
3. **Accurate:** Retrieval Hit@5 ≥ 0.7, citations are relevant
4. **Observable:** Logs show latency, tokens, cost, cache performance
5. **Reproducible:** Anyone can run `docker compose up` and use the system
6. **Documented:** README explains chunk size, top-k, caching choices and trade-offs

---

## Post-MVP Vision

### Phase 2 Features (If Time Permits)

- **Reranker Model:** Add lightweight reranking step to improve top-k precision
- **Query Expansion:** Auto-expand queries with synonyms or related terms before search
- **Hybrid Search Tuning:** Experiment with weighting between keyword and vector scores
- **Better Tag Model:** Try transformer-based multi-label classifier instead of TF-IDF baseline
- **Feedback Loop:** Capture user feedback (thumbs up/down) to build evaluation dataset
- **Persistent Metrics:** Store telemetry in time-series DB for trend analysis

### Long-Term Vision (1-2 Years, If This Were a Product)

- Expand to multiple knowledge domains (documentation, GitHub issues, internal wikis)
- Production-grade deployment with monitoring, alerting, auto-scaling
- User accounts with history, saved queries, personalized recommendations
- Active learning: system identifies low-confidence answers and routes to human experts
- Integration with IDE/Slack/Teams for in-context assistance

### Expansion Opportunities

- **Educational Platform:** Turn this into a teaching tool for RAG fundamentals
- **Internal Knowledge Base:** Apply pattern to company documentation and support tickets
- **Multi-Modal Search:** Add code search capabilities with syntax-aware chunking
- **Comparative Analysis Tool:** Let users compare answers from different retrieval strategies side-by-side

---

## Technical Considerations

### Platform Requirements

- **Target Platforms:** Local development (Windows/Linux/macOS via Docker)
- **Runtime:** .NET 8 minimal API
- **Browser/OS Support:** Any browser for simple UI; cross-platform CLI
- **Performance Requirements:**
  - First answer <1-2s (local, uncached)
  - Ingest 10k-50k documents in <10 minutes
  - Support 10+ concurrent queries without degradation

### Technology Preferences

**Backend:**
- .NET 8 minimal API (fast startup, simple routing)
- Semantic Kernel or direct HttpClient for LLM calls (evaluate both)
- ML.NET or Python microservice for TF-IDF + sklearn (decide based on complexity)

**Vector Store:**
- Options: Qdrant, Weaviate, Milvus, or Azure AI Search (local vs. cloud trade-off)
- Preference: Start with Qdrant (simple Docker deployment, good .NET client)

**Caching:**
- Redis (TTL-based, simple key-value for embeddings and responses)

**LLM:**
- Local: Ollama with Llama 3.2 or Phi-3 (for speed, offline capability)
- Cloud fallback: OpenAI GPT-4o-mini or Azure OpenAI (for quality comparison)
- Prefer streaming responses for better UX

**Embedding Model:**
- Start with `text-embedding-3-small` (OpenAI) or `all-MiniLM-L6-v2` (local via Ollama)
- Prioritize speed over ultimate quality for MVP

**UI:**
- Simple Blazor page or static HTML + JavaScript
- Alternative: CLI with rich formatting (Spectre.Console)

### Architecture Considerations

**Repository Structure:**
- Single repository, monolithic API initially
- Separate projects: API, Core (business logic), Data (ingestion), ML (tag suggestion)

**Service Architecture:**
- Minimal API with dependency injection
- Separate services: IngestionService, RetrievalService, TagSuggestionService, TelemetryService
- Docker Compose orchestrates: API, Qdrant, Redis, (optionally) Ollama

**Integration Requirements:**
- HTTP calls to LLM APIs (OpenAI or Azure OpenAI)
- Vector store client SDK (Qdrant .NET client)
- Redis client for caching
- CSV parsing for Kaggle data

**Security/Compliance:**
- API keys in environment variables (not committed)
- No PII in logs
- Stack Overflow data used per Kaggle terms of service
- Local execution only for MVP (no public exposure)

---

## Constraints & Assumptions

### Constraints

**Budget:**
- Minimal cloud costs (prefer local execution)
- LLM API usage kept low (<$5 for entire development)
- Use free-tier or open-source services where possible

**Timeline:**
- ~2-3 weeks of evening/weekend work
- Prioritize working system over perfect implementation

**Resources:**
- Solo developer (you)
- Local hardware: assume modern laptop with 16GB+ RAM, decent CPU
- No access to GPUs (favor CPU-friendly or API-based models)

**Technical:**
- Must run on Docker Compose (no Kubernetes)
- Kaggle dataset subset only (10k-50k rows to keep fast)
- Favor small models and short contexts for speed

### Key Assumptions

- Stack Overflow data quality is sufficient for learning purposes
- Retrieval quality will be "good enough" with simple chunking strategy
- Local LLM (Ollama) or small cloud model will provide adequate answer quality
- TF-IDF baseline will produce plausible tag suggestions
- Docker Compose is sufficient for orchestration
- Hybrid search will outperform keyword-only or vector-only approaches
- Caching will significantly improve repeat query performance
- 500-token chunks with small overlap is a reasonable starting point
- You'll sync with architect before making final production technology choices

---

## Risks & Open Questions

### Key Risks

- **Chunk Strategy Risk:** Chosen chunk size (500 tokens) may not align with answer boundaries, degrading retrieval quality
  - *Mitigation:* Test multiple chunk sizes on golden set; document trade-offs

- **Local Model Performance Risk:** Local LLMs may be too slow or low-quality for good user experience
  - *Mitigation:* Benchmark Ollama models early; fallback to cloud API if needed

- **Dataset Size Risk:** 10k-50k rows may not provide enough diversity for interesting retrieval challenges
  - *Mitigation:* Start with 10k; incrementally add data if retrieval seems too easy

- **Tag Suggestion Accuracy Risk:** TF-IDF baseline may produce nonsensical tag predictions
  - *Mitigation:* Evaluate on sample questions; document when it fails and why

- **Integration Complexity Risk:** Combining .NET, Python (for ML), Qdrant, Redis may create setup friction
  - *Mitigation:* Docker Compose handles orchestration; document any manual steps clearly

### Open Questions

- Which vector store provides best balance of simplicity, .NET support, and performance? (Qdrant vs. Weaviate vs. Azure AI Search)
- Should hybrid search weighting be configurable or hardcoded initially?
- Is chunk overlap necessary, or does it add complexity without benefit?
- Should we rerank results, or is top-k from hybrid search sufficient?
- What's the right cache TTL: hours, days, or indefinite until eviction?
- Should tag suggestion use the same embedding pipeline or separate TF-IDF features?

### Areas Needing Further Research

- .NET client libraries for chosen vector store (API quality, documentation)
- Streaming response patterns in Semantic Kernel vs. raw HttpClient
- Qdrant vs. Weaviate deployment simplicity and .NET SDK maturity
- Cost estimation formulas for token usage (depends on chosen LLM)
- Golden test set creation: how to select representative questions?

---

## Appendices

### A. Research Summary

**Dataset:**
- Kaggle StackSample: 10% sample of Stack Overflow posts (questions, answers, tags)
- Verified availability and licensing (public dataset)
- Plan to ingest small subset initially (10k-50k rows) for speed

**Technical Feasibility:**
- .NET 8 minimal APIs well-suited for fast prototyping
- Qdrant has official .NET client and Docker image
- Semantic Kernel supports streaming and structured outputs
- Redis has mature .NET clients (StackExchange.Redis)

### C. References

- **Kaggle StackSample Dataset:** [Stack Overflow 10% Sample](https://www.kaggle.com/datasets/stackoverflow/stacksample)
- **Qdrant Vector DB:** https://qdrant.tech/
- **Semantic Kernel (.NET):** https://learn.microsoft.com/en-us/semantic-kernel/
- **Ollama (Local LLMs):** https://ollama.ai/
- **RAG Fundamentals:** (your reading list / blog posts)

---

## Next Steps

### Immediate Actions

1. **Download Kaggle StackSample dataset** and explore structure (questions, answers, tags tables)
2. **Set up basic .NET 8 minimal API project** with Docker Compose scaffolding
3. **Spike: Test Qdrant .NET client** with sample embedding + search to validate integration
4. **Spike: Test LLM streaming** (Ollama or OpenAI) to confirm .NET integration pattern
5. **Create golden test set:** Select 10-20 Stack Overflow questions with known good answers
6. **Sync with architect:** Share this brief and discuss vector store choice (Qdrant vs. Azure AI Search) and reranking approach
7. **Begin implementation:** Start with `/ingest` endpoint (CSV → chunks → embeddings → Qdrant)

### PM Handoff

This Project Brief provides the full context for **Stack Overflow RAG Assistant**. If transitioning to a Product Manager or Architect role, please review this brief thoroughly and work with the user to create a detailed PRD section by section, asking for any necessary clarification or suggesting improvements based on production requirements, scalability needs, or architectural best practices.
