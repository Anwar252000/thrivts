import { NavLink, Outlet, useNavigate } from 'react-router-dom'
import { motion } from 'framer-motion'
import { LogOut, RefreshCw, Clock, XCircle, ShieldAlert } from 'lucide-react'
import { Button, ThemeToggle, Badge } from '@/components/ui'
import { NotificationBell } from '@/components/admin'
import { Logo } from '@/components/marketing/Logo'
import { useAppDispatch, useAppSelector } from '@/app/hooks'
import { signOut } from '@/features/auth/authSlice'
import { useGetMyProfileQuery, sellerApi } from '@/features/seller/sellerApi'
import { cn } from '@/lib/utils'

const NAV_ITEMS = [
  { to: '/seller', label: 'Dashboard', end: true },
  { to: '/seller/requirements', label: 'Open requirements' },
  { to: '/seller/quotes', label: 'My quotes' },
  { to: '/seller/offers', label: 'Direct offers' },
  { to: '/seller/deals', label: 'My deals' },
  { to: '/seller/profile', label: 'Profile' },
]

/** The seller portal's chrome — mirrors BuyerShell's top-nav pattern (seller.html's own topbar +
 * tab strip, converted to real routes). Gates on approval status the same way. */
export function SellerShell() {
  const user = useAppSelector((state) => state.auth.user)

  if (user?.approvalStatus === 'pending') return <PendingScreen />
  if (user?.approvalStatus === 'rejected') return <RejectedScreen />
  if (user?.approvalStatus === 'suspended') return <SuspendedScreen />

  return <ApprovedShell />
}

function ApprovedShell() {
  const dispatch = useAppDispatch()
  const navigate = useNavigate()
  const user = useAppSelector((state) => state.auth.user)
  const { data: profile } = useGetMyProfileQuery()

  const handleSignOut = async () => {
    await dispatch(signOut())
    navigate('/login')
  }

  const handleSync = () => {
    dispatch(sellerApi.util.invalidateTags(['Dashboard', 'Requirement', 'Quote', 'Offer', 'Deal']))
  }

  return (
    <div className="flex min-h-screen flex-col bg-[var(--color-cream)] text-[var(--color-ink)]">
      <nav className="sticky top-0 z-40 border-b border-[var(--color-line)] bg-[var(--color-cream)]/92 backdrop-blur-xl">
        <div className="mx-auto flex max-w-[1280px] items-center gap-5 px-5 py-3">
          <Logo className="text-[1.4rem] shrink-0" />

          <div className="flex flex-1 flex-wrap items-center gap-1 overflow-x-auto">
            {NAV_ITEMS.map((item) => (
              <NavLink
                key={item.to}
                to={item.to}
                end={item.end}
                className={({ isActive }) =>
                  cn(
                    'relative shrink-0 whitespace-nowrap rounded-full px-3.5 py-1.5 text-[0.82rem] font-medium transition-colors',
                    isActive ? 'text-[var(--color-white)]' : 'text-[var(--color-ink-soft)] hover:text-[var(--color-ink)]',
                  )
                }
              >
                {({ isActive }) => (
                  <>
                    {isActive && (
                      <motion.span
                        layoutId="seller-active-nav"
                        className="absolute inset-0 rounded-full bg-[var(--color-sage-dark)]"
                        style={{ zIndex: -1 }}
                        transition={{ duration: 0.25, ease: [0.19, 1, 0.22, 1] }}
                      />
                    )}
                    {item.label}
                  </>
                )}
              </NavLink>
            ))}
          </div>

          <div className="flex shrink-0 items-center gap-2.5">
            {profile && (
              <Badge tone="neutral" className="hidden sm:inline-flex">
                {profile.sellerCode ?? '—'} · {profile.tier}
              </Badge>
            )}
            {profile && (
              <Badge tone={profile.kycVerified ? 'success' : 'warning'} className="hidden md:inline-flex">
                {profile.kycVerified ? '✓ Verified' : 'Not verified'}
              </Badge>
            )}
            <Button size="sm" variant="outline" onClick={handleSync}>
              <RefreshCw size={14} /> Sync
            </Button>
            <NotificationBell />
            <ThemeToggle />
            <button
              onClick={() => navigate('/seller/profile')}
              className="flex items-center gap-2 rounded-full border border-[var(--color-line)] py-1 pl-1 pr-3 transition-colors hover:bg-[var(--color-sage-mist)]"
            >
              <span className="flex size-7 items-center justify-center rounded-full bg-[var(--color-sage-dark)] text-xs font-bold text-white">
                {(user?.fullName ?? user?.email)?.[0]?.toUpperCase() ?? 'S'}
              </span>
              <span className="max-w-[140px] truncate text-sm font-medium">{user?.fullName ?? 'Seller'}</span>
            </button>
            <button
              onClick={handleSignOut}
              title="Sign out"
              aria-label="Sign out"
              className="flex size-9 items-center justify-center rounded-full text-[var(--color-ink-faint)] transition-colors hover:bg-[var(--color-sage-mist)] hover:text-[var(--color-ink)]"
            >
              <LogOut size={16} />
            </button>
          </div>
        </div>
      </nav>

      <main className="mx-auto w-full max-w-[1280px] flex-1 p-5 md:p-8">
        <Outlet />
      </main>
    </div>
  )
}

function StatusShell({ icon: Icon, tone, title, body, children }: {
  icon: typeof Clock; tone: 'warning' | 'danger'; title: string; body: string; children?: React.ReactNode
}) {
  const dispatch = useAppDispatch()
  const navigate = useNavigate()
  const handleSignOut = async () => {
    await dispatch(signOut())
    navigate('/login')
  }

  return (
    <div className="flex min-h-screen flex-col bg-[var(--color-cream)] text-[var(--color-ink)]">
      <nav className="border-b border-[var(--color-line)] px-5 py-4">
        <div className="mx-auto flex max-w-[1280px] items-center justify-between">
          <Logo className="text-[1.4rem]" />
          <Button size="sm" variant="outline" onClick={handleSignOut}>
            <LogOut size={14} /> Sign out
          </Button>
        </div>
      </nav>
      <div className="flex flex-1 items-center justify-center p-6">
        <div className="w-full max-w-lg rounded-[var(--radius-lg)] border border-[var(--color-line)] bg-[var(--color-white)] p-8 text-center shadow-[var(--shadow-sm)]">
          <div
            className={cn(
              'mx-auto mb-4 flex size-14 items-center justify-center rounded-full',
              tone === 'warning' ? 'bg-[var(--color-warning-soft)] text-[var(--color-warning)]' : 'bg-[var(--color-danger-soft)] text-[var(--color-danger)]',
            )}
          >
            <Icon size={26} />
          </div>
          <h2 className="mb-2 text-xl font-bold uppercase tracking-tight">{title}</h2>
          <p className="mb-4 text-sm leading-relaxed text-[var(--color-ink-soft)]">{body}</p>
          {children}
        </div>
      </div>
    </div>
  )
}

function PendingScreen() {
  const { data: profile } = useGetMyProfileQuery()
  return (
    <StatusShell icon={Clock} tone="warning" title="Application under review" body="Thanks for applying. Our team is verifying your details — usually within 24 hours. We'll reach out via WhatsApp.">
      {profile && (
        <div className="mt-2 grid grid-cols-2 gap-3 rounded-[var(--radius-sm)] bg-[var(--color-sage-mist)] p-4 text-left text-sm">
          <div>
            <p className="text-xs font-semibold uppercase tracking-wider text-[var(--color-ink-faint)]">Company</p>
            <p className="font-medium">{profile.companyName ?? '—'}</p>
          </div>
          <div>
            <p className="text-xs font-semibold uppercase tracking-wider text-[var(--color-ink-faint)]">Location</p>
            <p className="font-medium">{profile.locationCity}, {profile.locationCountry}</p>
          </div>
        </div>
      )}
      <p className="mt-4 text-xs text-[var(--color-ink-faint)]">Need to expedite? WhatsApp <strong>+44 7988 595541</strong></p>
    </StatusShell>
  )
}

function RejectedScreen() {
  const rejectionReason = useAppSelector((state) => state.auth.user?.rejectionReason)
  return (
    <StatusShell icon={XCircle} tone="danger" title="Application not approved" body="Unfortunately we weren't able to approve your application at this time.">
      {rejectionReason && (
        <div className="mb-2 rounded-[var(--radius-sm)] bg-[var(--color-sage-mist)] p-3 text-left text-sm">{rejectionReason}</div>
      )}
      <p className="text-xs text-[var(--color-ink-faint)]">
        Questions? WhatsApp <strong>+44 7988 595541</strong>
      </p>
    </StatusShell>
  )
}

function SuspendedScreen() {
  return (
    <StatusShell icon={ShieldAlert} tone="danger" title="Account suspended" body="Your account has been suspended. Contact support if you believe this is a mistake.">
      <p className="text-xs text-[var(--color-ink-faint)]">
        <a className="underline" href="mailto:support@thrivts.com">support@thrivts.com</a>
      </p>
    </StatusShell>
  )
}
