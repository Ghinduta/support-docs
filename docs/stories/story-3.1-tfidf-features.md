# Story 3.1: TF-IDF Feature Extraction for Tag Suggestion

**Epic:** 3 - Tag Suggestion & Observability
**Story ID:** 3.1
**Status:** Draft
**Assigned To:** Dev Agent
**Story Points:** 3

---

## Story

As a **developer**,
I want **the system to extract TF-IDF features from question text**,
so that **tag prediction has meaningful input features**.

---

## Acceptance Criteria

1. TF-IDF vectorizer using ML.NET
2. Training data: Stack Overflow questions + tags from ingested dataset
3. Feature vectorization for input (title + body)
4. Vocabulary size documented
5. Unit tests verify feature extraction

---

## Tasks

### Task 1: Setup ML.NET
- [ ] Add Microsoft.ML NuGet package
- [ ] Create ML models folder structure

### Task 2: Create Tag Training Data Model
- [ ] Create `TagTrainingData` class
- [ ] Properties: Text (title+body), Tags (comma-separated)

### Task 3: Implement TF-IDF Vectorizer
- [ ] Create `ITfidfVectorizer` interface
- [ ] Implement using ML.NET TextFeaturizingEstimator
- [ ] Train on Stack Overflow data
- [ ] Save fitted model

### Task 4: Implement Feature Extraction
- [ ] Method: `ExtractFeatures(title, body)`
- [ ] Return feature vector

### Task 5: Write Tests
- [ ] Test feature extraction
- [ ] Test vocabulary size reasonable
- [ ] Test model serialization/loading

---

## Dev Notes

**ML.NET Pipeline:**
```csharp
var pipeline = mlContext.Transforms.Text
    .FeaturizeText("Features", "Text")
    .Append(...);
```

**Training Data:**
Use questions from ingested CSV to train TF-IDF vocabulary.

---

## Testing

**Unit Tests:**
- [ ] Extract features for sample question
- [ ] Verify feature vector size
- [ ] Test with empty input

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
