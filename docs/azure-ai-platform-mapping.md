# Azure AI Platform Mapping

How this Stack Overflow RAG project maps to the Azure enterprise AI platform stack (Azure OpenAI, AI Search, AI Foundry, APIM, Entra ID, Cosmos DB, IaC), what best practices it is missing, and what to add.

## 1. Project section → Azure counterpart

| Current project | Azure counterpart | What changes |
|---|---|---|
| Qdrant (`QdrantVectorStoreRepository`) | Azure AI Search (vector + BM25 index) | The hand-rolled hybrid search (`QdrantVectorStoreRepository.cs:193-260`) becomes native: one query does vector + BM25 search, fuses them with RRF, then re-ranks with the semantic ranker. |
| CSV parser + `ChunkingService` + `IngestionService` | AI Search indexer + skillset (Text Split skill, Azure OpenAI Embedding skill = "integrated vectorization") over Blob Storage or SharePoint | Ingestion runs as a scheduled indexer pipeline instead of `POST /ingest`, with change tracking and deletion detection. |
| `OpenAIEmbeddingService` | Azure OpenAI embedding deployment (`text-embedding-3-large`) | Same Semantic Kernel code, but through `AzureOpenAIClient` with Entra ID (`DefaultAzureCredential`) instead of an API key. |
| `LlmService` (Semantic Kernel chat) | Azure OpenAI chat deployment + other models from the AI Foundry model catalog (Llama, Mistral, etc.) | `AddOpenAIChatCompletion` becomes `AddAzureOpenAIChatCompletion`; deployments are PTU (provisioned throughput) or pay-as-you-go. |
| Endpoints in `Program.cs` | App Service / Container Apps behind APIM | APIM becomes the only entry point; nobody calls the API or Azure OpenAI directly. |
| `RedisCacheService` (exact-match key from `CacheKeyHelper.GenerateResponseKey`) | APIM `azure-openai-semantic-cache-lookup/store` policies + Azure Managed Redis | The cache matches on meaning (embedding similarity) rather than exact strings. |
| `CostEstimator` + `TelemetryService` (logs only) | APIM `azure-openai-emit-token-metric` → App Insights / Log Analytics + Azure Cost Management budgets | Token usage is tracked per consumer (subscription key or app ID) at the gateway, not estimated in the app. |
| `.env` with `OpenAI__ApiKey` | Key Vault + Managed Identity | Ideally no secrets at all: the app's managed identity calls Azure OpenAI and AI Search. |
| No auth (no `AddAuthentication` in `Program.cs`) | Entra ID: APIM `validate-jwt`, app roles, user groups | Entirely new. |
| `docker-compose.yml` | Bicep or Terraform | Required skill; nothing exists yet. |
| `GoldenTestSetValidation` (Hit@K) | AI Foundry evaluations (groundedness, relevance, retrieval metrics) | Covers the "LLMOps / model evaluation" requirement; the existing golden set is a good talking point. |
| Tag classifier (ML.NET) | Azure ML, or leave as is | Not relevant to the role. |
| React chat (`web/`) | Static Web Apps with MSAL sign-in | Minor. |
| *(not present)* | Cosmos DB | Chat history, conversation state and feedback. |

## 2. Best practices missing

| Area | Gap | Where | Azure / best-practice fix |
|---|---|---|---|
| Security | No authentication or authorization on any endpoint, including `/ingest` and `/tags/train` | `Program.cs` | Entra ID + APIM `validate-jwt`, app roles |
| Security | No permission-aware retrieval: every user sees every chunk | `QdrantVectorStoreRepository` | Store `allowed_groups` per chunk; add an AI Search security filter from the user's Entra groups |
| Security | The cache key holds only the question, `TopK` and `UseHybrid`, so once permissions exist, user B gets an answer built from user A's documents | `Program.cs:478` | Add a user or permission scope to the cache key |
| Security | Prompt-injection exposure: retrieved chunk text is inserted raw into the prompt, with no delimiting or output check | `LlmService.cs:149-156` | Content Safety Prompt Shields via the APIM `llm-content-safety` policy |
| Security | The question text (PII) is logged | `TelemetryService.cs:30,46` | Redaction + a retention policy |
| Security | No rate limiting or quotas | — | APIM `azure-openai-token-limit` per consumer |
| Retrieval | Broken hybrid score fusion: a keyword hit is a flat `1.0`, added to a cosine score on a different scale | `QdrantVectorStoreRepository.cs:238,251` | RRF (Reciprocal Rank Fusion), native in AI Search |
| Retrieval | No re-ranker and no minimum-relevance threshold, so low-scoring chunks still reach the LLM | `RetrievalService` | AI Search semantic ranker + score cutoff |
| Resilience | No model fallback or load balancing | — | APIM backend pools with circuit breakers across multiple Azure OpenAI regions / PTU deployments |
| Observability | No distributed tracing, only logs | — | OpenTelemetry → App Insights, with Semantic Kernel's GenAI spans |
| LLMOps | Evaluation doesn't run in CI: the golden set is `Skip`ped | `GoldenTestSetValidation.cs` | Run the evaluation as a gate on prompt/model changes |

## 3. What to add

Put APIM in front of the API with Entra `validate-jwt`, a token-limit policy, a semantic-cache policy and a load-balanced Azure OpenAI backend pool, move retrieval to Azure AI Search with per-group security filters, store chat history in Cosmos DB, and deploy it all with Bicep. That gives you one demo project that covers every "required" line in the posting.
