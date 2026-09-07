import { formatDate } from '@/lib/utils'
import { useGetPublicActivityQuery } from '@/features/buyer/buyerApi'

/** Shared by both marketing landing screens — the same anonymized public.get_public_activity feed
 * buyer.html and seller.html both rendered on their own landing pages. */
export function LiveActivityFeed({ emptyLabel }: { emptyLabel: string }) {
  const { data } = useGetPublicActivityQuery(12)

  if (!data || data.length === 0) {
    return (
      <div className="rounded-[var(--radius-lg)] border border-dashed border-[var(--color-line-strong)] bg-[repeating-linear-gradient(135deg,transparent,transparent_10px,var(--color-sage-mist)_10px,var(--color-sage-mist)_11px)] p-10 text-center text-sm text-[var(--color-ink-faint)]">
        {emptyLabel}
      </div>
    )
  }

  return (
    <div className="flex flex-col gap-2.5">
      {data.map((item, i) => (
        <div
          key={i}
          className="flex flex-wrap items-center justify-between gap-2 rounded-[var(--radius-md)] border border-[var(--color-line)] bg-[var(--color-white)] px-5 py-3.5 text-sm"
        >
          <span>
            <span className="font-semibold">{item.itemName}</span>
            <span className="text-[var(--color-ink-faint)]"> · {item.quantityPcs.toLocaleString()} pcs · Grade {item.grade} · {item.destinationCountry}</span>
          </span>
          <span className="text-xs uppercase tracking-wider text-[var(--color-ink-faint)]">{item.activityType} · {formatDate(item.activityTime)}</span>
        </div>
      ))}
    </div>
  )
}
