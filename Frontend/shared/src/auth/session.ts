import { API_ROOT } from '../api/http'
import type { AppKind, SessionResponse, SessionUser } from '../api/types'

export interface Session {
  accessToken: string
  accessTokenExpiresAt: string
  user: SessionUser
}

type Listener = () => void

/**
 * Holds the signed-in session for one app. The short-lived access token stays in memory only;
 * the rotating refresh token is kept in localStorage (per app) so a reload or a new tab stays
 * signed in. Every refresh rotates it, and reuse of an old one revokes the whole family
 * server-side. Concurrent refreshes share one request.
 */
export function createSessionStore(app: AppKind) {
  const storageKey = `vh.${app.toLowerCase()}.refresh`
  let session: Session | null = null
  let inflight: Promise<Session | null> | null = null
  const listeners = new Set<Listener>()

  const emit = () => listeners.forEach((l) => l())

  const readRefresh = () => {
    try {
      return localStorage.getItem(storageKey)
    } catch {
      return null
    }
  }

  const writeRefresh = (token: string | null) => {
    try {
      if (token) localStorage.setItem(storageKey, token)
      else localStorage.removeItem(storageKey)
    } catch {
      // Private mode / blocked storage: the session simply won't survive a reload.
    }
  }

  function apply(response: SessionResponse) {
    if (response.user.app !== app) throw new Error('This account belongs to the other app.')
    session = { accessToken: response.accessToken, accessTokenExpiresAt: response.accessTokenExpiresAt, user: response.user }
    writeRefresh(response.refreshToken)
    emit()
    return session
  }

  function clear() {
    session = null
    writeRefresh(null)
    emit()
  }

  async function refresh(): Promise<Session | null> {
    if (inflight) return inflight
    const refreshToken = readRefresh()
    if (!refreshToken) {
      if (session) clear()
      return null
    }

    inflight = (async () => {
      try {
        const res = await fetch(`${API_ROOT}/auth/refresh`, {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({ refreshToken }),
        })
        if (res.status === 401 || res.status === 403 || res.status === 404 || res.status === 422) {
          clear()
          return null
        }
        if (!res.ok) return session // server/network trouble: keep what we have, try again later
        return apply((await res.json()) as SessionResponse)
      } catch {
        return session
      } finally {
        inflight = null
      }
    })()
    return inflight
  }

  async function logout() {
    const refreshToken = readRefresh()
    clear()
    if (refreshToken) {
      await fetch(`${API_ROOT}/auth/logout`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ refreshToken }),
      }).catch(() => undefined)
    }
  }

  return {
    app,
    current: () => session,
    hasStoredSession: () => readRefresh() !== null,
    apply,
    refresh,
    logout,
    subscribe(listener: Listener) {
      listeners.add(listener)
      return () => listeners.delete(listener)
    },
  }
}

export type SessionStore = ReturnType<typeof createSessionStore>
