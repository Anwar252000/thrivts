import { useState } from 'react'
import { num } from '@shared/format'
import { useErrorToast, useToast } from '@shared/ui/toast'
import type { FloorLot, FloorStage } from '@shared/api/ownerTypes'
import { useDeleteLot, useFloor, useLotDetail } from '../../api/owner'
import { Kpi, Modal, QueryState } from '../../ui'

const STAGES: { key: FloorStage; name: string; who: string; ic: string; v: string }[] = [
  { key: 'Gate', name: 'Gate', who: 'Guard', ic: '⇥', v: '--gate' },
  { key: 'Counting', name: 'Counting', who: 'Supervisor', ic: '#', v: '--count' },
  { key: 'Grading', name: 'Grading', who: 'Supervisor', ic: '◫', v: '--grade' },
  { key: 'Cosign', name: 'Co-sign', who: 'Inventory / GM', ic: '✓', v: '--cosign' },
  { key: 'Pricing', name: 'Pricing', who: 'You', ic: 'Rs', v: '--price' },
  { key: 'Finalized', name: 'Finalized', who: 'In stock', ic: '★', v: '--final' },
]

const qty = (l: FloorLot) => (l.pieces > 0 ? `${num(l.pieces)} pcs` : l.sacks > 0 ? `${num(l.sacks)} sacks` : '—')

export function FloorPage() {
  const floor = useFloor()
  const [peek, setPeek] = useState<FloorLot | null>(null)
  const f = floor.data

  return (
    <>
      <QueryState query={floor} label="the floor" />
      {f && (
        <>
          {f.lots === 0 && (
            <div className="banner">No lots on the floor yet. Lots appear here the moment the gate logs an arrival, and move along as they are counted, graded and co-signed.</div>
          )}
          <div className="kpis">
            <Kpi n={num(f.piecesOnFloor)} l="Pieces on floor" />
            <Kpi n={num(f.lots)} l="Lots" />
            <Kpi n={num(f.waitingForPrice)} l="Waiting your price" />
            <Kpi n={num(f.finalized)} l="Finalized" />
            <Kpi n={num(f.suppliers)} l="Suppliers" />
          </div>
          <div className="sec-t">The floor — live from your staff app</div>
          <div className="floorwrap">
            <div className="floor">
              {STAGES.map((s) => {
                const here = f.items.filter((l) => l.stage === s.key)
                return (
                  <div key={s.key} className="station" style={{ ['--sc' as string]: `var(${s.v})` }}>
                    <div className="st-h">
                      <div className="st-ic">{s.ic}</div>
                      <div><div className="st-t">{s.name}</div><div className="st-who">{s.who}</div></div>
                      <div className="st-c">{here.length}</div>
                    </div>
                    <div className="lane">
                      {here.length === 0 && <div className="empty">—</div>}
                      {here.map((l) => (
                        <div key={l.id} className="lot" style={{ ['--sc' as string]: `var(${s.v})` }} onClick={() => setPeek(l)} role="button" tabIndex={0}
                          onKeyDown={(e) => e.key === 'Enter' && setPeek(l)}>
                          <div className="r1"><span className="ser">{l.serial}</span><span className="pcs">{qty(l)}</span></div>
                          <div className="r2">{l.supplierName}</div>
                        </div>
                      ))}
                    </div>
                  </div>
                )
              })}
            </div>
          </div>
          <div className="hint" style={{ marginTop: 14 }}>
            Updates live as your team works — no refresh needed. Pricing = graded lots waiting for your invoice; generate it from Supplier Hisab.
          </div>
        </>
      )}
      {peek && <LotPeek lot={peek} onClose={() => setPeek(null)} />}
    </>
  )
}

function LotPeek({ lot, onClose }: { lot: FloorLot; onClose: () => void }) {
  const detail = useLotDetail(lot.id)
  const del = useDeleteLot()
  const toast = useToast()
  const showError = useErrorToast()
  const stage = STAGES.find((s) => s.key === lot.stage)

  const remove = async () => {
    if (!confirm(`Delete lot ${lot.serial}?\n\nIt disappears from every screen. If it is already in stock, its stock is removed too — this is only allowed while none of it has been packed, washed or dispatched, and before it is invoiced.`)) return
    try {
      await del.mutateAsync(lot.id)
      toast(`Lot ${lot.serial} deleted`)
      onClose()
    } catch (e) {
      showError(e)
    }
  }

  return (
    <Modal onClose={onClose}>
      <div style={{ fontSize: 19, fontWeight: 800, marginBottom: 2 }}>{lot.serial}</div>
      <div className="muted" style={{ fontSize: 12, marginBottom: 12 }}>{stage?.name} · {lot.supplierName} · {qty(lot)}</div>
      <QueryState query={detail} label="the lot" />
      {detail.data && (
        <div className="tablecard"><div className="tblscroll">
          <table>
            <thead><tr><th>Item</th><th className="num">Recv</th><th className="num">A</th><th className="num">C</th><th className="num">Rej</th></tr></thead>
            <tbody>
              {detail.data.items.length === 0 && <tr><td colSpan={5} className="muted" style={{ textAlign: 'center' }}>No items counted yet</td></tr>}
              {detail.data.items.map((it) => (
                <tr key={it.id}>
                  <td><b>{it.itemName}</b></td>
                  <td className="num">{num(it.received)}</td>
                  <td className="num">{num(it.qtyA)}</td>
                  <td className="num">{num(it.qtyC)}</td>
                  <td className="num">{num(it.rejected + it.washRejected)}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div></div>
      )}
      <div style={{ display: 'flex', gap: 8, marginTop: 14 }}>
        <button className="btn" onClick={onClose}>Close</button>
        <button className="btn" disabled={del.isPending} style={{ marginLeft: 'auto', borderColor: 'var(--price)', color: 'var(--price)' }} onClick={() => void remove()}>
          🗑 Delete this lot
        </button>
      </div>
    </Modal>
  )
}
