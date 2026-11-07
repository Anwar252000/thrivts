import { useState } from 'react'
import { num, rs, todayKarachi } from '@shared/format'
import { useErrorToast, useToast } from '@shared/ui/toast'
import type { ClientOrderStatus, OrderLineStatus, OwnerOrder } from '@shared/api/ownerTypes'
import { useOrderActions, useOwnerOrders } from '../../api/owner'
import { Modal, NumberCell, QueryState, TextCell } from '../../ui'
import { usePageParams } from '../../shell/nav'

const ORDER_STATUS: { v: ClientOrderStatus; l: string }[] = [
  { v: 'InProcess', l: 'In Process' }, { v: 'BeingGraded', l: 'Being Graded' }, { v: 'WaitingForPayment', l: 'Waiting for Payment' },
  { v: 'ReadyToShip', l: 'Ready to Ship' }, { v: 'Completed', l: 'Completed' }, { v: 'Shipped', l: 'Shipped' }, { v: 'OnHold', l: 'On Hold' },
]
const LINE_STATUS: { v: OrderLineStatus; l: string }[] = [
  { v: 'Pending', l: 'Pending' }, { v: 'BeingGraded', l: 'Being Graded' }, { v: 'PartialReady', l: 'Partly ready' },
  { v: 'Ready', l: 'Ready' }, { v: 'OnHold', l: 'On Hold' }, { v: 'Shipped', l: 'Shipped' },
]

const statusCol = (s: ClientOrderStatus) => (s === 'Completed' || s === 'Shipped' || s === 'ReadyToShip') ? 'var(--good)' : (s === 'WaitingForPayment' || s === 'OnHold') ? 'var(--warn)' : 'var(--gate)'
const field = { background: 'var(--panel2)', border: '1px solid var(--line2)', borderRadius: 6, padding: '4px 6px', fontFamily: 'var(--mono)', fontSize: 11, color: 'var(--ink)' } as const

export function ClientsPage() {
  const params = usePageParams()
  const [view, setView] = useState<'active' | 'completed'>('active')
  const [selected, setSelected] = useState<string | null>(params.order ?? null)
  const orders = useOwnerOrders(view === 'completed')
  const other = useOwnerOrders(view !== 'completed')
  const actions = useOrderActions()
  const showError = useErrorToast()
  const [adding, setAdding] = useState(false)

  const list = orders.data ?? []
  const order = list.find((o) => o.id === selected) ?? list[0] ?? null
  const counts = { active: view === 'active' ? list.length : other.data?.length ?? 0, completed: view === 'completed' ? list.length : other.data?.length ?? 0 }

  const chip = (v: 'active' | 'completed', label: string) => (
    <button onClick={() => { setView(v); setSelected(null) }}
      style={{ border: `1px solid ${view === v ? 'var(--brand)' : 'var(--line2)'}`, background: view === v ? 'var(--brandglow)' : 'transparent', color: view === v ? 'var(--ink)' : 'var(--mut)', borderRadius: 9, padding: '8px 14px', fontSize: 12.5, fontWeight: 800, cursor: 'pointer' }}>
      {label} ({counts[v]})
    </button>
  )

  const toggleDone = async (o: OwnerOrder) => {
    try {
      await actions.setCompleted.mutateAsync({ id: o.id, value: !o.isCompleted })
      setSelected(null)
    } catch (e) {
      showError(e)
    }
  }

  return (
    <>
      <div style={{ display: 'flex', gap: 8, flexWrap: 'wrap', margin: '2px 0 10px' }}>
        <button className="btn p" onClick={() => setAdding(true)}>➕ New client order</button>
      </div>
      <div style={{ display: 'flex', gap: 10, alignItems: 'center', flexWrap: 'wrap', marginBottom: 12 }}>
        {chip('active', '🟢 Active')}{chip('completed', '✅ Completed')}
        <select value={order?.id ?? ''} onChange={(e) => setSelected(e.target.value)}
          style={{ flex: 1, minWidth: 200, background: 'var(--panel2)', border: '1px solid var(--line2)', borderRadius: 11, color: 'var(--ink)', padding: '9px 12px', fontWeight: 700 }}>
          {list.length === 0 && <option>— none —</option>}
          {list.map((x) => <option key={x.id} value={x.id}>{x.name}{x.clientName ? ` · ${x.clientName}` : ''}</option>)}
        </select>
        {order && <button className="btn" disabled={actions.setCompleted.isPending} onClick={() => void toggleDone(order)}>{order.isCompleted ? '↩ Reactivate' : '✓ Mark completed'}</button>}
      </div>
      <QueryState query={orders} label="client orders" />
      {orders.data && (order ? <OrderCard key={order.id} order={order} onRemoved={() => setSelected(null)} />
        : <div className="minibox muted" style={{ textAlign: 'center' }}>No {view} orders.</div>)}
      <div className="hint" style={{ marginTop: 12 }}>The client sees this order on their portal with the login code — status, items and the <b>client-ready</b> figure. Value and payments are never shown to them.</div>
      {adding && <NewOrder onClose={() => setAdding(false)} onCreated={(id) => { setView('active'); setSelected(id) }} />}
    </>
  )
}

function OrderCard({ order: o, onRemoved }: { order: OwnerOrder; onRemoved: () => void }) {
  const actions = useOrderActions()
  const toast = useToast()
  const showError = useErrorToast()
  const run = async (p: Promise<unknown>, ok?: string) => {
    try {
      await p
      if (ok) toast(ok)
    } catch (e) {
      showError(e)
    }
  }

  const pct = o.required > 0 ? Math.min(100, Math.round((o.clientReady / o.required) * 100)) : 0
  const col = pct >= 100 ? 'var(--good)' : pct >= 70 ? 'var(--gate)' : pct >= 40 ? 'var(--grade)' : 'var(--brand)'
  const update = (patch: Partial<OwnerOrder>) => run(actions.update.mutateAsync({ ...o, ...patch }))
  const ownerFields = (patch: { totalValue?: number; loginCode?: string | null }) =>
    run(actions.ownerFields.mutateAsync({ id: o.id, totalValue: patch.totalValue ?? o.totalValue, loginCode: patch.loginCode !== undefined ? patch.loginCode : o.loginCode }))

  const remove = async () => {
    if (!confirm(`Remove ${o.name} and its items? It disappears from the requirements board and the client's portal.`)) return
    await run(actions.remove.mutateAsync(o.id), `${o.name} removed`)
    onRemoved()
  }

  return (
    <div className="card pad">
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', gap: 10, flexWrap: 'wrap', marginBottom: 10 }}>
        <div>
          <div style={{ fontSize: 19, fontWeight: 800 }}>{o.name}</div>
          <div className="muted" style={{ fontSize: 11, marginTop: 2 }}>Client: {o.clientName ?? '—'}</div>
          <div className="muted" style={{ display: 'flex', alignItems: 'center', gap: 6, marginTop: 7, fontSize: 11 }}>
            🔑 Portal login code:
            <input defaultValue={o.loginCode ?? ''} key={o.loginCode ?? ''} placeholder="set a code (6+ letters/digits)" maxLength={40}
              onBlur={(e) => { const v = e.target.value.trim() || null; if (v !== o.loginCode) void ownerFields({ loginCode: v }) }}
              onKeyDown={(e) => e.key === 'Enter' && (e.target as HTMLInputElement).blur()}
              style={{ background: 'var(--panel2)', border: '1px solid var(--gate)', borderRadius: 5, padding: '3px 8px', fontFamily: 'var(--mono)', fontSize: 12, color: 'var(--gate)', width: 170 }} />
          </div>
        </div>
        <div style={{ textAlign: 'right' }}><div style={{ fontSize: 24, fontWeight: 800, color: col }}>{pct}%</div><div className="muted" style={{ fontSize: 11 }}>client-ready</div></div>
      </div>
      <div style={{ height: 8, marginBottom: 13, background: 'var(--line)', borderRadius: 999, overflow: 'hidden' }}><div style={{ width: `${pct}%`, height: '100%', background: col }} /></div>

      <div style={{ display: 'flex', gap: 12, flexWrap: 'wrap', alignItems: 'center', marginBottom: 13, fontSize: 12 }}>
        <span style={{ display: 'flex', alignItems: 'center', gap: 6 }}>🌐 Status:
          <select value={o.status} onChange={(e) => void update({ status: e.target.value as ClientOrderStatus })}
            style={{ background: 'var(--panel2)', border: '1px solid var(--line2)', borderRadius: 6, padding: '5px 8px', fontSize: 12, fontWeight: 700, color: statusCol(o.status) }}>
            {ORDER_STATUS.map((s) => <option key={s.v} value={s.v}>{s.l}</option>)}
          </select></span>
        <span style={{ display: 'flex', alignItems: 'center', gap: 6 }}>📅 Deadline:
          <input type="date" value={o.deadline ?? ''} onChange={(e) => void update({ deadline: e.target.value || null })} style={field} /></span>
        <span style={{ display: 'flex', alignItems: 'center', gap: 6 }}>💷 Value Rs:
          <NumberCell value={o.totalValue} width={110} onCommit={(v) => void ownerFields({ totalValue: v })} /></span>
        <button className="btn" style={{ marginLeft: 'auto', padding: '5px 11px', fontSize: 11, background: 'rgba(221,107,107,.1)', borderColor: 'rgba(221,107,107,.3)', color: 'var(--bad)' }} onClick={() => void remove()}>🗑 Remove</button>
      </div>

      <div className="muted" style={{ display: 'flex', gap: 14, flexWrap: 'wrap', fontSize: 12, marginBottom: 10 }}>
        <span>Ordered <b style={{ color: 'var(--ink)' }}>{num(o.required)}</b></span>
        <span>Client ready <b style={{ color: 'var(--good)' }}>{num(o.clientReady)}</b></span>
        <span>Remaining <b style={{ color: 'var(--grade)' }}>{num(Math.max(0, o.required - o.clientReady))}</b></span>
        <span>{o.lines.length} item{o.lines.length !== 1 ? 's' : ''}</span>
        <span>Paid <b style={{ color: 'var(--good)' }}>{rs(o.paid)}</b> of {rs(o.totalValue)}</span>
      </div>

      <div className="tablecard"><div className="tblscroll">
        <table>
          <thead><tr><th>Item</th><th>Grade</th><th className="num">Required</th><th className="num">Warehouse ready</th><th className="num" style={{ color: 'var(--gate)' }}>Client-ready</th><th className="num">Remain</th><th>Status</th><th>Note</th><th /></tr></thead>
          <tbody>
            {o.lines.length === 0 && <tr><td colSpan={9} className="muted" style={{ textAlign: 'center' }}>No items yet.</td></tr>}
            {o.lines.map((it) => {
              const rem = Math.max(0, it.required - it.clientReady)
              const save = (patch: Partial<typeof it>) => run(actions.saveLine.mutateAsync({ orderId: o.id, line: { ...it, ...patch } }))
              return (
                <tr key={it.id}>
                  <td><TextCell value={it.itemName} onCommit={(v) => v && void save({ itemName: v })} /></td>
                  <td><TextCell value={it.grade ?? ''} onCommit={(v) => void save({ grade: v || null })} style={{ width: 46, fontSize: 12, color: 'var(--mut)' }} /></td>
                  <td className="num"><NumberCell value={it.required} width={70} onCommit={(v) => void save({ required: v })} /></td>
                  <td className="num muted" title="Counted by the warehouse (sacks assigned to this line)">{num(it.ready)}</td>
                  <td className="num"><NumberCell value={it.clientReady} width={78} color="var(--gate)" onCommit={(v) => void run(actions.clientReady.mutateAsync({ orderId: o.id, lineId: it.id, clientReady: v }))} /></td>
                  <td className="num" style={{ color: rem > 0 ? 'var(--grade)' : 'var(--mut)' }}>{num(rem)}</td>
                  <td>
                    <select value={it.status} onChange={(e) => void save({ status: e.target.value as OrderLineStatus })}
                      style={{ background: 'var(--panel2)', border: '1px solid var(--line2)', borderRadius: 4, padding: 4, fontSize: 11, color: 'var(--mut)' }}>
                      {LINE_STATUS.map((s) => <option key={s.v} value={s.v}>{s.l}</option>)}
                    </select>
                  </td>
                  <td><TextCell value={it.note ?? ''} placeholder="—" onCommit={(v) => void save({ note: v || null })} style={{ fontSize: 12, color: 'var(--mut)' }} /></td>
                  <td><button className="btn" style={{ padding: '3px 7px', fontSize: 13 }} onClick={() => void run(actions.removeLine.mutateAsync({ orderId: o.id, lineId: it.id }))}>✕</button></td>
                </tr>
              )
            })}
          </tbody>
        </table>
      </div></div>
      <div style={{ marginTop: 9 }}>
        <button className="btn" style={{ borderStyle: 'dashed' }} disabled={actions.addLine.isPending}
          onClick={() => { const name = prompt('Item name:', ''); if (name?.trim()) void run(actions.addLine.mutateAsync({ orderId: o.id, itemName: name.trim() })) }}>+ Add item</button>
      </div>

      <Payments order={o} />
    </div>
  )
}

function Payments({ order: o }: { order: OwnerOrder }) {
  const actions = useOrderActions()
  const showError = useErrorToast()
  const [label, setLabel] = useState('')
  const [amount, setAmount] = useState('')
  const run = async (p: Promise<unknown>) => {
    try {
      await p
    } catch (e) {
      showError(e)
    }
  }
  const add = async () => {
    const v = parseFloat(amount)
    if (!(v > 0)) return
    await run(actions.addPayment.mutateAsync({ orderId: o.id, label: label.trim() || null, amount: v, isPaid: false, paidOn: null }))
    setLabel('')
    setAmount('')
  }

  return (
    <>
      <div className="sec-t" style={{ marginTop: 18 }}>Payment milestones · {rs(o.paid)} received</div>
      <div className="tablecard"><div className="tblscroll">
        <table>
          <thead><tr><th>Milestone</th><th className="num">Amount</th><th>Paid?</th><th>Paid on</th><th /></tr></thead>
          <tbody>
            {o.payments.length === 0 && <tr><td colSpan={5} className="muted" style={{ textAlign: 'center' }}>No milestones yet — e.g. "Advance 30%".</td></tr>}
            {o.payments.map((p) => (
              <tr key={p.id}>
                <td>{p.label ?? '—'}</td>
                <td className="num">{rs(p.amount)}</td>
                <td><input type="checkbox" checked={p.isPaid}
                  onChange={(e) => void run(actions.savePayment.mutateAsync({ orderId: o.id, paymentId: p.id, label: p.label, amount: p.amount, isPaid: e.target.checked, paidOn: e.target.checked ? todayKarachi() : null }))} /></td>
                <td className="muted">{p.paidOn ?? '—'}</td>
                <td className="num"><button className="btn danger" style={{ padding: '3px 8px', fontSize: 11 }}
                  onClick={() => confirm('Remove this milestone?') && void run(actions.removePayment.mutateAsync({ orderId: o.id, paymentId: p.id }))}>🗑</button></td>
              </tr>
            ))}
          </tbody>
        </table>
      </div></div>
      <div style={{ display: 'flex', gap: 8, marginTop: 9, flexWrap: 'wrap' }}>
        <input value={label} placeholder="Milestone (e.g. Advance)" maxLength={80} onChange={(e) => setLabel(e.target.value)} style={{ ...field, fontFamily: 'inherit', fontSize: 12, padding: '7px 10px' }} />
        <input type="number" min={1} value={amount} placeholder="Rs" onChange={(e) => setAmount(e.target.value)} style={{ ...field, width: 120, padding: '7px 10px' }} />
        <button className="btn" disabled={actions.addPayment.isPending} onClick={() => void add()}>+ Add milestone</button>
      </div>
    </>
  )
}

function NewOrder({ onClose, onCreated }: { onClose: () => void; onCreated: (id: string) => void }) {
  const actions = useOrderActions()
  const toast = useToast()
  const showError = useErrorToast()
  const [f, setF] = useState({ name: '', client: '', deadline: '' })
  const submit = async () => {
    if (!f.name.trim()) return toast('Enter the order name', true)
    try {
      const { id } = await actions.create.mutateAsync({ name: f.name.trim(), clientName: f.client.trim() || null, deadline: f.deadline || null })
      toast(`✓ ${f.name.trim()} created`)
      onCreated(id)
      onClose()
    } catch (e) {
      showError(e)
    }
  }
  return (
    <Modal onClose={onClose} width={440}>
      <div style={{ fontSize: 17, fontWeight: 800, marginBottom: 12 }}>New client order</div>
      <div className="form-grid">
        <div className="fld"><label>Order name</label><input autoFocus value={f.name} maxLength={80} placeholder="e.g. Brandor-12" onChange={(e) => setF({ ...f, name: e.target.value })} /></div>
        <div className="fld"><label>Client</label><input value={f.client} maxLength={80} onChange={(e) => setF({ ...f, client: e.target.value })} /></div>
        <div className="fld"><label>Deadline</label><input type="date" value={f.deadline} onChange={(e) => setF({ ...f, deadline: e.target.value })} /></div>
      </div>
      <div className="row-actions" style={{ marginTop: 14 }}>
        <button className="btn" onClick={onClose}>Cancel</button>
        <button className="btn p" disabled={actions.create.isPending} onClick={() => void submit()}>Create</button>
      </div>
    </Modal>
  )
}
