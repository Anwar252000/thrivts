import { useState } from 'react'
import { Plus } from 'lucide-react'
import { useNavigate } from 'react-router-dom'
import { PageTransition, Badge, Button, Select } from '@/components/ui'
import { DataTable, SearchField, type Column } from '@/components/admin'
import { statusTone, humanizeStatus } from '@/features/admin/statusTone'
import { formatDate } from '@/lib/utils'
import { useGetMyRequirementsQuery } from '@/features/buyer/buyerApi'
import type { MyRequirementListItem, RequirementStatus } from '@/features/buyer/buyerTypes'
import { RequirementDetailModal } from './RequirementDetailModal'

const STATUS_OPTIONS: RequirementStatus[] = ['PendingReview', 'Posted', 'Matching', 'ReadyToOrder', 'Confirmed', 'Settled', 'Cancelled']

export function BuyerRequirements() {
  const navigate = useNavigate()
  const [search, setSearch] = useState('')
  const [status, setStatus] = useState<RequirementStatus | ''>('')
  const [selected, setSelected] = useState<string | null>(null)
  const { data, isLoading } = useGetMyRequirementsQuery()

  const rows = (data ?? [])
    .filter((r) => !status || r.status === status)
    .filter((r) => !search || r.itemName.toLowerCase().includes(search.toLowerCase()) || r.requirementNumber.toLowerCase().includes(search.toLowerCase()))

  const columns: Column<MyRequirementListItem>[] = [
    { header: 'Req #', accessor: (r) => <span className="font-mono font-semibold">{r.requirementNumber}</span> },
    { header: 'Item', accessor: (r) => <div><p className="font-medium">{r.itemName}</p><p className="text-xs text-[var(--color-ink-faint)]">{r.categoryName ?? '—'}</p></div> },
    { header: 'Qty', accessor: (r) => r.quantityPcs.toLocaleString(), numeric: true },
    { header: 'Grade', accessor: (r) => (r.grade === 'AB' ? 'A/B' : r.grade) },
    { header: 'Target', accessor: (r) => `${r.targetPricePerPc.toFixed(2)} ${r.currency}`, numeric: true },
    { header: 'Destination', accessor: (r) => r.destinationCountry },
    { header: 'Status', accessor: (r) => <Badge tone={statusTone(r.status)}>{humanizeStatus(r.status)}</Badge> },
    { header: 'Posted', accessor: (r) => formatDate(r.createdAt) },
  ]

  return (
    <PageTransition>
      <div className="mb-6 flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-semibold">My requirements</h1>
          <p className="mt-1 text-sm text-[var(--color-ink-faint)]">Everything you've posted, with current status.</p>
        </div>
        <Button size="sm" onClick={() => navigate('/buyer/post-requirement')}>
          <Plus size={14} /> Post requirement
        </Button>
      </div>

      <div className="mb-4 flex flex-wrap gap-3">
        <SearchField placeholder="Search requirements…" value={search} onChange={(e) => setSearch(e.target.value)} />
        <Select className="w-52" value={status} onChange={(e) => setStatus(e.target.value as RequirementStatus | '')}>
          <option value="">All statuses</option>
          {STATUS_OPTIONS.map((s) => (
            <option key={s} value={s}>{humanizeStatus(s)}</option>
          ))}
        </Select>
      </div>

      <DataTable columns={columns} rows={rows} keyFor={(r) => r.id} onRowClick={(r) => setSelected(r.id)} loading={isLoading} emptyTitle="No requirements found" />

      <RequirementDetailModal requirementId={selected} onClose={() => setSelected(null)} />
    </PageTransition>
  )
}
