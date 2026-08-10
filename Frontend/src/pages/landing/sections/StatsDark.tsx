import { Recycle, TrendingUp, ShieldCheck } from 'lucide-react'
import { Reveal } from '@/components/marketing/Reveal'
import { CountUp } from '@/components/marketing/CountUp'

const STATS = [
  {
    icon: Recycle,
    to: 92,
    suffix: 'M+',
    body: 'Tonnes of clothing sent to landfill every single year. Circular trade is the answer.',
  },
  {
    icon: TrendingUp,
    to: 1000,
    suffix: 's',
    body: 'Of wholesale listings live across the network — graded, sorted and ready to ship.',
  },
  {
    icon: ShieldCheck,
    to: 100,
    suffix: '%',
    body: 'Of stock sorted, graded and verified before it ever leaves the warehouse.',
  },
]

export function StatsDark() {
  return (
    <section className="overflow-hidden bg-[var(--color-sage-darker)] py-[clamp(90px,13vh,170px)] text-[var(--color-cream)]">
      <div className="w-[min(1240px,90vw)] mx-auto">
        <Reveal className="mb-16 max-w-[24ch] text-[clamp(1.5rem,3.4vw,2.7rem)] font-medium leading-snug tracking-tight">
          Vintage isn't a niche. <em className="not-italic text-[var(--color-sage-soft)]">It's the fastest-growing</em> corner
          of the global clothing trade.
        </Reveal>

        <div className="grid gap-12 md:grid-cols-3">
          {STATS.map((stat, i) => (
            <Reveal key={stat.body} delay={i * 0.1} className="border-t border-white/15 pt-8">
              <stat.icon className="mb-4.5 text-[var(--color-sage-soft)]" size={34} strokeWidth={1.5} />
              <div className="text-[clamp(3rem,7.5vw,6.4rem)] font-black leading-none tracking-tight">
                <CountUp
                  to={stat.to}
                  suffix={<span className="ml-0.5 align-super text-[0.46em] text-[var(--color-sage-soft)]">{stat.suffix}</span>}
                />
              </div>
              <p className="mt-3.5 max-w-[26ch] text-[0.96rem] font-normal leading-relaxed text-[var(--color-sage-soft)]">
                {stat.body}
              </p>
            </Reveal>
          ))}
        </div>
      </div>
    </section>
  )
}
