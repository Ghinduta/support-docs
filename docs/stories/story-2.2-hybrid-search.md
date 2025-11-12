# Story 2.2: Hybrid Search (Keyword + Vector)

**Epic:** 2 - RAG Query Pipeline
**Story ID:** 2.2
**Status:** Draft
**Assigned To:** Dev Agent
**Story Points:** 3

---

## Story

As a **developer**,
I want **the system to combine keyword search with vector search**,
so that **retrieval benefits from both semantic similarity and exact keyword matches**.

---

## Acceptance Criteria

1. Keyword search using Qdrant's full-text search
2. Hybrid search combines vector + keyword scores (50/50 weighting)
3. Top-k results ranked by combined score
4. Weighting configurable via environment
5. Tests verify hybrid outperforms vector-only

---

## Tasks

### Task 1: Enable Qdrant Full-Text Search
- [ ] Update Qdrant collection creation to index text fields
- [ ] Configure sparse vectors for keyword matching

### Task 2: Implement Hybrid Search
- [ ] Update `SearchAsync` to perform both searches
- [ ] Combine scores: `final = (0.5 * vector) + (0.5 * keyword)`
- [ ] Re-rank by combined score
- [ ] Return top-k

### Task 3: Add Configuration
- [ ] Add VECTOR_WEIGHT and KEYWORD_WEIGHT to config
- [ ] Make configurable for experiments

### Task 4: Write Tests
- [ ] Test keyword-only matching
- [ ] Test hybrid scoring
- [ ] Test weight configuration
- [ ] Compare hybrid vs vector-only results

---

## Dev Notes

**Hybrid Search Formula:**
```
final_score = (VECTOR_WEIGHT * vector_score) + (KEYWORD_WEIGHT * keyword_score)
```

**Qdrant Sparse Vectors:**
Enable text indexing for BM25-style keyword matching.

---

## Testing

**Integration Tests:**
- [ ] Query with exact term ("async/await")
- [ ] Verify keyword matching boosts relevant results
- [ ] Query with paraphrase
- [ ] Verify semantic matching works
- [ ] Test different weight configurations

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
