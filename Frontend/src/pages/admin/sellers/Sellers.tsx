import { useState } from 'react'
import { PageTransition, Badge } from '@/components/ui'
import { DataTable, SearchField, Pagination, type Column } from '@/components/admin'
import { useGetSellersQuery } from '@/features/admin/adminApi'
import type { SellerListItem } from '@/features/admin/adminTypes'
import { statusTone } from '@/features/admin/statusTone'
import { formatDate, formatUsd } from '@/lib/utils'
import { SellerDetailModal } from './SellerDetailModal'

export function AdminSellers() {
  const [page, setPage] = useState(1)
  const [search, setSearch] = useState('')
  const [selected, setSelected] = useState<string | null>(null)
  const { data, isLoading } = useGetSellersQuery({ page, pageSize: 25 })

  const rows = (data?.items ?? []).filter(
    (s) =>
      !search ||
      s.publicAlias.toLowerCase().includes(search.toLowerCase()) ||
      s.companyName?.toLowerCase().includes(search.toLowerCase()) ||
      s.email.toLowerCase().includes(search.toLowerCase()),
  )

  const columns: Column<SellerListItem>[] = [
    { header: 'Seller', accessor: (s) => <span className="font-semibold">{s.companyName ?? s.publicAlias}</span> },
    { header: 'Email', accessor: (s) => s.email },
    { header: 'Location', accessor: (s) => `${s.locationCity}, ${s.locationCountry}` },
    { header: 'Tier', accessor: (s) => <Badge tone="accent">{s.tier}</Badge> },
    { header: 'KYC', accessor: (s) => <Badge tone={s.kycVerified ? 'success' : 'warning'}>{s.kycVerified ? 'Verified' : 'Pending'}</Badge> },
    { header: 'Status', accessor: (s) => <Badge tone={statusTone(s.approvalStatus)}>{s.approvalStatus}</Badge> },
    { header: 'Orders', accessor: (s) => s.totalOrdersFulfilled, numeric: true },
    { header: 'Total paid', accessor: (s) => formatUsd(s.totalPaidUsd), numeric: true },
    { header: 'Joined', accessor: (s) => formatDate(s.createdAt) },
  ]

  return (
    <PageTransition>
      <h1 className="mb-1 text-2xl font-semibold">Sellers</h1>
      <p className="mb-6 text-sm text-[var(--color-ink-faint)]">{data?.totalCount ?? 0} registered sellers.</p>

      <div className="mb-4">
        <SearchField placeholder="Search sellers…" value={search} onChange={(e) => setSearch(e.target.value)} />
      </div>

      <DataTable
        columns={columns}
        rows={rows}
        keyFor={(s) => s.id}
        onRowClick={(s) => setSelected(s.id)}
        loading={isLoading}
        emptyTitle="No sellers found"
      />

      {data && data.totalCount > data.pageSize && (
        <Pagination page={page} pageSize={data.pageSize} total={data.totalCount} onChange={setPage} />
      )}

      <SellerDetailModal sellerId={selected} onClose={() => setSelected(null)} />
    </PageTransition>
  )
}
