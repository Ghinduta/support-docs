# Web chat

React front end for the Stack Overflow RAG API: ask a question, watch the answer stream in, see the cited posts and the cost/latency of each answer.

```bash
docker compose up -d   # from the repo root: API on :5000, Qdrant, Redis
cd web
npm install
npm run dev            # http://localhost:5173
```

The dev server proxies `/api/*` to `http://localhost:5000`; set `API_URL` to point it elsewhere. Data must be ingested first (`POST /ingest` via Swagger).
