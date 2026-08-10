import { Reveal } from '@/components/marketing/Reveal'

export function IntroLine() {
  return (
    <section className="border-b border-[var(--color-line)] bg-[var(--color-cream)]">
      <Reveal className="w-[min(1240px,90vw)] mx-auto py-[clamp(34px,5vh,58px)]">
        <p className="max-w-[30ch] text-[clamp(1.5rem,3vw,2.5rem)] font-medium leading-tight tracking-tight text-[var(--color-ink)]">
          Built by vintage wholesalers with{' '}
          <b className="font-extrabold text-[var(--color-sage)]">over 10 years</b> of experience and
          a network of <b className="font-extrabold text-[var(--color-sage)]">100+ verified suppliers</b>{' '}
          worldwide.
        </p>
      </Reveal>
    </section>
  )
}
