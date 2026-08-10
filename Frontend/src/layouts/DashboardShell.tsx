import { NavLink, Outlet } from 'react-router-dom'
import { motion } from 'framer-motion'
import type { LucideIcon } from 'lucide-react'
import { ThemeToggle } from '@/components/ui'
import { cn } from '@/lib/utils'

export interface NavItem {
  to: string
  label: string
  icon: LucideIcon
}

interface DashboardShellProps {
  roleLabel: string
  navItems: NavItem[]
}

/** Shared sidebar+topbar chrome for the Buyer/Seller/Admin portals — one layout, three role configs. */
export function DashboardShell({ roleLabel, navItems }: DashboardShellProps) {
  return (
    <div className="flex min-h-screen bg-[var(--color-cream)] text-[var(--color-ink)]">
      <aside className="hidden w-64 shrink-0 flex-col border-r border-[var(--color-line)] bg-[var(--color-white)] p-6 md:flex">
        <div className="mb-8 flex items-center gap-2">
          <span className="flex size-9 items-center justify-center rounded-[var(--radius-sm)] bg-[var(--color-sage-dark)] text-sm font-bold text-[var(--color-cream)]">
            T
          </span>
          <div>
            <p className="text-sm font-semibold leading-none">Thrivts</p>
            <p className="text-xs text-[var(--color-ink-faint)]">{roleLabel}</p>
          </div>
        </div>

        <nav className="flex flex-1 flex-col gap-1">
          {navItems.map((item) => (
            <NavLink
              key={item.to}
              to={item.to}
              className={({ isActive }) =>
                cn(
                  'group relative flex items-center gap-3 rounded-[var(--radius-sm)] px-3 py-2.5 text-sm font-medium text-[var(--color-ink-soft)] transition-colors',
                  isActive
                    ? 'bg-[var(--color-sage-mist)] text-[var(--color-ink)]'
                    : 'hover:bg-[var(--color-sage-mist)]/60 hover:text-[var(--color-ink)]',
                )
              }
            >
              {({ isActive }) => (
                <>
                  {isActive && (
                    <motion.span
                      layoutId="active-nav-pill"
                      className="absolute inset-0 rounded-[var(--radius-sm)] bg-[var(--color-sage-mist)]"
                      transition={{ duration: 0.25, ease: [0.19, 1, 0.22, 1] }}
                      style={{ zIndex: -1 }}
                    />
                  )}
                  <item.icon size={18} />
                  {item.label}
                </>
              )}
            </NavLink>
          ))}
        </nav>
      </aside>

      <div className="flex min-h-screen flex-1 flex-col">
        <header className="flex items-center justify-between border-b border-[var(--color-line)] bg-[var(--color-white)] px-6 py-4">
          <p className="text-sm text-[var(--color-ink-faint)]">{roleLabel} portal</p>
          <ThemeToggle />
        </header>

        <main className="flex-1 p-6 md:p-8">
          <Outlet />
        </main>
      </div>
    </div>
  )
}
