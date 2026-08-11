import { useState } from 'react'
import { PageTransition, Badge, Select } from '@/components/ui'
import { DataTable, SearchField, Pagination, type Column } from '@/components/admin'
import { useGetDealsQuery } from '@/features/admin/adminApi'
import type { DealListItem, DealStatus } from '@/features/admin/adminTypes'
import { statusTone } from '@/features/admin/statusTone'
import { formatDate, formatUsd } from '@/lib/utils'
import { DealDetailModal } from './DealDetailModal'

const STATUS_OPTIONS: DealStatus[] = ['Draft', 'Confirmed', 'AwaitingPayment', 'Paid', 'InFulfillment', 'Dispatched', 'Delivered', 'Settled', 'Cancelled', 'Disputed']

export function AdminDeals() {
  const [page, setPage] = useState(1)
  const [status, setStatus] = useState<DealStatus | ''>('')
  const [search, setSearch] = useState('')
  const [selected, setSelected] = useState<string | null>(null)
  const { data, isLoading } = useGetDealsQuery({ status: status || undefined, page, pageSize: 25 })

  const rows = (data?.items ?? []).filter((d) => !search || d.dealNumber.toLowerCase().includes(search.toLowerCase()))

  const columns: Column<DealListItem>[] = [
    { header: 'Deal', accessor: (d) => <span className="font-semibold">{d.dealNumber}</span> },
    { header: 'Qty (pcs)', accessor: (d) => d.totalQuantityPcs, numeric: true },
    { header: 'Invoice', accessor: (d) => formatUsd(d.totalInvoiceUsd), numeric: true },
    { header: 'Spread (fee)', accessor: (d) => formatUsd(d.totalSpreadUsd), numeric: true },
    {
      header: 'Status',
      accessor: (d) => (
        <div className="flex gap-1.5">
          <Badge tone={statusTone(d.status)}>{d.status}</Badge>
          {d.hasDispute && <Badge tone="danger">Disputed</Badge>}
        </div>
      ),
    },
    { header: 'Created', accessor: (d) => formatDate(d.createdAt) },
  ]

  return (
    <PageTransition>
      <h1 className="mb-1 text-2xl font-semibold">Deals</h1>
      <p className="mb-6 text-sm text-[var(--color-ink-faint)]">{data?.totalCount ?? 0} deals.</p>

      <div className="mb-4 flex flex-wrap gap-3">
        <SearchField placeholder="Search by deal number…" value={search} onChange={(e) => setSearch(e.target.value)} />
        <Select className="w-52" value={status} onChange={(e) => { setStatus(e.target.value as DealStatus | ''); setPage(1) }}>
          <option value="">All statuses</option>
          {STATUS_OPTIONS.map((s) => (
            <option key={s} value={s}>{s}</option>
          ))}
        </Select>
      </div>

      <DataTable columns={columns} rows={rows} keyFor={(d) => d.id} onRowClick={(d) => setSelected(d.id)} loading={isLoading} emptyTitle="No deals found" />

      {data && data.totalCount > data.pageSize && (
        <Pagination page={page} pageSize={data.pageSize} total={data.totalCount} onChange={setPage} />
      )}

      <DealDetailModal dealId={selected} onClose={() => setSelected(null)} />
    </PageTransition>
  )
}
