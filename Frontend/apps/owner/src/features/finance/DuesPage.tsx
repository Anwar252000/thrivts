import { num, rs } from '@shared/format'
import { useDues } from '../../api/owner'
import { Kpi, QueryState } from '../../ui'
import { useNavigate } from '../../shell/nav'

export function DuesPage() {
  const dues = useDues()
  const go = useNavigate()
  const d = dues.data

  return (
    <>
      <QueryState query={dues} label="dues" />
      {d && (
        <>
          <div className="statrow" style={{ gridTemplateColumns: 'repeat(3,1fr)' }}>
            <Kpi n={rs(d.youOwe)} l="You owe suppliers" color="var(--price)" />
            <Kpi n={rs(d.clientsOwe)} l="Clients owe you" color="var(--good)" />
            <Kpi n={num(d.suppliersWithBalance)} l="Suppliers with balance" />
          </div>

          <div className="sec-t">⬅ You owe suppliers (payable)</div>
          <div className="tablecard"><div className="tblscroll">
            <table>
              <thead><tr><th>Supplier</th><th className="num">Billed</th><th className="num">Paid</th><th className="num">Due</th><th /></tr></thead>
              <tbody>
                {d.payables.length === 0 && <tr><td colSpan={5} className="muted" style={{ textAlign: 'center' }}>Nothing owed to suppliers. 🎉</td></tr>}
                {d.payables.map((s) => (
                  <tr key={s.supplierId} className="clk" onClick={() => go('hisab', { supplier: s.supplierId })}>
                    <td><b>{s.name}</b></td>
                    <td className="num">{rs(s.billed)}</td>
                    <td className="num ok">{rs(s.paid)}</td>
                    <td className="num"><b style={{ color: 'var(--price)' }}>{rs(s.due)}</b></td>
                    <td className="num muted">open ›</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div></div>

          <div className="sec-t">➡ Clients owe you (receivable)</div>
          <div className="tablecard"><div className="tblscroll">
            <table>
              <thead><tr><th>Order</th><th>Client</th><th>Status</th><th className="num">Value</th><th className="num">Paid</th><th className="num">Outstanding</th></tr></thead>
              <tbody>
                {d.receivables.length === 0 && <tr><td colSpan={6} className="muted" style={{ textAlign: 'center' }}>No active client orders.</td></tr>}
                {d.receivables.map((o) => (
                  <tr key={o.orderId} className="clk" onClick={() => go('vewc', { order: o.orderId })}>
                    <td><b>{o.orderName}</b></td>
                    <td>{o.clientName ?? ''}</td>
                    <td>{o.status}</td>
                    <td className="num">{rs(o.value)}</td>
                    <td className="num ok">{rs(o.paid)}</td>
                    <td className="num"><b style={{ color: o.outstanding > 0 ? 'var(--good)' : 'var(--mut)' }}>{rs(o.outstanding)}</b></td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div></div>
          <div className="hint" style={{ marginTop: 10 }}>Supplier dues come from your hisab (invoices − payments). Receivables are each active order's value minus the payments marked paid.</div>
        </>
      )}
    </>
  )
}
