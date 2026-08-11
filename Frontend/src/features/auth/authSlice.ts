import { createAsyncThunk, createSlice } from '@reduxjs/toolkit'
import { apiClient } from '@/lib/apiClient'
import type { AuthStatus, AuthUser } from './authTypes'

const STORAGE_KEY_REFRESH_TOKEN = 'thrivts:refresh-token'

interface AuthSession {
  accessToken: string
  refreshToken: string
  expiresIn: number
  userId: string
  email: string
}

interface CurrentUserResponse {
  id: string
  email: string
  fullName: string
  role: AuthUser['role']
  approvalStatus: string
  isActive: boolean
}

interface ResolvedSession {
  session: AuthSession
  user: CurrentUserResponse
}

interface AuthState {
  user: AuthUser | null
  accessToken: string | null
  status: AuthStatus
  error: string | null
}

const initialState: AuthState = {
  user: null,
  accessToken: null,
  status: 'idle',
  error: null,
}

function persistRefreshToken(refreshToken: string | null) {
  if (refreshToken) {
    window.localStorage.setItem(STORAGE_KEY_REFRESH_TOKEN, refreshToken)
  } else {
    window.localStorage.removeItem(STORAGE_KEY_REFRESH_TOKEN)
  }
}

/** Fetches /auth/me right after obtaining tokens — the only way the frontend learns its own role. */
async function resolveSession(session: AuthSession): Promise<ResolvedSession> {
  const user = await apiClient.get<CurrentUserResponse>('/api/v1/auth/me', { accessToken: session.accessToken })
  return { session, user }
}

/** Restores a session from the refresh token persisted in localStorage — call once at boot. */
export const restoreSession = createAsyncThunk<ResolvedSession | null>('auth/restoreSession', async () => {
  const refreshToken = window.localStorage.getItem(STORAGE_KEY_REFRESH_TOKEN)
  if (!refreshToken) return null

  const session = await apiClient.post<AuthSession>('/api/v1/auth/refresh', { refreshToken })
  return resolveSession(session)
})

export const signIn = createAsyncThunk(
  'auth/signIn',
  async ({ email, password }: { email: string; password: string }) => {
    const session = await apiClient.post<AuthSession>('/api/v1/auth/login', { email, password })
    return resolveSession(session)
  },
)

export const signOut = createAsyncThunk('auth/signOut', async (_: void, { getState }) => {
  const { auth } = getState() as { auth: AuthState }
  if (auth.accessToken) {
    try {
      await apiClient.post('/api/v1/auth/logout', undefined, { accessToken: auth.accessToken })
    } catch {
      // Best-effort — the client discards its token regardless (mirrors LogoutCommandHandler).
    }
  }
})

const authSlice = createSlice({
  name: 'auth',
  initialState,
  reducers: {},
  extraReducers: (builder) => {
    builder
      .addCase(restoreSession.pending, (state) => {
        state.status = 'loading'
      })
      .addCase(restoreSession.fulfilled, (state, action) => {
        applySession(state, action.payload)
      })
      .addCase(restoreSession.rejected, (state) => {
        persistRefreshToken(null)
        state.user = null
        state.accessToken = null
        state.status = 'unauthenticated'
      })
      .addCase(signIn.pending, (state) => {
        state.status = 'loading'
        state.error = null
      })
      .addCase(signIn.fulfilled, (state, action) => {
        applySession(state, action.payload)
      })
      .addCase(signIn.rejected, (state, action) => {
        state.status = 'unauthenticated'
        state.error = action.error.message ?? 'Sign in failed'
      })
      .addCase(signOut.fulfilled, (state) => {
        persistRefreshToken(null)
        state.user = null
        state.accessToken = null
        state.status = 'unauthenticated'
      })
  },
})

function applySession(state: AuthState, resolved: ResolvedSession | null) {
  if (!resolved) {
    state.status = 'unauthenticated'
    return
  }

  const { session, user } = resolved
  persistRefreshToken(session.refreshToken)
  state.user = { id: user.id, email: user.email, role: user.role }
  state.accessToken = session.accessToken
  state.status = 'authenticated'
}

export default authSlice.reducer
