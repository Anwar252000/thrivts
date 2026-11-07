import type { Session } from '../auth/session'

const API_BASE = (import.meta.env.VITE_API_BASE_URL ?? '').replace(/\/$/, '')
export const API_ROOT = `${API_BASE}/api/v1`
export const HUB_URL = `${API_BASE}/hubs/live`

/** A failed API call, carrying the server's RFC 7807 problem details. `title` is always a
 *  sentence written for the user; `fieldErrors` holds per-field validation messages. */
export class ApiError extends Error {
  readonly status: number
  readonly fieldErrors: Record<string, string[]>
  readonly traceId?: string

  constructor(status: number, title: string, fieldErrors: Record<string, string[]> = {}, traceId?: string) {
    super(title)
    this.name = 'ApiError'
    this.status = status
    this.fieldErrors = fieldErrors
    this.traceId = traceId
  }

  /** First validation message, falling back to the title — what a form should show. */
  get firstMessage(): string {
    const first = Object.values(this.fieldErrors)[0]?.[0]
    return first ?? this.message
  }
}

export const isApiError = (e: unknown): e is ApiError => e instanceof ApiError

/** Hooks the HTTP layer needs from the session, wired up once per app by `createApi`. */
export interface SessionAccess {
  current(): Session | null
  /** Exchanges the refresh token for a new session; resolves null when the user must log in again. */
  refresh(): Promise<Session | null>
}

export interface RequestOptions {
  method?: 'GET' | 'POST' | 'PUT' | 'DELETE'
  body?: unknown
  /** Sent as Idempotency-Key so a retried write is applied once. Writes get one automatically. */
  idempotencyKey?: string
  signal?: AbortSignal
  /** For the few anonymous endpoints (login, portal). */
  anonymous?: boolean
}

const REFRESH_EARLY_MS = 30_000
const NETWORK_RETRIES = 2

function newKey() {
  return crypto.randomUUID()
}

async function toApiError(response: Response): Promise<ApiError> {
  const problem = await response.json().catch(() => null) as
    | { title?: string; errors?: Record<string, string[]>; traceId?: string }
    | null
  const fallback =
    response.status === 429 ? 'Too many requests — please wait a moment and try again.'
      : response.status >= 500 ? 'Something went wrong on the server — please try again.'
        : `Request failed (${response.status}).`
  return new ApiError(response.status, problem?.title ?? fallback, problem?.errors ?? {}, problem?.traceId)
}

const sleep = (ms: number) => new Promise((r) => setTimeout(r, ms))

/**
 * The one way the apps talk to the API. It attaches the access token (refreshing it shortly
 * before expiry, and once more on a 401), gives every write an Idempotency-Key and retries it on
 * network failure — safe, because the server applies a key only once — and turns error responses
 * into ApiError with the server's user-facing message.
 */
export function createHttp(session: SessionAccess) {
  async function token(): Promise<string | null> {
    let s = session.current()
    if (s && new Date(s.accessTokenExpiresAt).getTime() - Date.now() < REFRESH_EARLY_MS)
      s = await session.refresh()
    return s?.accessToken ?? null
  }

  async function send(path: string, options: RequestOptions, accessToken: string | null, idempotencyKey?: string) {
    const headers: Record<string, string> = { Accept: 'application/json' }
    if (options.body !== undefined) headers['Content-Type'] = 'application/json'
    if (accessToken) headers.Authorization = `Bearer ${accessToken}`
    if (idempotencyKey) headers['Idempotency-Key'] = idempotencyKey

    let attempt = 0
    for (;;) {
      try {
        return await fetch(`${API_ROOT}${path}`, {
          method: options.method ?? 'GET',
          headers,
          body: options.body === undefined ? undefined : JSON.stringify(options.body),
          signal: options.signal,
        })
      } catch {
        // Reads and keyed writes are safe to repeat; anything else surfaces the failure.
        const retryable = !options.signal?.aborted && (idempotencyKey || (options.method ?? 'GET') === 'GET')
        if (!retryable || attempt++ >= NETWORK_RETRIES)
          throw new ApiError(0, navigator.onLine ? 'Could not reach the server — check the connection and try again.' : 'You are offline — reconnect and try again.')
        await sleep(400 * 2 ** attempt)
      }
    }
  }

  async function request<T>(path: string, options: RequestOptions = {}): Promise<T> {
    const method = options.method ?? 'GET'
    const idempotencyKey = method === 'GET' ? undefined : options.idempotencyKey ?? newKey()

    let response = await send(path, options, options.anonymous ? null : await token(), idempotencyKey)
    if (response.status === 401 && !options.anonymous) {
      const renewed = await session.refresh()
      if (renewed) response = await send(path, options, renewed.accessToken, idempotencyKey)
    }

    if (!response.ok) throw await toApiError(response)
    if (response.status === 204) return undefined as T
    const text = await response.text()
    return (text ? JSON.parse(text) : undefined) as T
  }

  /** Streams a file (CSV export) and hands it to the browser as a download. */
  async function download(path: string, fallbackName: string) {
    let response = await send(path, {}, await token())
    if (response.status === 401) {
      const renewed = await session.refresh()
      if (renewed) response = await send(path, {}, renewed.accessToken)
    }
    if (!response.ok) throw await toApiError(response)

    const name = /filename="?([^";]+)"?/.exec(response.headers.get('Content-Disposition') ?? '')?.[1] ?? fallbackName
    const url = URL.createObjectURL(await response.blob())
    const a = Object.assign(document.createElement('a'), { href: url, download: name })
    document.body.append(a)
    a.click()
    a.remove()
    setTimeout(() => URL.revokeObjectURL(url), 2000)
  }

  /** Fetches an authorized image (signatures) as an object URL for an <img>. */
  async function blobUrl(path: string) {
    const response = await send(path, {}, await token())
    if (!response.ok) throw await toApiError(response)
    return URL.createObjectURL(await response.blob())
  }

  return {
    request,
    download,
    blobUrl,
    /** A valid access token (refreshed if about to expire) — for the realtime connection. */
    accessToken: token,
    get: <T>(path: string, signal?: AbortSignal) => request<T>(path, { signal }),
    post: <T>(path: string, body?: unknown, idempotencyKey?: string) => request<T>(path, { method: 'POST', body, idempotencyKey }),
    put: <T>(path: string, body?: unknown, idempotencyKey?: string) => request<T>(path, { method: 'PUT', body, idempotencyKey }),
    del: <T>(path: string) => request<T>(path, { method: 'DELETE' }),
  }
}

export type Http = ReturnType<typeof createHttp>

/** Builds a query string, dropping empty values. */
export function qs(params: Record<string, string | number | boolean | null | undefined>) {
  const search = new URLSearchParams()
  for (const [k, v] of Object.entries(params))
    if (v !== undefined && v !== null && v !== '') search.set(k, String(v))
  const s = search.toString()
  return s ? `?${s}` : ''
}
