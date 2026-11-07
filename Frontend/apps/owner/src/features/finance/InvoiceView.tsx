import { dateLabel, num, rs } from '@shared/format'
import { useInvoice } from '../../api/owner'
import { Modal, QueryState } from '../../ui'
import { printInvoice } from './invoicePdf'

/** A generated invoice, read-only, with its printable PDF. */
export function InvoiceView({ id, onClose }: { id: string; onClose: () => void }) {
  const invoice = useInvoice(id)
  const i = invoice.data

  return (
    <Modal onClose={onClose} width={680}>
      <QueryState query={invoice} label="the invoice" />
      {i && (
        <>
          <div style={{ display: 'flex', alignItems: 'center', gap: 10, flexWrap: 'wrap' }}>
            <div>
              <div style={{ fontSize: 18, fontWeight: 800 }}>{i.number} — {i.lotSerial}</div>
              <div className="muted" style={{ fontSize: 12 }}>{i.supplierName} · {dateLabel(i.generatedAt.slice(0, 10))}</div>
            </div>
            <span className={`tag ${i.mode === 'Final' ? 't-good' : 't-warn'}`} style={{ marginLeft: 'auto' }}>{i.mode === 'Final' ? 'Finalized (graded)' : 'Non-graded'}</span>
          </div>
          <div className="tablecard" style={{ marginTop: 12 }}><div className="tblscroll">
            <table>
              <thead><tr><th>Item</th><th className="num">Pcs</th><th className="num">Rs/pc</th><th className="num">Total</th></tr></thead>
              <tbody>
                {i.lines.map((l, ix) => (
                  <tr key={ix}>
                    <td><b>{l.itemName}</b>{l.mergedFrom?.length ? <div className="muted" style={{ fontSize: 10 }}>merged: {l.mergedFrom.join(', ')}</div> : null}
                      {l.isAcceptedRejection && <div className="muted" style={{ fontSize: 10 }}>accepted rejections</div>}</td>
                    <td className="num">{num(l.pieces)}</td>
                    <td className="num">{rs(l.buyPrice)}</td>
                    <td className="num">{rs(l.lineTotal)}</td>
                  </tr>
                ))}
                <tr><td colSpan={3} style={{ textAlign: 'right', fontWeight: 700 }}>Invoice total</td><td className="num"><b style={{ color: 'var(--price)' }}>{rs(i.total)}</b></td></tr>
              </tbody>
            </table>
          </div></div>
          {i.rejections.length > 0 && (
            <div className="tablecard" style={{ marginTop: 10 }}><div className="tblscroll">
              <table>
                <thead><tr><th>Rejections</th><th className="num">Rejected</th><th>Reason</th><th className="num">Kept</th><th className="num">Back</th></tr></thead>
                <tbody>
                  {i.rejections.map((r, ix) => (
                    <tr key={ix}><td>{r.itemName}</td><td className="num">{num(r.rejected)}</td><td className="muted">{r.reason ?? '—'}</td><td className="num">{num(r.accepted)}</td><td className="num" style={{ color: 'var(--price)' }}>{num(r.returned)}</td></tr>
                  ))}
                </tbody>
              </table>
            </div></div>
          )}
          <div className="row-actions" style={{ marginTop: 14 }}>
            <button className="btn" onClick={() => printInvoice({
              number: i.number, lotSerial: i.lotSerial ?? '—', supplierName: i.supplierName, final: i.mode === 'Final',
              date: dateLabel(i.generatedAt.slice(0, 10)),
              lines: i.lines.map((l) => ({ itemName: l.itemName, pieces: l.pieces, buyPrice: l.buyPrice, accepted: l.isAcceptedRejection })),
              rejections: i.rejections,
            })}>🧾 Invoice PDF</button>
            <button className="btn p" onClick={onClose}>Close</button>
          </div>
        </>
      )}
    </Modal>
  )
}
