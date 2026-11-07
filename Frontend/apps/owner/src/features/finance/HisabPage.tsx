import { useState } from 'react'
import { dateLabel, initial, num, rs } from '@shared/format'
import { useErrorToast, useToast } from '@shared/ui/toast'
import { useHisab, useSaveSupplier, useSupplierAccount, useDeleteLot, useDeleteInvoice, useDeletePayment } from '../../api/owner'
import { Kpi, Modal, QueryState } from '../../ui'
import { usePageParams } from '../../shell/nav'
import { InvoiceBuilder } from './InvoiceBuilder'
import { PaymentForm } from './PaymentForm'
import { InvoiceView } from './InvoiceView'

type View = { kind: 'list' } | { kind: 'supplier'; id: string } | { kind: 'invoice'; supplierId: string; lotId: string }

export function HisabPage() {
  const params = usePageParams()
  const [view, setView] = useState<View>(() => (params.supplier ? { kind: 'supplier', id: params.supplier } : { kind: 'list' }))

  if (view.kind === 'supplier') return <SupplierAccountView id={view.id} onBack={() => setView({ kind: 'list' })} onInvoice={(lotId) => setView({ kind: 'invoice', supplierId: view.id, lotId })} />
  if (view.kind === 'invoice') return <InvoiceBuilder lotId={view.lotId} onDone={() => setView({ kind: 'supplier', id: view.supplierId })} />
  return <HisabList onOpen={(id) => setView({ kind: 'supplier', id })} />
}

function HisabList({ onOpen }: { onOpen: (id: string) => void }) {
  const [q, setQ] = useState('')
  const hisab = useHisab(q.trim())
  const [adding, setAdding] = useState(false)
  const h = hisab.data

  return (
    <>
      <div className="searchbar">
        <input placeholder="🔍 Search supplier…" value={q} onChange={(e) => setQ(e.target.value)} />
        <button className="btn" onClick={() => setAdding(true)}>➕ Add supplier</button>
      </div>
      <QueryState query={hisab} label="the hisab" />
      {h && (
        <>
          <div className="statrow">
            <Kpi n={rs(h.totalDue)} l="Total you owe" />
            <Kpi n={num(h.suppliers)} l="Suppliers" />
            <Kpi n={num(h.withBalance)} l="With balance" />
          </div>
          <div className="sec-t">Suppliers — highest due first · tap to open</div>
          <div className="tablecard"><div className="tblscroll">
            <table>
              <thead><tr><th>Supplier</th><th className="num">Invoices</th><th className="num">New lots</th><th className="num">Billed</th><th className="num">Paid</th><th className="num">Due</th></tr></thead>
              <tbody>
                {h.rows.length === 0 && <tr><td colSpan={6} className="muted" style={{ textAlign: 'center' }}>{q ? 'No match.' : 'No suppliers yet.'}</td></tr>}
                {h.rows.map((s) => (
                  <tr key={s.supplierId} className="clk" onClick={() => onOpen(s.supplierId)}>
                    <td><b>{s.name}</b>{s.newLots > 0 && <> <span className="tag t-warn">{s.newLots} new lot{s.newLots > 1 ? 's' : ''}</span></>}</td>
                    <td className="num">{num(s.invoices)}</td>
                    <td className="num">{s.newLots || '—'}</td>
                    <td className="num">{rs(s.billed)}</td>
                    <td className="num ok">{rs(s.paid)}</td>
                    <td className="num"><b style={{ color: s.due > 0 ? 'var(--price)' : 'var(--good)' }}>{s.due > 0 ? rs(s.due) : '—'}</b></td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div></div>
          <div className="hint" style={{ marginTop: 10 }}>🔒 Owner-only. A lot shows under its supplier the moment it's counted; generate an invoice (non-graded or finalized) at your buy prices → the total becomes that supplier's hisab due.</div>
        </>
      )}
      {adding && <SupplierForm onClose={() => setAdding(false)} onSaved={onOpen} />}
    </>
  )
}

function SupplierForm({ initial: init, onClose, onSaved }: {
  initial?: { id: string; name: string; phone: string | null; city: string | null; since: string | null; notes: string | null }
  onClose: () => void
  onSaved?: (id: string) => void
}) {
  const save = useSaveSupplier()
  const toast = useToast()
  const showError = useErrorToast()
  const [f, setF] = useState({ name: init?.name ?? '', phone: init?.phone ?? '', city: init?.city ?? '', since: init?.since ?? '', notes: init?.notes ?? '' })

  const submit = async () => {
    if (!f.name.trim()) return toast('Enter the supplier name', true)
    try {
      const id = await save.mutateAsync({ id: init?.id ?? null, name: f.name.trim(), phone: f.phone.trim() || null, city: f.city.trim() || null, since: f.since.trim() || null, notes: f.notes.trim() || null })
      toast(init ? '✓ Supplier updated' : `✓ ${f.name.trim()} added`)
      onClose()
      onSaved?.(id)
    } catch (e) {
      showError(e)
    }
  }

  return (
    <Modal onClose={onClose} width={480}>
      <div style={{ fontSize: 17, fontWeight: 800, marginBottom: 12 }}>{init ? `Edit ${init.name}` : 'New supplier'}</div>
      <div className="form-grid">
        <div className="fld"><label>Name</label><input autoFocus value={f.name} maxLength={60} onChange={(e) => setF({ ...f, name: e.target.value })} /></div>
        <div className="fld"><label>Phone</label><input value={f.phone} maxLength={30} onChange={(e) => setF({ ...f, phone: e.target.value })} /></div>
        <div className="fld"><label>City</label><input value={f.city} maxLength={60} onChange={(e) => setF({ ...f, city: e.target.value })} /></div>
        <div className="fld"><label>Supplier since</label><input value={f.since} maxLength={30} placeholder="e.g. 2023" onChange={(e) => setF({ ...f, since: e.target.value })} /></div>
      </div>
      <div className="fld" style={{ marginTop: 11 }}><label>Notes</label><textarea rows={3} value={f.notes} maxLength={1000} onChange={(e) => setF({ ...f, notes: e.target.value })} /></div>
      <div className="row-actions" style={{ marginTop: 14 }}>
        <button className="btn" onClick={onClose}>Cancel</button>
        <button className="btn p" disabled={save.isPending} onClick={() => void submit()}>{save.isPending ? 'Saving…' : 'Save'}</button>
      </div>
    </Modal>
  )
}

function SupplierAccountView({ id, onBack, onInvoice }: { id: string; onBack: () => void; onInvoice: (lotId: string) => void }) {
  const account = useSupplierAccount(id)
  const delLot = useDeleteLot()
  const delInvoice = useDeleteInvoice()
  const delPayment = useDeletePayment()
  const toast = useToast()
  const showError = useErrorToast()
  const [paying, setPaying] = useState(false)
  const [editing, setEditing] = useState(false)
  const [openInvoice, setOpenInvoice] = useState<string | null>(null)
  const s = account.data

  const run = async (question: string, action: () => Promise<unknown>, done: string) => {
    if (!confirm(question)) return
    try {
      await action()
      toast(done)
    } catch (e) {
      showError(e)
    }
  }

  return (
    <div className="page">
      <button className="btn" style={{ margin: '2px 0 10px' }} onClick={onBack}>← Back to Supplier Hisab</button>
      <QueryState query={account} label="the supplier" />
      {s && (
        <>
          <div className="card pad" style={{ display: 'flex', alignItems: 'center', gap: 14, flexWrap: 'wrap' }}>
            <div style={{ width: 46, height: 46, borderRadius: 12, background: 'var(--panel2)', border: '1px solid var(--line)', display: 'grid', placeItems: 'center', fontSize: 18, fontWeight: 800, color: 'var(--mut)' }}>{initial(s.name)}</div>
            <div>
              <div style={{ fontSize: 19, fontWeight: 800 }}>{s.name}</div>
              <div className="muted" style={{ fontSize: 12 }}>{[s.city, s.phone, s.since && `since ${s.since}`].filter(Boolean).join(' · ')}</div>
            </div>
            <div style={{ marginLeft: 'auto', display: 'flex', gap: 8 }}>
              <button className="btn" onClick={() => setEditing(true)}>Edit</button>
              <button className="btn p" onClick={() => setPaying(true)}>💷 Log payment</button>
            </div>
          </div>
          {s.notes && <div className="hint" style={{ margin: '8px 2px 0', whiteSpace: 'pre-line' }}>{s.notes}</div>}

          <div className="statrow" style={{ gridTemplateColumns: 'repeat(4,1fr)', marginTop: 12 }}>
            <Kpi n={num(s.invoices.length)} l="Invoices" />
            <Kpi n={rs(s.billed)} l="Billed" />
            <Kpi n={rs(s.paid)} l="Paid" />
            <Kpi n={s.due > 0 ? rs(s.due) : 'cleared'} l="Balance due" color={s.due > 0 ? 'var(--price)' : 'var(--good)'} />
          </div>

          {s.newLots.length > 0 && (
            <>
              <div className="sec-t">New lots — not invoiced yet</div>
              <div className="tablecard"><div className="tblscroll">
                <table>
                  <thead><tr><th>Lot</th><th className="num">Pcs</th><th>Stage</th><th>Arrived</th><th /></tr></thead>
                  <tbody>
                    {s.newLots.map((l) => (
                      <tr key={l.lotId}>
                        <td><b>{l.serial}</b></td>
                        <td className="num">{num(l.pieces)}</td>
                        <td><span className={`tag ${l.stage === 'Finalized' ? 't-good' : 't-warn'}`}>{l.stage === 'Finalized' ? 'graded & finalized' : l.stage === 'AwaitingCosign' ? 'graded, awaiting co-sign' : 'counted'}</span></td>
                        <td className="muted">{dateLabel(l.arrivalDate)}</td>
                        <td className="num" style={{ whiteSpace: 'nowrap' }}>
                          <button className="btn" style={{ padding: '5px 10px', fontSize: 11 }} onClick={() => onInvoice(l.lotId)}>Generate invoice →</button>
                          <button className="btn" style={{ padding: '5px 9px', fontSize: 11, borderColor: 'var(--price)', color: 'var(--price)', marginLeft: 4 }} disabled={delLot.isPending}
                            onClick={() => void run(`Delete lot ${l.serial}? It disappears from every screen.`, () => delLot.mutateAsync(l.lotId), `Lot ${l.serial} deleted`)}>🗑</button>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div></div>
            </>
          )}

          <div className="sec-t">Invoices &amp; balance</div>
          <div className="tablecard"><div className="tblscroll">
            <table>
              <thead><tr><th>Invoice</th><th>Lot</th><th>Type</th><th>Rejections</th><th className="num">Total</th><th>Date</th><th /></tr></thead>
              <tbody>
                {s.invoices.length === 0 && <tr><td colSpan={7} className="muted" style={{ textAlign: 'center' }}>No invoices yet.</td></tr>}
                {s.invoices.map((i) => (
                  <tr key={i.id} className="clk" onClick={() => setOpenInvoice(i.id)}>
                    <td className="mono"><b>{i.number}</b></td>
                    <td><b>{i.lotSerial ?? '—'}</b></td>
                    <td><span className={`tag ${i.mode === 'Final' ? 't-good' : 't-info'}`}>{i.mode === 'Final' ? 'finalized' : 'non-graded'}</span></td>
                    <td style={{ fontSize: 12 }}>
                      {i.rejected ? <>{num(i.rejected)} rej{i.accepted ? ` · ${num(i.accepted)} kept` : ''}{i.returned ? ` · ${num(i.returned)} back` : ''}
                        {i.rejectionReasons && <div className="muted" style={{ fontSize: 10 }}>{i.rejectionReasons}</div>}</> : <span className="muted">—</span>}
                    </td>
                    <td className="num">{rs(i.total)}</td>
                    <td className="muted">{dateLabel(i.generatedAt.slice(0, 10))}</td>
                    <td className="num" onClick={(e) => e.stopPropagation()}>
                      <button className="btn danger" style={{ padding: '3px 8px', fontSize: 11 }} disabled={delInvoice.isPending}
                        onClick={() => void run(`Delete invoice ${i.number}? Its total comes off ${s.name}'s hisab and the lot can be invoiced again.`, () => delInvoice.mutateAsync(i.id), `Invoice ${i.number} deleted`)}>🗑</button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div></div>

          {s.payments.length > 0 && (
            <>
              <div className="sec-t">Payments</div>
              <div className="tablecard"><div className="tblscroll">
                <table>
                  <thead><tr><th>Date</th><th>Method</th><th>Against</th><th>Note</th><th className="num">Amount</th><th /></tr></thead>
                  <tbody>
                    {s.payments.map((p) => (
                      <tr key={p.id}>
                        <td>{dateLabel(p.paidOn)}</td>
                        <td>{p.method}</td>
                        <td className="mono muted">{p.invoiceNumber ?? '—'}</td>
                        <td className="muted">{p.note ?? ''}</td>
                        <td className="num ok">{rs(p.amount)}</td>
                        <td className="num">
                          <button className="btn danger" style={{ padding: '3px 8px', fontSize: 11 }} disabled={delPayment.isPending}
                            onClick={() => void run(`Delete this ${rs(p.amount)} payment? The due goes back up.`, () => delPayment.mutateAsync(p.id), 'Payment deleted')}>🗑</button>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div></div>
            </>
          )}
          <div className="hint" style={{ marginTop: 12 }}>Hisab is separate from the warehouse stock-in flow. Deleted invoices and payments are kept in the audit history.</div>

          {paying && <PaymentForm supplier={s} onClose={() => setPaying(false)} />}
          {editing && <SupplierForm initial={s} onClose={() => setEditing(false)} />}
          {openInvoice && <InvoiceView id={openInvoice} onClose={() => setOpenInvoice(null)} />}
        </>
      )}
    </div>
  )
}
