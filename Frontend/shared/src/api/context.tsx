import { createContext, useContext, useEffect, useState, useSyncExternalStore, type ReactNode } from 'react'
import { QueryClientProvider } from '@tanstack/react-query'
import type { Api } from './client'
import { LiveProvider } from '../realtime/live'

const ApiContext = createContext<Api | null>(null)

export function AppProviders({ api, children }: { api: Api; children: ReactNode }) {
  return (
    <ApiContext.Provider value={api}>
      <QueryClientProvider client={api.queryClient}>
        <LiveProvider api={api}>{children}</LiveProvider>
      </QueryClientProvider>
    </ApiContext.Provider>
  )
}

export function useApi(): Api {
  const api = useContext(ApiContext)
  if (!api) throw new Error('useApi must be used inside <AppProviders>.')
  return api
}

export function useSession() {
  const { session } = useApi()
  return useSyncExternalStore(session.subscribe, session.current)
}

/** True when the signed-in user holds the permission (mirrors the API's policies — the API
 *  still enforces every call; this only decides what to show). */
export function useCan(permission: string) {
  return useSession()?.user.permissions.includes(permission) ?? false
}

export type BootState = 'checking' | 'signed-out' | 'signed-in'

/** Restores a session from the stored refresh token on startup. */
export function useBootstrap(): BootState {
  const { session } = useApi()
  const current = useSession()
  const [checked, setChecked] = useState(() => !session.hasStoredSession())

  useEffect(() => {
    if (checked) return
    let cancelled = false
    session.refresh().finally(() => !cancelled && setChecked(true))
    return () => {
      cancelled = true
    }
  }, [checked, session])

  if (current) return 'signed-in'
  return checked ? 'signed-out' : 'checking'
}
