import { Reveal } from '@/components/marketing/Reveal'
import vendor1 from '@/assets/marketing/vendor-1.jpg'
import vendor2 from '@/assets/marketing/vendor-2.jpg'
import vendor3 from '@/assets/marketing/vendor-3.jpg'
import vendor4 from '@/assets/marketing/vendor-4.jpg'

const VENDORS = [
  { img: vendor1, name: 'Salman', meta: 'KHI, PK · 8 Years' },
  { img: vendor2, name: 'Faizan', meta: 'KHI, PK · 12 Years' },
  { img: vendor3, name: 'Asmat', meta: 'KHI, PK · 8 Years' },
  { img: vendor4, name: 'Afnan', meta: 'KHI, PK · 6 Years' },
]

export function Vendors() {
  return (
    <section id="vendors" className="bg-[var(--color-cream)] py-[clamp(90px,13vh,170px)]">
      <div className="w-[min(1240px,90vw)] mx-auto">
        <div className="mb-16 flex flex-wrap items-end justify-between gap-6">
          <Reveal>
            <h2 className="text-[clamp(2.4rem,6.4vw,5.4rem)] font-black leading-[0.94] tracking-tight text-[var(--color-ink)]">
              Featured
              <br />
              <em className="not-italic text-[var(--color-sage)]">Suppliers</em>
            </h2>
          </Reveal>
          <Reveal delay={0.1} className="max-w-[34ch] text-right">
            <span className="text-xs font-bold uppercase tracking-[0.3em] text-[var(--color-sage)]">The Network</span>
            <p className="mt-3 text-base text-[var(--color-ink-soft)]">
              Every supplier on Thrivts is verified before joining the marketplace. Browse trusted
              wholesalers with established sourcing histories and transparent grading standards.
            </p>
          </Reveal>
        </div>

        <div className="grid grid-cols-2 gap-5 lg:grid-cols-4">
          {VENDORS.map((v, i) => (
            <Reveal
              key={v.name}
              delay={i * 0.1}
              data-cursor-hover
              className="group relative aspect-[3/4] overflow-hidden rounded-[var(--radius-lg)] bg-[var(--color-sage-mist)] shadow-[var(--shadow-sm)] transition-transform duration-700 hover:-translate-y-2.5 hover:shadow-[var(--shadow-lg)]"
            >
              <span className="absolute left-3 top-3 z-20 inline-flex items-center gap-1.5 rounded-full bg-[var(--color-cream)]/90 px-3.5 py-1.5 text-[0.64rem] font-bold uppercase tracking-wide text-[var(--color-sage-dark)] opacity-100 sm:opacity-0 sm:transition-opacity sm:duration-500 sm:group-hover:opacity-100">
                ✦ Verified
              </span>
              <img
                src={v.img}
                alt={v.name}
                loading="lazy"
                className="h-full w-full object-cover grayscale contrast-[1.04] transition-[filter,transform] duration-700 group-hover:scale-[1.06] group-hover:grayscale-0"
              />
              <div className="absolute inset-0 z-10" style={{ background: 'linear-gradient(180deg, transparent 42%, rgba(42,50,45,.85))' }} />
              <div className="absolute inset-x-0 bottom-0 z-20 p-6 text-[var(--color-cream)]">
                <div className="text-xl font-extrabold tracking-tight">{v.name}</div>
                <div className="mt-1 text-xs uppercase tracking-wide text-[var(--color-sage-soft)]">{v.meta}</div>
              </div>
            </Reveal>
          ))}
        </div>
      </div>
    </section>
  )
}
