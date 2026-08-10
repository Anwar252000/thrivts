import { useEffect, type ReactNode } from 'react'
import { useAppDispatch } from '@/app/hooks'
import { restoreSession } from '@/features/auth/authSlice'

/** Restores a session from the persisted refresh token (if any) once, on app load. */
export function AuthBootstrapper({ children }: { children: ReactNode }) {
  const dispatch = useAppDispatch()

  useEffect(() => {
    dispatch(restoreSession())
  }, [dispatch])

  return children
}
