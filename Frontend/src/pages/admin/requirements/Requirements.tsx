import { useState } from 'react'
import { Download } from 'lucide-react'
import { PageTransition, Badge, Button, Select } from '@/components/ui'
import { DataTable, SearchField, Pagination, type Column } from '@/components/admin'
import { useGetRequirementsQuery } from '@/features/admin/adminApi'
import type { RequirementListItem, RequirementStatus } from '@/features/admin/adminTypes'
import { statusTone } from '@/features/admin/statusTone'
import { formatDate, exportToCsv } from '@/lib/utils'
import { RequirementDetailModal } from './RequirementDetailModal'

const STATUS_OPTIONS: RequirementStatus[] = [
  'PendingReview', 'Posted', 'Matching', 'ReadyToOrder', 'Confirmed', 'AwaitingPayment',
  'Paid', 'InFulfillment', 'SellersPaid', 'Dispatched', 'Delivered', 'Disputed', 'Settled', 'Cancelled', 'Expired', 'Stale',
]

export function AdminRequirements() {
  const [page, setPage] = useState(1)
  const [status, setStatus] = useState<RequirementStatus | ''>('')
  const [search, setSearch] = useState('')
  const [selected, setSelected] = useState<string | null>(null)
  const { data, isLoading } = useGetRequirementsQuery({ status: status || undefined, page, pageSize: 25 })

  const rows = (data?.items ?? []).filter(
    (r) => !search || r.itemName.toLowerCase().includes(search.toLowerCase()) || r.requirementNumber.toLowerCase().includes(search.toLowerCase()),
  )

  const columns: Column<RequirementListItem>[] = [
    { header: 'Requirement', accessor: (r) => <span className="font-semibold">{r.requirementNumber}</span> },
    { header: 'Item', accessor: (r) => r.itemName },
    { header: 'Qty (pcs)', accessor: (r) => r.quantityPcs, numeric: true },
    { header: 'Grade', accessor: (r) => (r.grade === 'AB' ? 'A/B' : r.grade) },
    { header: 'Destination', accessor: (r) => r.destinationCountry },
    { header: 'Status', accessor: (r) => <Badge tone={statusTone(r.status)}>{r.status}</Badge> },
    { header: 'Posted', accessor: (r) => formatDate(r.createdAt) },
  ]

  return (
    <PageTransition>
      <h1 className="mb-1 text-2xl font-semibold">Requirements</h1>
      <p className="mb-6 text-sm text-[var(--color-ink-faint)]">{data?.totalCount ?? 0} buyer requirements.</p>

      <div className="mb-4 flex flex-wrap gap-3">
        <SearchField placeholder="Search requirements…" value={search} onChange={(e) => setSearch(e.target.value)} />
        <Select className="w-52" value={status} onChange={(e) => { setStatus(e.target.value as RequirementStatus | ''); setPage(1) }}>
          <option value="">All statuses</option>
          {STATUS_OPTIONS.map((s) => (
            <option key={s} value={s}>{s}</option>
          ))}
        </Select>
        <Button size="sm" variant="outline" className="ml-auto" onClick={() => exportToCsv('requirements', rows)}>
          <Download size={14} /> Export CSV
        </Button>
      </div>

      <DataTable
        columns={columns}
        rows={rows}
        keyFor={(r) => r.id}
        onRowClick={(r) => setSelected(r.id)}
        loading={isLoading}
        emptyTitle="No requirements found"
      />

      {data && data.totalCount > data.pageSize && (
        <Pagination page={page} pageSize={data.pageSize} total={data.totalCount} onChange={setPage} />
      )}

      <RequirementDetailModal requirementId={selected} onClose={() => setSelected(null)} />
    </PageTransition>
  )
}
