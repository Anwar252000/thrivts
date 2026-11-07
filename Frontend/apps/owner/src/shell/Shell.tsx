import { useMemo, useState } from 'react'
import { useApi, useSession } from '@shared/api/context'
import { useLiveStatus } from '@shared/realtime/live'
import { ErrorBoundary } from '@shared/ui/ErrorBoundary'
import { NavContext } from './nav'
import { PAGES } from './pages'

const PAGE_KEY = 'vh.owner.page'

const CONN = {
  live: { cls: 'ok', text: 'live' },
  connecting: { cls: '', text: 'connecting…' },
  offline: { cls: 'bad', text: 'offline' },
} as const

export function Shell() {
  const api = useApi()
  const user = useSession()!.user
  const live = useLiveStatus()
  const pages = useMemo(() => PAGES.filter((p) => !p.permission || user.permissions.includes(p.permission)), [user.permissions])
  const [menuOpen, setMenuOpen] = useState(false)
  const [params, setParams] = useState<Record<string, string>>({})
  const [visit, setVisit] = useState(0)
  const [active, setActive] = useState(() => {
    try {
      const saved = sessionStorage.getItem(PAGE_KEY)
      return pages.some((p) => p.key === saved) ? saved! : pages[0].key
    } catch {
      return pages[0].key
    }
  })

  const go = (key: string, next: Record<string, string> = {}) => {
    setActive(key)
    setParams(next)
    setVisit((v) => v + 1) // re-open the page fresh, so it reads its parameters
    setMenuOpen(false)
    try {
      sessionStorage.setItem(PAGE_KEY, key)
    } catch {
      // only remembers the page across reloads
    }
    window.scrollTo({ top: 0 })
  }

  const page = pages.find((p) => p.key === active) ?? pages[0]
  const conn = CONN[live]

  return (
    <NavContext.Provider value={{ go, params }}>
      <div className="app">
        <div className={`side-scrim${menuOpen ? ' on' : ''}`} onClick={() => setMenuOpen(false)} />
        <aside className={`side${menuOpen ? ' on' : ''}`}>
          <div className="brand">
            <div className="lg" />
            <div><b>Vintage Hub</b><small>{user.displayName} · {user.title}</small></div>
          </div>
          <nav className="nav">
            {pages.map((p) => (
              <button key={p.key} className={p.key === page.key ? 'on' : undefined} onClick={() => go(p.key)}>
                <span className="ic">{p.icon}</span>{p.title}
              </button>
            ))}
          </nav>
          <button className="logoutb" onClick={() => void api.auth.logout()}>Log out</button>
        </aside>

        <div className="main">
          <div className="top">
            <button className="menu-btn" onClick={() => setMenuOpen((o) => !o)} aria-label="Menu">☰</button>
            <div><h1>{page.title}</h1><div className="sub">{page.sub}</div></div>
            <div style={{ marginLeft: 'auto' }}>
              <div className={`conn ${conn.cls}`}><span className="dot" /><span>{conn.text}</span></div>
            </div>
          </div>
          <div className="content">
            <ErrorBoundary resetKey={`${page.key}-${visit}`}>
              <page.Component key={`${page.key}-${visit}`} />
            </ErrorBoundary>
          </div>
        </div>
      </div>
    </NavContext.Provider>
  )
}
