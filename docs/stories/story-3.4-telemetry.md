# Story 3.4: Telemetry & Metrics Logging

**Epic:** 3 - Tag Suggestion & Observability
**Story ID:** 3.4
**Status:** Draft
**Assigned To:** Dev Agent
**Story Points:** 2

---

## Story

As a **developer**,
I want **comprehensive telemetry logged for all requests**,
so that **I can measure system performance, cost, and cache effectiveness**.

---

## Acceptance Criteria

1. Telemetry service logs per-request metrics
2. `/ask` metrics: timestamp, query, latency, tokens, cost, cache hit, retrieval count
3. `/tags/suggest` metrics: timestamp, input length, predicted tags, latency
4. Ingestion metrics: docs loaded, chunks, embeddings, duration
5. Structured JSON logs to console
6. README documents log interpretation and cost formulas

---

## Tasks

### Task 1: Implement Telemetry Service
- [ ] Create `ITelemetryService` interface
- [ ] Implement `TelemetryService`
- [ ] Methods: LogQueryMetrics, LogIngestionMetrics, LogTagMetrics

### Task 2: Create Metadata Models
- [ ] Create `QueryMetadata` class
- [ ] Create `IngestionMetadata` class
- [ ] Create `TagMetadata` class

### Task 3: Integrate Telemetry
- [ ] Update `/ask` endpoint to log metrics
- [ ] Update `/ingest` endpoint to log metrics
- [ ] Update `/tags/suggest` to log metrics

### Task 4: Configure Serilog
- [ ] Output structured JSON logs
- [ ] Configure log levels
- [ ] Add timestamp formatting

### Task 5: Document Cost Calculation
- [ ] Document OpenAI pricing
- [ ] Add cost estimation formulas
- [ ] Update README with examples

---

## Dev Notes

**Cost Formulas:**
- text-embedding-3-small: $0.00002 per 1K tokens
- gpt-4o-mini input: $0.00015 per 1K tokens
- gpt-4o-mini output: $0.0006 per 1K tokens

**Log Format:**
```json
{
  "timestamp": "2025-11-08T10:30:45Z",
  "eventType": "QueryCompleted",
  "question": "How to use async?",
  "latencyMs": 1834,
  "tokensInput": 1200,
  "tokensOutput": 350,
  "estimatedCost": 0.00039,
  "cacheHit": false
}
```

---

## Testing

**Unit Tests:**
- [ ] Test cost calculation formulas
- [ ] Test metadata creation

**Integration Tests:**
- [ ] Execute /ask → verify metrics logged
- [ ] Execute /ingest → verify metrics logged
- [ ] Verify JSON structure

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
