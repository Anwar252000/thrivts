import { useRef, useState } from 'react'
import { motion, useMotionValueEvent, useScroll, useTransform } from 'framer-motion'
import { useMediaQuery } from '@/lib/useMediaQuery'
import { Reveal } from '@/components/marketing/Reveal'
import catWomens from '@/assets/marketing/cat-womens.jpg'
import catMens from '@/assets/marketing/cat-mens.jpg'
import catSports from '@/assets/marketing/cat-sports.jpg'
import catAccessories from '@/assets/marketing/cat-accessories.jpg'

const CATEGORIES = [
  {
    img: catWomens,
    alt: 'Womens',
    index: '01 — Womenswear',
    title: 'Womens',
    titleEm: 'Edit',
    body: "Curated women's vintage from trusted suppliers worldwide. From everyday essentials to premium branded pieces.",
    tags: ['Outerwear', 'Dresses', 'Knitwear', 'Denim'],
  },
  {
    img: catMens,
    alt: 'Mens',
    index: '02 — Menswear',
    title: 'Mens',
    titleEm: 'Edit',
    body: 'Discover branded, workwear, sportswear, and everyday vintage sourced from trusted wholesalers.',
    tags: ['Jackets', 'Shirts', 'Trousers', 'Tees'],
  },
  {
    img: catSports,
    alt: 'Sportswear',
    index: '03 — Sportswear',
    title: 'Sports',
    titleEm: 'Edit',
    body: 'Branded tracksuits, jerseys, hoodies and retro athletic wear. High-demand, fast-moving stock sorted, checked, and shipment-ready.',
    tags: ['Tracksuits', 'Jerseys', 'Hoodies', 'Retro'],
  },
  {
    img: catAccessories,
    alt: 'Accessories',
    index: '04 — Accessories',
    title: 'Access',
    titleEm: 'ories',
    body: 'Bags, belts, hats and the finishing pieces that round out an order. Curated, graded, and packed to ship with your container.',
    tags: ['Bags', 'Belts', 'Hats', 'Scarves'],
  },
]

export function Categories() {
  const isDesktop = useMediaQuery('(min-width: 861px)')

  return isDesktop ? <CategoriesPinned /> : <CategoriesStacked />
}

function CategoriesPinned() {
  const containerRef = useRef<HTMLDivElement>(null)
  const [active, setActive] = useState(0)
  const { scrollYProgress } = useScroll({ target: containerRef, offset: ['start start', 'end end'] })
  const progressWidth = useTransform(scrollYProgress, (p) => `${p * 100}%`)

  useMotionValueEvent(scrollYProgress, 'change', (p) => {
    const idx = Math.min(CATEGORIES.length - 1, Math.floor(p * CATEGORIES.length))
    setActive(idx)
  })

  return (
    <section id="categories" ref={containerRef} className="relative bg-[var(--color-sage-paper)]" style={{ height: `${CATEGORIES.length * 100}vh` }}>
      <div className="sticky top-0 flex h-[100svh] items-center overflow-hidden">
        <div className="relative w-[min(1240px,90vw)] mx-auto h-full">
          <div className="pointer-events-none absolute inset-x-0 top-[3.5vh] z-10 flex items-start justify-between">
            <span className="ml-[calc(50%+2.5vw)] text-xs font-bold uppercase tracking-[0.3em] text-[var(--color-sage-deep)]">
              Categories
            </span>
            <span className="text-sm font-bold tracking-widest text-[var(--color-sage-deep)]">
              <b className="text-base text-[var(--color-ink)]">{String(active + 1).padStart(2, '0')}</b> / 0{CATEGORIES.length}
            </span>
          </div>

          {CATEGORIES.map((cat, i) => (
            <div
              key={cat.title}
              className="absolute inset-0 grid grid-cols-2 items-center gap-[5vw] transition-opacity duration-700"
              style={{ opacity: active === i ? 1 : 0, visibility: active === i ? 'visible' : 'hidden' }}
            >
              <motion.div
                animate={{ scale: active === i ? 1 : 0.94 }}
                transition={{ duration: 1.1, ease: [0.19, 1, 0.22, 1] }}
                className="relative aspect-[4/5] overflow-hidden rounded-[var(--radius-xl)] shadow-[var(--shadow-xl)]"
              >
                <img src={cat.img} alt={cat.alt} loading="lazy" className="h-full w-full object-cover [filter:grayscale(.25)_contrast(1.03)]" />
                <div className="absolute inset-0" style={{ background: 'linear-gradient(160deg, transparent 50%, rgba(42,50,45,.35))' }} />
              </motion.div>

              <div>
                <span className="text-sm font-bold tracking-[0.3em] text-[var(--color-sage)]">{cat.index}</span>
                <h3 className="my-4.5 text-[clamp(2.6rem,7vw,6rem)] font-black leading-[0.9] tracking-tight text-[var(--color-ink)]">
                  {cat.title}
                  <em className="block not-italic text-transparent [-webkit-text-stroke:1.5px_var(--color-ink)]">{cat.titleEm}</em>
                </h3>
                <p className="max-w-[38ch] text-[1.06rem] leading-relaxed text-[var(--color-ink-soft)]">{cat.body}</p>
                <div className="mt-6 flex flex-wrap gap-2">
                  {cat.tags.map((tag) => (
                    <span
                      key={tag}
                      className="rounded-full border border-[var(--color-line)] bg-[var(--color-cream)] px-4 py-2 text-xs font-semibold uppercase tracking-wide text-[var(--color-sage-deep)]"
                    >
                      {tag}
                    </span>
                  ))}
                </div>
              </div>
            </div>
          ))}

          <div className="absolute inset-x-0 bottom-[12vh] h-0.5 bg-[var(--color-line)]">
            <motion.i className="absolute left-0 top-0 block h-full bg-[var(--color-sage)]" style={{ width: progressWidth }} />
          </div>
        </div>
      </div>
    </section>
  )
}

function CategoriesStacked() {
  return (
    <section id="categories" className="bg-[var(--color-sage-paper)] py-16">
      <div className="flex flex-col gap-20">
        {CATEGORIES.map((cat) => (
          <Reveal key={cat.title}>
            <div className="relative aspect-[3/4] w-full overflow-hidden">
              <img src={cat.img} alt={cat.alt} loading="lazy" className="h-full w-full object-cover [filter:grayscale(.25)_contrast(1.03)]" />
            </div>
            <div className="px-[7vw] pt-7">
              <span className="text-sm font-bold tracking-[0.3em] text-[var(--color-sage)]">{cat.index}</span>
              <h3 className="my-4 text-[clamp(2.6rem,15vw,4.2rem)] font-black leading-[0.9] tracking-tight text-[var(--color-ink)]">
                {cat.title}
                <em className="block not-italic text-transparent [-webkit-text-stroke:1.5px_var(--color-ink)]">{cat.titleEm}</em>
              </h3>
              <p className="max-w-[38ch] text-[1.06rem] leading-relaxed text-[var(--color-ink-soft)]">{cat.body}</p>
              <div className="mt-6 flex flex-wrap gap-2">
                {cat.tags.map((tag) => (
                  <span
                    key={tag}
                    className="rounded-full border border-[var(--color-line)] bg-[var(--color-cream)] px-4 py-2 text-xs font-semibold uppercase tracking-wide text-[var(--color-sage-deep)]"
                  >
                    {tag}
                  </span>
                ))}
              </div>
            </div>
          </Reveal>
        ))}
      </div>
    </section>
  )
}
