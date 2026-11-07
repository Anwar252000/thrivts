import { useState } from 'react'
import { useApi, useSession } from '@shared/api/context'
import { useLiveStatus } from '@shared/realtime/live'
import { ErrorBoundary } from '@shared/ui/ErrorBoundary'
import { initial } from '@shared/format'
import { TABS } from './tabs'

const TAB_KEY = 'vh.staff.tab'

export function Shell() {
  const api = useApi()
  const user = useSession()!.user
  const live = useLiveStatus()
  const tabs = TABS[user.role] ?? []
  const [active, setActive] = useState(() => {
    try {
      const saved = sessionStorage.getItem(TAB_KEY)
      return tabs.some((t) => t.id === saved) ? saved! : tabs[0]?.id
    } catch {
      return tabs[0]?.id
    }
  })

  const select = (id: string) => {
    setActive(id)
    try {
      sessionStorage.setItem(TAB_KEY, id)
    } catch {
      // ignore — only remembers the tab across reloads
    }
    window.scrollTo({ top: 0 })
  }

  const current = tabs.find((t) => t.id === active) ?? tabs[0]

  return (
    <div id="appShell" className="active">
      <nav className="top-nav">
        <div className="nav-brand">
          <div className="nav-dot" style={live === 'live' ? undefined : { background: live === 'connecting' ? 'var(--amber)' : 'var(--text3)', animation: 'none' }} title={live === 'live' ? 'Live' : live === 'connecting' ? 'Reconnecting…' : 'Offline — changes from others will show when you reconnect'} />
          Vintage Hub
        </div>
        <div className="nav-right">
          <span className="nav-uname">{user.displayName}</span>
          <div className="nav-av" style={{ background: user.color }}>{initial(user.displayName)}</div>
          <button className="logout-btn" onClick={() => void api.auth.logout()}>Sign out</button>
        </div>
      </nav>

      {tabs.length > 1 && (
        <div className="section-tabs">
          {tabs.map((t) => (
            <button key={t.id} className={`tab-btn${t.id === current?.id ? ' active' : ''}`} onClick={() => select(t.id)}>
              {t.label}
            </button>
          ))}
        </div>
      )}

      <main className="main-content">
        {current ? (
          <ErrorBoundary resetKey={current.id}>
            <current.Component key={current.id} />
          </ErrorBoundary>
        ) : <div className="loading-line">This login has no staff screens.</div>}
      </main>
    </div>
  )
}
