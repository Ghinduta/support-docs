# Story 3.5: Golden Test Set & Hit@5 Validation

**Epic:** 3 - Tag Suggestion & Observability
**Story ID:** 3.5
**Status:** Draft
**Assigned To:** Dev Agent
**Story Points:** 3

---

## Story

As a **developer**,
I want **a golden test set of 10-20 question/answer pairs with Hit@5 metric validation**,
so that **I can objectively measure retrieval quality**.

---

## Acceptance Criteria

1. Golden test set created: 10-20 Stack Overflow Q&A pairs with known post IDs
2. Validation script runs: for each question, check if correct answer in top-5 chunks
3. Hit@5 metric calculated: (correct answers in top-5) / (total questions)
4. Target: Hit@5 ≥ 0.7
5. Script outputs per-question results and overall Hit@5 score
6. README documents golden set creation and results

---

## Tasks

### Task 1: Create Golden Test Set
- [ ] Select 10-20 diverse Stack Overflow questions from ingested data
- [ ] Record: question text, correct answer post ID
- [ ] Store in `tests/golden-test-set.json`

### Task 2: Create Validation Script
- [ ] Create console app or test project
- [ ] For each question: embed → search → check top-5
- [ ] Calculate Hit@5 metric
- [ ] Output detailed results

### Task 3: Run Validation
- [ ] Execute script on current system
- [ ] Document Hit@5 score
- [ ] Identify failed cases

### Task 4: Document Results
- [ ] Add section to README
- [ ] Document Hit@5 score
- [ ] Note observations (what works, what doesn't)

---

## Dev Notes

**Golden Test Set Format:**
```json
[
  {
    "questionId": "123",
    "questionText": "How to use async/await?",
    "correctPostId": "456",
    "tags": ["c#", "async-await"]
  }
]
```

**Hit@5 Calculation:**
```
Hit@5 = (# questions with correct answer in top 5) / (total questions)
```

**Selection Criteria:**
- Diverse topics (C#, Python, JavaScript, etc.)
- Varying difficulty
- Clear correct answers

---

## Testing

**Validation Script:**
- [ ] Load golden test set
- [ ] For each question:
  - [ ] Retrieve top-5 chunks
  - [ ] Check if correct post ID in results
  - [ ] Log hit/miss
- [ ] Calculate overall Hit@5
- [ ] Output: `Hit@5 = 0.75 (15/20)`

**Manual Review:**
Review failed cases to understand retrieval gaps.

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
