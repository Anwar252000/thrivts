import { Outlet, Link } from 'react-router-dom'
import { ThemeToggle } from '@/components/ui'

export function PublicLayout() {
  return (
    <div className="flex min-h-screen flex-col bg-[var(--color-cream)] text-[var(--color-ink)]">
      <header className="flex items-center justify-between px-6 py-5 md:px-12">
        <Link to="/" className="flex items-center gap-2">
          <span className="flex size-9 items-center justify-center rounded-[var(--radius-sm)] bg-[var(--color-sage-dark)] text-sm font-bold text-[var(--color-cream)]">
            T
          </span>
          <span className="text-lg font-semibold">Thrivts</span>
        </Link>
        <ThemeToggle />
      </header>

      <main className="flex flex-1 flex-col">
        <Outlet />
      </main>

      <footer className="border-t border-[var(--color-line)] px-6 py-6 text-center text-xs text-[var(--color-ink-faint)] md:px-12">
        © {new Date().getFullYear()} Thrivts — invitation-only B2B marketplace.
      </footer>
    </div>
  )
}
