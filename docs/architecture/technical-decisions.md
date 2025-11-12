# Technical Decision Records - Stack Overflow RAG Assistant

This document captures the key architectural decisions made for the Stack Overflow RAG Assistant, including alternatives considered and the rationale behind each choice. These records serve as learning material and reference for future projects.

---

## Decision 1: Vector Database Selection

**Decision:** Use **Qdrant** as the vector database

### Alternatives Considered

| Option | Pros | Cons | Use When |
|--------|------|------|----------|
| **Qdrant** ✅ | • Official .NET SDK with good docs<br>• Simple Docker deployment<br>• Built-in hybrid search (dense + sparse)<br>• Excellent UI for debugging<br>• Good performance for small-medium datasets<br>• Free and open source | • Smaller community than competitors<br>• Relatively newer (less battle-tested)<br>• Limited managed cloud options | Learning projects, .NET ecosystems, need local development, hybrid search required |
| **Weaviate** | • Mature, large community<br>• Excellent documentation<br>• Strong hybrid search<br>• Good GraphQL API<br>• More production references | • .NET client is community-maintained (less polished)<br>• More complex configuration<br>• Heavier resource usage | Python/JS ecosystems, production systems, need managed cloud options |
| **Azure AI Search** | • Best-in-class .NET SDK (Microsoft)<br>• Semantic ranking built-in<br>• Enterprise-grade security<br>• Managed service (no ops) | • **Cloud-only** (no local Docker)<br>• **Costs money** (defeats local/free goal)<br>• Overkill for learning projects<br>• Requires Azure account | Production .NET apps on Azure, enterprise requirements, budget available |
| **Pinecone** | • Fully managed (easiest ops)<br>• Great performance<br>• Simple API | • **Cloud-only** (no local)<br>• **Expensive** for high usage<br>• Poor .NET support (REST only)<br>• Limited hybrid search | Python/JS ecosystems, serverless apps, willing to pay for managed service |
| **ChromaDB** | • Very simple to use<br>• Good for prototyping<br>• Python-first design | • **No official .NET client**<br>• Limited production features<br>• Primarily for Python ML workflows | Python prototypes, Jupyter notebooks, not for .NET production |
| **Milvus** | • High performance at scale<br>• Rich feature set<br>• Open source | • Complex deployment (needs Kafka, MinIO)<br>• Steeper learning curve<br>• .NET client less mature | Large-scale production, distributed systems, have DevOps resources |

### Decision Rationale

**Chose Qdrant** because:
1. **Learning Goal Alignment:** Official .NET SDK lets us learn proper client integration patterns
2. **Local Development:** Docker image runs perfectly locally without cloud costs
3. **Hybrid Search:** Built-in support for combining vector + keyword search (key RAG feature)
4. **Debugging:** Web UI at `localhost:6333/dashboard` makes it easy to inspect vectors and search results
5. **Simplicity:** Single Docker container, zero configuration complexity

**When to Choose Differently:**
- **Azure AI Search:** If building production .NET app on Azure with budget for managed services
- **Weaviate:** If using Python/JS ecosystem or need more mature managed cloud options
- **Pinecone:** If prioritizing managed service over local development and have budget

---

## Decision 2: LLM Integration Approach

**Decision:** Use **Semantic Kernel** for LLM orchestration

### Alternatives Considered

| Option | Pros | Cons | Use When |
|--------|------|------|----------|
| **Semantic Kernel** ✅ | • Official Microsoft library for .NET<br>• Abstracts OpenAI, Azure OpenAI, Ollama<br>• Handles streaming, retries, structured outputs<br>• Active development and updates<br>• Good learning resource for production patterns<br>• Plugin/function calling support | • Adds dependency/abstraction layer<br>• Some overhead for simple use cases<br>• Still evolving (breaking changes possible) | .NET projects, production systems, need multi-provider support, want best practices |
| **Raw HttpClient** | • **Maximum control** and transparency<br>• Zero dependencies<br>• See exactly what's happening (great for learning internals)<br>• Minimal overhead | • **More boilerplate** for streaming<br>• Manual retry/timeout logic<br>• Manual prompt management<br>• Harder to switch providers | Learning LLM API internals, simple use cases, want full control, avoid dependencies |
| **LangChain .NET Port** | • Familiar if coming from Python<br>• Rich ecosystem (chains, agents) | • **Less mature than Python version**<br>• Smaller .NET community<br>• Heavier abstraction<br>• May be overkill for RAG | Python background, need advanced chains/agents, complex workflows |
| **OpenAI .NET SDK (Betalgo)** | • Community-maintained OpenAI client<br>• Good feature coverage<br>• Active development | • **Only works with OpenAI** (no Ollama)<br>• Not officially supported<br>• Less abstraction than Semantic Kernel | OpenAI-only projects, simpler than Semantic Kernel, not using local models |

### Decision Rationale

**Chose Semantic Kernel** because:
1. **Learning Production Patterns:** Shows the "right way" to integrate LLMs in .NET (official Microsoft guidance)
2. **Multi-Provider Support:** Easy to swap between OpenAI (cloud) and Ollama (local) without code changes
3. **Streaming Abstraction:** Handles Server-Sent Events complexity cleanly
4. **Future-Proof:** Active development, will stay current with LLM capabilities
5. **Structured Outputs:** Built-in support for JSON mode, function calling (useful for Phase 2)

**When to Choose Differently:**
- **Raw HttpClient:** If learning goal is "understand LLM APIs from first principles" or building ultra-lightweight service
- **OpenAI .NET SDK:** If only using OpenAI and want simpler than Semantic Kernel
- **LangChain .NET:** If porting existing Python LangChain code or need complex agent workflows

---

## Decision 3: Tag Suggestion Implementation

**Decision:** Use **ML.NET** for TF-IDF + Logistic Regression

### Alternatives Considered

| Option | Pros | Cons | Use When |
|--------|------|------|----------|
| **ML.NET** ✅ | • Pure .NET (no separate service)<br>• Learn ML.NET patterns<br>• Simpler deployment (monolithic)<br>• No cross-language communication<br>• Model training integrated | • **More verbose than sklearn**<br>• TF-IDF API less intuitive<br>• Smaller ML community than Python<br>• Fewer examples/tutorials | .NET-only projects, monolithic apps, want to learn ML.NET, avoid microservices |
| **Python Microservice (sklearn)** | • **sklearn is dead-simple** for TF-IDF + LogReg<br>• Huge community, many examples<br>• Fast prototyping (3-5 lines of code)<br>• Rich Python ML ecosystem | • **Adds deployment complexity** (second container)<br>• Cross-language communication (HTTP/gRPC)<br>• Another runtime to manage<br>• Serialization overhead | Python ML expertise, complex ML needs, already using Python elsewhere |
| **Azure ML / Cognitive Services** | • Fully managed<br>• AutoML capabilities<br>• No model training code | • **Cloud-only, costs money**<br>• Overkill for simple TF-IDF<br>• Defeats local learning goal | Production Azure apps, complex ML needs, budget available, avoid ops |
| **Pre-trained Transformer (BERT)** | • State-of-the-art accuracy<br>• Transfer learning benefits | • **Massive overkill** for tag suggestion<br>• Slow inference (need GPU)<br>• Complex deployment | High accuracy critical, have GPU, dataset is large and complex |

### Decision Rationale

**Chose ML.NET** because:
1. **Simplicity:** Keeps entire stack in .NET (no polyglot complexity)
2. **Deployment:** Single Docker container for the whole app
3. **Learning Goal:** Opportunity to learn ML.NET patterns (useful for future .NET ML projects)
4. **Performance:** TF-IDF + LogReg is fast, no GPU needed
5. **"Good Enough":** Baseline model will be plausible for learning purposes

**Trade-off Accepted:**
- More code than sklearn (worth it to avoid microservice complexity for this project)

**When to Choose Differently:**
- **Python sklearn:** If team has strong Python ML skills and microservices are acceptable
- **Transformer Models:** If tag accuracy is critical and you have GPU infrastructure
- **Azure ML:** If building production Azure app with budget for managed services

---

## Decision 4: Embedding Model Selection

**Decision:** Use **OpenAI text-embedding-3-small** (cloud API)

### Alternatives Considered

| Option | Pros | Cons | Use When |
|--------|------|------|----------|
| **OpenAI text-embedding-3-small** ✅ | • **High quality** (SOTA for size)<br>• Fast API responses<br>• **Very cheap** ($0.02/1M tokens)<br>• 1536 dimensions (good balance)<br>• Simple integration | • **Requires internet**<br>• Costs money (minimal)<br>• Data leaves local environment | Learning projects with budget, production apps, prioritize quality, OK with cloud |
| **Local all-MiniLM-L6-v2 (via Ollama)** | • **100% free, offline**<br>• Fast inference (small model)<br>• 384 dimensions (compact)<br>• Privacy (data stays local) | • **Lower quality** than OpenAI<br>• Need to run Ollama locally<br>• Manual model download<br>• Worse retrieval accuracy | Zero budget, privacy critical, offline required, acceptable to sacrifice quality |
| **OpenAI text-embedding-3-large** | • Best quality available<br>• 3072 dimensions | • **3x more expensive** ($0.13/1M)<br>• Slower API responses<br>• Overkill for learning | Production apps where retrieval quality is critical, budget available |
| **Azure OpenAI Embeddings** | • Same quality as OpenAI<br>• Data stays in Azure region<br>• Enterprise SLAs | • **Requires Azure account**<br>• More complex setup<br>• Higher minimum cost | Enterprise apps on Azure, compliance requirements, already using Azure |
| **Sentence Transformers (local)** | • Many model options<br>• Open source<br>• Good quality | • **Need to run separate Python service** or use ONNX Runtime<br>• Complex .NET integration | Python ecosystem, custom model needs, avoid OpenAI dependency |

### Decision Rationale

**Chose OpenAI text-embedding-3-small** because:
1. **Learning Quality:** Best way to learn "what good retrieval looks like" (high baseline quality)
2. **Cost:** For 10k-50k documents + queries, total cost <$1 (negligible for learning)
3. **Simplicity:** Simple HTTP API, no local model management
4. **Speed:** Fast API responses keep iteration quick
5. **Future Flexibility:** Easy to swap to local model later (Semantic Kernel abstraction)

**Cost Calculation (for perspective):**
- Embedding 50k chunks (~500 tokens each) = 25M tokens = $0.50
- Embedding 1000 queries (~20 tokens each) = 20k tokens = $0.0004
- **Total: ~$0.50 for entire learning project**

**When to Choose Differently:**
- **Local all-MiniLM-L6-v2:** If zero budget, offline requirement, or exploring quality trade-offs
- **text-embedding-3-large:** If optimizing production retrieval quality (worth 3x cost)
- **Azure OpenAI:** If deploying production app on Azure with compliance needs

---

## Decision 5: Caching Strategy

**Decision:** Use **Redis** with cache-aside pattern and TTL expiration

### Alternatives Considered

| Option | Pros | Cons | Use When |
|--------|------|------|----------|
| **Redis** ✅ | • **Industry standard**, mature<br>• Simple key-value semantics<br>• Built-in TTL expiration<br>• Excellent .NET client (StackExchange.Redis)<br>• Easy Docker deployment<br>• Good observability | • Separate service to run<br>• In-memory only (data lost on restart) | Most caching scenarios, need TTL, distributed systems, production apps |
| **In-Memory Cache (IMemoryCache)** | • **Built into .NET** (zero dependencies)<br>• Fastest possible (no network)<br>• Simplest to use | • **Lost on app restart**<br>• **Not distributed** (single instance only)<br>• Limited eviction strategies<br>• Can't inspect externally | Single-instance apps, ephemeral caching, avoiding infrastructure |
| **Memcached** | • Very fast<br>• Simple protocol<br>• Battle-tested | • **Less feature-rich** than Redis (no data structures)<br>• Poorer .NET support<br>• No persistence option | Simple key-value caching, legacy systems, extreme simplicity needed |
| **SQL Database (e.g., SQLite)** | • Persistent across restarts<br>• Queryable | • **Much slower** than in-memory<br>• Overkill for caching<br>• Manual TTL management | Need persistence, caching is secondary, already have DB |

### Decision Rationale

**Chose Redis** because:
1. **Learning Production Patterns:** Redis is the standard caching solution (learn once, use everywhere)
2. **TTL Support:** Automatic expiration (set 24h TTL, forget about it)
3. **Observability:** Can connect to Redis CLI to inspect cached values (great for debugging)
4. **Docker Integration:** One line in docker-compose.yml
5. **Future Flexibility:** Can add advanced features later (pub/sub, data structures)

**Cache Strategy Details:**
- **Cache Keys:** Hash of query text (MD5 or SHA256)
- **TTL:** 24 hours (configurable via environment variable)
- **Pattern:** Cache-aside (check cache → miss → fetch → store)
- **What to Cache:**
  - Query embeddings (avoid re-embedding same questions)
  - Full LLM responses (avoid expensive API calls)

**When to Choose Differently:**
- **IMemoryCache:** If truly single-instance app with no Docker requirement
- **Memcached:** If legacy system already uses it
- **No Cache:** If dataset so small that cold queries are fast enough

---

## Decision 6: API Documentation/UI Approach

**Decision:** Use **Swagger/OpenAPI** auto-generated UI (no custom frontend)

### Alternatives Considered

| Option | Pros | Cons | Use When |
|--------|------|------|----------|
| **Swagger/OpenAPI** ✅ | • **Zero effort** (auto-generated)<br>• Standard industry format<br>• Interactive testing built-in<br>• API documentation + UI in one<br>• Export OpenAPI spec for clients | • Basic UI (not pretty)<br>• Not suitable for end users<br>• Limited customization | APIs, internal tools, developer-facing products, learning projects |
| **Blazor Server** | • Pure .NET (no JavaScript)<br>• Real-time updates via SignalR<br>• Fast to build with Razor components | • Stateful connections (poor scalability)<br>• Not great for production<br>• Overkill for simple testing | .NET-only teams, rapid prototypes, internal dashboards |
| **Blazor WebAssembly** | • Runs in browser (no server)<br>• .NET all the way | • Large download size<br>• Slower startup<br>• Significant development effort | Production .NET SPAs, rich client apps, .NET expertise |
| **React/Vue/Angular SPA** | • Best user experience<br>• Production-ready<br>• Rich ecosystem | • **Requires frontend expertise**<br>• Separate build pipeline<br>• Overkill for learning project | Production user-facing apps, have frontend team, need best UX |
| **Spectre.Console CLI** | • Very fast to build<br>• Great for demos<br>• Rich formatting | • **Not web-accessible**<br>• Terminal-only<br>• No remote access | Developer tools, scripts, local-only usage |
| **Static HTML + JavaScript** | • Simplest web UI<br>• No framework | • Manual fetch() calls<br>• More effort than Swagger<br>• Harder to maintain | Ultra-simple UIs, learning web basics, avoid all frameworks |

### Decision Rationale

**Chose Swagger/OpenAPI** because:
1. **Zero Development Time:** Auto-generated from endpoint definitions
2. **Complete Functionality:** Can test all endpoints (`/ingest`, `/ask`, `/tags/suggest`) interactively
3. **Documentation:** Doubles as API reference (request/response schemas)
4. **Industry Standard:** Learning OpenAPI spec is valuable skill
5. **Streaming Support:** Can visualize SSE responses (with browser DevTools)

**What You Get:**
- Interactive UI at `http://localhost:5000/swagger`
- "Try it out" buttons for each endpoint
- Request/response examples
- Schema validation
- Exportable OpenAPI spec (for Postman, client generation, etc.)

**When to Choose Differently:**
- **React/Vue SPA:** If building production user-facing product
- **Blazor:** If .NET-only team needs richer UI than Swagger
- **Spectre.Console:** If building CLI tool or terminal-based workflow

---

## Decision 7: Chunking Strategy

**Decision:** Fixed-size chunking with **~500 tokens** and **50 token overlap**

### Alternatives Considered

| Strategy | Pros | Cons | Use When |
|----------|------|------|----------|
| **Fixed-size (500 tokens, 50 overlap)** ✅ | • **Simple to implement**<br>• Predictable chunk sizes<br>• Works well for most content<br>• Easy to reason about | • May split mid-sentence<br>• Doesn't respect semantic boundaries<br>• Not optimal for all content types | General purpose, learning projects, starting point before optimization |
| **Sentence-based chunking** | • Respects natural boundaries<br>• Better semantic coherence | • Variable chunk sizes (hard to optimize)<br>• Complex boundary detection<br>• May create tiny or huge chunks | High-quality requirements, structured documents, willing to handle variability |
| **Recursive character splitting** | • Tries to respect structure (paragraphs, sentences)<br>• LangChain pattern | • More complex logic<br>• Still not perfect<br>• Harder to debug | Need better than fixed but simpler than semantic |
| **Semantic chunking (embeddings)** | • Best semantic coherence<br>• Groups related content | • **Very expensive** (embed every sentence)<br>• Slow ingestion<br>• Complex to implement | High accuracy critical, budget for embeddings, advanced use case |
| **Smaller chunks (200 tokens)** | • More precise retrieval<br>• Better for specific questions | • More chunks = slower search<br>• May lose context | Short, factual content (FAQs, definitions) |
| **Larger chunks (1000 tokens)** | • More context per chunk<br>• Fewer chunks to search | • May exceed LLM context limits<br>• Less precise retrieval | Long-form content, need full context |

### Decision Rationale

**Chose 500 tokens with 50 overlap** because:
1. **Learning Baseline:** Simple to implement and understand, easy to measure impact
2. **LLM Context Window:** 10 chunks × 500 tokens = 5000 tokens (fits comfortably in GPT-4o-mini context)
3. **Retrieval Precision:** Small enough for specific answers, large enough for context
4. **Overlap:** 50 tokens ensures we don't lose critical info at boundaries
5. **Iterative Improvement:** Easy to experiment with different sizes later (100, 300, 750)

**Configurable via Environment:**
```bash
CHUNK_SIZE=500          # Target tokens per chunk
CHUNK_OVERLAP=50        # Overlap to preserve boundaries
```

**Planned Experiment (Post-MVP):**
Test Hit@5 metric with different chunk sizes:
- 300 tokens (more precise)
- 500 tokens (baseline)
- 750 tokens (more context)

**When to Choose Differently:**
- **200-300 tokens:** If questions are very specific and factual
- **750-1000 tokens:** If answers require broader context (e.g., tutorials)
- **Semantic chunking:** If production app needs highest retrieval quality and budget allows

---

## Decision 8: Hybrid Search Approach

**Decision:** Combine **vector similarity** and **keyword search** with equal weighting (50/50)

### Alternatives Considered

| Approach | Pros | Cons | Use When |
|----------|------|------|----------|
| **Hybrid (Vector + Keyword, 50/50)** ✅ | • **Best of both worlds**<br>• Catches exact matches (keywords)<br>• Catches semantic matches (vectors)<br>• Qdrant built-in support<br>• Industry best practice | • Slightly more complex<br>• Need to tune weighting | Most RAG systems, production apps, balanced accuracy |
| **Vector-only** | • Simplest implementation<br>• Good for semantic similarity | • **Misses exact term matches**<br>• Worse for technical queries (e.g., "async/await")<br>• Lower precision | Content without technical jargon, pure semantic search |
| **Keyword-only (BM25)** | • Fast<br>• Deterministic<br>• Great for exact matches | • **No semantic understanding**<br>• Fails on synonyms/paraphrasing<br>• Not modern RAG | Legacy search, exact match critical, avoid embeddings |
| **Hybrid with reranker** | • Best possible accuracy<br>• Two-stage retrieval | • **More complex**<br>• Slower (two models)<br>• Cost of reranker inference | Production with accuracy SLA, willing to pay latency/cost |

### Decision Rationale

**Chose Hybrid (50/50 weighting)** because:
1. **Best Practice:** Industry consensus for RAG (combine semantic + lexical)
2. **Qdrant Native:** Built-in support, zero extra code
3. **Technical Content:** Stack Overflow has lots of exact terms ("async/await", "LINQ") that need keyword matching
4. **Configurable:** Easy to experiment with different weights (70/30, 30/70) later

**How It Works:**
```
Final Score = (0.5 × Vector Similarity Score) + (0.5 × BM25 Keyword Score)
```

**Configuration:**
```bash
VECTOR_WEIGHT=0.5      # Vector similarity weight
KEYWORD_WEIGHT=0.5     # BM25 keyword weight
```

**Planned Experiment:**
Test different weightings on golden set:
- 70/30 (favor semantic)
- 50/50 (balanced)
- 30/70 (favor keywords)

**When to Choose Differently:**
- **Vector-only:** If content is conversational with no technical jargon
- **Keyword-only:** If building traditional search engine
- **Hybrid with reranker:** If production app needs highest accuracy (add Cohere reranker, etc.)

---

## Decision 9: Testing Approach

**Decision:** **Unit + Integration tests** (no E2E for MVP)

### Alternatives Considered

| Approach | Pros | Cons | Use When |
|----------|------|------|----------|
| **Unit + Integration** ✅ | • **Fast feedback loop**<br>• Tests critical paths<br>• Good coverage without E2E complexity<br>• Can mock expensive calls (OpenAI) | • Doesn't test full user flows<br>• Integration gaps possible | Most projects, learning, MVP stage, limited time |
| **Unit only** | • Fastest tests<br>• Easy to write | • **Doesn't catch integration bugs**<br>• False confidence | Pure libraries, algorithmic code, no external dependencies |
| **Full pyramid (Unit + Integration + E2E)** | • Best coverage<br>• Catches all bugs | • **Slow test suite**<br>• Complex E2E setup<br>• Overkill for learning project | Production apps, critical systems, large teams |
| **Manual testing only** | • Zero test code<br>• Fast initial development | • **Regressions likely**<br>• Not repeatable<br>• Poor learning practice | Throwaway prototypes, UI-heavy apps |

### Decision Rationale

**Chose Unit + Integration** because:
1. **Learning Goal:** Practice test-driven development without E2E complexity
2. **Critical Paths:** Integration tests cover Qdrant, Redis, OpenAI (with mocks)
3. **Fast Iteration:** Test suite runs in <30 seconds
4. **Golden Test Set:** Serves as manual "E2E" validation (Hit@5 metric)
5. **Cost:** Mock OpenAI calls to avoid API costs in tests

**Test Distribution:**
- **70% Unit Tests:** Chunking, caching, parsing, prompt building
- **25% Integration Tests:** Qdrant operations, Redis operations, mocked OpenAI
- **5% Golden Set Validation:** Manual quality check (Hit@5, answer relevance)

**Key Testing Patterns:**
- Mock OpenAI with **WireMock.NET** (avoid costs, deterministic tests)
- Use **TestContainers** for Qdrant/Redis (real integration, isolated)
- **Golden test set:** 10-20 curated Q&A pairs for retrieval validation

**When to Choose Differently:**
- **Add E2E:** If building production app with complex user workflows
- **Unit only:** If building pure library with no external services
- **Heavy E2E:** If UI is complex and critical to business value

---

## Decision 10: Deployment Strategy

**Decision:** **Docker Compose** for local deployment (no cloud for MVP)

### Alternatives Considered

| Strategy | Pros | Cons | Use When |
|----------|------|------|----------|
| **Docker Compose** ✅ | • **One command deploy** (`docker compose up`)<br>• Reproducible environment<br>• No cloud costs<br>• Fast iteration<br>• Perfect for learning | • **Not production-grade**<br>• Single machine only<br>• No auto-scaling<br>• Manual start/stop | Learning projects, local development, prototypes, demos |
| **Kubernetes (local)** | • Production-like environment<br>• Learn K8s patterns | • **Massive overkill** for learning<br>• Complex setup (minikube, kind)<br>• Slow iteration | Learning K8s specifically, need production parity |
| **Azure Container Apps** | • Managed containers<br>• Auto-scaling<br>• Easy .NET integration | • **Costs money**<br>• Cloud dependency<br>• Defeats local learning goal | Production .NET apps, Azure ecosystem, budget available |
| **Fly.io / Render** | • Free tier available<br>• Simple deployment<br>• Real URLs | • **Requires internet**<br>• Limited free tier<br>• Less control than local | Share demos publicly, want real URL, acceptable cloud dependency |
| **Bare metal (dotnet run)** | • No Docker overhead<br>• Simplest possible | • **Manual service management** (Redis, Qdrant)<br>• Not reproducible<br>• Platform-dependent | Quick experiments, avoiding Docker |

### Decision Rationale

**Chose Docker Compose** because:
1. **Learning Goal:** Focus on RAG, not DevOps/infrastructure
2. **Reproducibility:** Anyone can run `docker compose up` and it works
3. **Zero Cost:** No cloud spend, no credit card required
4. **Iteration Speed:** Change code, rebuild, restart in <10 seconds
5. **Real-World Pattern:** Docker Compose is common for development environments

**What You Get:**
```yaml
services:
  api:        # .NET 8 API
  qdrant:     # Vector database
  redis:      # Cache
```

**One command:** `docker compose up -d`

**When to Choose Differently:**
- **Kubernetes:** If learning goal includes container orchestration
- **Azure/AWS:** If building production cloud-native app
- **Fly.io:** If want to share demo with others via public URL

---

## Summary: Decision Matrix

Quick reference for when to deviate from these choices:

| Decision | Chosen | Choose Alternative If... |
|----------|--------|-------------------------|
| Vector DB | Qdrant | Azure ecosystem → Azure AI Search<br>Python/JS ecosystem → Weaviate |
| LLM Integration | Semantic Kernel | Learning API internals → Raw HttpClient<br>OpenAI-only → OpenAI SDK |
| Tag ML | ML.NET | Strong Python team → sklearn microservice<br>Budget for managed → Azure ML |
| Embedding | OpenAI API | Zero budget → Local all-MiniLM-L6-v2<br>Highest quality → text-embedding-3-large |
| Cache | Redis | Single instance → IMemoryCache<br>Legacy system → Memcached |
| UI | Swagger | Production UX → React/Vue SPA<br>CLI workflow → Spectre.Console |
| Chunking | 500 tokens, 50 overlap | Specific queries → 200-300 tokens<br>Broad context → 750-1000 tokens |
| Search | Hybrid 50/50 | No jargon → Vector-only<br>Highest accuracy → Add reranker |
| Testing | Unit + Integration | Throwaway prototype → Manual only<br>Production → Add E2E |
| Deployment | Docker Compose | Production app → Kubernetes/Cloud<br>Public demo → Fly.io |

---

## Learning Takeaways

### Key Principles Applied

1. **Start Simple, Optimize Later:** Chose simple baselines (fixed chunking, 50/50 hybrid) that can be refined
2. **Optimize for Learning Goals:** Local execution, observable internals, standard patterns
3. **Use Boring Technology:** Redis, Docker, REST API - proven, well-documented choices
4. **Measure Everything:** Telemetry built-in to validate decisions with data
5. **Make It Configurable:** Environment variables for chunk size, weights, TTL - easy to experiment

### What Makes a Good Technology Choice?

✅ **Good reasons to choose a technology:**
- Aligns with project goals (learning, production, cost, speed)
- Mature with good documentation and community
- Reduces complexity in your specific context
- Enables measurement and iteration

❌ **Bad reasons to choose a technology:**
- "Résumé-driven development" (learning new tech for sake of it)
- Cargo-culting (copying what big tech does without their constraints)
- Premature optimization (choosing for scale you'll never need)
- Avoiding learning (choosing only what you already know)

### When to Revisit These Decisions

**Signals to reconsider:**
1. **Performance:** Hit@5 <0.7, latency >5s, cost >budget
2. **Complexity:** Spending more time fighting tools than building features
3. **Scale:** Moving from 10k docs to 1M docs, or local to production
4. **Team:** Different expertise (e.g., hire Python ML expert → reconsider ML.NET)
5. **Requirements:** New needs (e.g., need offline → reconsider OpenAI embeddings)

---

## References and Further Reading

- **Qdrant vs Alternatives:** [Qdrant Benchmarks](https://qdrant.tech/benchmarks/)
- **Semantic Kernel:** [Microsoft Learn Docs](https://learn.microsoft.com/semantic-kernel/)
- **RAG Best Practices:** [OpenAI RAG Guide](https://platform.openai.com/docs/guides/rag)
- **Chunking Strategies:** [LangChain Text Splitters](https://python.langchain.com/docs/modules/data_connection/document_transformers/)
- **Hybrid Search:** [Qdrant Hybrid Search](https://qdrant.tech/documentation/tutorials/hybrid-search/)
- **ML.NET:** [ML.NET Documentation](https://dotnet.microsoft.com/apps/machinelearning-ai/ml-dotnet)

---

**Document Version:** 1.0
**Last Updated:** 2025-11-08
**Author:** Winston (Architect Agent)
