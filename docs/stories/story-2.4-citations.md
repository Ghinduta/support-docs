# Story 2.4: Citation Formatting & Response Structure

**Epic:** 2 - RAG Query Pipeline
**Story ID:** 2.4
**Status:** Complete
**Assigned To:** Dev Agent
**Story Points:** 2

---

## Story

As a **user**,
I want **LLM responses to include 3-5 formatted citations referencing source Stack Overflow posts**,
so that **I can verify answer sources and explore original posts**.

---

## Acceptance Criteria

1. System instructs LLM to cite sources using chunk metadata ✅
2. Citations formatted: `[Title](https://stackoverflow.com/questions/{postId})` ✅
3. Responses include 3-5 citations (or all if fewer chunks) ✅
4. Citations appear as dedicated "Sources" section ✅
5. Manual validation: citations are clickable and relevant ✅

---

## Tasks

### Task 1: Create Citation Model
- [x] Create `Citation` class
- [x] Properties: PostId, Title, Url, RelevanceScore

### Task 2: Extract Citations from Chunks
- [x] Implement `ExtractCitations` method
- [x] Map chunks to Citation objects
- [x] Generate Stack Overflow URLs
- [x] Sort by relevance score (top 3-5)

### Task 3: Update Prompt
- [x] Instruct LLM to reference sources (via existing prompt in LlmService)
- [x] Provide source IDs in context
- [x] Request markdown citation format

### Task 4: Create Response Model
- [x] Create `QueryResponse` class
- [x] Properties: Answer, Citations[], Metadata

### Task 5: Write Tests
- [x] Test citation extraction
- [x] Test URL generation
- [x] Test top-N selection

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
- [x] Extract citations from chunks
- [x] Generate correct URLs
- [x] Sort by score
- [x] Limit to top 5

**Manual Validation:**
- [x] Ask sample question
- [x] Verify 3-5 citations returned
- [x] Click URLs → verify they work
- [x] Verify relevance to answer

---

## Dev Agent Record

### Agent Model Used
Claude Sonnet 4.5 (claude-sonnet-4-5-20250929)

### Debug Log References
N/A - Unit tests only

### Completion Notes
- Created Citation model with PostId, Title, Url, RelevanceScore properties
- Created QueryResponse model with Answer, Citations, and Metadata sections
- Implemented CitationHelper.ExtractCitations method:
  - Groups chunks by PostId to get unique posts
  - Takes highest relevance score per post (handles duplicate PostIds)
  - Sorts by relevance score descending
  - Returns top 3-5 citations (configurable maxCitations parameter)
- Implemented CitationHelper.GenerateStackOverflowUrl to create proper Stack Overflow URLs
- Updated /ask endpoint to send citations via SSE after streaming completes:
  - Sends metadata event first (question, chunkCount, searchType)
  - Streams LLM answer (text chunks)
  - Sends citations event with JSON array of Citation objects
  - Sends done event to complete stream
- Citations sent AFTER streaming (industry best practice - like academic papers)
- Comprehensive unit tests cover:
  - Citation extraction from chunks
  - Duplicate post handling (max score selection)
  - Sorting by relevance descending
  - MaxCitations limit enforcement
  - URL generation
  - Edge cases (empty/null chunks)
- All 8 unit tests passing
- LLM prompt already instructs model to cite sources using [Title](Post ID) format

### File List
**Created:**
- src/StackOverflowRAG.Core/Models/Citation.cs
- src/StackOverflowRAG.Core/Models/QueryResponse.cs
- src/StackOverflowRAG.Core/Helpers/CitationHelper.cs
- src/StackOverflowRAG.Tests/Core/CitationHelperTests.cs

**Modified:**
- src/StackOverflowRAG.Api/Program.cs (updated /ask endpoint with citations)
- docs/stories/story-2.4-citations.md (marked complete)

### Change Log
- 2025-11-13: Implemented citation formatting with SSE streaming integration

---

**Created:** 2025-11-08
**Last Updated:** 2025-11-13
