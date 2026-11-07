import { QueryClient } from '@tanstack/react-query'
import { createSessionStore } from '../auth/session'
import { createHttp, isApiError, qs } from './http'
import type { AppKind, LoginUserCard, SessionResponse } from './types'

/**
 * Everything one app needs to talk to the API: its session, the HTTP client bound to that
 * session, and the query cache. Created once at startup (staff or owner).
 */
export function createApi(app: AppKind) {
  const session = createSessionStore(app)
  const http = createHttp(session)

  const queryClient = new QueryClient({
    defaultOptions: {
      queries: {
        // Live updates (SignalR) invalidate what changed, so data can be considered fresh for a
        // while instead of refetching on every render; focus refetch covers missed signals.
        staleTime: 30_000,
        gcTime: 10 * 60_000,
        refetchOnWindowFocus: true,
        retry: (count, error) => count < 2 && !(isApiError(error) && error.status >= 400 && error.status < 500),
      },
      mutations: { retry: false },
    },
  })

  const auth = {
    users: () => http.request<LoginUserCard[]>(`/auth/users${qs({ app })}`, { anonymous: true }),
    async login(userId: string, pin: string) {
      const res = await http.request<SessionResponse>('/auth/login', { method: 'POST', body: { userId, pin, app }, anonymous: true })
      return session.apply(res)
    },
    changePin: (currentPin: string, newPin: string) => http.post<void>('/auth/change-pin', { currentPin, newPin }),
    async logout() {
      await session.logout()
      queryClient.clear()
    },
  }

  return { app, session, http, auth, queryClient }
}

export type Api = ReturnType<typeof createApi>
