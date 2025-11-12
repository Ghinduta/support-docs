# Story 3.2: Logistic Regression Multi-Label Classifier

**Epic:** 3 - Tag Suggestion & Observability
**Story ID:** 3.2
**Status:** Draft
**Assigned To:** Dev Agent
**Story Points:** 4

---

## Story

As a **developer**,
I want **a trained logistic regression model that predicts Stack Overflow tags**,
so that **the system can suggest relevant tags for new questions**.

---

## Acceptance Criteria

1. Multi-label logistic regression trained on Stack Overflow data
2. Model serialized and loaded at API startup
3. Prediction returns top 3-5 tags with confidence scores
4. Unit tests verify predictions on known examples
5. Model performance documented (accuracy on training set)

---

## Tasks

### Task 1: Prepare Training Data
- [ ] Extract questions + tags from ingested data
- [ ] Convert tags to multi-label format
- [ ] Split train/test (80/20)

### Task 2: Train Classifier
- [ ] Use ML.NET multi-class classification
- [ ] Train logistic regression
- [ ] Evaluate on test set
- [ ] Document accuracy

### Task 3: Save/Load Model
- [ ] Serialize model to .zip file
- [ ] Load at API startup
- [ ] Cache in memory

### Task 4: Implement Prediction
- [ ] Create `IMultiLabelClassifier` interface
- [ ] Implement prediction logic
- [ ] Return top-K tags with scores

### Task 5: Write Tests
- [ ] Test prediction on known examples
- [ ] Test top-K selection
- [ ] Test model loading

---

## Dev Notes

**ML.NET Multi-Label:**
Train separate binary classifier per tag, or use multi-class with one-vs-rest.

**Model File:**
Store in `src/StackOverflowRAG.ML/Models/tag-classifier.zip`

**Training Script:**
Create one-time console app or script to train model.

---

## Testing

**Unit Tests:**
- [ ] Load model successfully
- [ ] Predict tags for sample question
- [ ] Verify top 3-5 tags returned
- [ ] Verify confidence scores 0-1

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
