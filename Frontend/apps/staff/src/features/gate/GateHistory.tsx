import { dateLabel, num } from '@shared/format'
import { useGateHistory } from '../../api/gate'

const COLS = '110px 1fr 90px 110px'

export function GateHistory() {
  const history = useGateHistory()
  const entries = history.data?.pages.flatMap((p) => p.items) ?? []
  const inCount = entries.filter((e) => e.direction === 'In').length

  return (
    <div style={{ maxWidth: 900, margin: '0 auto' }}>
      <div style={{ fontSize: 22, fontWeight: 800, marginBottom: 4 }}>📜 Gate History</div>
      <div style={{ fontSize: 13, color: 'var(--text2)', marginBottom: 16 }}>
        {history.hasNextPage ? `Latest ${num(entries.length)}` : `${num(entries.length)} total`} · {num(inCount)} in · {num(entries.length - inCount)} out
      </div>

      <div style={{ background: 'var(--surface2)', borderRadius: 14, overflow: 'hidden', border: '1px solid var(--border)' }}>
        <div style={{ display: 'grid', gridTemplateColumns: COLS, gap: 10, padding: '10px 14px', fontSize: 11, textTransform: 'uppercase', letterSpacing: '.05em', color: 'var(--text3)' }}>
          <span>Serial</span><span>Supplier</span><span style={{ textAlign: 'right' }}>Sacks</span><span style={{ textAlign: 'right' }}>Date</span>
        </div>

        {history.isLoading && <div className="loading-line">Loading…</div>}
        {history.isError && (
          <div className="error-line">
            Couldn't load the gate history.
            <button className="logout-btn" onClick={() => void history.refetch()}>Retry</button>
          </div>
        )}
        {history.isSuccess && entries.length === 0 && <div style={{ padding: 20, textAlign: 'center', color: 'var(--text3)' }}>No entries yet.</div>}

        {entries.map((e) => {
          const inbound = e.direction === 'In'
          return (
            <div key={e.id} style={{ display: 'grid', gridTemplateColumns: COLS, gap: 10, alignItems: 'center', padding: '12px 14px', borderTop: '1px solid var(--border)', fontSize: 14 }}>
              <span style={{ fontFamily: 'var(--font-mono)', fontWeight: 700, color: inbound ? '#0EA5E9' : '#F5A623' }}>{e.serial}</span>
              <span>{inbound ? '📥 ' : '📤 '}{e.supplierName}</span>
              <span style={{ fontFamily: 'var(--font-mono)', textAlign: 'right' }}>{num(e.sacks)} sacks</span>
              <span style={{ fontFamily: 'var(--font-mono)', color: 'var(--text3)', textAlign: 'right', fontSize: 12 }}>{dateLabel(e.date)}</span>
            </div>
          )
        })}
      </div>

      {history.hasNextPage && (
        <button className="add-card-btn" style={{ marginTop: 12 }} disabled={history.isFetchingNextPage} onClick={() => void history.fetchNextPage()}>
          {history.isFetchingNextPage ? 'Loading…' : 'Load older entries'}
        </button>
      )}
    </div>
  )
}
