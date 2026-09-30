# RAG improvements — to do

Each change is measured in the **Evals** tab against the current test set: search changes move **Hit@K / MRR**,
answer changes move **Faithful / Correct**. Judge scores vary about ±0.1 between identical runs; treat smaller
differences as noise.

| # | Topic | Best practice | Status |
|---|---|---|---|
| 1 | **Splitting** | Fixed-size chunks with overlap are a fine start. For Q&A data, split on natural boundaries: question as one chunk, each answer as another, never cut a code block. Count tokens with a real tokenizer. | ✅ Real tokenizer (`Microsoft.ML.Tokenizers`)<br>⏳ Natural-boundary splitting |
| 2 | **Question rewriting** | Short questions are embedded whole (standard). Advanced: rewrite or expand the question first — multi-query (3–5 rephrasings, merged with RRF), HyDE, conversation rewriting for follow-ups, decomposition of complex questions. | ✅ **Multi-query** built as an option (off by default): 3 rephrasings by `gpt-4o-mini`, results merged with RRF; toggle in Chat and Evals, `useMultiQuery` on `POST /ask`<br>⏳ Measure it in Evals (watch vague questions, latency and cost) |
| 3 | **Scoring and combining** | BM25 for keywords instead of a yes/no match; merge vector and keyword results with Reciprocal Rank Fusion (RRF); optionally a reranker model on the top ~20. | ✅ BM25 + RRF built (Qdrant sparse vectors + RRF query) — no gain on this data, so **vector-only is the default**, hybrid stays a toggle<br>⏳ Reranker |
| 4 | **Sources** | Building the source list in code is common; better: the model marks which sources it used ([1], [2]) and only those are shown. | ⏳ |
| 5 | **Tags** | Filtering by metadata is common practice. Safer than excluding: boost chunks whose tags match the predicted tags, so a strong match from another tag can still appear. Verify with the eval. | ⏳ Needs: tags stored on each chunk (re-ingest), a trained tag model (`POST /tags/train`), then a boost in search |

## Notes on tags

- `TagClassifier` (ML.NET, `FeaturizeText` + `LbfgsMaximumEntropy`) predicts **one** tag per question, not several.
- No trained model exists yet: `/tags/suggest` works only after `/tags/train` has saved `models/tag-classifier.zip`.

## Eval findings so far

- Test set: 100 questions, split evenly into **specific** (own words), **keyword** (exact identifiers) and
  **vague** (doesn't know the right terms).
- Search only, top 5, all 100 questions:

| Hit@5 / MRR | All | Specific | Keyword | Vague |
|---|---|---|---|---|
| Vector-only | 0.94 / 0.85 | 0.88 / 0.79 | 1.00 / 1.00 | 0.94 / 0.78 |
| Hybrid (BM25 + RRF) | 0.92 / 0.83 | 0.85 / 0.73 | 1.00 / 1.00 | 0.91 / 0.75 |

- Keyword questions are already solved by embeddings; the room to improve is in **specific** and **vague** questions.
