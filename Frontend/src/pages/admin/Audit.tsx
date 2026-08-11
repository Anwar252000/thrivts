import { PageTransition, Badge } from '@/components/ui'
import { DataTable, type Column } from '@/components/admin'
import { useGetAuditLogQuery } from '@/features/admin/adminApi'
import type { AuditLogEntry } from '@/features/admin/adminTypes'
import { humanizeStatus } from '@/features/admin/statusTone'
import { formatDateTime } from '@/lib/utils'

export function AdminAudit() {
  const { data: entries, isLoading } = useGetAuditLogQuery({ take: 200 })

  const columns: Column<AuditLogEntry>[] = [
    { header: 'Action', accessor: (e) => <span className="font-semibold">{humanizeStatus(e.action)}</span> },
    { header: 'Entity', accessor: (e) => e.entityType },
    { header: 'Entity id', accessor: (e) => (e.entityId ? e.entityId.slice(0, 8) : '—') },
    { header: 'Actor role', accessor: (e) => (e.actorRole ? <Badge tone="neutral">{e.actorRole}</Badge> : '—') },
    { header: 'When', accessor: (e) => formatDateTime(e.createdAt) },
  ]

  return (
    <PageTransition>
      <h1 className="mb-1 text-2xl font-semibold">Audit log</h1>
      <p className="mb-6 text-sm text-[var(--color-ink-faint)]">Every admin action, most recent first.</p>

      <DataTable columns={columns} rows={entries ?? []} keyFor={(e) => e.id} loading={isLoading} emptyTitle="No audit entries yet" />
    </PageTransition>
  )
}
