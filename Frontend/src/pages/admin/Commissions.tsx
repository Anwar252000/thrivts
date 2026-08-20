import { useState } from 'react'
import { Download } from 'lucide-react'
import { PageTransition, Badge, Button, Card } from '@/components/ui'
import { DataTable, Tabs, type Column } from '@/components/admin'
import { useGetCommissionsQuery, useGetFeeRevenueQuery } from '@/features/admin/adminApi'
import type { CommissionListItem, CommissionStatus } from '@/features/admin/adminTypes'
import { statusTone } from '@/features/admin/statusTone'
import { formatDate, formatUsd, formatNumber, exportToCsv } from '@/lib/utils'

type TabValue = 'all' | 'Pending' | 'ReadyToRelease' | 'Released'

export function AdminCommissions() {
  const [tab, setTab] = useState<TabValue>('all')
  const { data: commissions, isLoading } = useGetCommissionsQuery(tab === 'all' ? undefined : { status: tab as CommissionStatus })
  const { data: revenue } = useGetFeeRevenueQuery()

  const columns: Column<CommissionListItem>[] = [
    { header: 'Deal', accessor: (c) => c.dealId.slice(0, 8) },
    { header: 'Agency', accessor: (c) => c.agencyId.slice(0, 8) },
    { header: 'Rate', accessor: (c) => `${c.commissionRate}%` },
    { header: 'Amount', accessor: (c) => formatUsd(c.commissionAmountUsd), numeric: true },
    { header: 'Status', accessor: (c) => <Badge tone={statusTone(c.status)}>{c.status}</Badge> },
    { header: 'Release due', accessor: (c) => formatDate(c.releaseDueAt) },
    { header: 'Released', accessor: (c) => formatDate(c.releasedAt) },
  ]

  return (
    <PageTransition>
      <h1 className="mb-1 text-2xl font-semibold">Commissions</h1>
      <p className="mb-6 text-sm text-[var(--color-ink-faint)]">Agency commission accrual and release lifecycle.</p>

      {revenue && revenue.length > 0 && (
        <div className="mb-6 grid gap-3 sm:grid-cols-3">
          {revenue.slice(0, 3).map((m) => (
            <Card key={`${m.year}-${m.month}`} className="flex flex-col gap-1">
              <p className="text-[0.68rem] font-semibold uppercase tracking-[0.2em] text-[var(--color-sage)]">
                {new Date(m.year, m.month - 1).toLocaleString('en-US', { month: 'long', year: 'numeric' })}
              </p>
              <p className="text-xl font-bold">{formatUsd(m.feeRevenueUsd)}</p>
              <p className="text-xs text-[var(--color-ink-faint)]">{formatNumber(m.pcs)} pcs · {m.allocations} allocations</p>
            </Card>
          ))}
        </div>
      )}

      <div className="mb-4 flex flex-wrap items-center gap-3">
        <Tabs
          value={tab}
          onChange={setTab}
          options={[
            { value: 'all', label: 'All' },
            { value: 'Pending', label: 'Pending' },
            { value: 'ReadyToRelease', label: 'Ready to release' },
            { value: 'Released', label: 'Released' },
          ]}
        />
        <Button size="sm" variant="outline" className="ml-auto" onClick={() => exportToCsv('commissions', commissions ?? [])}>
          <Download size={14} /> Export CSV
        </Button>
      </div>

      <DataTable columns={columns} rows={commissions ?? []} keyFor={(c) => c.id} loading={isLoading} emptyTitle="No commissions found" />
    </PageTransition>
  )
}
