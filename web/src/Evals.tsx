import { useEffect, useState } from 'react'
import {
  generateEvalDataset,
  getEvalDataset,
  getEvalRun,
  listEvalRuns,
  runEval,
  type EvalDataset,
  type EvalRun,
  type EvalSettings,
  type EvalStyleScores,
} from './api'

const SIZES = [20, 50, 100]
const TOP_KS = [3, 5, 10]
const STYLES = ['specific', 'keyword', 'vague'] as const

/** Per-style values under a run's overall score: s = specific, k = keyword, v = vague. */
function StyleBreakdown({ run, pick }: { run: EvalRun; pick: (s: EvalStyleScores) => number }) {
  const parts = STYLES.filter((s) => run.byStyle?.[s]).map(
    (s) => `${s[0]} ${pick(run.byStyle![s]).toFixed(2).replace(/^0/, '')}`,
  )
  return parts.length > 0 ? <div className="muted small">{parts.join(' · ')}</div> : null
}

const errorText = (err: unknown) => (err instanceof Error ? err.message : String(err))

// Runs are only comparable when they used the same questions and the repeatable answer temperature.
const isComparable = (run: EvalRun, datasetId: string | undefined) =>
  run.datasetId === datasetId && run.answerTemperature === 0

export default function Evals() {
  const [dataset, setDataset] = useState<EvalDataset | null>(null)
  const [runs, setRuns] = useState<EvalRun[]>([])
  const [loadError, setLoadError] = useState<string | null>(null)

  const [settings, setSettings] = useState<EvalSettings>({ size: 50, topK: 5, useHybrid: false, useMultiQuery: false })
  const [busy, setBusy] = useState<'generating' | 'running' | null>(null)
  const [abort, setAbort] = useState<AbortController | null>(null)
  const [elapsed, setElapsed] = useState(0)
  const [actionError, setActionError] = useState<string | null>(null)

  const [selected, setSelected] = useState<EvalRun | null>(null)
  const [onlyProblems, setOnlyProblems] = useState(true)
  const [showQuestions, setShowQuestions] = useState(false)

  useEffect(() => {
    Promise.all([getEvalDataset(), listEvalRuns()])
      .then(([d, r]) => {
        setDataset(d)
        setRuns(r)
      })
      .catch((err) => setLoadError(errorText(err)))
  }, [])

  useEffect(() => {
    if (!busy) return
    const started = Date.now()
    const timer = setInterval(() => setElapsed(Math.floor((Date.now() - started) / 1000)), 1000)
    return () => clearInterval(timer)
  }, [busy])

  async function generate() {
    setBusy('generating')
    setElapsed(0)
    setActionError(null)
    try {
      setDataset(await generateEvalDataset())
    } catch (err) {
      setActionError(errorText(err))
    } finally {
      setBusy(null)
    }
  }

  async function run() {
    const controller = new AbortController()
    setAbort(controller)
    setBusy('running')
    setElapsed(0)
    setActionError(null)
    try {
      const result = await runEval(settings, controller.signal)
      setRuns((prev) => [{ ...result, results: null }, ...prev])
      setSelected(result)
    } catch (err) {
      setActionError(controller.signal.aborted ? 'Cancelled.' : errorText(err))
    } finally {
      setBusy(null)
      setAbort(null)
    }
  }

  async function open(run: EvalRun) {
    if (selected?.id === run.id) {
      setSelected(null)
      return
    }
    try {
      setSelected(await getEvalRun(run.id))
    } catch (err) {
      setActionError(errorText(err))
    }
  }

  if (loadError) {
    return (
      <div className="admin">
        <section className="card">
          <p className="error">{loadError}</p>
        </section>
      </div>
    )
  }

  const cases = dataset?.cases ?? []
  const styleCounts = STYLES.map((s) => `${cases.filter((c) => c.style === s).length} ${s}`)
    .filter((label) => !label.startsWith('0 '))
    .join(', ')
  const selectedResults = (selected?.results ?? [])
    .filter((r) => !onlyProblems || r.error || r.rank !== 1 || r.faithfulness < 4 || r.correctness < 4)
    .sort((a, b) => a.correctness + a.faithfulness - (b.correctness + b.faithfulness))

  return (
    <div className="admin">
      <section className="card">
        <div className="card-head">
          <h2>Test set</h2>
          {dataset?.id && <span className="badge">{dataset.id}</span>}
        </div>
        {cases.length === 0 ? (
          <p className="muted">No test set yet. Generate one from the ingested posts.</p>
        ) : (
          <p className="muted">
            {cases.length} questions ({styleCounts}), each written by an LLM
            from an ingested post, so the right post and a reference answer are known.
          </p>
        )}

        <div className="actions spaced">
          <button type="button" className="secondary" onClick={generate} disabled={!!busy}>
            {cases.length === 0 ? 'Generate test set' : 'Regenerate'}
          </button>
          {cases.length > 0 && (
            <button type="button" className="secondary" onClick={() => setShowQuestions((v) => !v)}>
              {showQuestions ? 'Hide questions' : 'Show questions'}
            </button>
          )}
          {busy === 'generating' ? (
            <span className="muted">Generating… {elapsed}s</span>
          ) : (
            cases.length > 0 && (
              <span className="muted">Regenerating makes earlier runs incomparable.</span>
            )
          )}
        </div>

        {showQuestions && (
          <ol className="question-list">
            {cases.map((c) => (
              <li key={c.id}>
                <span className={`style ${c.style}`}>{c.style}</span> {c.question}
                <span className="muted small"> ← {c.originalTitle}</span>
              </li>
            ))}
          </ol>
        )}
      </section>

      <section className="card">
        <h2>Run an eval</h2>
        <p className="muted">
          Each question goes through the same search and answer steps as the chat, then an LLM judge scores the answer.
          About 1 second and $0.0004 per question.
        </p>

        <div className="eval-form">
          <label className="field">
            <span>Questions</span>
            <div className="presets">
              {SIZES.map((n) => (
                <button
                  key={n}
                  type="button"
                  className={n === settings.size ? 'preset active' : 'preset'}
                  onClick={() => setSettings((s) => ({ ...s, size: n }))}
                  disabled={!!busy}
                >
                  {n}
                </button>
              ))}
            </div>
          </label>
          <label className="field">
            <span>Sources (top K)</span>
            <div className="presets">
              {TOP_KS.map((k) => (
                <button
                  key={k}
                  type="button"
                  className={k === settings.topK ? 'preset active' : 'preset'}
                  onClick={() => setSettings((s) => ({ ...s, topK: k }))}
                  disabled={!!busy}
                >
                  {k}
                </button>
              ))}
            </div>
          </label>
          <div className="field toggle-field">
            <span>Search</span>
            <div className="toggles">
              <label className="toggle">
                <input
                  type="checkbox"
                  checked={settings.useHybrid}
                  onChange={(e) => setSettings((s) => ({ ...s, useHybrid: e.target.checked }))}
                  disabled={!!busy}
                />
                <span>Hybrid</span>
              </label>
              <label className="toggle">
                <input
                  type="checkbox"
                  checked={settings.useMultiQuery}
                  onChange={(e) => setSettings((s) => ({ ...s, useMultiQuery: e.target.checked }))}
                  disabled={!!busy}
                />
                <span>Multi-query</span>
              </label>
            </div>
          </div>
        </div>

        <div className="actions">
          {busy === 'running' ? (
            <>
              <button type="button" className="secondary" onClick={() => abort?.abort()}>
                Cancel
              </button>
              <span className="muted">Running {settings.size} questions… {elapsed}s</span>
            </>
          ) : (
            <button type="button" onClick={run} disabled={!!busy || cases.length === 0}>
              Run eval
            </button>
          )}
        </div>
        {actionError && <p className="error">{actionError}</p>}
      </section>

      <section className="card">
        <h2>Runs</h2>
        {runs.length === 0 ? (
          <p className="muted">No runs yet.</p>
        ) : (
          <>
            <p className="muted">
              Hit@K and MRR are exact; the small line under each splits it by question style (s = specific, k = keyword, v = vague). Judge scores (1–5) vary by about ±0.1 between identical runs, so treat smaller
              differences as noise. Greyed runs used other questions or settings and can't be compared.
            </p>
            <div className="table-wrap">
              <table className="runs">
                <thead>
                  <tr>
                    <th>Run</th>
                    <th>Settings</th>
                    <th title="Share of questions whose post was in the top K">Hit@K</th>
                    <th title="How high the right post ranked: 1st = 1, 2nd = 0.5, …">MRR</th>
                    <th title="Answer only says what the sources support">Faithful</th>
                    <th title="Answer agrees with the accepted answer">Correct</th>
                    <th title="Answer addresses the question">Relevant</th>
                    <th>Latency</th>
                    <th>Cost</th>
                  </tr>
                </thead>
                <tbody>
                  {runs.map((r) => (
                    <tr
                      key={r.id}
                      className={[
                        isComparable(r, dataset?.id) ? '' : 'stale',
                        selected?.id === r.id ? 'selected' : '',
                      ].join(' ')}
                      onClick={() => open(r)}
                    >
                      <td>
                        {new Date(r.createdAt).toLocaleString([], {
                          month: 'short',
                          day: 'numeric',
                          hour: '2-digit',
                          minute: '2-digit',
                        })}
                        {!isComparable(r, dataset?.id) && (
                          <div className="muted small">
                            {r.datasetId !== dataset?.id ? 'other test set' : `temp ${r.answerTemperature}`}
                          </div>
                        )}
                      </td>
                      <td>
                        {r.caseCount} Qs · top {r.settings.topK} · {r.settings.useHybrid ? 'hybrid' : 'vector'}
                        {r.settings.useMultiQuery && ' · multi-query'}
                      </td>
                      <td>
                        {r.hitRate.toFixed(2)}
                        <StyleBreakdown run={r} pick={(s) => s.hitRate} />
                      </td>
                      <td>
                        {r.mrr.toFixed(2)}
                        <StyleBreakdown run={r} pick={(s) => s.mrr} />
                      </td>
                      <td>{r.avgFaithfulness.toFixed(2)}</td>
                      <td>{r.avgCorrectness.toFixed(2)}</td>
                      <td>{r.avgRelevance.toFixed(2)}</td>
                      <td>{(r.avgLatencyMs / 1000).toFixed(1)}s</td>
                      <td>${r.totalCost.toFixed(3)}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </>
        )}
      </section>

      {selected && (
        <section className="card">
          <div className="card-head">
            <h2>Run {selected.id}</h2>
            <label className="toggle muted">
              <input type="checkbox" checked={onlyProblems} onChange={(e) => setOnlyProblems(e.target.checked)} />
              <span>Only problems</span>
            </label>
          </div>
          <p className="muted">
            Worst first. A problem is: right post not ranked 1st, or faithfulness / correctness below 4.
          </p>
          {selectedResults.length === 0 ? (
            <p className="muted">Nothing to show.</p>
          ) : (
            <ul className="case-list">
              {selectedResults.map((r) => (
                <li key={r.caseId}>
                  <div className="case-head">
                    <span className={`style ${r.style}`}>{r.style}</span>
                    <strong>{r.question}</strong>
                  </div>
                  {r.error ? (
                    <p className="error">{r.error}</p>
                  ) : (
                    <>
                      <div className="stats">
                        <span className={r.rank === null ? 'bad' : r.rank === 1 ? '' : 'warn'}>
                          {r.rank === null ? 'post not found' : `post ranked #${r.rank}`}
                        </span>
                        <span className={r.faithfulness < 4 ? 'bad' : ''}>faithful {r.faithfulness}</span>
                        <span className={r.correctness < 4 ? 'bad' : ''}>correct {r.correctness}</span>
                        <span>relevant {r.relevance}</span>
                        <a
                          href={`https://stackoverflow.com/questions/${r.expectedPostId}`}
                          target="_blank"
                          rel="noreferrer"
                        >
                          expected post
                        </a>
                      </div>
                      <p className="muted small">{r.judgeReason}</p>
                      {r.searchedQueries && r.searchedQueries.length > 0 && (
                        <p className="muted small">Also searched: {r.searchedQueries.join(' · ')}</p>
                      )}
                      <details>
                        <summary className="muted small">Answer</summary>
                        <p className="answer-text">{r.answer}</p>
                      </details>
                    </>
                  )}
                </li>
              ))}
            </ul>
          )}
        </section>
      )}
    </div>
  )
}
