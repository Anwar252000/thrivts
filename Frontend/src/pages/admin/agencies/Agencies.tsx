import { useState } from 'react'
import { Plus, Download } from 'lucide-react'
import { PageTransition, Badge, Button } from '@/components/ui'
import { DataTable, SearchField, type Column } from '@/components/admin'
import { useGetAgenciesQuery } from '@/features/admin/adminApi'
import type { AgencyListItem } from '@/features/admin/adminTypes'
import { formatUsd, exportToCsv } from '@/lib/utils'
import { AgencyDetailModal } from './AgencyDetailModal'
import { CreateAgencyModal } from './CreateAgencyModal'

export function AdminAgencies() {
  const [search, setSearch] = useState('')
  const [selected, setSelected] = useState<string | null>(null)
  const [creating, setCreating] = useState(false)
  const { data, isLoading } = useGetAgenciesQuery()

  const rows = (data ?? []).filter(
    (a) => !search || a.agencyName.toLowerCase().includes(search.toLowerCase()) || a.ownerFullName.toLowerCase().includes(search.toLowerCase()),
  )

  const columns: Column<AgencyListItem>[] = [
    { header: 'Agency', accessor: (a) => <span className="font-semibold">{a.agencyName}</span> },
    { header: 'Owner', accessor: (a) => a.ownerFullName },
    { header: 'Country', accessor: (a) => a.country },
    { header: 'Commission', accessor: (a) => `${a.commissionRate}%` },
    { header: 'Status', accessor: (a) => <Badge tone={a.isActive ? 'success' : 'danger'}>{a.isActive ? 'Active' : 'Blocked'}</Badge> },
    { header: 'Buyers', accessor: (a) => a.totalBuyersReferred, numeric: true },
    { header: 'Deals', accessor: (a) => a.totalDealsClosed, numeric: true },
    { header: 'Commission pending', accessor: (a) => formatUsd(a.totalCommissionPendingUsd), numeric: true },
  ]

  return (
    <PageTransition>
      <div className="mb-6 flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-semibold">Agencies</h1>
          <p className="mt-1 text-sm text-[var(--color-ink-faint)]">{data?.length ?? 0} partner agencies.</p>
        </div>
        <Button size="sm" onClick={() => setCreating(true)}>
          <Plus size={14} /> Add agency
        </Button>
      </div>

      <div className="mb-4 flex flex-wrap items-center gap-3">
        <SearchField placeholder="Search agencies…" value={search} onChange={(e) => setSearch(e.target.value)} />
        <Button size="sm" variant="outline" className="ml-auto" onClick={() => exportToCsv('agencies', rows)}>
          <Download size={14} /> Export CSV
        </Button>
      </div>

      <DataTable
        columns={columns}
        rows={rows}
        keyFor={(a) => a.id}
        onRowClick={(a) => setSelected(a.id)}
        loading={isLoading}
        emptyTitle="No agencies found"
      />

      <AgencyDetailModal agencyId={selected} onClose={() => setSelected(null)} />
      <CreateAgencyModal open={creating} onClose={() => setCreating(false)} />
    </PageTransition>
  )
}
