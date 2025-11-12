# Story 2.6: `/ask` Endpoint Integration

**Epic:** 2 - RAG Query Pipeline
**Story ID:** 2.6
**Status:** Draft
**Assigned To:** Dev Agent
**Story Points:** 3

---

## Story

As a **user**,
I want **a `/ask` endpoint that accepts my question and returns a streaming answer with citations**,
so that **I can get grounded responses to technical questions**.

---

## Acceptance Criteria

1. `POST /ask` endpoint accepts JSON: `{ "question": "...", "topK": 10 }`
2. Orchestrates: cache check → embed → hybrid search → LLM streaming → citations
3. Streaming response via SSE includes answer text + citations + metadata
4. Error responses with helpful messages
5. Manual test: submit question, verify streaming + citations
6. p50 latency <2s for uncached queries (validate with 10 samples)

---

## Tasks

### Task 1: Create Query Endpoint
- [ ] Add `POST /ask` in Program.cs
- [ ] Accept `QueryRequest` (question, topK)
- [ ] Return streaming response

### Task 2: Orchestrate Pipeline
- [ ] Check cache for response
- [ ] If miss: call RetrievalService → LlmService
- [ ] Extract citations
- [ ] Create QueryResponse with metadata

### Task 3: Implement SSE Streaming
- [ ] Stream answer chunks via `Results.Stream()`
- [ ] Include citations after answer completes
- [ ] Include metadata (latency, tokens, cache hit)

### Task 4: Add Telemetry
- [ ] Create `ITelemetryService` interface
- [ ] Implement basic telemetry logging
- [ ] Log query metadata

### Task 5: Error Handling
- [ ] Handle empty question
- [ ] Handle no results found
- [ ] Handle LLM service unavailable
- [ ] Return appropriate HTTP codes

### Task 6: Write Tests
- [ ] Integration test: full /ask flow
- [ ] Test caching behavior
- [ ] Test error cases
- [ ] Manual latency validation

---

## Dev Notes

**SSE Response Format:**
```
data: {"answer": "chunk1", "type": "text"}
data: {"answer": "chunk2", "type": "text"}
data: {"citations": [...], "type": "citations"}
data: {"metadata": {...}, "type": "metadata"}
```

**Metadata:**
```json
{
  "latencyMs": 1834,
  "tokensUsed": 2500,
  "estimatedCost": 0.0015,
  "cacheHit": false,
  "retrievedChunks": 10
}
```

---

## Testing

**Integration Tests:**
- [ ] POST valid question → 200 + streaming response
- [ ] POST empty question → 400
- [ ] Verify citations included
- [ ] Verify metadata accurate
- [ ] Test cache hit vs miss latency

**Manual Validation:**
1. Ask: "How to use async/await in C#?"
2. Verify streaming works
3. Verify 3-5 citations
4. Verify latency <2s
5. Ask same question → verify cache hit (instant)

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
