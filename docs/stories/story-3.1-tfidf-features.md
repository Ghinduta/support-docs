# Story 3.1: TF-IDF Feature Extraction for Tag Suggestion

**Epic:** 3 - Tag Suggestion & Observability
**Story ID:** 3.1
**Status:** Ready for Review
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
- [x] Add Microsoft.ML NuGet package
- [x] Create ML models folder structure

### Task 2: Create Tag Training Data Model
- [x] Create `TagTrainingData` class
- [x] Properties: Text (title+body), Tags (comma-separated)

### Task 3: Implement TF-IDF Vectorizer
- [x] Create `ITfidfVectorizer` interface
- [x] Implement using ML.NET TextFeaturizingEstimator
- [x] Train on Stack Overflow data
- [x] Save fitted model

### Task 4: Implement Feature Extraction
- [x] Method: `ExtractFeatures(title, body)`
- [x] Return feature vector

### Task 5: Write Tests
- [x] Test feature extraction
- [x] Test vocabulary size reasonable
- [x] Test model serialization/loading

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
- [x] Extract features for sample question
- [x] Verify feature vector size
- [x] Test with empty input

---

## Dev Agent Record

### Agent Model Used
claude-sonnet-4-5-20250929

### Debug Log References
None

### Completion Notes
- Implemented TF-IDF vectorizer using ML.NET with n-gram (unigrams and bigrams) support
- Pipeline includes: text normalization → tokenization → stop word removal → TF-IDF weighting
- All 13 unit tests passing
- Vocabulary size tested to be reasonable (10-10000 features for test dataset)
- Model serialization/deserialization working correctly

### File List
- src/StackOverflowRAG.ML/Models/TagTrainingData.cs (new)
- src/StackOverflowRAG.ML/Interfaces/ITfidfVectorizer.cs (new)
- src/StackOverflowRAG.ML/Services/TfidfVectorizer.cs (new)
- src/StackOverflowRAG.Tests/ML/TfidfVectorizerTests.cs (new)
- src/StackOverflowRAG.ML/StackOverflowRAG.ML.csproj (modified - added Microsoft.ML 5.0.0)

### Change Log
- Added Microsoft.ML NuGet package (v5.0.0)
- Created folder structure: Models, Interfaces, Services
- Implemented TagTrainingData with LoadColumn attributes for CSV loading
- Implemented ITfidfVectorizer interface with Train, ExtractFeatures, SaveModel, LoadModel methods
- Implemented TfidfVectorizer service with ML.NET pipeline
- Created comprehensive unit tests with 13 test cases covering all functionality

---

**Created:** 2025-11-08
**Last Updated:** 2025-11-08
