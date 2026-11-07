interface PaginationProps {
  page: number
  pageSize: number
  total: number
  onChange: (page: number) => void
}

export function Pagination({ page, pageSize, total, onChange }: PaginationProps) {
  const pageCount = Math.max(1, Math.ceil(total / pageSize))
  return (
    <div className="mt-4 flex items-center justify-between text-sm text-[var(--color-ink-faint)]">
      <span>
        Page {page} of {pageCount}
      </span>
      <div className="flex gap-2">
        <button
          onClick={() => onChange(Math.max(1, page - 1))}
          disabled={page <= 1}
          className="rounded-[var(--radius-sm)] border border-[var(--color-line-strong)] px-3 py-1.5 transition-colors hover:bg-[var(--color-sage-mist)] disabled:pointer-events-none disabled:opacity-40"
        >
          Previous
        </button>
        <button
          onClick={() => onChange(Math.min(pageCount, page + 1))}
          disabled={page >= pageCount}
          className="rounded-[var(--radius-sm)] border border-[var(--color-line-strong)] px-3 py-1.5 transition-colors hover:bg-[var(--color-sage-mist)] disabled:pointer-events-none disabled:opacity-40"
        >
          Next
        </button>
      </div>
    </div>
  )
}
