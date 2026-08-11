import { Link } from 'react-router-dom'
import { Users, Store, Handshake, ClipboardList, AlertTriangle, Bell, Activity } from 'lucide-react'
import { Card, Badge, PageTransition, Skeleton } from '@/components/ui'
import {
  useGetDashboardStatsQuery, useGetPendingApprovalsQuery, useGetDisputesQuery,
  useGetCommissionsQuery, useGetAuditLogQuery, useGetRequirementsQuery,
} from '@/features/admin/adminApi'
import { formatUsd, formatNumber, formatDateTime } from '@/lib/utils'
import { humanizeStatus } from '@/features/admin/statusTone'

export function AdminDashboard() {
  const { data: stats, isLoading: statsLoading } = useGetDashboardStatsQuery()
  const { data: approvals } = useGetPendingApprovalsQuery()
  const { data: openDisputes } = useGetDisputesQuery({ status: 'Open' })
  const { data: readyCommissions } = useGetCommissionsQuery({ status: 'ReadyToRelease' })
  const { data: liveRequirements } = useGetRequirementsQuery({ status: 'Posted', pageSize: 1 })
  const { data: auditLog } = useGetAuditLogQuery({ take: 15 })

  const pendingBuyers = approvals?.filter((a) => a.role === 'Buyer').length ?? 0
  const pendingSellers = approvals?.filter((a) => a.role === 'Seller').length ?? 0
  const pendingAgencies = approvals?.filter((a) => a.role === 'Agency').length ?? 0
  const pendingCommissionUsd = readyCommissions?.reduce((sum, c) => sum + c.commissionAmountUsd, 0) ?? 0

  const pendingActions = [
    approvals && approvals.length > 0
      ? { to: '/admin/approvals', label: `${approvals.length} pending approval${approvals.length === 1 ? '' : 's'}`, tone: 'warning' as const }
      : null,
    openDisputes && openDisputes.length > 0
      ? { to: '/admin/disputes', label: `${openDisputes.length} open dispute${openDisputes.length === 1 ? '' : 's'} need resolution`, tone: 'danger' as const }
      : null,
    readyCommissions && readyCommissions.length > 0
      ? { to: '/admin/commissions', label: `${readyCommissions.length} commission${readyCommissions.length === 1 ? '' : 's'} ready to release`, tone: 'info' as const }
      : null,
  ].filter((x): x is NonNullable<typeof x> => x !== null)

  return (
    <PageTransition>
      <h1 className="mb-6 text-2xl font-semibold">Platform overview</h1>

      <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
        <SummaryCard label="Total buyers" value={stats?.activeBuyers} sub={`${pendingBuyers} pending approval`} icon={Users} loading={statsLoading} />
        <SummaryCard label="Total sellers" value={stats?.activeSellers} sub={`${pendingSellers} pending approval`} icon={Store} loading={statsLoading} />
        <SummaryCard label="Active agencies" value={stats?.activeAgencies} sub={`${pendingAgencies} pending`} icon={Handshake} loading={statsLoading} />
        <SummaryCard label="Live requirements" value={liveRequirements?.totalCount} sub="Posted to sellers" icon={ClipboardList} loading={statsLoading} />
      </div>

      <div className="mt-4 grid gap-4 sm:grid-cols-3">
        <Card className="flex flex-col gap-2 bg-[var(--color-sage-darker)] text-[var(--color-cream)]">
          <p className="text-[0.68rem] font-semibold uppercase tracking-[0.2em] text-[var(--color-sage-soft)]">Total volume</p>
          <p className="text-[2.2rem] font-black leading-none tracking-tight">
            {statsLoading ? <Skeleton className="h-9 w-32" /> : formatUsd(stats?.totalVolumeUsd ?? 0)}
          </p>
          <p className="text-sm font-normal text-[var(--color-sage-soft)]">All-time settled deals</p>
        </Card>
        <Card className="flex flex-col gap-2">
          <p className="text-[0.68rem] font-semibold uppercase tracking-[0.2em] text-[var(--color-sage)]">Pending commissions</p>
          <p className="text-[2.2rem] font-black leading-none tracking-tight">{formatUsd(pendingCommissionUsd)}</p>
          <p className="text-sm font-normal text-[var(--color-ink-soft)]">{readyCommissions?.length ?? 0} ready to release</p>
        </Card>
        <Card className="flex flex-col gap-2">
          <p className="text-[0.68rem] font-semibold uppercase tracking-[0.2em] text-[var(--color-sage)]">Open disputes</p>
          <p className="text-[2.2rem] font-black leading-none tracking-tight">{openDisputes?.length ?? 0}</p>
          <p className="text-sm font-normal text-[var(--color-ink-soft)]">Need resolution</p>
        </Card>
      </div>

      <div className="mt-4 grid gap-5 lg:grid-cols-[1.3fr_1fr]">
        <Card>
          <div className="mb-4 flex items-center justify-between">
            <p className="text-xs font-semibold uppercase tracking-[0.15em] text-[var(--color-ink-faint)]">Pending actions</p>
            <Bell size={14} className="text-[var(--color-ink-faint)]" />
          </div>
          {pendingActions.length === 0 ? (
            <p className="py-3 text-sm text-[var(--color-ink-faint)]">Nothing needs your attention right now.</p>
          ) : (
            <div className="flex flex-col">
              {pendingActions.map((action) => (
                <Link
                  key={action.to}
                  to={action.to}
                  className="flex items-center justify-between border-b border-[var(--color-line-soft)] py-3 text-sm last:border-b-0 hover:text-[var(--color-accent)]"
                >
                  <span className="flex items-center gap-2">
                    <AlertTriangle size={14} className="text-[var(--color-ink-faint)]" />
                    {action.label}
                  </span>
                  <Badge tone={action.tone}>Review</Badge>
                </Link>
              ))}
            </div>
          )}
        </Card>

        <Card className="bg-[var(--color-sage-darker)] text-[var(--color-cream)]">
          <div className="mb-4 flex items-center justify-between">
            <p className="text-xs font-semibold uppercase tracking-[0.15em] text-[var(--color-sage-soft)]">Recent activity</p>
            <span className="relative flex size-2">
              <span className="absolute inline-flex size-full animate-ping rounded-full bg-[var(--color-success)] opacity-75" />
              <span className="relative inline-flex size-2 rounded-full bg-[var(--color-success)]" />
            </span>
          </div>
          <div className="flex max-h-[380px] flex-col overflow-y-auto">
            {!auditLog || auditLog.length === 0 ? (
              <p className="py-3 text-sm text-[var(--color-sage-soft)]">No activity recorded yet.</p>
            ) : (
              auditLog.map((entry) => (
                <div key={entry.id} className="flex items-start gap-3 border-b border-white/10 py-3 text-sm font-normal last:border-b-0">
                  <Activity size={14} className="mt-0.5 shrink-0 text-[var(--color-sage-soft)]" />
                  <div className="min-w-0">
                    <p className="text-[var(--color-cream)]">{humanizeStatus(entry.action)}</p>
                    <p className="mt-0.5 text-xs text-[var(--color-sage-soft)]">
                      {entry.entityType} · {formatDateTime(entry.createdAt)}
                    </p>
                  </div>
                </div>
              ))
            )}
          </div>
        </Card>
      </div>
    </PageTransition>
  )
}

function SummaryCard({
  label, value, sub, icon: Icon, loading,
}: {
  label: string
  value: number | undefined
  sub: string
  icon: typeof Users
  loading: boolean
}) {
  return (
    <Card className="flex flex-col gap-2">
      <div className="flex items-center justify-between">
        <p className="text-[0.68rem] font-semibold uppercase tracking-[0.2em] text-[var(--color-sage)]">{label}</p>
        <Icon size={16} className="text-[var(--color-ink-faint)]" />
      </div>
      <p className="text-[2.2rem] font-black leading-none tracking-tight text-[var(--color-ink)]">
        {loading ? <Skeleton className="h-9 w-16" /> : formatNumber(value ?? 0)}
      </p>
      <p className="text-sm font-normal text-[var(--color-ink-soft)]">{sub}</p>
    </Card>
  )
}
