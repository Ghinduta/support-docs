import { useEffect, useState } from 'react'
import { getIngestStatus, runIngest, type IngestionResult, type IngestStatus } from './api'

const ROW_PRESETS = [100, 1000, 10000]
const POLL_MS = 2000

const errorText = (err: unknown) => (err instanceof Error ? err.message : String(err))

function formatDuration(seconds: number) {
  const m = Math.floor(seconds / 60)
  const s = seconds % 60
  return m > 0 ? `${m}m ${s}s` : `${s}s`
}

export default function Admin() {
  const [status, setStatus] = useState<IngestStatus | null>(null)
  const [statusError, setStatusError] = useState<string | null>(null)
  const [maxRows, setMaxRows] = useState(1000)
  const [abort, setAbort] = useState<AbortController | null>(null)
  const [now, setNow] = useState(() => Date.now())
  const [result, setResult] = useState<IngestionResult | null>(null)
  const [ingestError, setIngestError] = useState<string | null>(null)

  const progress = status?.progress
  // Running on the server, whether started here, in another tab, or via Swagger/curl.
  const running = !!abort || !!progress?.running

  async function refreshStatus() {
    try {
      setStatus(await getIngestStatus())
      setStatusError(null)
    } catch (err) {
      setStatusError(errorText(err))
    }
  }

  useEffect(() => {
    getIngestStatus()
      .then(setStatus)
      .catch((err) => setStatusError(errorText(err)))
  }, [])

  // Poll the server while an ingestion runs, and tick the elapsed-time clock.
  useEffect(() => {
    if (!running) return
    const poll = setInterval(() => {
      getIngestStatus()
        .then(setStatus)
        .catch(() => {})
    }, POLL_MS)
    const tick = setInterval(() => setNow(Date.now()), 1000)
    return () => {
      clearInterval(poll)
      clearInterval(tick)
    }
  }, [running])

  async function ingest() {
    const controller = new AbortController()
    setAbort(controller)
    setResult(null)
    setIngestError(null)
    // Show the new run's progress straight away instead of the previous run's.
    setTimeout(refreshStatus, 300)
    try {
      setResult(await runIngest(maxRows, controller.signal))
    } catch (err) {
      setIngestError(controller.signal.aborted ? 'Cancelled.' : errorText(err))
    } finally {
      setAbort(null)
      refreshStatus()
    }
  }

  const elapsedSeconds = progress?.startedAt
    ? Math.max(0, Math.floor(((progress.running ? now : Date.parse(progress.finishedAt ?? '')) - Date.parse(progress.startedAt)) / 1000))
    : 0
  const percent = progress && progress.total > 0 ? Math.round((progress.done / progress.total) * 100) : null
  const chunkCount = status?.chunkCount ?? null

  return (
    <div className="admin">
      <section className="card">
        <div className="card-head">
          <h2>Database</h2>
          <button type="button" className="secondary" onClick={refreshStatus}>
            Refresh
          </button>
        </div>
        {statusError ? (
          <p className="error">{statusError}</p>
        ) : chunkCount === null ? (
          <p className="muted">Checking…</p>
        ) : (
          <>
            <p className="big-number">{chunkCount.toLocaleString()}</p>
            <p className="muted">
              {chunkCount === 0
                ? 'No data yet: the chat has nothing to search. Run an ingestion below.'
                : 'chunks stored in Qdrant and searchable from the chat.'}
            </p>
          </>
        )}
      </section>

      <section className="card">
        <h2>Ingest Stack Overflow data</h2>
        <p className="muted">
          Reads <code>Questions.csv</code>, <code>Answers.csv</code> and <code>Tags.csv</code> from the
          repo's <code>data/stacksample</code> folder, splits them into chunks, embeds them with OpenAI
          and stores them in Qdrant.
        </p>

        <label className="field">
          <span>Questions to load</span>
          <div className="presets">
            {ROW_PRESETS.map((n) => (
              <button
                key={n}
                type="button"
                className={n === maxRows ? 'preset active' : 'preset'}
                onClick={() => setMaxRows(n)}
                disabled={running}
              >
                {n.toLocaleString()}
              </button>
            ))}
            <input
              type="number"
              min={1}
              value={maxRows}
              onChange={(e) => setMaxRows(Math.max(1, Number(e.target.value)))}
              disabled={running}
            />
          </div>
        </label>

        <div className="actions">
          {abort ? (
            <button type="button" className="secondary" onClick={() => abort.abort()}>
              Cancel
            </button>
          ) : (
            <button type="button" onClick={ingest} disabled={running}>
              Run ingestion
            </button>
          )}
          {running && !abort && <span className="muted">An ingestion started elsewhere is running.</span>}
        </div>

        {progress && (progress.running || !result) && progress.startedAt && (
          <div className="progress">
            <div className="progress-head">
              <strong>
                {progress.running
                  ? `Step ${progress.stepNumber}/${progress.totalSteps}: ${progress.step || 'Starting'}`
                  : progress.error
                    ? 'Last ingestion stopped'
                    : 'Last ingestion finished'}
              </strong>
              <span className="muted">
                {progress.maxRows.toLocaleString()} questions · {formatDuration(elapsedSeconds)}
              </span>
            </div>
            {progress.running && (
              <>
                <div
                  className={percent === null ? 'bar indeterminate' : 'bar'}
                  role="progressbar"
                  aria-valuenow={percent ?? undefined}
                  aria-valuemin={0}
                  aria-valuemax={100}
                >
                  <div style={{ width: percent === null ? undefined : `${percent}%` }} />
                </div>
                <p className="muted small">
                  {percent !== null
                    ? `${progress.done.toLocaleString()} of ${progress.total.toLocaleString()} chunks embedded (${percent}%)`
                    : 'Working…'}
                  {abort && ' · Keep this tab open; closing it cancels the ingestion.'}
                </p>
              </>
            )}
            {!progress.running && progress.error && <p className="error">{progress.error}</p>}
          </div>
        )}

        {ingestError && <p className="error">{ingestError}</p>}

        {result && (
          <div className="result">
            <p className={result.validationPassed ? 'ok' : 'error'}>
              {result.validationPassed ? 'Ingestion finished.' : 'Ingestion finished with problems.'}
            </p>
            <dl>
              <dt>Questions loaded</dt>
              <dd>{result.documentsLoaded.toLocaleString()}</dd>
              <dt>Chunks created</dt>
              <dd>{result.chunksCreated.toLocaleString()}</dd>
              <dt>Embeddings generated</dt>
              <dd>{result.embeddingsGenerated.toLocaleString()}</dd>
              <dt>Stored in Qdrant</dt>
              <dd>{result.qdrantUpserts.toLocaleString()}</dd>
              <dt>Skipped (errors)</dt>
              <dd>{result.errors.toLocaleString()}</dd>
              <dt>Duration</dt>
              <dd>{result.durationSeconds.toFixed(1)}s</dd>
            </dl>
            {result.errorMessages.length > 0 && (
              <ul className="error">
                {result.errorMessages.map((m, i) => (
                  <li key={i}>{m}</li>
                ))}
              </ul>
            )}
          </div>
        )}
      </section>
    </div>
  )
}
