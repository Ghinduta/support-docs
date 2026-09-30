export type Citation = {
  postId: number
  title: string
  url: string
  relevanceScore: number
}

export type AnswerStats = {
  latencyMs: number
  tokensUsed: number
  estimatedCost: number
  cacheHit: boolean
  retrievedChunks: number
}

export type AskOptions = {
  topK: number
  useHybrid: boolean
  useMultiQuery: boolean
}

type Handlers = {
  onText: (text: string) => void
  onCitations: (citations: Citation[]) => void
  onStats: (stats: AnswerStats) => void
  onSearchedQueries: (queries: string[]) => void
}

// The API serializes citations with default (PascalCase) System.Text.Json settings.
type RawCitation = { PostId: number; Title: string; Url: string; RelevanceScore: number }

const toCitation = (c: RawCitation): Citation => ({
  postId: c.PostId,
  title: c.Title,
  url: c.Url,
  relevanceScore: c.RelevanceScore,
})

export type IngestionResult = {
  documentsLoaded: number
  chunksCreated: number
  embeddingsGenerated: number
  qdrantUpserts: number
  durationSeconds: number
  errors: number
  totalChunksInQdrant: number
  validationPassed: boolean
  errorMessages: string[]
}

async function problemMessage(response: Response): Promise<string> {
  const problem = await response.json().catch(() => null)
  return problem?.detail ?? problem?.error ?? `Request failed (${response.status})`
}

export type IngestionProgress = {
  running: boolean
  step: string
  stepNumber: number
  totalSteps: number
  done: number
  total: number
  maxRows: number
  startedAt: string | null
  finishedAt: string | null
  error: string | null
}

export type IngestStatus = { chunkCount: number; progress: IngestionProgress | null }

export async function getIngestStatus(): Promise<IngestStatus> {
  const response = await fetch('/api/ingest/status')
  if (!response.ok) throw new Error(await problemMessage(response))
  return response.json()
}

/** POST /ingest. Returns the result body for both success (200) and validation failure (400). */
export async function runIngest(maxRows: number, signal: AbortSignal): Promise<IngestionResult> {
  const response = await fetch('/api/ingest', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ maxRows }),
    signal,
  })
  if (response.status === 400) return response.json()
  if (!response.ok) throw new Error(await problemMessage(response))
  return response.json()
}

export type EvalCase = {
  id: number
  question: string
  style: 'specific' | 'keyword' | 'vague'
  expectedPostId: number
  originalTitle: string
}

export type EvalDataset = { id: string; cases: EvalCase[] }

export type EvalSettings = { size: number; topK: number; useHybrid: boolean; useMultiQuery: boolean }

export type EvalCaseResult = {
  caseId: number
  question: string
  style: string
  expectedPostId: number
  searchedQueries: string[] | null
  retrievedPostIds: number[]
  rank: number | null
  answer: string
  faithfulness: number
  relevance: number
  correctness: number
  judgeReason: string
  latencyMs: number
  cost: number
  error: string | null
}

export type EvalRun = {
  id: string
  createdAt: string
  settings: EvalSettings
  datasetId: string | null
  answerTemperature: number
  caseCount: number
  hitRate: number
  mrr: number
  avgFaithfulness: number
  avgRelevance: number
  avgCorrectness: number
  avgLatencyMs: number
  totalCost: number
  errors: number
  // Missing on runs saved before per-style scores existed
  byStyle?: Record<string, EvalStyleScores>
  results: EvalCaseResult[] | null
}

export type EvalStyleScores = {
  count: number
  hitRate: number
  mrr: number
  avgFaithfulness: number
  avgCorrectness: number
}

async function getJson<T>(url: string, init?: RequestInit): Promise<T> {
  const response = await fetch(url, init)
  if (!response.ok) throw new Error(await problemMessage(response))
  return response.json()
}

const postJson = (body?: unknown, signal?: AbortSignal): RequestInit => ({
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: body === undefined ? undefined : JSON.stringify(body),
  signal,
})

export const getEvalDataset = () => getJson<EvalDataset>('/api/evals/dataset')
export const generateEvalDataset = () => getJson<EvalDataset>('/api/evals/dataset', postJson())
export const listEvalRuns = () => getJson<EvalRun[]>('/api/evals/runs')
export const getEvalRun = (id: string) => getJson<EvalRun>(`/api/evals/runs/${id}`)
export const runEval = (settings: EvalSettings, signal: AbortSignal) =>
  getJson<EvalRun>('/api/evals/runs', postJson(settings, signal))

/**
 * POST /ask and read its Server-Sent Events stream.
 * Events arrive as `data: {"type": "metadata" | "text" | "citations" | "final_metadata" | "done", ...}` lines.
 */
export async function streamAsk(
  question: string,
  options: AskOptions,
  handlers: Handlers,
  signal: AbortSignal,
): Promise<void> {
  const response = await fetch('/api/ask', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ question, ...options }),
    signal,
  })

  if (!response.ok) throw new Error(await problemMessage(response))

  // With no matching chunks the API answers with plain JSON instead of a stream.
  if (!response.headers.get('content-type')?.includes('text/event-stream')) {
    const body = await response.json()
    handlers.onText(body.answer)
    return
  }

  const reader = response.body!.pipeThrough(new TextDecoderStream()).getReader()
  let buffer = ''

  for (;;) {
    const { value, done } = await reader.read()
    if (done) break
    buffer += value

    const lines = buffer.split('\n')
    buffer = lines.pop() ?? ''

    for (const line of lines) {
      if (!line.startsWith('data: ')) continue
      const event = JSON.parse(line.slice('data: '.length))

      if (event.type === 'text') handlers.onText(event.content)
      else if (event.type === 'metadata' && event.searchedQueries) handlers.onSearchedQueries(event.searchedQueries)
      else if (event.type === 'citations') handlers.onCitations(event.sources.map(toCitation))
      else if (event.type === 'final_metadata') handlers.onStats(event)
    }
  }
}
