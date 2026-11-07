import { useNavigate } from 'react-router-dom'
import { LogOut } from 'lucide-react'
import { Button, Card, ThemeToggle } from '@/components/ui'
import { Logo } from '@/components/common'
import { useAppDispatch, useAppSelector } from '@/app/hooks'
import { signOut } from '@/features/auth/authSlice'

/** Landing screen after sign-in until the new role portals are built. */
export function Home() {
  const dispatch = useAppDispatch()
  const navigate = useNavigate()
  const user = useAppSelector((state) => state.auth.user)

  const handleSignOut = async () => {
    await dispatch(signOut())
    navigate('/login', { replace: true })
  }

  return (
    <div className="flex min-h-screen flex-col bg-[var(--color-cream)] text-[var(--color-ink)]">
      <header className="flex items-center justify-between border-b border-[var(--color-line)] px-6 py-4">
        <Logo className="text-xl" />
        <div className="flex items-center gap-2">
          <ThemeToggle />
          <Button size="sm" variant="outline" onClick={handleSignOut}>
            <LogOut size={14} /> Sign out
          </Button>
        </div>
      </header>

      <main className="flex flex-1 items-center justify-center p-6">
        <Card className="w-full max-w-md p-8">
          <h1 className="text-2xl font-semibold">Welcome{user?.fullName ? `, ${user.fullName}` : ''}</h1>
          <dl className="mt-6 grid grid-cols-[auto_1fr] gap-x-6 gap-y-2 text-sm">
            <dt className="text-[var(--color-ink-faint)]">Email</dt>
            <dd>{user?.email ?? '—'}</dd>
            <dt className="text-[var(--color-ink-faint)]">Role</dt>
            <dd className="capitalize">{user?.role ?? '—'}</dd>
            <dt className="text-[var(--color-ink-faint)]">Status</dt>
            <dd className="capitalize">{user?.approvalStatus ?? '—'}</dd>
          </dl>
        </Card>
      </main>
    </div>
  )
}
