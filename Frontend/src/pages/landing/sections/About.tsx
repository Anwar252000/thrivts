import { Reveal } from '@/components/marketing/Reveal'
import about1 from '@/assets/marketing/about-1.jpg'
import about2 from '@/assets/marketing/about-2.jpg'
import about3 from '@/assets/marketing/about-3.jpg'

const STEPS = [
  {
    n: '01',
    title: 'Discover the right inventory',
    body: 'Browse trusted suppliers, explore categories, and find stock that matches your business needs.',
  },
  {
    n: '02',
    title: 'Connect with verified suppliers',
    body: 'Chat directly with vetted wholesalers, compare offers, and build long-term sourcing relationships.',
  },
  {
    n: '03',
    title: 'Buy with confidence',
    body: 'Transparent grading, buyer protection, and secure transactions help you source inventory with peace of mind.',
  },
]

export function About() {
  return (
    <section id="about" className="bg-[var(--color-cream)] py-[clamp(90px,13vh,170px)]">
      <div className="w-[min(1240px,90vw)] mx-auto">
        <div className="mb-16 grid gap-14 lg:grid-cols-2">
          <Reveal>
            <p className="max-w-[16ch] text-[clamp(1.5rem,3vw,2.5rem)] font-medium leading-tight tracking-tight text-[var(--color-ink)]">
              Where serious buyers <span className="font-extrabold text-[var(--color-sage)]">source</span> vintage
              at <span className="font-extrabold text-[var(--color-sage)]">scale.</span>
            </p>
            <div className="mt-9 flex max-w-[440px] flex-col gap-5 border-t border-[var(--color-line)] pt-7">
              {STEPS.map((step) => (
                <div key={step.n} className="flex items-start gap-4">
                  <span className="min-w-[26px] pt-0.5 text-xs font-extrabold tracking-widest text-[var(--color-sage)]">
                    {step.n}
                  </span>
                  <div>
                    <b className="block text-lg font-bold leading-tight tracking-tight text-[var(--color-ink)]">
                      {step.title}
                    </b>
                    <small className="mt-1 block text-sm leading-relaxed text-[var(--color-sage-deep)]">
                      {step.body}
                    </small>
                  </div>
                </div>
              ))}
            </div>
          </Reveal>

          <div>
            <Reveal>
              <span className="inline-flex items-center gap-2.5 text-xs font-bold uppercase tracking-[0.34em] text-[var(--color-sage-deep)] before:h-px before:w-6 before:bg-[var(--color-sage)]">
                Who Are We?
              </span>
            </Reveal>
            <Reveal delay={0.1} className="mt-5">
              <p className="text-[1.05rem] leading-relaxed text-[var(--color-ink-soft)]">
                Thrivts was built by people who've spent over a decade in the vintage wholesale
                trade — sourcing, grading, packing, and shipping millions of garments, and building
                relationships with 100+ verified suppliers across multiple countries.
              </p>
            </Reveal>
            <Reveal delay={0.2} className="mt-5">
              <p className="text-[1.05rem] leading-relaxed text-[var(--color-ink-soft)]">
                Whether you need a single bale, curated bundles, or multiple containers a month,
                Thrivts connects you directly with trusted suppliers who can support your growth at
                any scale.
              </p>
            </Reveal>
          </div>
        </div>

        <div className="grid grid-cols-1 items-start gap-5 sm:grid-cols-[1.3fr_0.7fr]">
          <Reveal className="relative aspect-[3/4.1] overflow-hidden rounded-[var(--radius-lg)] bg-[var(--color-sage-mist)] shadow-[var(--shadow-lg)]">
            <img src={about1} alt="Curated secondhand fashion" loading="lazy" className="h-full w-full object-cover [filter:grayscale(.2)_contrast(1.02)]" />
            <div className="absolute -left-[2%] bottom-[6%] z-10 rounded-[var(--radius-md)] bg-[var(--color-sage-darker)] px-6 py-5 text-[var(--color-cream)] shadow-[var(--shadow-md)]">
              <b className="block text-2xl font-black tracking-tight">Biggest</b>
              <small className="text-[0.66rem] uppercase tracking-[0.2em] text-[var(--color-sage-soft)]">Suppliers Network</small>
            </div>
          </Reveal>

          <div className="grid gap-5 pt-0 sm:pt-14">
            <Reveal delay={0.1} className="relative aspect-[4/3] overflow-hidden rounded-[var(--radius-lg)] bg-[var(--color-sage-mist)] shadow-[var(--shadow-lg)]">
              <img src={about2} alt="Thrivts editorial" loading="lazy" className="h-full w-full object-cover [filter:grayscale(.2)_contrast(1.02)]" />
              <div className="absolute bottom-[5%] right-[5%] z-10 rounded-[var(--radius-md)] bg-[var(--color-sage-darker)] px-4.5 py-3.5 text-[var(--color-cream)] shadow-[var(--shadow-md)]">
                <b className="block text-xl font-black tracking-tight">10+</b>
                <small className="text-[0.56rem] uppercase tracking-[0.16em] text-[var(--color-sage-soft)]">Years Experience</small>
              </div>
            </Reveal>
            <Reveal delay={0.2} className="relative aspect-square overflow-hidden rounded-[var(--radius-lg)] bg-[var(--color-sage-mist)] shadow-[var(--shadow-lg)]">
              <img src={about3} alt="Graded stock" loading="lazy" className="h-full w-full object-cover [filter:grayscale(.2)_contrast(1.02)]" />
              <div className="absolute bottom-[5%] right-[5%] z-10 rounded-[var(--radius-md)] bg-[var(--color-sage-darker)] px-4.5 py-3.5 text-[var(--color-cream)] shadow-[var(--shadow-md)]">
                <b className="block text-xl font-black tracking-tight">1M+</b>
                <small className="text-[0.56rem] uppercase tracking-[0.16em] text-[var(--color-sage-soft)]">Clothes Sold</small>
              </div>
            </Reveal>
          </div>
        </div>
      </div>
    </section>
  )
}
