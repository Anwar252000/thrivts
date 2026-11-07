import { useState } from 'react'
import { ago } from '@shared/format'
import { useErrorToast, useToast } from '@shared/ui/toast'
import type { Goal, GoalPeriod } from '@shared/api/ownerTypes'
import { useApprovalsAndGoals, useManageActions } from '../../api/owner'
import { Modal, QueryState } from '../../ui'

const PERIOD: Record<GoalPeriod, string> = { Monthly: 'monthly', SixMonth: '6 months', Yearly: 'yearly' }

export function ManagePage() {
  const data = useApprovalsAndGoals()
  const actions = useManageActions()
  const toast = useToast()
  const showError = useErrorToast()
  const [goal, setGoal] = useState<Goal | 'new' | null>(null)
  const approvals = data.data?.approvals ?? []
  const goals = data.data?.goals ?? []

  const decide = (id: string, approve: boolean) => actions.decide.mutate({ id, approve }, { onError: showError, onSuccess: () => toast(approve ? 'Approved' : 'Rejected') })

  return (
    <>
      <QueryState query={data} label="approvals" />
      {data.data && (
        <>
          <div className="sec-t">Approvals <span style={{ color: 'var(--faint)', fontSize: 11, textTransform: 'none', letterSpacing: 0 }}>{approvals.filter((a) => a.status === 'Pending').length} pending</span></div>
          <div className="card pad">
            {approvals.length === 0 && <div className="muted" style={{ textAlign: 'center', padding: 12 }}>No approvals yet. Your GM can send spends and decisions here for your OK.</div>}
            {approvals.map((a) => (
              <div key={a.id} style={{ display: 'flex', alignItems: 'center', gap: 10, padding: '10px 2px', borderBottom: '1px solid var(--line)' }}>
                <div style={{ flex: 1 }}><b>{a.description}</b><div className="muted" style={{ fontSize: 11 }}>by {a.fromName}{a.valueText ? ` · ${a.valueText}` : ''} · {ago(a.createdAt)}</div></div>
                {a.status === 'Pending' ? (
                  <>
                    <button className="btn danger" style={{ padding: '5px 11px', fontSize: 11 }} disabled={actions.decide.isPending} onClick={() => decide(a.id, false)}>Reject</button>
                    <button className="btn p" style={{ padding: '5px 11px', fontSize: 11 }} disabled={actions.decide.isPending} onClick={() => decide(a.id, true)}>Approve</button>
                  </>
                ) : (
                  <span className={`tag ${a.status === 'Approved' ? 't-good' : 't-warn'}`}>{a.status.toLowerCase()}</span>
                )}
              </div>
            ))}
          </div>

          <div className="sec-t">Goals &amp; targets</div>
          <div className="card pad">
            {goals.length === 0 && <div className="muted" style={{ textAlign: 'center', padding: 10 }}>No goals yet.</div>}
            {goals.map((g) => (
              <div key={g.id} className="clk" onClick={() => setGoal(g)} style={{ display: 'flex', alignItems: 'center', gap: 13, padding: '12px 2px', borderBottom: '1px solid var(--line)', cursor: 'pointer' }}>
                <div style={{ width: 44, height: 44, flex: 'none', borderRadius: '50%', background: `conic-gradient(var(--brand) calc(${g.progress}*1%),var(--line) 0)`, display: 'grid', placeItems: 'center' }}>
                  <div style={{ width: 33, height: 33, borderRadius: '50%', background: 'var(--card)', display: 'grid', placeItems: 'center', fontSize: 11, fontWeight: 800 }}>{g.progress}%</div>
                </div>
                <div style={{ flex: 1 }}><b>{g.label}</b><div className="muted" style={{ fontSize: 12 }}>{[g.target, PERIOD[g.period]].filter(Boolean).join(' · ')}</div></div>
                <span className="muted" style={{ fontSize: 11 }}>edit ›</span>
              </div>
            ))}
            <button className="btn" style={{ marginTop: 10 }} onClick={() => setGoal('new')}>➕ Add goal</button>
          </div>
        </>
      )}
      {goal && <GoalForm goal={goal === 'new' ? null : goal} onClose={() => setGoal(null)} />}
    </>
  )
}

function GoalForm({ goal, onClose }: { goal: Goal | null; onClose: () => void }) {
  const { saveGoal, deleteGoal } = useManageActions()
  const toast = useToast()
  const showError = useErrorToast()
  const [f, setF] = useState({ label: goal?.label ?? '', target: goal?.target ?? '', period: goal?.period ?? 'Monthly' as GoalPeriod, progress: goal?.progress ?? 0 })

  const submit = async () => {
    if (!f.label.trim()) return toast('Name the goal', true)
    try {
      await saveGoal.mutateAsync({ id: goal?.id ?? null, label: f.label.trim(), target: f.target.trim() || null, period: f.period, progress: Math.max(0, Math.min(100, f.progress)) })
      onClose()
    } catch (e) {
      showError(e)
    }
  }
  const remove = async () => {
    if (!goal || !confirm(`Delete the goal "${goal.label}"?`)) return
    try {
      await deleteGoal.mutateAsync(goal.id)
      onClose()
    } catch (e) {
      showError(e)
    }
  }

  return (
    <Modal onClose={onClose} width={440}>
      <div style={{ fontSize: 17, fontWeight: 800, marginBottom: 12 }}>{goal ? 'Edit goal' : 'New goal'}</div>
      <div className="form-grid">
        <div className="fld" style={{ gridColumn: '1/-1' }}><label>Goal</label><input autoFocus value={f.label} maxLength={120} onChange={(e) => setF({ ...f, label: e.target.value })} /></div>
        <div className="fld"><label>Target</label><input value={f.target} maxLength={120} placeholder="e.g. 50,000 pcs" onChange={(e) => setF({ ...f, target: e.target.value })} /></div>
        <div className="fld"><label>Period</label>
          <select value={f.period} onChange={(e) => setF({ ...f, period: e.target.value as GoalPeriod })}>
            <option value="Monthly">Monthly</option><option value="SixMonth">6 months</option><option value="Yearly">Yearly</option>
          </select></div>
        <div className="fld"><label>Progress %</label><input type="number" min={0} max={100} value={f.progress} onChange={(e) => setF({ ...f, progress: parseInt(e.target.value) || 0 })} /></div>
      </div>
      <div className="row-actions" style={{ marginTop: 14 }}>
        {goal && <button className="btn danger" style={{ marginRight: 'auto' }} onClick={() => void remove()}>Delete</button>}
        <button className="btn" onClick={onClose}>Cancel</button>
        <button className="btn p" disabled={saveGoal.isPending} onClick={() => void submit()}>Save</button>
      </div>
    </Modal>
  )
}
