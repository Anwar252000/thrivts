import { Link } from 'react-router-dom'

export function NotFound() {
  return (
    <div className="flex min-h-screen flex-col items-center justify-center gap-3 bg-[var(--color-cream)] p-6 text-center text-[var(--color-ink)]">
      <p className="text-5xl font-semibold">404</p>
      <p className="text-sm text-[var(--color-ink-soft)]">This page doesn't exist.</p>
      <Link to="/" className="text-sm font-medium text-[var(--color-accent)] underline">Go home</Link>
    </div>
  )
}
