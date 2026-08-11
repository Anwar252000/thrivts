import type { LucideIcon } from 'lucide-react'
import { Inbox } from 'lucide-react'

interface EmptyStateProps {
  icon?: LucideIcon
  title: string
  description?: string
}

/** Matches admin.html's .empty-state — dashed border, centered icon + copy. */
export function EmptyState({ icon: Icon = Inbox, title, description }: EmptyStateProps) {
  return (
    <div className="rounded-[var(--radius-md)] border border-dashed border-[var(--color-line-strong)] bg-[var(--color-white)] px-5 py-14 text-center">
      <Icon size={28} className="mx-auto mb-3 text-[var(--color-ink-faint)]" />
      <p className="text-sm font-medium text-[var(--color-ink)]">{title}</p>
      {description && <p className="mt-1 text-sm text-[var(--color-ink-faint)]">{description}</p>}
    </div>
  )
}
