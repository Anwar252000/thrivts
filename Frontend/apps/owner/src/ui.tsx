import { useEffect, type ReactNode } from 'react'
import { isApiError } from '@shared/api/http'

/** The legacy owner modal (dark scrim, panel card). Esc or a click outside closes it. */
export function Modal({ onClose, children, width = 560 }: { onClose: () => void; children: ReactNode; width?: number }) {
  useEffect(() => {
    const onKey = (e: KeyboardEvent) => e.key === 'Escape' && onClose()
    window.addEventListener('keydown', onKey)
    return () => window.removeEventListener('keydown', onKey)
  }, [onClose])

  return (
    <div onClick={(e) => e.target === e.currentTarget && onClose()}
      style={{ position: 'fixed', inset: 0, background: 'rgba(0,0,0,.55)', display: 'grid', placeItems: 'center', zIndex: 9999, padding: 20 }}>
      <div role="dialog" aria-modal="true" style={{ background: 'var(--panel)', border: '1px solid var(--line)', borderRadius: 16, padding: 20, maxWidth: width, width: '100%', maxHeight: '85vh', overflow: 'auto' }}>
        {children}
      </div>
    </div>
  )
}

export function Kpi({ n, l, color }: { n: ReactNode; l: string; color?: string }) {
  return (
    <div className="kpi">
      <div className="n" style={color ? { color } : undefined}>{n}</div>
      <div className="l">{l}</div>
    </div>
  )
}

/** Loading / error states for a query, in the owner style. */
export function QueryState({ query, label }: { query: { isLoading: boolean; isError: boolean; error: unknown; refetch: () => unknown }; label: string }) {
  if (query.isLoading) return <div className="loading-line">Loading {label}…</div>
  if (query.isError)
    return (
      <div className="error-line">
        Couldn't load {label}{isApiError(query.error) ? ` — ${query.error.message}` : ''}.
        <button className="btn" onClick={() => void query.refetch()}>Retry</button>
      </div>
    )
  return null
}

/** Small numeric input that commits on blur/Enter (like the legacy onchange inputs). */
export function NumberCell({ value, onCommit, width = 80, color, min = 0, disabled }: {
  value: number
  onCommit: (v: number) => void
  width?: number
  color?: string
  min?: number
  disabled?: boolean
}) {
  return (
    <input type="number" min={min} defaultValue={value} key={value} disabled={disabled}
      onBlur={(e) => {
        const v = Math.max(min, Math.round(parseFloat(e.target.value) || 0))
        if (v !== value) onCommit(v)
      }}
      onKeyDown={(e) => e.key === 'Enter' && (e.target as HTMLInputElement).blur()}
      style={{ width, textAlign: 'right', background: 'var(--panel2)', border: `1px solid ${color ?? 'var(--line2)'}`, borderRadius: 6, padding: 4, fontFamily: 'var(--mono)', fontSize: 12, color: color ?? 'var(--ink)' }} />
  )
}

/** Inline text input that commits on blur/Enter. */
export function TextCell({ value, onCommit, placeholder, style }: { value: string; onCommit: (v: string) => void; placeholder?: string; style?: React.CSSProperties }) {
  return (
    <input defaultValue={value} key={value} placeholder={placeholder}
      onBlur={(e) => {
        const v = e.target.value.trim()
        if (v !== value) onCommit(v)
        e.target.style.borderColor = 'transparent'
      }}
      onFocus={(e) => (e.target.style.borderColor = 'var(--line2)')}
      onKeyDown={(e) => e.key === 'Enter' && (e.target as HTMLInputElement).blur()}
      style={{ background: 'transparent', border: '1px solid transparent', borderRadius: 4, padding: 4, fontSize: 13, color: 'var(--ink)', width: '100%', ...style }} />
  )
}
