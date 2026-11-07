import { createContext, useContext, useEffect, useState, useSyncExternalStore, type ReactNode } from 'react'
import { HubConnectionBuilder, HubConnectionState, LogLevel, type HubConnection } from '@microsoft/signalr'
import { HUB_URL } from '../api/http'
import type { Api } from '../api/client'

export type LiveStatus = 'connecting' | 'live' | 'offline'

/**
 * Which cached queries each server topic can affect. Query keys in both apps start with one of
 * these prefixes, so a "stock changed" signal refetches every visible stock-related screen (and
 * nothing else). The server only ever says *what* changed — data always comes back through the
 * normal authorized endpoints, so a staff device can't receive owner data over the socket.
 */
const TOPIC_KEYS: Record<string, string[]> = {
  lots: ['lots', 'floor', 'dashboards', 'gate', 'finance', 'returns'],
  stock: ['stock', 'inventory', 'dispatches', 'sku', 'dashboards', 'returns', 'wash', 'finance'],
  sku: ['sku', 'orders', 'dashboards'],
  orders: ['orders', 'dashboards'],
  operations: ['operations', 'attendance'],
  escalations: ['escalations'],
  catalog: ['catalog'],
  finance: ['finance'],
  wash: ['wash', 'stock', 'inventory'],
  'client-messages': ['client-messages'],
}

const LiveContext = createContext<LiveStatus>('offline')

export function useLiveStatus() {
  return useContext(LiveContext)
}

export function LiveProvider({ api, children }: { api: Api; children: ReactNode }) {
  const session = useSyncExternalStore(api.session.subscribe, api.session.current)
  const signedIn = session !== null
  const [status, setStatus] = useState<LiveStatus>('offline')

  useEffect(() => {
    if (!signedIn) return

    let stopped = false
    let retryTimer: ReturnType<typeof setTimeout> | undefined
    const connection: HubConnection = new HubConnectionBuilder()
      .withUrl(HUB_URL, { accessTokenFactory: async () => (await api.http.accessToken()) ?? '' })
      .withAutomaticReconnect([0, 2_000, 5_000, 10_000, 30_000])
      .configureLogging(LogLevel.Warning)
      .build()

    const refetchEverything = () => api.queryClient.invalidateQueries()

    connection.on('changed', (message: { topic?: string }) => {
      const prefixes = (message.topic && TOPIC_KEYS[message.topic]) || []
      for (const prefix of prefixes) void api.queryClient.invalidateQueries({ queryKey: [prefix] })
    })
    connection.onreconnecting(() => setStatus('connecting'))
    connection.onreconnected(() => {
      setStatus('live')
      void refetchEverything() // signals sent while we were away were missed
    })
    connection.onclose(() => {
      if (stopped) return
      setStatus('offline')
      retryTimer = setTimeout(start, 30_000) // automatic reconnect gave up — keep trying slowly
    })

    async function start() {
      if (stopped) return
      setStatus('connecting')
      try {
        await connection.start()
        if (!stopped) setStatus('live')
      } catch {
        if (!stopped) {
          setStatus('offline')
          retryTimer = setTimeout(start, 15_000)
        }
      }
    }

    // Start on the next tick: React's development double-mount stops the first connection
    // immediately, and this way it never begins negotiating at all.
    retryTimer = setTimeout(start, 0)
    return () => {
      stopped = true
      clearTimeout(retryTimer)
      if (connection.state !== HubConnectionState.Disconnected) void connection.stop()
      setStatus('offline')
    }
  }, [api, signedIn])

  return <LiveContext.Provider value={status}>{children}</LiveContext.Provider>
}
