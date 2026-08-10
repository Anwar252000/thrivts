interface TickerProps {
  items: string[]
}

/** Infinite horizontal marquee — duplicates the list once so the CSS animation loops seamlessly. */
export function Ticker({ items }: TickerProps) {
  const track = [...items, ...items]

  return (
    <div className="overflow-hidden whitespace-nowrap border-y border-[var(--color-line)] bg-[var(--color-cream)] py-6">
      <div className="animate-ticker inline-flex [animation-play-state:running] hover:[animation-play-state:paused]">
        {track.map((item, i) => (
          <span
            key={i}
            className="inline-flex items-center gap-8 px-8 text-lg font-semibold uppercase tracking-wide text-[var(--color-ink-soft)] after:content-['✦'] after:text-[0.66em] after:text-[var(--color-sage)]"
          >
            {item}
          </span>
        ))}
      </div>
    </div>
  )
}
