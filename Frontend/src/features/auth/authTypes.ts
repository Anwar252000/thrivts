/** Mirrors Thrivts.Domain.Enums.UserRole on the backend — keep the two in sync. */
export type UserRole = 'buyer' | 'seller' | 'agency' | 'influencer' | 'admin'

export interface AuthUser {
  id: string
  email: string | null
  fullName: string | null
  /** Resolved from GET /api/v1/auth/me right after sign-in/session-restore — see authSlice. */
  role: UserRole | null
}

export type AuthStatus = 'idle' | 'loading' | 'authenticated' | 'unauthenticated'
