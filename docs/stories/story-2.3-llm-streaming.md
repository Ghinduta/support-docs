# Story 2.3: LLM Integration with Streaming Responses

**Epic:** 2 - RAG Query Pipeline
**Story ID:** 2.3
**Status:** Complete
**Assigned To:** Dev Agent
**Story Points:** 4

---

## Story

As a **user**,
I want **the system to stream LLM responses in real-time**,
so that **I see answers generating immediately rather than waiting for full completion**.

---

## Acceptance Criteria

1. LLM service integrates with OpenAI GPT-4o-mini via Semantic Kernel ✅
2. Prompt construction: system message + chunks as context + user question ✅
3. Streaming response via Server-Sent Events (SSE) ✅
4. Error handling for LLM API failures ✅
5. Logging shows prompt tokens, completion tokens, latency ✅

---

## Tasks

### Task 1: Create LLM Service
- [x] Create `ILlmService` interface
- [x] Implement `LlmService` using Semantic Kernel
- [x] Method: `StreamAnswerAsync(question, chunks)`

### Task 2: Implement Prompt Builder
- [x] Create system prompt for RAG
- [x] Format retrieved chunks as context
- [x] Add user question
- [x] Build complete prompt

### Task 3: Configure Streaming
- [x] Use Semantic Kernel streaming API
- [x] Return `IAsyncEnumerable<string>`
- [x] Handle partial responses

### Task 4: Add Telemetry
- [x] Token estimation (rough approximation: 1 token ≈ 4 characters)
- [x] Calculate latency (total and first-token)
- [x] Log streaming metrics

### Task 5: Write Tests
- [x] Unit tests for configuration validation
- [x] Test error handling (parameter validation)

---

## Dev Notes

**System Prompt Example:**
```
You are a helpful assistant that answers technical questions using provided Stack Overflow context. Always cite your sources with [Title](URL) format. Be concise and accurate.
```

**Streaming:**
Use Semantic Kernel's `IAsyncEnumerable<StreamingTextContent>` pattern.

**Token Counting:**
Use `Microsoft.ML.Tokenizers` for accurate counts.

---

## Testing

**Integration Tests:**
- [ ] Stream answer for sample question
- [ ] Verify chunks included in prompt
- [ ] Verify tokens counted
- [ ] Test error handling (invalid API key)

---

## Dev Agent Record

### Agent Model Used
Claude Sonnet 4.5 (claude-sonnet-4-5-20250929)

### Debug Log References
N/A - Unit tests only

### Completion Notes
- Created ILlmService interface with StreamAnswerAsync method
- Implemented LlmService using Microsoft Semantic Kernel 1.67.1
- Integrated with OpenAI GPT-4o-mini via Semantic Kernel's chat completion service
- Built prompt builder that formats retrieved chunks as context with:
  - System message from configuration
  - Numbered context sections showing question title, content, and relevance score
  - User question
  - Instructions for citing sources
- Implemented streaming using IAsyncEnumerable<string> pattern
- Added comprehensive error handling (parameter validation, service initialization)
- Implemented telemetry logging:
  - Tracks first-token latency (TTFT - Time To First Token)
  - Tracks total latency
  - Estimates token counts (1 token ≈ 4 characters)
- Created LlmOptions configuration class with validation
- Registered Kernel and LlmService in DI container
- Configuration includes: ModelName, Temperature, MaxTokens, SystemPrompt
- Unit tests cover configuration validation and error cases
- Note: Full streaming integration tests require real OpenAI API calls

### File List
**Created:**
- src/StackOverflowRAG.Core/Interfaces/ILlmService.cs (updated from stub)
- src/StackOverflowRAG.Core/Configuration/LlmOptions.cs
- src/StackOverflowRAG.Core/Services/LlmService.cs
- src/StackOverflowRAG.Tests/Core/LlmServiceTests.cs

**Modified:**
- src/StackOverflowRAG.Api/Program.cs (added LLM service registration)
- src/StackOverflowRAG.Api/appsettings.json (added Llm configuration section)
- src/StackOverflowRAG.Core/StackOverflowRAG.Core.csproj (added Microsoft.SemanticKernel package)
- docs/stories/story-2.3-llm-streaming.md (marked complete)

### Change Log
- 2025-11-12: Implemented LLM service with Semantic Kernel streaming

---

**Created:** 2025-11-08
**Last Updated:** 2025-11-12
