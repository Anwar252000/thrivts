import type { ReactNode } from 'react'
import { Navigate, useLocation } from 'react-router-dom'
import { useAppSelector } from '@/app/hooks'
import type { UserRole } from '@/features/auth/authTypes'
import { Spinner } from '@/components/ui'

interface ProtectedRouteProps {
  children: ReactNode
  /** Restricts access to a single role — resolved from GET /api/v1/auth/me on sign-in/restore. */
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

  if (role && user.role !== role) {
    return <Navigate to="/login" replace />
  }

  return children
}
