import { useRef, useState } from 'react'
import { todayKarachi } from '@shared/format'
import { useErrorToast, useToast } from '@shared/ui/toast'
import type { GateDirection, Supplier } from '@shared/api/types'
import { useCreateSupplier, useSuppliers } from '../../api/catalog'
import { useNextGateSerials, useRecordGateEntry } from '../../api/gate'

const COPY = {
  In: {
    accent: '#0EA5E9',
    title: '📥 Stock Arriving',
    sub: 'New stock at the gate. Tap the supplier it came from, enter sacks, save — a lot number is created for counting.',
    nextLabel: 'Next lot number:',
    save: '✓ Save Arrival',
  },
  Out: {
    accent: '#F5A623',
    title: '📤 Stock Going Out',
    sub: 'Returning sacks to a supplier (rejections). Tap the supplier, enter sacks, save.',
    nextLabel: 'Next serial:',
    save: '✓ Save Stock Out',
  },
} as const

const bigBox = { height: 60, borderRadius: 14, border: '2px solid var(--border)', background: 'var(--surface2)', color: 'var(--text)' } as const

/** Ali's gate screen (legacy layout): supplier → sacks → date → save. */
export function GateEntry({ direction }: { direction: GateDirection }) {
  const copy = COPY[direction]
  const toast = useToast()
  const showError = useErrorToast()
  const suppliers = useSuppliers()
  const next = useNextGateSerials()
  const record = useRecordGateEntry()
  const createSupplier = useCreateSupplier()

  const [supplier, setSupplier] = useState<Supplier | null>(null)
  const [sacks, setSacks] = useState(0)
  const [date, setDate] = useState(todayKarachi)
  const [newOpen, setNewOpen] = useState(false)
  const [newName, setNewName] = useState('')
  // One key per arrival being entered: a double tap or a retry on a bad connection can't
  // create a second lot. A fresh key is made only after a successful save.
  const key = useRef(crypto.randomUUID())

  const addSupplier = async () => {
    const name = newName.replace(/[<>"'`\\]/g, '').replace(/\s+/g, ' ').trim()
    if (!name) return toast('Type the supplier name first', true)
    if (name.length > 60) return toast('Name is too long (max 60 letters)', true)

    const existing = suppliers.data?.find((s) => s.name.toLowerCase() === name.toLowerCase())
    if (existing) {
      toast(`"${existing.name}" already exists — selected it`, true)
    } else if (!confirm(`Add new supplier "${name}"?\n\nIt will appear on every screen.`)) {
      return
    }

    try {
      const saved = existing ?? (await createSupplier.mutateAsync(name))
      if (!existing) toast(`✓ Supplier "${saved.name}" added and selected`)
      setSupplier(saved)
      setNewOpen(false)
      setNewName('')
    } catch (e) {
      showError(e)
    }
  }

  const save = async () => {
    if (!supplier) return toast('Tap the supplier first', true)
    if (sacks <= 0) return toast('Enter number of sacks', true)
    try {
      const entry = await record.mutateAsync({ direction, supplierId: supplier.id, sacks, date, idempotencyKey: key.current })
      key.current = crypto.randomUUID()
      toast(direction === 'In'
        ? `✓ ${entry.serial} — ${entry.sacks} sacks from ${entry.supplierName} → sent for counting`
        : `✓ ${entry.serial} — ${entry.sacks} sacks out to ${entry.supplierName}`)
      setSupplier(null)
      setSacks(0)
    } catch (e) {
      showError(e)
    }
  }

  const nextSerial = direction === 'In' ? next.data?.nextLot : next.data?.nextExit

  return (
    <div style={{ maxWidth: 900, margin: '0 auto' }}>
      <div style={{ textAlign: 'center', marginBottom: 6 }}>
        <div style={{ fontSize: 26, fontWeight: 800, color: copy.accent }}>{copy.title}</div>
        <div style={{ fontSize: 14, color: 'var(--text2)', marginTop: 4 }}>{copy.sub}</div>
      </div>
      <div style={{ textAlign: 'center', margin: '14px 0' }}>
        <span style={{ fontSize: 13, color: 'var(--text3)' }}>{copy.nextLabel}</span>{' '}
        <span style={{ fontFamily: 'var(--font-mono)', fontSize: 20, fontWeight: 800, color: copy.accent }}>{nextSerial ?? '…'}</span>
      </div>

      <div style={{ fontSize: 14, fontWeight: 700, color: 'var(--text2)', margin: '18px 0 10px' }}>1 · Supplier</div>
      {suppliers.isLoading && <div className="loading-line">Loading suppliers…</div>}
      {suppliers.isError && (
        <div className="error-line">
          Couldn't load suppliers.
          <button className="logout-btn" onClick={() => void suppliers.refetch()}>Retry</button>
        </div>
      )}
      <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill,minmax(150px,1fr))', gap: 10 }}>
        {suppliers.data?.map((s) => {
          const on = supplier?.id === s.id
          return (
            <button key={s.id} onClick={() => setSupplier(s)}
              style={{ padding: '18px 12px', borderRadius: 14, border: `2px solid ${on ? copy.accent : 'var(--border)'}`, background: on ? copy.accent : 'var(--surface2)', color: on ? '#fff' : 'var(--text)', fontSize: 15, fontWeight: 700, cursor: 'pointer', minHeight: 64 }}>
              {s.name}
            </button>
          )
        })}
        <button onClick={() => { setNewOpen(true); setNewName('') }}
          style={{ padding: '18px 12px', borderRadius: 14, border: `2px dashed ${copy.accent}`, background: 'transparent', color: copy.accent, fontSize: 15, fontWeight: 800, cursor: 'pointer', minHeight: 64 }}>
          ＋ New supplier
        </button>
      </div>

      {newOpen && (
        <div style={{ marginTop: 12, padding: 14, border: `2px solid ${copy.accent}`, borderRadius: 14, background: 'var(--surface2)' }}>
          <div style={{ fontSize: 14, fontWeight: 700, color: 'var(--text2)', marginBottom: 8 }}>New supplier name</div>
          <div style={{ display: 'flex', gap: 10, flexWrap: 'wrap' }}>
            <input autoFocus type="text" autoComplete="off" value={newName} placeholder="e.g. Shehzad" maxLength={60}
              onChange={(e) => setNewName(e.target.value)} onKeyDown={(e) => e.key === 'Enter' && void addSupplier()}
              style={{ flex: 1, minWidth: 200, height: 56, fontSize: 18, background: 'var(--surface)', border: '2px solid var(--border)', borderRadius: 12, color: 'var(--text)', padding: '0 14px' }} />
            <button onClick={() => void addSupplier()} disabled={createSupplier.isPending}
              style={{ height: 56, padding: '0 22px', borderRadius: 12, border: 'none', background: copy.accent, color: '#fff', fontSize: 16, fontWeight: 800, cursor: 'pointer' }}>
              ✓ Add supplier
            </button>
            <button onClick={() => setNewOpen(false)}
              style={{ height: 56, padding: '0 18px', borderRadius: 12, border: '2px solid var(--border)', background: 'transparent', color: 'var(--text2)', fontSize: 15, fontWeight: 700, cursor: 'pointer' }}>
              Cancel
            </button>
          </div>
        </div>
      )}

      <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 16, marginTop: 22 }}>
        <div>
          <div style={{ fontSize: 14, fontWeight: 700, color: 'var(--text2)', marginBottom: 10 }}>2 · Number of sacks</div>
          <div style={{ display: 'flex', alignItems: 'center', gap: 12 }}>
            <button onClick={() => setSacks((n) => Math.max(0, n - 1))} style={{ ...bigBox, width: 60, fontSize: 28, fontWeight: 800, cursor: 'pointer' }} aria-label="One sack less">−</button>
            <input type="number" inputMode="numeric" min={0} placeholder="0" value={sacks || ''}
              onChange={(e) => setSacks(Math.max(0, Math.min(10_000, parseInt(e.target.value) || 0)))}
              style={{ ...bigBox, flex: 1, minWidth: 0, textAlign: 'center', fontSize: 30, fontWeight: 800, fontFamily: 'var(--font-mono)' }} />
            <button onClick={() => setSacks((n) => Math.min(10_000, n + 1))} style={{ ...bigBox, width: 60, fontSize: 28, fontWeight: 800, cursor: 'pointer' }} aria-label="One sack more">+</button>
          </div>
        </div>
        <div>
          <div style={{ fontSize: 14, fontWeight: 700, color: 'var(--text2)', marginBottom: 10 }}>3 · Date</div>
          <input type="date" value={date} max={todayKarachi()} onChange={(e) => setDate(e.target.value || todayKarachi())}
            style={{ ...bigBox, width: '100%', fontSize: 18, fontFamily: 'var(--font-mono)', padding: '0 16px' }} />
        </div>
      </div>

      <button onClick={() => void save()} disabled={record.isPending} className={record.isPending ? 'btn-busy' : undefined}
        style={{ width: '100%', marginTop: 26, padding: 22, borderRadius: 16, border: 'none', background: copy.accent, color: '#fff', fontSize: 20, fontWeight: 800, cursor: 'pointer' }}>
        {record.isPending ? 'Saving…' : copy.save}
      </button>
    </div>
  )
}
