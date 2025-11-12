# Story 2.4: Citation Formatting & Response Structure

**Epic:** 2 - RAG Query Pipeline
**Story ID:** 2.4
**Status:** Draft
**Assigned To:** Dev Agent
**Story Points:** 2

---

## Story

As a **user**,
I want **LLM responses to include 3-5 formatted citations referencing source Stack Overflow posts**,
so that **I can verify answer sources and explore original posts**.

---

## Acceptance Criteria

1. System instructs LLM to cite sources using chunk metadata
2. Citations formatted: `[Title](https://stackoverflow.com/questions/{postId})`
3. Responses include 3-5 citations (or all if fewer chunks)
4. Citations appear as dedicated "Sources" section
5. Manual validation: citations are clickable and relevant

---

## Tasks

### Task 1: Create Citation Model
- [ ] Create `Citation` class
- [ ] Properties: PostId, Title, Url, RelevanceScore

### Task 2: Extract Citations from Chunks
- [ ] Implement `ExtractCitations` method
- [ ] Map chunks to Citation objects
- [ ] Generate Stack Overflow URLs
- [ ] Sort by relevance score (top 3-5)

### Task 3: Update Prompt
- [ ] Instruct LLM to reference sources
- [ ] Provide source IDs in context
- [ ] Request markdown citation format

### Task 4: Create Response Model
- [ ] Create `QueryResponse` class
- [ ] Properties: Answer, Citations[], Metadata

### Task 5: Write Tests
- [ ] Test citation extraction
- [ ] Test URL generation
- [ ] Test top-N selection

---

## Dev Notes

**Citation Format:**
```markdown
[How to use async/await properly](https://stackoverflow.com/questions/123456)
```

**Response Structure:**
```json
{
  "answer": "To use async/await...",
  "citations": [
    {"postId": "123", "title": "...", "url": "...", "score": 0.89}
  ],
  "metadata": {"latency": 1500, "tokens": 2500, ...}
}
```

---

## Testing

**Unit Tests:**
- [ ] Extract citations from chunks
- [ ] Generate correct URLs
- [ ] Sort by score
- [ ] Limit to top 5

**Manual Validation:**
- [ ] Ask sample question
- [ ] Verify 3-5 citations returned
- [ ] Click URLs → verify they work
- [ ] Verify relevance to answer

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
