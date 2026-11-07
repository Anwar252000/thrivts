import { Fragment, useMemo, useState } from 'react'
import { ago, rs } from '@shared/format'
import { useErrorToast, useToast } from '@shared/ui/toast'
import type { ItemCriteria } from '@shared/api/types'
import type { OwnerItem, OwnerItemRequest } from '@shared/api/ownerTypes'
import { useDecideItemRequest, useItemRequests, useOwnerItems, useSaveItem, useSetItemArchived } from '../../api/owner'
import { Modal, QueryState } from '../../ui'

const CRITERIA: { key: keyof ItemCriteria; label: string }[] = [
  { key: 'brands', label: 'Brands' },
  { key: 'grades', label: 'Grades' },
  { key: 'style', label: 'Style' },
  { key: 'rejectIf', label: 'Reject if' },
  { key: 'notes', label: 'Notes' },
]

const criteriaText = (c: ItemCriteria) => CRITERIA.filter((x) => c[x.key]).map((x) => `${x.label}: ${c[x.key]}`).join('\n')

/** The ~179-item catalogue with buy/sell prices (owner-only), grades and buying criteria. */
export function CatalogPage() {
  const [showArchived, setShowArchived] = useState(false)
  const items = useOwnerItems(showArchived)
  const [q, setQ] = useState('')
  const [cat, setCat] = useState('all')
  const [open, setOpen] = useState<string | null>(null)
  const [editing, setEditing] = useState<OwnerItem | 'new' | null>(null)
  const archive = useSetItemArchived()
  const toast = useToast()
  const showError = useErrorToast()

  const all = useMemo(() => items.data ?? [], [items.data])
  const cats = useMemo(() => [...new Set(all.map((i) => i.category).filter(Boolean) as string[])].sort(), [all])
  const rows = useMemo(() => {
    const term = q.trim().toLowerCase()
    return all.filter((i) => (!term || i.name.toLowerCase().includes(term) || (i.category ?? '').toLowerCase().includes(term)) && (cat === 'all' || i.category === cat))
  }, [all, cat, q])

  const toggleArchive = async (i: OwnerItem) => {
    if (!i.isArchived && !confirm(`Archive ${i.name}? It stops appearing for staff when they count new lots. Existing stock is not affected.`)) return
    try {
      await archive.mutateAsync({ id: i.id, archived: !i.isArchived })
      toast(i.isArchived ? `${i.name} restored` : `${i.name} archived`)
    } catch (e) {
      showError(e)
    }
  }

  return (
    <>
      <ItemRequests />
      <div className="searchbar">
        <input placeholder="🔍 Search product or brand…" value={q} onChange={(e) => setQ(e.target.value)} />
        <select value={cat} onChange={(e) => setCat(e.target.value)}>
          <option value="all">All categories</option>
          {cats.map((c) => <option key={c}>{c}</option>)}
        </select>
        <button className="btn" onClick={() => setShowArchived((s) => !s)}>{showArchived ? 'Hide archived' : 'Show archived'}</button>
        <button className="btn p" onClick={() => setEditing('new')}>➕ Add item</button>
      </div>
      <div className="hint" style={{ margin: '0 2px 10px' }}>One <b>buy</b> price + one <b>sell</b> price per item (optional separate A / C sell prices), plus grade &amp; criteria. 🔒 prices owner-only. Tap a row for criteria.</div>

      <QueryState query={items} label="the catalogue" />
      {items.data && (
        <>
          <div className="tablecard"><div className="tblscroll">
            <table>
              <thead><tr><th>Item</th><th>Category</th><th className="num">Buy</th><th className="num">Sell</th><th className="num">Margin</th><th className="num">Wt</th><th>Grade</th></tr></thead>
              <tbody>
                {rows.length === 0 && <tr><td colSpan={7} className="muted" style={{ textAlign: 'center' }}>No items match.</td></tr>}
                {rows.map((i) => {
                  const isOpen = open === i.id
                  return (
                    <Fragment key={i.id}>
                      <tr className={`clk${isOpen ? ' open' : ''}`} onClick={() => setOpen(isOpen ? null : i.id)} style={i.isArchived ? { opacity: 0.5 } : undefined}>
                        <td><span className="caret">▸</span><b>{i.name}</b>{i.isArchived && <span className="tag t-warn" style={{ marginLeft: 6 }}>archived</span>}</td>
                        <td className="muted">{i.category ?? ''}</td>
                        <td className="num lockx">{i.buyPrice ? rs(i.buyPrice) : <span className="muted">—</span>}</td>
                        <td className="num lockx">{i.sellPrice ? rs(i.sellPrice) : <span className="muted">—</span>}</td>
                        <td className="num ok">{i.marginPercent != null ? `${i.marginPercent}%` : '—'}</td>
                        <td className="num">{i.weightKg ?? '—'}kg</td>
                        <td>{i.gradeNote ?? ''}</td>
                      </tr>
                      {isOpen && (
                        <tr className="detail"><td colSpan={7}>
                          <div className="detail-in">
                            <div className="minibox"><div className="k">Buy / Sell</div><div className="v">{i.buyPrice ? rs(i.buyPrice) : '— not set'} / {i.sellPrice ? rs(i.sellPrice) : '—'}
                              {(i.sellA || i.sellC) ? <span className="muted"> · A {i.sellA ? rs(i.sellA) : '—'} · C {i.sellC ? rs(i.sellC) : '—'}</span> : null}</div></div>
                            <div className="minibox"><div className="k">Weight · grade · SKU</div><div className="v">{i.weightKg ?? '—'}kg · {i.gradeNote ?? '—'} · {i.skuCategory ?? '—'}</div></div>
                            <div className="minibox" style={{ gridColumn: '1/3' }}><div className="k">Buy criteria</div><div className="catnote">{criteriaText(i.criteria) || '—'}</div></div>
                            {i.rejectRule && <div className="minibox" style={{ gridColumn: '1/3' }}><div className="k">Reject rules</div><div className="catnote">{i.rejectRule}</div></div>}
                            <div className="row-actions" style={{ gridColumn: '1/3' }}>
                              <button className="btn" onClick={() => void toggleArchive(i)}>{i.isArchived ? 'Restore' : 'Archive'}</button>
                              <button className="btn p" onClick={() => setEditing(i)}>Edit</button>
                            </div>
                          </div>
                        </td></tr>
                      )}
                    </Fragment>
                  )
                })}
              </tbody>
            </table>
          </div></div>
          <div className="hint" style={{ marginTop: 8 }}>{rows.length} of {all.length} items.</div>
        </>
      )}
      {editing && <ItemForm item={editing === 'new' ? null : editing} categories={cats} onClose={() => setEditing(null)} />}
    </>
  )
}

function ItemForm({ item, categories, onClose }: { item: OwnerItem | null; categories: string[]; onClose: () => void }) {
  const save = useSaveItem()
  const toast = useToast()
  const showError = useErrorToast()
  const numStr = (n: number | null | undefined) => (n == null ? '' : String(n))
  const [f, setF] = useState({
    name: item?.name ?? '', category: item?.category ?? '', skuCategory: item?.skuCategory ?? '', gradeNote: item?.gradeNote ?? '',
    weightKg: numStr(item?.weightKg), rejectRule: item?.rejectRule ?? '',
    buyPrice: numStr(item?.buyPrice), sellPrice: numStr(item?.sellPrice), sellA: numStr(item?.sellA), sellC: numStr(item?.sellC),
    criteria: { brands: '', grades: '', rejectIf: '', notes: '', style: '', ...Object.fromEntries(Object.entries(item?.criteria ?? {}).map(([k, v]) => [k, v ?? ''])) } as Record<keyof ItemCriteria, string>,
  })
  const toNum = (s: string) => (s.trim() === '' ? null : Math.max(0, parseFloat(s) || 0))
  const toText = (s: string) => s.trim() || null

  const submit = async () => {
    if (!f.name.trim()) return toast('Enter the item name', true)
    try {
      await save.mutateAsync({
        id: item?.id ?? null, name: f.name.trim(), category: toText(f.category), skuCategory: toText(f.skuCategory), gradeNote: toText(f.gradeNote),
        weightKg: toNum(f.weightKg), rejectRule: toText(f.rejectRule), buyPrice: toNum(f.buyPrice), sellPrice: toNum(f.sellPrice), sellA: toNum(f.sellA), sellC: toNum(f.sellC),
        criteria: { brands: toText(f.criteria.brands), grades: toText(f.criteria.grades), rejectIf: toText(f.criteria.rejectIf), notes: toText(f.criteria.notes), style: toText(f.criteria.style) },
      })
      toast(item ? `✓ ${f.name.trim()} saved` : `✓ ${f.name.trim()} added to the catalogue`)
      onClose()
    } catch (e) {
      showError(e)
    }
  }

  return (
    <Modal onClose={onClose} width={640}>
      <div style={{ fontSize: 17, fontWeight: 800, marginBottom: 12 }}>{item ? `Edit ${item.name}` : 'New item'}</div>
      <datalist id="vh-cats">{categories.map((c) => <option key={c} value={c} />)}</datalist>
      <div className="form-grid">
        <div className="fld" style={{ gridColumn: '1/-1' }}><label>Name</label><input autoFocus value={f.name} maxLength={120} onChange={(e) => setF({ ...f, name: e.target.value })} /></div>
        <div className="fld"><label>Category</label><input list="vh-cats" value={f.category} maxLength={60} onChange={(e) => setF({ ...f, category: e.target.value })} /></div>
        <div className="fld"><label>Grade</label><input value={f.gradeNote} maxLength={40} placeholder="e.g. A, B" onChange={(e) => setF({ ...f, gradeNote: e.target.value })} /></div>
        <div className="fld"><label>Weight (kg)</label><input type="number" min={0} step="0.05" value={f.weightKg} onChange={(e) => setF({ ...f, weightKg: e.target.value })} /></div>
        <div className="fld"><label>SKU sack category</label><input value={f.skuCategory} maxLength={60} placeholder="auto from name if empty" onChange={(e) => setF({ ...f, skuCategory: e.target.value })} /></div>
        <div className="fld"><label>Buy Rs/pc</label><input type="number" min={0} value={f.buyPrice} onChange={(e) => setF({ ...f, buyPrice: e.target.value })} /></div>
        <div className="fld"><label>Sell Rs/pc</label><input type="number" min={0} value={f.sellPrice} onChange={(e) => setF({ ...f, sellPrice: e.target.value })} /></div>
        <div className="fld"><label>Sell A (optional)</label><input type="number" min={0} value={f.sellA} onChange={(e) => setF({ ...f, sellA: e.target.value })} /></div>
        <div className="fld"><label>Sell C (optional)</label><input type="number" min={0} value={f.sellC} onChange={(e) => setF({ ...f, sellC: e.target.value })} /></div>
      </div>
      <div className="sec-t" style={{ margin: '16px 2px 8px' }}>Buying criteria (shown to staff while grading)</div>
      <div className="form-grid">
        {CRITERIA.map((c) => (
          <div key={c.key} className="fld"><label>{c.label}</label>
            <input value={f.criteria[c.key]} maxLength={500} onChange={(e) => setF({ ...f, criteria: { ...f.criteria, [c.key]: e.target.value } })} /></div>
        ))}
      </div>
      <div className="fld" style={{ marginTop: 11 }}><label>Reject rules</label><textarea rows={2} value={f.rejectRule} maxLength={500} onChange={(e) => setF({ ...f, rejectRule: e.target.value })} /></div>
      <div className="row-actions" style={{ marginTop: 14 }}>
        <button className="btn" onClick={onClose}>Cancel</button>
        <button className="btn p" disabled={save.isPending} onClick={() => void submit()}>{save.isPending ? 'Saving…' : 'Save'}</button>
      </div>
    </Modal>
  )
}

/** Staff asking for an item that isn't in the catalogue — approve (adds it, priced) or reject. */
function ItemRequests() {
  const requests = useItemRequests('Pending')
  const [deciding, setDeciding] = useState<OwnerItemRequest | null>(null)
  if (!requests.data?.length) return null
  return (
    <>
      <div className="sec-t" style={{ marginTop: 4 }}>Item requests from staff · {requests.data.length}</div>
      <div className="card pad" style={{ marginBottom: 12 }}>
        {requests.data.map((r) => (
          <div key={r.id} style={{ display: 'flex', alignItems: 'center', gap: 10, padding: '9px 2px', borderBottom: '1px solid var(--line)' }}>
            <div style={{ flex: 1 }}><b>{r.proposedName}</b><div className="muted" style={{ fontSize: 11 }}>by {r.requestedBy} · {ago(r.createdAt)}{r.note ? ` · ${r.note}` : ''}</div></div>
            <button className="btn p" style={{ padding: '5px 11px', fontSize: 11 }} onClick={() => setDeciding(r)}>Review</button>
          </div>
        ))}
      </div>
      {deciding && <DecideRequest request={deciding} onClose={() => setDeciding(null)} />}
    </>
  )
}

function DecideRequest({ request, onClose }: { request: OwnerItemRequest; onClose: () => void }) {
  const decide = useDecideItemRequest()
  const toast = useToast()
  const showError = useErrorToast()
  const [f, setF] = useState({ category: '', weightKg: '', buyPrice: '', sellPrice: '', note: '' })
  const toNum = (s: string) => (s.trim() === '' ? null : Math.max(0, parseFloat(s) || 0))

  const send = async (approve: boolean) => {
    try {
      await decide.mutateAsync({ id: request.id, approve, category: f.category.trim() || null, weightKg: toNum(f.weightKg), buyPrice: toNum(f.buyPrice), sellPrice: toNum(f.sellPrice), note: f.note.trim() || null })
      toast(approve ? `✓ ${request.proposedName} added to the catalogue` : `Request for ${request.proposedName} rejected`)
      onClose()
    } catch (e) {
      showError(e)
    }
  }

  return (
    <Modal onClose={onClose} width={480}>
      <div style={{ fontSize: 17, fontWeight: 800 }}>{request.proposedName}</div>
      <div className="muted" style={{ fontSize: 12, margin: '2px 0 12px' }}>Requested by {request.requestedBy}{request.note ? ` — “${request.note}”` : ''}</div>
      <div className="form-grid">
        <div className="fld"><label>Category</label><input value={f.category} onChange={(e) => setF({ ...f, category: e.target.value })} /></div>
        <div className="fld"><label>Weight (kg)</label><input type="number" min={0} step="0.05" value={f.weightKg} onChange={(e) => setF({ ...f, weightKg: e.target.value })} /></div>
        <div className="fld"><label>Buy Rs/pc</label><input type="number" min={0} value={f.buyPrice} onChange={(e) => setF({ ...f, buyPrice: e.target.value })} /></div>
        <div className="fld"><label>Sell Rs/pc</label><input type="number" min={0} value={f.sellPrice} onChange={(e) => setF({ ...f, sellPrice: e.target.value })} /></div>
      </div>
      <div className="fld" style={{ marginTop: 11 }}><label>Note to staff (optional)</label><input value={f.note} maxLength={300} onChange={(e) => setF({ ...f, note: e.target.value })} /></div>
      <div className="row-actions" style={{ marginTop: 14 }}>
        <button className="btn danger" disabled={decide.isPending} onClick={() => void send(false)}>Reject</button>
        <button className="btn p" disabled={decide.isPending} onClick={() => void send(true)}>Approve &amp; add</button>
      </div>
    </Modal>
  )
}
