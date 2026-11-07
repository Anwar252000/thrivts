import { useMemo, useState } from 'react'
import { num, rs } from '@shared/format'
import type { ValuationRow } from '@shared/api/ownerTypes'
import { useValuation } from '../../api/owner'
import { Kpi, QueryState } from '../../ui'

type Group = 'category' | 'brand' | 'flat'

const catOf = (r: ValuationRow) => r.category?.trim() || 'Other'

/** What we hold and what it's worth (sell prices). Owner-only — prices never reach the staff app. */
export function InventoryPage() {
  const valuation = useValuation()
  const [q, setQ] = useState('')
  const [group, setGroup] = useState<Group>('category')
  const [collapsed, setCollapsed] = useState<Record<string, boolean>>({})

  const rows = useMemo(() => {
    const term = q.trim().toLowerCase()
    return (valuation.data?.rows ?? []).filter((r) => !term
      || r.itemName.toLowerCase().includes(term) || catOf(r).toLowerCase().includes(term)
      || r.supplierName.toLowerCase().includes(term) || r.lotSerial.toLowerCase().includes(term))
  }, [q, valuation.data])

  const totalPcs = rows.reduce((a, r) => a + r.quantity, 0)
  const totalVal = rows.reduce((a, r) => a + r.value, 0)

  const head = (
    <thead><tr><th>Item</th><th>Cat</th><th>Grade</th><th>Lot</th><th>From</th><th className="num">Qty</th><th className="num">Rs/pc</th><th className="num">Value</th></tr></thead>
  )
  const row = (r: ValuationRow, i: number) => (
    <tr key={`${r.lotSerial}-${r.itemName}-${r.grade}-${i}`}>
      <td><b>{r.itemName}</b></td>
      <td className="muted">{catOf(r)}</td>
      <td><span className={`tag ${r.grade === 'A' ? 't-good' : 't-warn'}`}>{r.grade}</span></td>
      <td className="mono">{r.lotSerial}</td>
      <td className="muted">{r.supplierName}</td>
      <td className="num">{num(r.quantity)}</td>
      <td className="num lockx">{r.pricePerPiece == null ? <span className="muted">no price</span> : rs(r.pricePerPiece)}</td>
      <td className="num"><b>{rs(r.value)}</b></td>
    </tr>
  )

  const groups = useMemo(() => {
    if (group === 'flat') return []
    const map = new Map<string, ValuationRow[]>()
    for (const r of rows) {
      const k = group === 'brand' ? r.itemName : catOf(r)
      map.set(k, [...(map.get(k) ?? []), r])
    }
    return [...map.entries()].sort(([a, ga], [b, gb]) => {
      if (a === 'Other') return 1
      if (b === 'Other') return -1
      return gb.reduce((s, x) => s + x.quantity, 0) - ga.reduce((s, x) => s + x.quantity, 0)
    })
  }, [group, rows])

  return (
    <>
      <div className="searchbar">
        <input placeholder="🔍 Search item, category, supplier or lot…" value={q} onChange={(e) => setQ(e.target.value)} />
      </div>
      <div style={{ display: 'flex', gap: 6, margin: '0 0 4px' }}>
        <button className={`btn seg${group === 'category' ? ' on' : ''}`} onClick={() => { setGroup('category'); setCollapsed({}) }}>📂 By category</button>
        <button className={`btn seg${group === 'brand' ? ' on' : ''}`} onClick={() => { setGroup('brand'); setCollapsed({}) }}>🏷️ By brand / item</button>
        <button className={`btn seg${group === 'flat' ? ' on' : ''}`} onClick={() => setGroup('flat')}>☰ Flat list</button>
      </div>

      <QueryState query={valuation} label="inventory" />
      {valuation.data && (
        <>
          <div className="statrow">
            <Kpi n={num(totalPcs)} l="Matching pcs" />
            <Kpi n={rs(totalVal)} l="Value" />
            <Kpi n={num(rows.length)} l="Lines" />
          </div>

          {valuation.data.rows.length === 0 && <div className="minibox muted" style={{ textAlign: 'center' }}>No stock in hand yet. Co-signed lots appear here automatically.</div>}
          {valuation.data.rows.length > 0 && rows.length === 0 && <div className="minibox muted" style={{ textAlign: 'center' }}>No items match “{q}”.</div>}

          {group === 'flat' && rows.length > 0 && (
            <div className="tablecard"><div className="tblscroll"><table>{head}<tbody>
              {[...rows].sort((a, b) => catOf(a).localeCompare(catOf(b)) || a.itemName.localeCompare(b.itemName) || a.grade.localeCompare(b.grade)).map(row)}
            </tbody></table></div></div>
          )}

          {groups.map(([name, g]) => {
            const open = !collapsed[name]
            return (
              <div key={name} className="tablecard" style={{ marginBottom: 10 }}>
                <div className="grphead" onClick={() => setCollapsed((c) => ({ ...c, [name]: open }))}
                  style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', cursor: 'pointer', padding: '12px 14px', borderBottom: open ? '1px solid var(--line)' : 'none' }}>
                  <div style={{ display: 'flex', alignItems: 'center', gap: 9 }}>
                    <span style={{ opacity: 0.6, fontSize: 12 }}>{open ? '▾' : '▸'}</span>
                    <b style={{ fontSize: 15 }}>{name}</b>
                    <span className="tag t-info" style={{ fontSize: 11 }}>{g.length} line{g.length > 1 ? 's' : ''}</span>
                  </div>
                  <div style={{ display: 'flex', gap: 16, alignItems: 'center' }}>
                    <span className="muted mono" style={{ fontSize: 13 }}>{num(g.reduce((s, x) => s + x.quantity, 0))} pcs</span>
                    <b className="mono" style={{ fontSize: 13 }}>{rs(g.reduce((s, x) => s + x.value, 0))}</b>
                  </div>
                </div>
                {open && <div className="tblscroll"><table>{head}<tbody>{[...g].sort((a, b) => b.quantity - a.quantity || a.itemName.localeCompare(b.itemName)).map(row)}</tbody></table></div>}
              </div>
            )
          })}
          <div className="hint" style={{ marginTop: 12 }}>Stock on hand from co-signed lots, minus everything dispatched, packed-and-shipped or at the washer. Value uses each item's sell price (Product Details).</div>
        </>
      )}
    </>
  )
}
