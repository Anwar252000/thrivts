import { useNavigate } from 'react-router-dom'
import { ClipboardList, Briefcase, DollarSign, Plus } from 'lucide-react'
import { StatCard } from '@/components/dashboard/StatCard'
import { Card, Badge, Button, PageTransition, Spinner } from '@/components/ui'
import { statusTone, humanizeStatus } from '@/features/admin/statusTone'
import { formatUsd, formatDate } from '@/lib/utils'
import { useGetDashboardQuery } from '@/features/buyer/buyerApi'

export function BuyerDashboard() {
  const navigate = useNavigate()
  const { data, isLoading } = useGetDashboardQuery()

  return (
    <PageTransition>
      <h1 className="mb-1 text-2xl font-semibold">Dashboard</h1>
      <p className="mb-6 text-sm text-[var(--color-ink-faint)]">Your platform overview.</p>

      <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
        <StatCard label={`${data?.pendingReviewCount ?? 0} awaiting review`} value={String(data?.liveRequirementsCount ?? '—')} icon={ClipboardList} tone="info" />
        <StatCard label={`${data?.inTransitCount ?? 0} in transit`} value={String(data?.openDealsCount ?? '—')} icon={Briefcase} tone="accent" />
        <StatCard label={`${data?.settledCount ?? 0} orders settled`} value={data ? formatUsd(data.totalSpentUsd) : '—'} icon={DollarSign} tone="success" />
        <Card className="flex flex-col justify-center gap-2 bg-[var(--color-sage-darker)] text-white">
          <p className="text-xs font-semibold uppercase tracking-wider text-[var(--color-sidebar-text-muted)]">Quick action</p>
          <Button size="sm" onClick={() => navigate('/buyer/post-requirement')}>
            <Plus size={14} /> Post new requirement
          </Button>
        </Card>
      </div>

      <Card className="mt-6">
        <div className="mb-4 flex items-center justify-between">
          <h2 className="font-semibold">Your recent activity</h2>
          <button onClick={() => navigate('/buyer/requirements')} className="text-sm font-medium text-[var(--color-accent)] hover:underline">
            View all →
          </button>
        </div>

        {isLoading ? (
          <div className="flex justify-center py-8">
            <Spinner />
          </div>
        ) : !data || data.recentRequirements.length === 0 ? (
          <div className="py-8 text-center">
            <p className="mb-3 text-sm text-[var(--color-ink-faint)]">No requirements yet. Post your first requirement to get started.</p>
            <Button size="sm" onClick={() => navigate('/buyer/post-requirement')}>
              <Plus size={14} /> Post requirement
            </Button>
          </div>
        ) : (
          <div className="flex flex-col gap-2">
            {data.recentRequirements.map((r) => (
              <button
                key={r.id}
                onClick={() => navigate('/buyer/requirements')}
                className="flex items-center justify-between gap-3 rounded-[var(--radius-sm)] border border-[var(--color-line)] px-4 py-3 text-left transition-colors hover:bg-[var(--color-sage-mist)]"
              >
                <div className="min-w-0">
                  <p className="truncate text-sm font-semibold">{r.requirementNumber} — {r.itemName}</p>
                  <p className="truncate text-xs text-[var(--color-ink-faint)]">
                    {r.quantityPcs.toLocaleString()} pcs · {r.grade === 'AB' ? 'A/B' : r.grade} → {r.destinationCountry} · {formatDate(r.createdAt)}
                  </p>
                </div>
                <Badge tone={statusTone(r.status)}>{humanizeStatus(r.status)}</Badge>
              </button>
            ))}
          </div>
        )}
      </Card>
    </PageTransition>
  )
}
