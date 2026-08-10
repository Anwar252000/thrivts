import { cn } from '@/lib/utils'

export function Spinner({ className }: { className?: string }) {
  return (
    <span
      role="status"
      aria-label="Loading"
      className={cn(
        'inline-block size-5 animate-spin rounded-full border-2 border-[var(--color-line-strong)] border-t-[var(--color-accent)]',
        className,
      )}
    />
  )
}
