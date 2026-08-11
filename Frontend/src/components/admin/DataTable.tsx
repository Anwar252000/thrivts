import type { ReactNode } from 'react'
import { Skeleton } from '@/components/ui'
import { EmptyState } from './EmptyState'
import { cn } from '@/lib/utils'

export interface Column<T> {
  header: string
  accessor: (row: T) => ReactNode
  className?: string
  numeric?: boolean
}

interface DataTableProps<T> {
  columns: Column<T>[]
  rows: T[]
  keyFor: (row: T) => string
  onRowClick?: (row: T) => void
  loading?: boolean
  emptyTitle?: string
  emptyDescription?: string
}

/** Matches admin.html's .table-wrap/.table exactly — sage-mist header, hover rows, tabular nums. */
export function DataTable<T>({
  columns, rows, keyFor, onRowClick, loading, emptyTitle = 'Nothing here yet', emptyDescription,
}: DataTableProps<T>) {
  if (!loading && rows.length === 0) {
    return <EmptyState title={emptyTitle} description={emptyDescription} />
  }

  return (
    <div className="overflow-x-auto rounded-[var(--radius-md)] border border-[var(--color-line)] bg-[var(--color-white)]">
      <table className="w-full border-collapse text-sm">
        <thead>
          <tr>
            {columns.map((col) => (
              <th
                key={col.header}
                className={cn(
                  'whitespace-nowrap border-b border-[var(--color-line)] bg-[var(--color-sage-mist)] px-4 py-3 text-left text-[0.68rem] font-bold uppercase tracking-[0.15em] text-[var(--color-sage-dark)]',
                  col.numeric && 'text-right',
                  col.className,
                )}
              >
                {col.header}
              </th>
            ))}
          </tr>
        </thead>
        <tbody>
          {loading
            ? Array.from({ length: 5 }).map((_, i) => (
                <tr key={i}>
                  {columns.map((_col, j) => (
                    <td key={j} className="border-b border-[var(--color-line-soft)] px-4 py-4">
                      <Skeleton className="h-4 w-24" />
                    </td>
                  ))}
                </tr>
              ))
            : rows.map((row) => (
                <tr
                  key={keyFor(row)}
                  onClick={() => onRowClick?.(row)}
                  className={cn(
                    'last:[&>td]:border-b-0',
                    onRowClick && 'cursor-pointer transition-colors hover:bg-[var(--color-sage-mist)]',
                  )}
                >
                  {columns.map((col) => (
                    <td
                      key={col.header}
                      className={cn(
                        'border-b border-[var(--color-line-soft)] px-4 py-4 align-middle text-[var(--color-ink)]',
                        col.numeric && 'text-right font-semibold tabular-nums',
                        col.className,
                      )}
                    >
                      {col.accessor(row)}
                    </td>
                  ))}
                </tr>
              ))}
        </tbody>
      </table>
    </div>
  )
}
