import { Truck, ShieldCheck, Headset } from 'lucide-react'
import { Reveal } from '@/components/marketing/Reveal'

const BENEFITS = [
  {
    n: '01',
    icon: Truck,
    title: 'Worldwide Shipping',
    body: 'Container freight handled end to end — from our warehouse to your port, across the UK, Europe and beyond, fully tracked.',
    link: 'Global Logistics →',
  },
  {
    n: '02',
    icon: ShieldCheck,
    title: 'Full Buyer Protection',
    body: "Every order is graded against spec and backed by our guarantee. If a lot doesn't match, you don't pay for it. Simple.",
    link: 'Graded To Spec →',
  },
  {
    n: '03',
    icon: Headset,
    title: 'Dedicated Account Support',
    body: 'A real point of contact who knows your business. From first order to repeat containers, someone is always on hand to keep things moving.',
    link: 'Always On Hand →',
  },
]

export function Benefits() {
  return (
    <section className="bg-[var(--color-sage-mist)] py-[clamp(90px,13vh,170px)]">
      <div className="w-[min(1240px,90vw)] mx-auto">
        <div className="mb-16 flex flex-wrap items-end justify-between gap-6">
          <Reveal>
            <h2 className="text-[clamp(2.4rem,6.4vw,5.4rem)] font-black leading-[0.94] tracking-tight text-[var(--color-ink)]">
              Why Trade
              <br />
              <em className="not-italic text-[var(--color-sage)]">With Us</em>
            </h2>
          </Reveal>
          <Reveal delay={0.1} className="max-w-[34ch] text-right">
            <span className="text-xs font-bold uppercase tracking-[0.3em] text-[var(--color-sage)]">The Edge</span>
            <p className="mt-3 text-base text-[var(--color-ink-soft)]">Everything a serious buyer needs — and nothing they don't.</p>
          </Reveal>
        </div>

        <div className="grid gap-5 md:grid-cols-3">
          {BENEFITS.map((b, i) => (
            <Reveal
              key={b.n}
              delay={i * 0.1}
              className="group relative flex min-h-[340px] flex-col overflow-hidden rounded-[var(--radius-lg)] bg-[var(--color-cream)] p-9 transition-transform duration-600 hover:-translate-y-2 hover:shadow-[var(--shadow-md)]"
            >
              <span
                aria-hidden
                className="pointer-events-none absolute right-[-6px] bottom-[-34px] select-none text-[11rem] font-black leading-[0.7] tracking-tight text-transparent transition-transform duration-700 group-hover:-translate-y-2 group-hover:-translate-x-1"
                style={{ WebkitTextStroke: '1.5px rgba(115,131,122,.14)' }}
              >
                {b.n}
              </span>

              <div className="relative z-10 flex flex-1 flex-col">
                <span className="text-xs font-bold tracking-[0.22em] text-[var(--color-sage)]">{b.n}</span>
                <div className="my-7 flex size-14 items-center justify-center rounded-2xl bg-[var(--color-sage-darker)] text-[var(--color-cream)] transition-transform duration-600 group-hover:-rotate-[8deg] group-hover:scale-110">
                  <b.icon size={24} strokeWidth={1.8} />
                </div>
                <h3 className="text-2xl font-extrabold tracking-tight text-[var(--color-ink)]">{b.title}</h3>
                <p className="mt-3 text-[0.98rem] leading-relaxed text-[var(--color-ink-soft)]">{b.body}</p>
                <span className="mt-auto pt-5 text-xs font-bold uppercase tracking-wide text-[var(--color-sage-deep)] transition-colors group-hover:text-[var(--color-sage-darker)]">
                  {b.link}
                </span>
              </div>
            </Reveal>
          ))}
        </div>
      </div>
    </section>
  )
}
