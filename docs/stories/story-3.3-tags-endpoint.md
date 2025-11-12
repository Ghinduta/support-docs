# Story 3.3: `/tags/suggest` Endpoint

**Epic:** 3 - Tag Suggestion & Observability
**Story ID:** 3.3
**Status:** Draft
**Assigned To:** Dev Agent
**Story Points:** 2

---

## Story

As a **user**,
I want **a `/tags/suggest` endpoint that predicts tags for my question**,
so that **I can see what Stack Overflow tags are relevant to my question**.

---

## Acceptance Criteria

1. `POST /tags/suggest` accepts JSON: `{ "title": "...", "body": "..." }`
2. Returns: `{ "tags": ["tag1", ...], "confidence": [0.8, ...] }`
3. Top 3-5 tags returned based on highest confidence
4. Error handling for malformed input
5. Manual test: sample questions produce plausible tags

---

## Tasks

### Task 1: Create Tag Suggestion Service
- [ ] Create `ITagSuggestionService` interface
- [ ] Implement using TfidfVectorizer + Classifier
- [ ] Method: `SuggestTagsAsync(title, body)`

### Task 2: Create Endpoint
- [ ] Add `POST /tags/suggest` in Program.cs
- [ ] Accept `TagSuggestionRequest`
- [ ] Return `TagSuggestionResponse`

### Task 3: Error Handling
- [ ] Validate input (non-empty title/body)
- [ ] Handle model errors
- [ ] Return appropriate HTTP codes

### Task 4: Write Tests
- [ ] Integration test: POST valid question → tags
- [ ] Test empty input → 400
- [ ] Manual validation: 10 sample questions

---

## Dev Notes

**Request:**
```json
{
  "title": "How to use async/await",
  "body": "I'm trying to understand..."
}
```

**Response:**
```json
{
  "tags": ["c#", "async-await", ".net", "task", "async"],
  "confidence": [0.89, 0.76, 0.68, 0.54, 0.48]
}
```

---

## Testing

**Integration Tests:**
- [ ] POST sample question
- [ ] Verify 3-5 tags returned
- [ ] Verify confidence scores descending

**Manual Validation:**
Test with 10 real Stack Overflow questions, verify tags are plausible.

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
