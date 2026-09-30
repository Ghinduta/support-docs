import { useEffect, useRef, useState, type FormEvent, type KeyboardEvent } from 'react'
import Markdown from 'react-markdown'
import Admin from './Admin'
import Evals from './Evals'
import { streamAsk, type AnswerStats, type Citation } from './api'

type Message =
  | { role: 'user'; text: string }
  | {
      role: 'assistant'
      text: string
      citations: Citation[]
      stats?: AnswerStats
      searchType: 'hybrid' | 'vector-only'
      multiQuery: boolean
      searchedQueries?: string[]
      error?: string
      pending: boolean
    }

const EXAMPLES = [
  'How do I use async/await in C#?',
  'How can I merge two dictionaries in Python?',
  'What is the difference between let and var in JavaScript?',
]

export default function App() {
  const [messages, setMessages] = useState<Message[]>([])
  const [input, setInput] = useState('')
  const [useHybrid, setUseHybrid] = useState(false)
  const [useMultiQuery, setUseMultiQuery] = useState(false)
  const [topK, setTopK] = useState(5)
  const [abort, setAbort] = useState<AbortController | null>(null)
  const [view, setView] = useState<'chat' | 'admin' | 'evals'>('chat')
  const bottomRef = useRef<HTMLDivElement>(null)

  useEffect(() => {
    bottomRef.current?.scrollIntoView({ behavior: 'smooth' })
  }, [messages])

  const updateLast = (patch: (m: Extract<Message, { role: 'assistant' }>) => Partial<Message>) =>
    setMessages((prev) => {
      const last = prev[prev.length - 1]
      if (last?.role !== 'assistant') return prev
      return [...prev.slice(0, -1), { ...last, ...patch(last) } as Message]
    })

  async function ask(question: string) {
    const trimmed = question.trim()
    if (trimmed.length < 3 || abort) return

    const controller = new AbortController()
    setAbort(controller)
    setInput('')
    setMessages((prev) => [
      ...prev,
      { role: 'user', text: trimmed },
      {
        role: 'assistant',
        text: '',
        citations: [],
        searchType: useHybrid ? 'hybrid' : 'vector-only',
        multiQuery: useMultiQuery,
        pending: true,
      },
    ])

    try {
      await streamAsk(
        trimmed,
        { topK, useHybrid, useMultiQuery },
        {
          onText: (chunk) => updateLast((m) => ({ text: m.text + chunk })),
          onCitations: (citations) => updateLast(() => ({ citations })),
          onStats: (stats) => updateLast(() => ({ stats })),
          onSearchedQueries: (searchedQueries) => updateLast(() => ({ searchedQueries })),
        },
        controller.signal,
      )
    } catch (err) {
      if (!controller.signal.aborted) {
        updateLast(() => ({ error: err instanceof Error ? err.message : String(err) }))
      }
    } finally {
      updateLast(() => ({ pending: false }))
      setAbort(null)
    }
  }

  function onSubmit(e: FormEvent) {
    e.preventDefault()
    ask(input)
  }

  function onKeyDown(e: KeyboardEvent<HTMLTextAreaElement>) {
    if (e.key === 'Enter' && !e.shiftKey) {
      e.preventDefault()
      ask(input)
    }
  }

  return (
    <div className="app">
      <header className="header">
        <div className="brand">
          <span className="logo">SO</span>
          <div>
            <h1>Stack Overflow RAG</h1>
            <p>Answers grounded in real Stack Overflow posts</p>
          </div>
        </div>
        <nav className="tabs">
          <button type="button" className={view === 'chat' ? 'active' : ''} onClick={() => setView('chat')}>
            Chat
          </button>
          <button type="button" className={view === 'admin' ? 'active' : ''} onClick={() => setView('admin')}>
            Admin
          </button>
          <button type="button" className={view === 'evals' ? 'active' : ''} onClick={() => setView('evals')}>
            Evals
          </button>
        </nav>
        <div className="settings" hidden={view !== 'chat'}>
          <label className="toggle">
            <input
              type="checkbox"
              checked={useHybrid}
              onChange={(e) => setUseHybrid(e.target.checked)}
            />
            <span>Hybrid search</span>
          </label>
          <label className="toggle" title="The AI rewrites your question into 3 extra phrasings and searches with all of them">
            <input
              type="checkbox"
              checked={useMultiQuery}
              onChange={(e) => setUseMultiQuery(e.target.checked)}
            />
            <span>Multi-query</span>
          </label>
          <label className="topk">
            <span>Sources</span>
            <select value={topK} onChange={(e) => setTopK(Number(e.target.value))}>
              {[3, 5, 10, 20].map((k) => (
                <option key={k} value={k}>
                  {k}
                </option>
              ))}
            </select>
          </label>
        </div>
      </header>

      {/* All views stay mounted so a running chat, ingestion or eval survives switching tabs. */}
      <div className="admin-view" hidden={view !== 'admin'}>
        <Admin />
      </div>
      <div className="admin-view" hidden={view !== 'evals'}>
        <Evals />
      </div>

      <main className="thread" hidden={view !== 'chat'}>
        {messages.length === 0 && (
          <div className="empty">
            <h2>Ask a programming question</h2>
            <p>The answer is written by an LLM using only the Stack Overflow posts it retrieves.</p>
            <div className="examples">
              {EXAMPLES.map((q) => (
                <button key={q} type="button" onClick={() => ask(q)}>
                  {q}
                </button>
              ))}
            </div>
          </div>
        )}

        {messages.map((m, i) =>
          m.role === 'user' ? (
            <div key={i} className="msg user">
              {m.text}
            </div>
          ) : (
            <div key={i} className="msg assistant">
              {m.error ? (
                <p className="error">{m.error}</p>
              ) : m.text ? (
                <div className="answer">
                  <Markdown>{m.text}</Markdown>
                </div>
              ) : (
                <p className="thinking">
                  {m.pending ? (m.multiQuery ? 'Rewriting question and searching…' : 'Searching posts…') : 'Stopped.'}
                </p>
              )}

              {m.searchedQueries && m.searchedQueries.length > 0 && (
                <div className="searched">
                  <span className="muted small">Also searched:</span>
                  <ul>
                    {m.searchedQueries.map((q) => (
                      <li key={q}>{q}</li>
                    ))}
                  </ul>
                </div>
              )}

              {m.citations.length > 0 && (
                <ol className="sources">
                  {m.citations.map((c) => (
                    <li key={c.postId}>
                      <a href={c.url} target="_blank" rel="noreferrer">
                        {c.title}
                      </a>
                      <span className="score">{c.relevanceScore.toFixed(2)}</span>
                    </li>
                  ))}
                </ol>
              )}

              {m.stats && (
                <div className="stats">
                  <span>{m.searchType}</span>
                  {m.multiQuery && <span>multi-query</span>}
                  <span>{(m.stats.latencyMs / 1000).toFixed(2)}s</span>
                  {m.stats.cacheHit ? (
                    <span className="cached">cached</span>
                  ) : (
                    <>
                      <span>{m.stats.tokensUsed.toLocaleString()} tokens</span>
                      <span>${m.stats.estimatedCost.toFixed(5)}</span>
                    </>
                  )}
                </div>
              )}
            </div>
          ),
        )}
        <div ref={bottomRef} />
      </main>

      <form className="composer" onSubmit={onSubmit} hidden={view !== 'chat'}>
        <textarea
          value={input}
          onChange={(e) => setInput(e.target.value)}
          onKeyDown={onKeyDown}
          placeholder="Ask about C#, Python, JavaScript…"
          rows={1}
        />
        {abort ? (
          <button type="button" onClick={() => abort.abort()}>
            Stop
          </button>
        ) : (
          <button type="submit" disabled={input.trim().length < 3}>
            Send
          </button>
        )}
      </form>
    </div>
  )
}
