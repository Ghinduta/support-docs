# Story 2.5: Redis Caching for Queries & Responses

**Epic:** 2 - RAG Query Pipeline
**Story ID:** 2.5
**Status:** Draft
**Assigned To:** Dev Agent
**Story Points:** 3

---

## Story

As a **developer**,
I want **the system to cache query embeddings and LLM responses in Redis**,
so that **repeat queries return instantly and reduce API costs**.

---

## Acceptance Criteria

1. Redis client configured via StackExchange.Redis
2. Cache stores: query → embedding (TTL: 24h)
3. Cache stores: query + chunks → LLM response (TTL: 24h)
4. Cache hit returns immediately; cache miss executes full pipeline
5. Integration test: first query misses, second hits cache
6. Logging shows cache hit/miss, cache keys

---

## Tasks

### Task 1: Create Cache Service
- [ ] Create `ICacheService` interface
- [ ] Implement `RedisCacheService`
- [ ] Methods: GetAsync, SetAsync, ExistsAsync
- [ ] Configure connection from environment

### Task 2: Implement Cache Keys
- [ ] Generate deterministic keys (MD5 hash of query text)
- [ ] Key formats: `embedding:{hash}`, `response:{hash}`

### Task 3: Integrate Caching
- [ ] Update RetrievalService to check embedding cache
- [ ] Update LlmService to check response cache
- [ ] Cache on miss, return on hit

### Task 4: Add Configuration
- [ ] Add REDIS_CONNECTION_STRING
- [ ] Add REDIS_CACHE_TTL_HOURS
- [ ] Register in DI

### Task 5: Write Tests
- [ ] Unit test: cache key generation
- [ ] Integration test: set → get → verify
- [ ] Integration test: TTL expiration
- [ ] Test cache hit/miss flow

---

## Dev Notes

**Dependencies:**
- StackExchange.Redis NuGet package

**Configuration:**
```
REDIS_CONNECTION_STRING=localhost:6379
REDIS_CACHE_TTL_HOURS=24
```

**Cache Keys:**
- Embedding: `emb:md5(query)`
- Response: `resp:md5(query+topK)`

---

## Testing

**Integration Tests (require Docker Redis):**
- [ ] Cache embedding, retrieve successfully
- [ ] Cache response, retrieve successfully
- [ ] Verify TTL set correctly
- [ ] Test cache miss → full pipeline
- [ ] Test cache hit → instant return

**Manual Validation:**
1. Query: "How to use async/await?" (cache miss, slow)
2. Same query again (cache hit, instant)
3. Check logs for cache hit

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
