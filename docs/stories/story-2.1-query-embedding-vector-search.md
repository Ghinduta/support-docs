# Story 2.1: Query Embedding & Vector Search

**Epic:** 2 - RAG Query Pipeline
**Story ID:** 2.1
**Status:** Draft
**Assigned To:** Dev Agent
**Story Points:** 2

---

## Story

As a **user**,
I want **the system to embed my question and perform vector similarity search**,
so that **the system retrieves relevant Stack Overflow chunks**.

---

## Acceptance Criteria

1. Query embedding using same model as ingestion (text-embedding-3-small)
2. Qdrant vector search retrieves top-k chunks (configurable, default: 10)
3. Retrieved chunks include metadata: post ID, title, text, similarity score
4. Integration test: query → embedding → search → results
5. Logging shows query, top-k, result count, latency

---

## Tasks

### Task 1: Create Retrieval Service
- [ ] Create `IRetrievalService` interface
- [ ] Implement `RetrievalService`
- [ ] Method: `SearchAsync(query, topK)`

### Task 2: Implement Vector Search
- [ ] Reuse EmbeddingService to embed query
- [ ] Call Qdrant vector search
- [ ] Extract top-k results with scores
- [ ] Map to `DocumentChunk` models

### Task 3: Add Configuration
- [ ] Add DEFAULT_TOP_K to config
- [ ] Update options class

### Task 4: Write Tests
- [ ] Unit test: RetrievalService logic
- [ ] Integration test: actual vector search
- [ ] Test top-k parameter
- [ ] Test empty results handling

---

## Dev Notes

**Vector Search:**
- Use cosine similarity
- Return top-k with scores
- Include all metadata for citation

**Configuration:**
```
DEFAULT_TOP_K=10
```

---

## Testing

**Integration Tests:**
- [ ] Embed query "How to use async/await?"
- [ ] Search Qdrant
- [ ] Verify top 10 chunks returned
- [ ] Verify scores are descending
- [ ] Verify metadata present

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
