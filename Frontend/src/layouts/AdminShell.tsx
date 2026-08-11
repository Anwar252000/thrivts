import { NavLink, Outlet, useNavigate } from 'react-router-dom'
import { motion } from 'framer-motion'
import {
  LayoutDashboard, UserCheck, ShoppingBag, Package, Handshake, ClipboardList, Briefcase,
  Percent, MessageSquare, AlertTriangle, Settings, FileText, RefreshCw, LogOut,
} from 'lucide-react'
import type { LucideIcon } from 'lucide-react'
import { ThemeToggle } from '@/components/ui'
import { useAppDispatch, useAppSelector } from '@/app/hooks'
import { signOut } from '@/features/auth/authSlice'
import {
  useGetPendingApprovalsQuery, useGetMessageThreadsQuery, useGetDisputesQuery, adminApi,
} from '@/features/admin/adminApi'
import { cn } from '@/lib/utils'

interface NavItem {
  to: string
  label: string
  icon: LucideIcon
  count?: number
  countTone?: 'default' | 'warning' | 'danger'
}

interface NavGroup {
  label?: string
  items: NavItem[]
}

/** The admin portal chrome — dark sage sidebar with nav groups/counts, sticky blurred header with
 * a Refresh action, matching thrivts/01_deploy_to_netlify/admin.html's .sidebar/.main-head 1:1. */
export function AdminShell() {
  const dispatch = useAppDispatch()
  const navigate = useNavigate()
  const user = useAppSelector((state) => state.auth.user)

  const { data: approvals } = useGetPendingApprovalsQuery()
  const { data: threads } = useGetMessageThreadsQuery()
  const { data: openDisputes } = useGetDisputesQuery({ status: 'Open' })

  const unreadMessages = threads?.reduce((sum, t) => sum + t.unreadCount, 0) ?? 0

  const groups: NavGroup[] = [
    { items: [{ to: '/admin', label: 'Dashboard', icon: LayoutDashboard }] },
    {
      label: 'Approvals',
      items: [{ to: '/admin/approvals', label: 'Pending approvals', icon: UserCheck, count: approvals?.length, countTone: 'warning' }],
    },
    {
      label: 'Users',
      items: [
        { to: '/admin/buyers', label: 'Buyers', icon: ShoppingBag },
        { to: '/admin/sellers', label: 'Sellers', icon: Package },
        { to: '/admin/agencies', label: 'Agencies', icon: Handshake },
      ],
    },
    {
      label: 'Trading',
      items: [
        { to: '/admin/requirements', label: 'Requirements', icon: ClipboardList },
        { to: '/admin/deals', label: 'Deals', icon: Briefcase },
        { to: '/admin/commissions', label: 'Commissions', icon: Percent },
      ],
    },
    {
      label: 'Support',
      items: [
        { to: '/admin/messages', label: 'Messages', icon: MessageSquare, count: unreadMessages || undefined },
        { to: '/admin/disputes', label: 'Disputes', icon: AlertTriangle, count: openDisputes?.length, countTone: 'danger' },
      ],
    },
    {
      label: 'System',
      items: [
        { to: '/admin/settings', label: 'Settings', icon: Settings },
        { to: '/admin/audit', label: 'Audit log', icon: FileText },
      ],
    },
  ]

  const handleSignOut = async () => {
    await dispatch(signOut())
    navigate('/login')
  }

  const handleRefresh = () => {
    dispatch(adminApi.util.invalidateTags(['Dashboard', 'Buyer', 'Seller', 'Agency', 'Requirement', 'Deal', 'Commission', 'Dispute', 'MessageThread', 'Approval', 'Audit']))
  }

  return (
    <div className="grid min-h-screen grid-cols-[260px_1fr] max-[900px]:grid-cols-[220px_1fr] max-[760px]:grid-cols-1">
      {/* SIDEBAR */}
      <aside className="sticky top-0 flex h-screen flex-col overflow-hidden bg-[var(--color-sage-darker)] text-[var(--color-cream)] max-[760px]:hidden">
        <div className="border-b border-white/[0.06] px-5 py-5">
          <div className="flex items-start text-[1.4rem] font-black leading-none tracking-[-0.045em] lowercase">
            thrivt<span className="ml-0.5 mt-[0.18em] text-[0.4em] text-[var(--color-sage-soft)]">✦</span>s
          </div>
          <div className="mt-2 text-[0.6rem] font-semibold uppercase tracking-[0.3em] text-[var(--color-sage-soft)]">Admin Panel</div>
        </div>

        <nav className="flex-1 overflow-y-auto p-3">
          {groups.map((group, gi) => (
            <div key={gi} className={gi > 0 ? 'mt-2' : undefined}>
              {group.label && (
                <div className="px-3 pb-2 pt-3 text-[0.6rem] font-semibold uppercase tracking-[0.3em] text-[var(--color-sage-soft)]">
                  {group.label}
                </div>
              )}
              {group.items.map((item) => (
                <NavLink
                  key={item.to}
                  to={item.to}
                  end={item.to === '/admin'}
                  className={({ isActive }) =>
                    cn(
                      'relative flex w-full items-center gap-2.5 rounded-[var(--radius-sm)] px-3 py-[9px] text-[0.84rem] font-medium text-[var(--color-sage-soft)] transition-colors',
                      isActive ? 'bg-white/10 text-[var(--color-cream)]' : 'hover:bg-white/[0.06] hover:text-[var(--color-cream)]',
                    )
                  }
                >
                  {({ isActive }) => (
                    <>
                      {isActive && (
                        <motion.span
                          layoutId="admin-active-nav"
                          className="absolute left-0 h-[18px] w-[3px] rounded-sm bg-[var(--color-accent)]"
                          transition={{ duration: 0.25, ease: [0.19, 1, 0.22, 1] }}
                        />
                      )}
                      <item.icon size={16} className="shrink-0" />
                      <span className="min-w-0 flex-1 truncate">{item.label}</span>
                      {!!item.count && (
                        <span
                          className={cn(
                            'ml-auto flex h-[18px] min-w-[18px] shrink-0 items-center justify-center rounded-full px-1.5 text-[0.62rem] font-bold text-[var(--color-cream)]',
                            item.countTone === 'danger' && 'bg-[var(--color-danger)]',
                            item.countTone === 'warning' && 'bg-[var(--color-warning)]',
                            !item.countTone && 'bg-[var(--color-accent)]',
                          )}
                        >
                          {item.count}
                        </span>
                      )}
                    </>
                  )}
                </NavLink>
              ))}
            </div>
          ))}
        </nav>

        <div className="border-t border-white/[0.06] p-3">
          <div className="flex items-center gap-2.5 rounded-[var(--radius-md)] bg-white/[0.06] px-2.5 py-2">
            <div className="flex size-[34px] shrink-0 items-center justify-center rounded-full border border-white/10 bg-[var(--color-sage-dark)] text-[0.78rem] font-bold tracking-wide text-[var(--color-cream)]">
              {user?.email?.[0]?.toUpperCase() ?? 'A'}
            </div>
            <div className="min-w-0 flex-1">
              <div className="truncate text-[0.82rem] font-semibold text-[var(--color-cream)]">Admin</div>
              <div className="truncate text-[0.7rem] text-[var(--color-sage-soft)]">{user?.email ?? '—'}</div>
            </div>
            <button
              onClick={handleSignOut}
              title="Sign out"
              aria-label="Sign out"
              className="flex size-7 shrink-0 items-center justify-center rounded-full text-[var(--color-sage-soft)] transition-colors hover:bg-white/10 hover:text-[var(--color-cream)]"
            >
              <LogOut size={14} />
            </button>
          </div>
        </div>
      </aside>

      {/* MAIN */}
      <div className="flex min-h-screen min-w-0 flex-col">
        <header className="sticky top-0 z-50 border-b border-[var(--color-line)] bg-[var(--color-cream)]/92 px-5 py-4 backdrop-blur-xl">
          <div className="flex items-center justify-between gap-4">
            <div>
              <h1 className="text-[1.4rem] font-black uppercase leading-none tracking-[-0.025em]">Admin</h1>
              <div className="mt-0.5 text-[0.85rem] font-normal text-[var(--color-ink-soft)]">Thrivts platform control</div>
            </div>
            <div className="flex items-center gap-3">
              <div className="flex items-center gap-1.5 text-[0.74rem] font-semibold uppercase tracking-[0.1em] text-[var(--color-ink-soft)]">
                <span className="relative flex size-2">
                  <span className="absolute inline-flex size-full animate-ping rounded-full bg-[var(--color-success)] opacity-75" />
                  <span className="relative inline-flex size-2 rounded-full bg-[var(--color-success)]" />
                </span>
                Live
              </div>
              <button
                onClick={handleRefresh}
                className="inline-flex h-9 items-center gap-2 rounded-[var(--radius-sm)] border border-[var(--color-line-strong)] bg-[var(--color-white)] px-3 text-sm font-medium text-[var(--color-ink)] transition-colors hover:bg-[var(--color-sage-mist)]"
              >
                <RefreshCw size={14} /> Refresh
              </button>
              <ThemeToggle />
            </div>
          </div>
        </header>

        <main className="flex-1 p-5 md:p-8">
          <Outlet />
        </main>
      </div>
    </div>
  )
}
