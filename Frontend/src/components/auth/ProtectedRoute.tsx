import type { ReactNode } from 'react'
import { Navigate, useLocation } from 'react-router-dom'
import { useAppSelector } from '@/app/hooks'
import type { UserRole } from '@/features/auth/authTypes'
import { Spinner } from '@/components/ui'

interface ProtectedRouteProps {
  children: ReactNode
  /** Restrict to a role once role resolution is wired up (see authTypes.ts AuthUser.role). */
  role?: UserRole
}

export function ProtectedRoute({ children, role }: ProtectedRouteProps) {
  const { user, status } = useAppSelector((state) => state.auth)
  const location = useLocation()

  if (status === 'idle' || status === 'loading') {
    return (
      <div className="flex min-h-screen items-center justify-center bg-[var(--color-cream)]">
        <Spinner />
      </div>
    )
  }

  if (status === 'unauthenticated' || !user) {
    return <Navigate to="/login" replace state={{ from: location }} />
  }

  // Role hasn't resolved yet (no /me endpoint wired up) — let an authenticated user through
  // rather than dead-end them. Once user.role is populated this becomes a real gate.
  if (role && user.role && user.role !== role) {
    return <Navigate to="/login" replace />
  }

  return children
}
