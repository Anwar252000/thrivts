import { useState } from 'react'
import { Download } from 'lucide-react'
import { PageTransition, Badge, Button } from '@/components/ui'
import { DataTable, SearchField, Pagination, type Column } from '@/components/admin'
import { useGetBuyersQuery } from '@/features/admin/adminApi'
import type { BuyerListItem } from '@/features/admin/adminTypes'
import { statusTone } from '@/features/admin/statusTone'
import { formatDate, formatUsd, exportToCsv } from '@/lib/utils'
import { BuyerDetailModal } from './BuyerDetailModal'

export function AdminBuyers() {
  const [page, setPage] = useState(1)
  const [search, setSearch] = useState('')
  const [selected, setSelected] = useState<string | null>(null)
  const { data, isLoading } = useGetBuyersQuery({ page, pageSize: 25 })

  const rows = (data?.items ?? []).filter(
    (b) => !search || b.companyName.toLowerCase().includes(search.toLowerCase()) || b.email.toLowerCase().includes(search.toLowerCase()),
  )

  const columns: Column<BuyerListItem>[] = [
    { header: 'Company', accessor: (b) => <span className="font-semibold">{b.companyName}</span> },
    { header: 'Email', accessor: (b) => b.email },
    { header: 'Location', accessor: (b) => [b.city, b.country].filter(Boolean).join(', ') },
    { header: 'Status', accessor: (b) => <Badge tone={statusTone(b.approvalStatus)}>{b.approvalStatus}</Badge> },
    { header: 'Orders', accessor: (b) => b.totalOrders, numeric: true },
    { header: 'Total spend', accessor: (b) => formatUsd(b.totalSpendUsd), numeric: true },
    { header: 'Joined', accessor: (b) => formatDate(b.createdAt) },
  ]

  return (
    <PageTransition>
      <h1 className="mb-1 text-2xl font-semibold">Buyers</h1>
      <p className="mb-6 text-sm text-[var(--color-ink-faint)]">{data?.totalCount ?? 0} registered buyers.</p>

      <div className="mb-4 flex flex-wrap items-center gap-3">
        <SearchField placeholder="Search buyers…" value={search} onChange={(e) => setSearch(e.target.value)} />
        <Button size="sm" variant="outline" className="ml-auto" onClick={() => exportToCsv('buyers', rows)}>
          <Download size={14} /> Export CSV
        </Button>
      </div>

      <DataTable
        columns={columns}
        rows={rows}
        keyFor={(b) => b.id}
        onRowClick={(b) => setSelected(b.id)}
        loading={isLoading}
        emptyTitle="No buyers found"
      />

      {data && data.totalCount > data.pageSize && (
        <Pagination page={page} pageSize={data.pageSize} total={data.totalCount} onChange={setPage} />
      )}

      <BuyerDetailModal buyerId={selected} onClose={() => setSelected(null)} />
    </PageTransition>
  )
}
