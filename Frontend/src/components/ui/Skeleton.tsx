import { cn } from '@/lib/utils'

/** Shimmer loading placeholder — same shimmer animation as the live portals. */
export function Skeleton({ className, ...props }: React.HTMLAttributes<HTMLDivElement>) {
  return (
    <div
      className={cn(
        'animate-shimmer rounded-[var(--radius-sm)] bg-[linear-gradient(110deg,var(--color-sage-mist)_8%,var(--color-sage-soft)_18%,var(--color-sage-mist)_33%)] bg-[length:200%_100%]',
        className,
      )}
      {...props}
    />
  )
}
