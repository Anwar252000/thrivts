import { useState } from 'react'
import { ago, initial } from '@shared/format'
import { useErrorToast, useToast } from '@shared/ui/toast'
import type { Escalation } from '@shared/api/types'
import { useEscalationActions, useEscalations } from '../../api/owner'
import { Modal, QueryState } from '../../ui'

/** Two-way escalations from the team: reply (the sender sees it on their dashboard) and resolve. */
export function EscalationsPage() {
  const escalations = useEscalations()
  const actions = useEscalationActions()
  const showError = useErrorToast()
  const [replying, setReplying] = useState<Escalation | null>(null)
  const items = escalations.data?.items ?? []
  const open = items.filter((e) => !e.isResolved).length

  return (
    <>
      <QueryState query={escalations} label="escalations" />
      {escalations.data && (
        <div className="card pad">
          {items.length === 0 && <div className="muted" style={{ textAlign: 'center', padding: 14 }}>No escalations. Anyone on the team can message you here.</div>}
          {items.map((e) => (
            <div key={e.id} style={{ display: 'flex', alignItems: 'flex-start', gap: 10, padding: '11px 2px', borderBottom: '1px solid var(--line)' }}>
              <div style={{ width: 30, height: 30, borderRadius: 9, background: 'var(--panel2)', border: '1px solid var(--line)', display: 'grid', placeItems: 'center', fontWeight: 800, color: 'var(--mut)', flex: 'none' }}>{initial(e.fromName)}</div>
              <div style={{ flex: 1 }}>
                <b>{e.fromName}</b> <span className="muted" style={{ fontSize: 11 }}>{e.department ? `· ${e.department} ` : ''}{e.source === 'Blocker' ? '· blocker ' : ''}· {ago(e.createdAt)}</span>
                <div className="muted" style={{ fontSize: 12, marginTop: 2, whiteSpace: 'pre-line' }}>{e.message}</div>
                {e.reply && <div style={{ fontSize: 12, marginTop: 4, color: 'var(--brand2)' }}>↳ {e.reply}</div>}
              </div>
              <div style={{ textAlign: 'right', whiteSpace: 'nowrap' }}>
                {e.isResolved ? (
                  <span className="tag t-good">resolved</span>
                ) : (
                  <>
                    <button className="btn" style={{ padding: '4px 10px', fontSize: 11 }} onClick={() => setReplying(e)}>Reply</button>{' '}
                    <button className="btn" style={{ padding: '4px 10px', fontSize: 11 }} onClick={() => actions.resolve.mutate({ id: e.id, resolved: true }, { onError: showError })}>Resolve</button>
                  </>
                )}
              </div>
            </div>
          ))}
        </div>
      )}
      <div className="hint" style={{ marginTop: 10 }}>Two-way: your reply reaches the sender; mark resolved to clear. {open} open.</div>
      {replying && <ReplyDialog escalation={replying} onClose={() => setReplying(null)} />}
    </>
  )
}

function ReplyDialog({ escalation: e, onClose }: { escalation: Escalation; onClose: () => void }) {
  const { reply } = useEscalationActions()
  const toast = useToast()
  const showError = useErrorToast()
  const [text, setText] = useState(e.reply ?? '')
  const [resolve, setResolve] = useState(false)
  const send = async () => {
    if (!text.trim()) return toast('Write a reply first', true)
    try {
      await reply.mutateAsync({ id: e.id, reply: text.trim(), resolve })
      toast(`Reply sent to ${e.fromName}`)
      onClose()
    } catch (err) {
      showError(err)
    }
  }
  return (
    <Modal onClose={onClose} width={480}>
      <div style={{ fontSize: 16, fontWeight: 800 }}>Reply to {e.fromName}</div>
      <div className="muted" style={{ fontSize: 12, margin: '4px 0 12px', whiteSpace: 'pre-line' }}>{e.message}</div>
      <div className="fld"><label>Your reply</label><textarea autoFocus rows={4} maxLength={1000} value={text} onChange={(ev) => setText(ev.target.value)} /></div>
      <label style={{ display: 'flex', gap: 8, alignItems: 'center', fontSize: 12.5, marginTop: 10 }}><input type="checkbox" checked={resolve} onChange={(ev) => setResolve(ev.target.checked)} /> Also mark it resolved</label>
      <div className="row-actions" style={{ marginTop: 14 }}>
        <button className="btn" onClick={onClose}>Cancel</button>
        <button className="btn p" disabled={reply.isPending} onClick={() => void send()}>Send reply</button>
      </div>
    </Modal>
  )
}
