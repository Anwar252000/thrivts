import { useState } from 'react'
import { PageTransition, Badge, Select } from '@/components/ui'
import { DataTable, SearchField, type Column } from '@/components/admin'
import { statusTone, humanizeStatus } from '@/features/admin/statusTone'
import { formatDate, formatUsd } from '@/lib/utils'
import { useGetMyDealsQuery } from '@/features/buyer/buyerApi'
import type { MyDealListItem, DealStatus } from '@/features/buyer/buyerTypes'
import { DealDetailModal } from './DealDetailModal'

const STATUS_OPTIONS: DealStatus[] = ['Confirmed', 'AwaitingPayment', 'Paid', 'InFulfillment', 'Dispatched', 'Delivered', 'Settled']

export function BuyerDeals() {
  const [search, setSearch] = useState('')
  const [status, setStatus] = useState<DealStatus | ''>('')
  const [selected, setSelected] = useState<string | null>(null)
  const { data, isLoading } = useGetMyDealsQuery()

  const rows = (data ?? [])
    .filter((d) => !status || d.status === status)
    .filter((d) => !search || d.dealNumber.toLowerCase().includes(search.toLowerCase()))

  const columns: Column<MyDealListItem>[] = [
    { header: 'Deal #', accessor: (d) => <span className="font-mono font-semibold">{d.dealNumber}</span> },
    { header: 'Requirement', accessor: (d) => <div><p className="font-medium">{d.itemName}</p><p className="text-xs text-[var(--color-ink-faint)]">{d.requirementNumber}</p></div> },
    { header: 'Qty', accessor: (d) => d.totalQuantityPcs.toLocaleString(), numeric: true },
    { header: 'Total', accessor: (d) => formatUsd(d.totalInvoiceUsd), numeric: true },
    {
      header: 'Status',
      accessor: (d) => (
        <div className="flex gap-1.5">
          <Badge tone={statusTone(d.status)}>{humanizeStatus(d.status)}</Badge>
          {d.hasDispute && <Badge tone="danger">Disputed</Badge>}
        </div>
      ),
    },
    { header: 'Confirmed', accessor: (d) => formatDate(d.createdAt) },
  ]

  return (
    <PageTransition>
      <h1 className="mb-1 text-2xl font-semibold">My deals</h1>
      <p className="mb-6 text-sm text-[var(--color-ink-faint)]">Confirmed orders moving through fulfilment.</p>

      <div className="mb-4 flex flex-wrap gap-3">
        <SearchField placeholder="Search deals…" value={search} onChange={(e) => setSearch(e.target.value)} />
        <Select className="w-52" value={status} onChange={(e) => setStatus(e.target.value as DealStatus | '')}>
          <option value="">All statuses</option>
          {STATUS_OPTIONS.map((s) => (
            <option key={s} value={s}>{humanizeStatus(s)}</option>
          ))}
        </Select>
      </div>

      <DataTable columns={columns} rows={rows} keyFor={(d) => d.id} onRowClick={(d) => setSelected(d.id)} loading={isLoading} emptyTitle="No deals yet" />

      <DealDetailModal dealId={selected} onClose={() => setSelected(null)} />
    </PageTransition>
  )
}
