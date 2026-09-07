import { useState } from 'react'
import { PageTransition, Badge } from '@/components/ui'
import { DataTable, SearchField, type Column } from '@/components/admin'
import { statusTone, humanizeStatus } from '@/features/admin/statusTone'
import { formatDate } from '@/lib/utils'
import { useGetOpenRequirementsQuery } from '@/features/seller/sellerApi'
import type { OpenRequirement } from '@/features/seller/sellerTypes'
import { QuoteModal } from './QuoteModal'

export function SellerRequirements() {
  const [search, setSearch] = useState('')
  const [selected, setSelected] = useState<OpenRequirement | null>(null)
  const { data, isLoading } = useGetOpenRequirementsQuery()

  const rows = (data ?? []).filter(
    (r) => !search || r.itemName.toLowerCase().includes(search.toLowerCase()) || r.requirementNumber.toLowerCase().includes(search.toLowerCase()),
  )

  const columns: Column<OpenRequirement>[] = [
    { header: 'Req #', accessor: (r) => <span className="font-mono font-semibold">{r.requirementNumber}</span> },
    { header: 'Item', accessor: (r) => r.itemName },
    { header: 'Qty needed', accessor: (r) => `${r.quantityPcs.toLocaleString()} pcs`, numeric: true },
    { header: 'Grade', accessor: (r) => (r.grade === 'AB' ? 'A/B' : r.grade) },
    { header: 'Destination', accessor: (r) => r.destinationCountry },
    { header: 'Shipping', accessor: (r) => (r.shippingMode ? r.shippingMode.replace('_', ' ') : '—') },
    { header: 'Timeline', accessor: (r) => (r.deliveryTimelineDays ? `Within ${r.deliveryTimelineDays}d` : '—') },
    { header: 'Status', accessor: (r) => <Badge tone={statusTone(r.status)}>{humanizeStatus(r.status)}</Badge> },
    { header: 'Posted', accessor: (r) => formatDate(r.postedAt) },
    {
      header: '',
      accessor: (r) => (r.hasQuoted ? <Badge tone="accent">Quoted</Badge> : <Badge tone="neutral">Not quoted</Badge>),
    },
  ]

  return (
    <PageTransition>
      <div className="mb-6">
        <h1 className="text-2xl font-semibold">Open requirements</h1>
        <p className="mt-1 text-sm text-[var(--color-ink-faint)]">
          Matched to your tier and tags. Click a row to submit or update your quote.
        </p>
      </div>

      <div className="mb-4">
        <SearchField placeholder="Search requirements…" value={search} onChange={(e) => setSearch(e.target.value)} />
      </div>

      <DataTable columns={columns} rows={rows} keyFor={(r) => r.id} onRowClick={setSelected} loading={isLoading} emptyTitle="No open requirements match your tags right now" />

      <QuoteModal requirement={selected} onClose={() => setSelected(null)} />
    </PageTransition>
  )
}
