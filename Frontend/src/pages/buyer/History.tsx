import { useState } from 'react'
import { Download, Archive } from 'lucide-react'
import { PageTransition, Button } from '@/components/ui'
import { DataTable, type Column } from '@/components/admin'
import { EmptyState } from '@/components/admin'
import { formatDate, formatUsd, exportToCsv } from '@/lib/utils'
import { useGetMyDealsQuery } from '@/features/buyer/buyerApi'
import type { MyDealListItem } from '@/features/buyer/buyerTypes'
import { DealDetailModal } from './deals/DealDetailModal'

export function BuyerHistory() {
  const { data, isLoading } = useGetMyDealsQuery()
  const [selected, setSelected] = useState<string | null>(null)
  const settled = (data ?? []).filter((d) => d.status === 'Settled')

  const columns: Column<MyDealListItem>[] = [
    { header: 'Deal #', accessor: (d) => <span className="font-mono font-semibold">{d.dealNumber}</span> },
    { header: 'Requirement', accessor: (d) => d.requirementNumber },
    { header: 'Item', accessor: (d) => d.itemName },
    { header: 'Qty', accessor: (d) => d.totalQuantityPcs.toLocaleString(), numeric: true },
    { header: 'Total', accessor: (d) => formatUsd(d.totalInvoiceUsd), numeric: true },
    { header: 'Settled', accessor: (d) => formatDate(d.createdAt) },
  ]

  return (
    <PageTransition>
      <div className="mb-6 flex items-end justify-between">
        <div>
          <h1 className="text-2xl font-semibold">Order history</h1>
          <p className="mt-1 text-sm text-[var(--color-ink-faint)]">Complete ledger of settled orders.</p>
        </div>
        <Button size="sm" variant="outline" onClick={() => exportToCsv('order_history', settled)}>
          <Download size={14} /> Export CSV
        </Button>
      </div>

      {!isLoading && settled.length === 0 ? (
        <EmptyState icon={Archive} title="No settled orders yet" description="Your completed orders will appear here." />
      ) : (
        <DataTable columns={columns} rows={settled} keyFor={(d) => d.id} onRowClick={(d) => setSelected(d.id)} loading={isLoading} emptyTitle="No settled orders yet" />
      )}

      <DealDetailModal dealId={selected} onClose={() => setSelected(null)} />
    </PageTransition>
  )
}
