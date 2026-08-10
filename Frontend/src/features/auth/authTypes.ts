/** Mirrors Thrivts.Domain.Enums.UserRole on the backend — keep the two in sync. */
export type UserRole = 'buyer' | 'seller' | 'agency' | 'influencer' | 'admin'

export interface AuthUser {
  id: string
  email: string | null
  /**
   * Not present on the raw Supabase session — Supabase is the identity provider only.
   * Populated by fetchCurrentProfile() once the .NET API's "who am I" endpoint exists
   * (see migration plan §6.1/§6.2). Until then this stays null and role-gated routes
   * treat "authenticated but roleless" as "not yet allowed into a portal".
   */
  role: UserRole | null
}

export type AuthStatus = 'idle' | 'loading' | 'authenticated' | 'unauthenticated'
