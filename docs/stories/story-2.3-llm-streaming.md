# Story 2.3: LLM Integration with Streaming Responses

**Epic:** 2 - RAG Query Pipeline
**Story ID:** 2.3
**Status:** Draft
**Assigned To:** Dev Agent
**Story Points:** 4

---

## Story

As a **user**,
I want **the system to stream LLM responses in real-time**,
so that **I see answers generating immediately rather than waiting for full completion**.

---

## Acceptance Criteria

1. LLM service integrates with OpenAI GPT-4o-mini via Semantic Kernel
2. Prompt construction: system message + chunks as context + user question
3. Streaming response via Server-Sent Events (SSE)
4. Error handling for LLM API failures
5. Logging shows prompt tokens, completion tokens, latency

---

## Tasks

### Task 1: Create LLM Service
- [ ] Create `ILlmService` interface
- [ ] Implement `LlmService` using Semantic Kernel
- [ ] Method: `StreamAnswerAsync(question, chunks)`

### Task 2: Implement Prompt Builder
- [ ] Create system prompt for RAG
- [ ] Format retrieved chunks as context
- [ ] Add user question
- [ ] Build complete prompt

### Task 3: Configure Streaming
- [ ] Use Semantic Kernel streaming API
- [ ] Return `IAsyncEnumerable<string>`
- [ ] Handle partial responses

### Task 4: Add Telemetry
- [ ] Count prompt tokens
- [ ] Count completion tokens
- [ ] Calculate latency
- [ ] Log to TelemetryService

### Task 5: Write Tests
- [ ] Unit test: prompt building
- [ ] Integration test: actual streaming (mock with WireMock.NET)
- [ ] Test error handling

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
<!-- Agent updates this -->

### Debug Log References
<!-- Agent adds debug log references -->

### Completion Notes
<!-- Agent notes -->

### File List
<!-- Agent lists files -->

### Change Log
<!-- Agent tracks changes -->

---

**Created:** 2025-11-08
**Last Updated:** 2025-11-08
