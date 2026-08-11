import { useState } from 'react'
import { PageTransition, Badge } from '@/components/ui'
import { DataTable, Tabs, type Column } from '@/components/admin'
import { useGetDisputesQuery } from '@/features/admin/adminApi'
import type { DisputeListItem, DisputeStatus } from '@/features/admin/adminTypes'
import { statusTone } from '@/features/admin/statusTone'
import { formatDate, formatUsd } from '@/lib/utils'
import { DisputeDetailModal } from './DisputeDetailModal'

type TabValue = 'Open' | 'Investigating' | 'Resolved'

export function AdminDisputes() {
  const [tab, setTab] = useState<TabValue>('Open')
  const [selected, setSelected] = useState<DisputeListItem | null>(null)
  const { data: disputes, isLoading } = useGetDisputesQuery({ status: tab as DisputeStatus })

  const columns: Column<DisputeListItem>[] = [
    { header: 'Dispute', accessor: (d) => <span className="font-semibold">{d.disputeNumber}</span> },
    { header: 'Category', accessor: (d) => d.category ?? '—' },
    { header: 'Description', accessor: (d) => <span className="line-clamp-1 max-w-xs">{d.description}</span> },
    { header: 'Refund requested', accessor: (d) => formatUsd(d.refundAmountUsd), numeric: true },
    { header: 'Status', accessor: (d) => <Badge tone={statusTone(d.status)}>{d.status}</Badge> },
    { header: 'Raised', accessor: (d) => formatDate(d.createdAt) },
  ]

  return (
    <PageTransition>
      <h1 className="mb-1 text-2xl font-semibold">Disputes</h1>
      <p className="mb-6 text-sm text-[var(--color-ink-faint)]">Deal disputes raised by buyers or sellers.</p>

      <div className="mb-4">
        <Tabs
          value={tab}
          onChange={setTab}
          options={[
            { value: 'Open', label: 'Open' },
            { value: 'Investigating', label: 'Under review' },
            { value: 'Resolved', label: 'Resolved' },
          ]}
        />
      </div>

      <DataTable columns={columns} rows={disputes ?? []} keyFor={(d) => d.id} onRowClick={setSelected} loading={isLoading} emptyTitle="No disputes in this view" />

      <DisputeDetailModal dispute={selected} onClose={() => setSelected(null)} />
    </PageTransition>
  )
}
