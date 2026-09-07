import { Package, Handshake, DollarSign } from 'lucide-react'
import { StatCard } from '@/components/dashboard/StatCard'
import { Card, Badge, PageTransition, Spinner } from '@/components/ui'
import { formatUsd } from '@/lib/utils'
import { useGetDashboardQuery, useGetMyProfileQuery } from '@/features/seller/sellerApi'

export function SellerDashboard() {
  const { data: stats, isLoading } = useGetDashboardQuery()
  const { data: profile } = useGetMyProfileQuery()

  return (
    <PageTransition>
      <h1 className="mb-6 text-2xl font-semibold">Welcome back</h1>

      {isLoading || !stats ? (
        <div className="flex justify-center py-10"><Spinner /></div>
      ) : (
        <div className="grid gap-4 sm:grid-cols-3">
          <StatCard label="Pieces fulfilled" value={stats.totalPcsFulfilled.toLocaleString()} icon={Package} tone="info" />
          <StatCard label="Orders fulfilled" value={String(stats.totalOrdersFulfilled)} icon={Handshake} tone="success" />
          <StatCard label="Total paid out" value={formatUsd(stats.totalPaidUsd)} icon={DollarSign} tone="accent" />
        </div>
      )}

      <Card className="mt-6">
        <div className="mb-4 flex items-center justify-between">
          <h2 className="font-semibold">Your public identity</h2>
          {profile && (
            <Badge tone="neutral">{profile.sellerCode ?? '—'} · {profile.tier}</Badge>
          )}
        </div>
        <p className="text-sm text-[var(--color-ink-soft)]">
          Buyers only ever see your seller code and tier — never your company name.
        </p>
      </Card>
    </PageTransition>
  )
}
