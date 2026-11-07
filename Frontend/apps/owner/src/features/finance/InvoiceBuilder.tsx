import { useMemo, useRef, useState } from 'react'
import { dateLabel, num, rs, todayKarachi } from '@shared/format'
import { useErrorToast, useToast } from '@shared/ui/toast'
import type { InvoiceDraft } from '@shared/api/ownerTypes'
import { useCreateInvoice, useInvoiceDraft } from '../../api/owner'
import { Modal, QueryState } from '../../ui'
import { printInvoice } from './invoicePdf'

interface Line { itemName: string; pieces: number; buyPrice: number; mergedFrom: string[] | null }
interface Rej { itemName: string; rejected: number; reason: string; accepted: number; acceptPrice: number }

const REASONS: Record<string, string> = { PakkaDaagh: 'Pakka Daagh', KataPhataFake: 'Kata Phata / Fake', WrongStock: 'Wrong stock' }
const REASON_OPTIONS = ['', 'Pakka Daagh', 'Kata Phata / Fake', 'Wrong stock']

const priceInput = { width: 96, textAlign: 'right', background: 'var(--panel2)', border: '1px solid var(--line2)', borderRadius: 8, padding: '6px 8px', color: 'var(--ink)', fontFamily: 'var(--mono)' } as const

/**
 * The vendor invoice builder (legacy layout): set your buying price per item, merge lines, and for
 * rejections choose a reason and how many you'll keep & pay for. The API recomputes every total,
 * checks the pieces match the lot, and posts the invoice to the supplier's hisab.
 */
export function InvoiceBuilder({ lotId, onDone }: { lotId: string; onDone: () => void }) {
  const draft = useInvoiceDraft(lotId)
  return (
    <div className="page">
      <QueryState query={draft} label="the invoice" />
      {draft.data && <Builder draft={draft.data} onDone={onDone} />}
      {!draft.data && <button className="btn" style={{ marginTop: 10 }} onClick={onDone}>← Back</button>}
    </div>
  )
}

function Builder({ draft, onDone }: { draft: InvoiceDraft; onDone: () => void }) {
  const create = useCreateInvoice()
  const toast = useToast()
  const showError = useErrorToast()
  const key = useRef(crypto.randomUUID())
  const final = draft.suggestedMode === 'Final'
  const original = useMemo(() => draft.lines.map<Line>((l) => ({ itemName: l.itemName, pieces: l.pieces, buyPrice: l.defaultBuyPrice ?? 0, mergedFrom: null })), [draft])
  const [lines, setLines] = useState<Line[]>(original)
  const [rejs, setRejs] = useState<Rej[]>(() => draft.rejections.map((r) => ({ itemName: r.itemName, rejected: r.rejected, reason: (r.reason && REASONS[r.reason]) || r.reason || '', accepted: 0, acceptPrice: 0 })))
  const [merging, setMerging] = useState(false)

  const itemsT = lines.reduce((a, l) => a + l.pieces * (l.buyPrice || 0), 0)
  const rejT = rejs.reduce((a, r) => a + Math.min(r.accepted, r.rejected) * (r.acceptPrice || 0), 0)
  const grand = itemsT + rejT

  const setLine = (ix: number, patch: Partial<Line>) => setLines((ls) => ls.map((l, i) => (i === ix ? { ...l, ...patch } : l)))
  const setRej = (ix: number, patch: Partial<Rej>) => setRejs((rs_) => rs_.map((r, i) => (i === ix ? { ...r, ...patch } : r)))

  const unmerge = (ix: number) => {
    const merged = lines[ix]
    const restored = original.filter((o) => merged.mergedFrom?.includes(o.itemName))
    setLines((ls) => [...ls.slice(0, ix), ...restored, ...ls.slice(ix + 1)])
  }

  const printable = () => ({
    lotSerial: draft.lotSerial, supplierName: draft.supplierName, final, date: dateLabel(todayKarachi()),
    lines: [
      ...lines.map((l) => ({ itemName: l.itemName, pieces: l.pieces, buyPrice: l.buyPrice || 0 })),
      ...rejs.filter((r) => r.accepted > 0 && r.acceptPrice > 0).map((r) => ({ itemName: r.itemName, pieces: Math.min(r.accepted, r.rejected), buyPrice: r.acceptPrice, accepted: true })),
    ],
    rejections: rejs.map((r) => ({ itemName: r.itemName, rejected: r.rejected, reason: r.reason || null, accepted: Math.min(r.accepted, r.rejected) })),
  })

  const save = async () => {
    if (grand <= 0 && !confirm('Total is 0 — generate a zero-total invoice anyway?')) return
    try {
      await create.mutateAsync({
        lotId: draft.lotId,
        mode: draft.suggestedMode,
        lines: lines.map((l) => ({ itemName: l.itemName, pieces: l.pieces, buyPrice: l.buyPrice || 0, mergedFrom: l.mergedFrom })),
        rejections: rejs.map((r) => ({ itemName: r.itemName, rejected: r.rejected, reason: r.reason || null, accepted: Math.min(r.accepted, r.rejected), acceptPrice: r.acceptPrice || 0 })),
        idempotencyKey: key.current,
      })
      toast(`Invoice generated — ${rs(grand)} posted to ${draft.supplierName}'s hisab.`)
      onDone()
    } catch (e) {
      showError(e)
    }
  }

  return (
    <>
      <button className="btn" style={{ margin: '2px 0 10px' }} onClick={onDone}>← Back to {draft.supplierName}</button>
      <div className="card pad" style={{ display: 'flex', alignItems: 'center', gap: 10, flexWrap: 'wrap' }}>
        <div><div style={{ fontSize: 18, fontWeight: 800 }}>Invoice — {draft.lotSerial}</div><div className="muted" style={{ fontSize: 12 }}>{draft.supplierName} · {num(draft.totalPieces)} pcs</div></div>
        <span className={`tag ${final ? 't-good' : 't-warn'}`} style={{ marginLeft: 6 }}>{final ? 'Finalized (graded)' : 'Non-graded'}</span>
        <span className="tag t-price" style={{ marginLeft: 'auto' }}>total posts to hisab</span>
      </div>

      <div className="sec-t" style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
        Items — set your buying price
        <button className="btn" style={{ marginLeft: 'auto', padding: '5px 12px', fontSize: 12 }} disabled={lines.length < 2} onClick={() => setMerging(true)}>⇲ Merge items</button>
      </div>
      <div className="tablecard"><div className="tblscroll">
        <table>
          <thead><tr><th>Item</th><th className="num">Pcs</th><th className="num">Buy Rs/pc</th><th className="num">Line total</th><th /></tr></thead>
          <tbody>
            {lines.map((l, ix) => (
              <tr key={`${l.itemName}-${ix}`}>
                <td><b>{l.itemName}</b>{l.mergedFrom && <div className="muted" style={{ fontSize: 10 }}>merged: {l.mergedFrom.join(', ')}</div>}</td>
                <td className="num">{num(l.pieces)}</td>
                <td className="num"><input type="number" min={0} step={1} value={l.buyPrice || ''} placeholder="0" onChange={(e) => setLine(ix, { buyPrice: Math.max(0, parseFloat(e.target.value) || 0) })} style={priceInput} /></td>
                <td className="num"><b>{l.buyPrice ? rs(l.pieces * l.buyPrice) : '—'}</b></td>
                <td className="num">{l.mergedFrom && <button className="btn" style={{ padding: '3px 7px', fontSize: 11 }} onClick={() => unmerge(ix)} title="Undo merge">↺</button>}</td>
              </tr>
            ))}
            <tr><td colSpan={3} style={{ textAlign: 'right', fontWeight: 700 }}>Items total</td><td className="num"><b>{rs(itemsT)}</b></td><td /></tr>
          </tbody>
        </table>
      </div></div>

      {rejs.length > 0 && (
        <>
          <div className="sec-t">Rejections — reason, and any pcs you agree to keep &amp; pay for</div>
          <div className="tablecard"><div className="tblscroll">
            <table>
              <thead><tr><th>Item</th><th className="num">Rejected</th><th>Reason</th><th className="num">Accept pcs</th><th className="num">My Rs/pc</th><th className="num">Charge</th><th className="num">Back</th></tr></thead>
              <tbody>
                {rejs.map((r, ix) => {
                  const acc = Math.min(r.accepted, r.rejected)
                  const options = REASON_OPTIONS.includes(r.reason) ? REASON_OPTIONS : [...REASON_OPTIONS, r.reason]
                  return (
                    <tr key={r.itemName}>
                      <td><b>{r.itemName}</b></td>
                      <td className="num">{num(r.rejected)}</td>
                      <td>
                        <select value={r.reason} onChange={(e) => setRej(ix, { reason: e.target.value })}
                          style={{ background: 'var(--panel2)', border: '1px solid var(--line2)', borderRadius: 7, padding: '5px 8px', color: 'var(--ink)', fontSize: 12 }}>
                          {options.map((o) => <option key={o} value={o}>{o || '—'}</option>)}
                        </select>
                      </td>
                      <td className="num"><input type="number" min={0} max={r.rejected} value={r.accepted || ''} placeholder="0"
                        onChange={(e) => setRej(ix, { accepted: Math.min(r.rejected, Math.max(0, parseInt(e.target.value) || 0)) })} style={{ ...priceInput, width: 70 }} /></td>
                      <td className="num"><input type="number" min={0} value={r.acceptPrice || ''} placeholder="0"
                        onChange={(e) => setRej(ix, { acceptPrice: Math.max(0, parseFloat(e.target.value) || 0) })} style={{ ...priceInput, width: 82 }} /></td>
                      <td className="num">{acc && r.acceptPrice ? rs(acc * r.acceptPrice) : '—'}</td>
                      <td className="num" style={{ color: 'var(--price)' }}>{num(r.rejected - acc)}</td>
                    </tr>
                  )
                })}
                <tr><td colSpan={5} style={{ textAlign: 'right', fontWeight: 700 }}>Accepted-rejection charges</td><td className="num"><b>{rs(rejT)}</b></td><td /></tr>
              </tbody>
            </table>
          </div></div>
          <div className="hint" style={{ marginTop: 6 }}>Accepted pcs are added to this invoice and <b>leave</b> the rejection count — only "Back" pcs return to the vendor.</div>
        </>
      )}

      <div className="card pad" style={{ display: 'flex', alignItems: 'center', marginTop: 12 }}>
        <div style={{ fontSize: 14, fontWeight: 700 }}>Invoice total</div>
        <div style={{ marginLeft: 'auto', fontSize: 20, fontWeight: 800, color: 'var(--price)' }}>{rs(grand)}</div>
      </div>
      <div style={{ display: 'flex', gap: 8, flexWrap: 'wrap', marginTop: 12 }}>
        <button className="btn p" disabled={create.isPending} onClick={() => void save()}>{create.isPending ? 'Saving…' : '✓ Generate & post to hisab'}</button>
        <button className="btn" onClick={() => printInvoice(printable())}>🧾 Invoice PDF</button>
        <button className="btn" onClick={onDone}>Cancel</button>
      </div>
      <div className="hint" style={{ marginTop: 10 }}>Vendor document — separate from the warehouse. Prices are your <b>buying</b> price (what you pay {draft.supplierName}).</div>

      {merging && <MergeDialog lines={lines} onClose={() => setMerging(false)} onMerge={(idxs, name) => {
        const picked = idxs.map((i) => lines[i])
        const merged: Line = {
          itemName: name, pieces: picked.reduce((a, l) => a + l.pieces, 0), buyPrice: 0,
          mergedFrom: picked.flatMap((l) => l.mergedFrom ?? [l.itemName]),
        }
        setLines([merged, ...lines.filter((_, i) => !idxs.includes(i))])
        setMerging(false)
      }} />}
    </>
  )
}

function MergeDialog({ lines, onClose, onMerge }: { lines: Line[]; onClose: () => void; onMerge: (idxs: number[], name: string) => void }) {
  const [picked, setPicked] = useState<number[]>([])
  const [name, setName] = useState('')
  const toggle = (i: number) => setPicked((p) => (p.includes(i) ? p.filter((x) => x !== i) : [...p, i]))
  return (
    <Modal onClose={onClose} width={460}>
      <div style={{ fontSize: 17, fontWeight: 800, marginBottom: 10 }}>Merge items into one line</div>
      <div style={{ display: 'flex', flexDirection: 'column', gap: 6, marginBottom: 12 }}>
        {lines.map((l, i) => (
          <label key={i} style={{ display: 'flex', gap: 8, alignItems: 'center', fontSize: 13, cursor: 'pointer' }}>
            <input type="checkbox" checked={picked.includes(i)} onChange={() => toggle(i)} /> {l.itemName} <span className="muted">({num(l.pieces)} pcs)</span>
          </label>
        ))}
      </div>
      <div className="fld"><label>New name for the merged item</label><input value={name} maxLength={160} onChange={(e) => setName(e.target.value)} /></div>
      <div className="row-actions" style={{ marginTop: 14 }}>
        <button className="btn" onClick={onClose}>Cancel</button>
        <button className="btn p" disabled={picked.length < 2 || !name.trim()} onClick={() => onMerge([...picked].sort((a, b) => a - b), name.trim())}>Merge {picked.length || ''}</button>
      </div>
    </Modal>
  )
}
