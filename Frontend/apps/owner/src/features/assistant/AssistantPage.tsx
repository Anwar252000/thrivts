import { useEffect, useRef, useState } from 'react'
import { useSession } from '@shared/api/context'
import { isApiError } from '@shared/api/http'
import type { AssistantTurn } from '@shared/api/ownerTypes'
import { useAskAssistant } from '../../api/owner'

const CHIPS = ['What is waiting for my price?', 'Who do I owe the most?', 'Total stock value?', 'What should I focus on today?']
const MAX_TURNS = 12

const bubble = (mine: boolean) => ({
  alignSelf: mine ? 'flex-end' : 'flex-start', maxWidth: '82%', borderRadius: 14, padding: '10px 13px', fontSize: 13, lineHeight: 1.55, whiteSpace: 'pre-wrap' as const,
  ...(mine
    ? { background: 'linear-gradient(135deg,var(--brand),var(--brand2))', color: '#06140c', borderBottomRightRadius: 5, fontWeight: 600 }
    : { background: 'var(--panel2)', border: '1px solid var(--line)', borderBottomLeftRadius: 5 }),
})

/** Ask Claude about the business. The server builds the live data context and holds the API key;
 *  only the conversation is sent from here. */
export function AssistantPage() {
  const me = useSession()!.user
  const ask = useAskAssistant()
  const [history, setHistory] = useState<AssistantTurn[]>([])
  const [failed, setFailed] = useState<string | null>(null)
  const [input, setInput] = useState('')
  const end = useRef<HTMLDivElement>(null)

  // Block body on purpose: newer browsers return a Promise from scrollIntoView, and an effect must
  // return nothing (or a cleanup function).
  useEffect(() => {
    end.current?.scrollIntoView({ behavior: 'smooth', block: 'nearest' })
  }, [history, ask.isPending, failed])

  const send = async (question: string) => {
    const q = question.trim()
    if (!q || ask.isPending) return
    const next = [...history, { role: 'User' as const, content: q }]
    setHistory(next)
    setInput('')
    setFailed(null)
    try {
      const reply = await ask.mutateAsync(next.slice(-MAX_TURNS))
      setHistory([...next, { role: 'Assistant', content: reply.text }])
    } catch (e) {
      setHistory(history) // drop the unanswered question so the conversation stays valid
      setInput(q)
      setFailed(isApiError(e) ? e.message : 'The assistant is unavailable right now — please try again.')
    }
  }

  return (
    <div className="card pad">
      <div style={{ display: 'flex', flexDirection: 'column', gap: 10, maxHeight: 460, overflow: 'auto' }}>
        <div style={bubble(false)}>Hi {me.displayName} 👋 — I'm your VH assistant. I read your live business data (floor, stock value, hisab, client orders, escalations), estimate, and suggest next steps. Ask me anything.</div>
        {history.map((t, i) => <div key={i} style={bubble(t.role === 'User')}>{t.content}</div>)}
        {ask.isPending && <div style={{ ...bubble(false), color: 'var(--mut)' }}>Thinking…</div>}
        {failed && <div style={{ ...bubble(false), color: 'var(--bad)' }}>⚠ {failed}</div>}
        <div ref={end} />
      </div>
      <div style={{ display: 'flex', gap: 7, flexWrap: 'wrap', marginTop: 12 }}>
        {CHIPS.map((q) => (
          <span key={q} role="button" tabIndex={0} onClick={() => void send(q)} onKeyDown={(e) => e.key === 'Enter' && void send(q)}
            style={{ background: 'var(--panel2)', border: '1px solid var(--line2)', color: 'var(--mut)', borderRadius: 999, padding: '6px 12px', fontSize: 11.5, cursor: 'pointer' }}>{q}</span>
        ))}
      </div>
      <div style={{ display: 'flex', gap: 8, marginTop: 12 }}>
        <input value={input} maxLength={4000} onChange={(e) => setInput(e.target.value)} onKeyDown={(e) => e.key === 'Enter' && void send(input)}
          placeholder="Ask about orders, stock, hisab, what to do next…"
          style={{ flex: 1, background: 'var(--panel2)', border: '1px solid var(--line2)', borderRadius: 12, color: 'var(--ink)', padding: '11px 14px', fontSize: 13 }} />
        <button className="btn p" disabled={ask.isPending} onClick={() => void send(input)}>{ask.isPending ? '…' : 'Ask'}</button>
      </div>
      <div className="hint" style={{ marginTop: 10 }}>Powered by Claude, grounded in your live data. The API key stays on the server — it is never sent to this browser.</div>
    </div>
  )
}
