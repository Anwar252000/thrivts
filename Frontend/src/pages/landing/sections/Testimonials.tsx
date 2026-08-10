import { Reveal } from '@/components/marketing/Reveal'

const TESTIMONIALS = [
  {
    quote:
      "Didn't think sourcing vintage at this scale could be this clean. Every lot arrives exactly as graded. Thrivts changed how we source completely.",
    who: 'Vintage Store Owner',
    place: 'United Kingdom',
    tag: 'UK',
  },
  {
    quote:
      "The buyer protection is the real deal. First container matched spec to the piece. We've ordered four since — no surprises, just stock that sells.",
    who: 'Wholesale Buyer',
    place: 'France',
    tag: 'FR',
  },
  {
    quote:
      'Reliable suppliers, faultless sorting, and a team that actually understands our business. Thrivts has become our main source for vintage stock.',
    who: 'Retailer',
    place: 'Germany',
    tag: 'DE',
  },
]

export function Testimonials() {
  return (
    <section className="bg-[var(--color-cream)] py-[clamp(90px,13vh,170px)]">
      <div className="w-[min(1240px,90vw)] mx-auto">
        <div className="mb-16 flex flex-wrap items-end justify-between gap-6">
          <Reveal>
            <h2 className="text-[clamp(2.4rem,6.4vw,5.4rem)] font-black leading-[0.94] tracking-tight text-[var(--color-ink)]">
              From Our
              <br />
              <em className="not-italic text-[var(--color-sage)]">Buyers</em>
            </h2>
          </Reveal>
          <Reveal delay={0.1} className="max-w-[34ch] text-right">
            <span className="text-xs font-bold uppercase tracking-[0.3em] text-[var(--color-sage)]">The Word</span>
            <p className="mt-3 text-base text-[var(--color-ink-soft)]">Traders who source through Thrivts, in their own words.</p>
          </Reveal>
        </div>

        <div className="grid gap-5 md:grid-cols-3">
          {TESTIMONIALS.map((t, i) => (
            <Reveal
              key={t.tag}
              delay={i * 0.1}
              className="rounded-[var(--radius-lg)] bg-[var(--color-sage-paper)] p-8 transition-transform duration-600 hover:-translate-y-1.5"
            >
              <span className="block h-6 text-4xl font-black leading-[0.6] text-[var(--color-sage)]">"</span>
              <p className="my-4.5 text-[1.06rem] leading-relaxed text-[var(--color-ink-soft)]">{t.quote}</p>
              <div className="flex items-center gap-3">
                <div className="flex size-11 shrink-0 items-center justify-center rounded-full bg-[var(--color-sage)] text-sm font-extrabold text-[var(--color-cream)]">
                  {t.tag}
                </div>
                <div>
                  <b className="text-sm font-bold text-[var(--color-ink)]">{t.who}</b>
                  <small className="mt-0.5 block text-xs uppercase tracking-wide text-[var(--color-sage-deep)]">{t.place}</small>
                </div>
              </div>
            </Reveal>
          ))}
        </div>
      </div>
    </section>
  )
}
