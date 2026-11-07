import { useState } from 'react'
import { ago } from '@shared/format'
import { useErrorToast } from '@shared/ui/toast'
import { useClientMessages, useSetMessageDone } from '../../api/owner'
import { QueryState } from '../../ui'

const ICON = { Request: '📝', Call: '📞', Video: '🎥' } as const

/** Calls, videos and notes buyers send from their order portals. */
export function RequestsPage() {
  const [openOnly, setOpenOnly] = useState(true)
  const messages = useClientMessages(openOnly)
  const setDone = useSetMessageDone()
  const showError = useErrorToast()

  return (
    <>
      <div style={{ display: 'flex', gap: 6, margin: '2px 0 10px' }}>
        <button className={`btn seg${openOnly ? ' on' : ''}`} onClick={() => setOpenOnly(true)}>Open</button>
        <button className={`btn seg${!openOnly ? ' on' : ''}`} onClick={() => setOpenOnly(false)}>All</button>
      </div>
      <QueryState query={messages} label="client requests" />
      {messages.data && (
        <div className="card pad">
          {messages.data.length === 0 && <div className="muted" style={{ textAlign: 'center', padding: 14 }}>{openOnly ? 'No open requests. 🎉' : 'No requests yet. Buyers can send them from their order portal.'}</div>}
          {messages.data.map((m) => (
            <div key={m.id} style={{ display: 'flex', alignItems: 'flex-start', gap: 10, padding: '11px 2px', borderBottom: '1px solid var(--line)', opacity: m.isDone ? 0.6 : 1 }}>
              <div style={{ width: 30, height: 30, borderRadius: 9, background: 'var(--panel2)', border: '1px solid var(--line)', display: 'grid', placeItems: 'center', flex: 'none' }}>{ICON[m.type]}</div>
              <div style={{ flex: 1 }}>
                <b>{m.clientName ?? 'Client'}</b> <span className="muted" style={{ fontSize: 11 }}>· {m.orderName} · {m.type}{m.requestedWhen ? ` · wants: ${m.requestedWhen}` : ''} · {ago(m.createdAt)}</span>
                <div style={{ fontSize: 12.5, marginTop: 3, whiteSpace: 'pre-line' }}>{m.message}</div>
              </div>
              <div>
                {m.isDone ? (
                  <button className="btn" style={{ padding: '4px 10px', fontSize: 11 }} onClick={() => setDone.mutate({ id: m.id, done: false }, { onError: showError })}>↩ Reopen</button>
                ) : (
                  <button className="btn p" style={{ padding: '4px 10px', fontSize: 11 }} onClick={() => setDone.mutate({ id: m.id, done: true }, { onError: showError })}>✓ Done</button>
                )}
              </div>
            </div>
          ))}
        </div>
      )}
      <div className="hint" style={{ marginTop: 10 }}>Buyers see whether their request is done on their portal. New requests appear here instantly.</div>
    </>
  )
}
