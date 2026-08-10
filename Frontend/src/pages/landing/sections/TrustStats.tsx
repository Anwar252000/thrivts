import { Reveal } from '@/components/marketing/Reveal'

const STATS = [
  { n: '100+', l: 'Verified Suppliers' },
  { n: '10+', l: 'Years Industry Experience' },
  { n: '1M+', l: 'Vintage Pieces Sold' },
  { n: '1 Bale–10+', l: 'Containers Supported Monthly' },
]

export function TrustStats() {
  return (
    <section className="bg-[var(--color-sage-darker)] text-[var(--color-cream)]">
      <div className="w-[min(1240px,90vw)] mx-auto py-[clamp(60px,9vh,110px)]">
        <div className="grid grid-cols-2 gap-px overflow-hidden rounded-[var(--radius-lg)] border border-white/15 bg-white/15 lg:grid-cols-4">
          {STATS.map((s, i) => (
            <Reveal key={s.l} delay={i * 0.1} className="bg-[var(--color-sage-darker)] px-6 py-9 sm:px-8">
              <div className="text-[clamp(2.4rem,5vw,4rem)] font-extrabold leading-none tracking-tight text-[var(--color-cream)]">
                {s.n}
              </div>
              <div className="mt-3.5 text-sm uppercase leading-snug tracking-wide text-[var(--color-sage-soft)]">
                {s.l}
              </div>
            </Reveal>
          ))}
        </div>
      </div>
    </section>
  )
}
