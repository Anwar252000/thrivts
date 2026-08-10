import { useRef } from 'react'
import { Link } from 'react-router-dom'
import { motion, useScroll, useTransform } from 'framer-motion'
import heroImg from '@/assets/marketing/hero.jpg'

const lines = [
  { text: 'Grow Your', delay: 0.05 },
  { text: 'Vintage Business', delay: 0.15 },
]

export function Hero() {
  const ref = useRef<HTMLElement>(null)
  const { scrollYProgress } = useScroll({ target: ref, offset: ['start start', 'end start'] })
  const imgY = useTransform(scrollYProgress, [0, 1], ['0%', '18%'])

  return (
    <header
      ref={ref}
      id="top"
      className="relative isolate flex min-h-[100svh] items-center overflow-hidden py-[19vh] pb-[17vh]"
      style={{ background: 'linear-gradient(165deg, var(--color-sage-mist) 0%, var(--color-sage-paper) 60%, var(--color-cream) 100%)' }}
    >
      <div className="absolute inset-0 -z-20 overflow-hidden">
        <motion.img
          src={heroImg}
          alt=""
          style={{ y: imgY }}
          className="h-[120%] w-full scale-105 object-cover object-[center_18%] [filter:grayscale(.35)_contrast(1.02)_brightness(.92)]"
        />
        <div
          className="absolute inset-0"
          style={{ background: 'linear-gradient(180deg, rgba(42,50,45,.28) 0%, rgba(42,50,45,.12) 38%, rgba(42,50,45,.5) 100%)' }}
        />
      </div>
      <div
        className="absolute inset-0 -z-10"
        style={{ background: 'radial-gradient(120% 90% at 15% 30%, rgba(252,251,247,.15), transparent 55%)' }}
      />

      <div className="w-[min(1240px,90vw)] mx-auto">
        <h1 className="max-w-[14ch] text-[clamp(2.7rem,9.4vw,9rem)] font-black uppercase leading-[0.96] tracking-tighter text-[var(--color-cream)] [text-shadow:0_8px_60px_rgba(42,50,45,.35)]">
          {lines.map((line) => (
            <span key={line.text} className="block overflow-hidden">
              <motion.span
                initial={{ y: '105%' }}
                animate={{ y: 0 }}
                transition={{ duration: 1.1, delay: line.delay, ease: [0.19, 1, 0.22, 1] }}
                className="block"
              >
                {line.text}
              </motion.span>
            </span>
          ))}
          <span className="block overflow-hidden">
            <motion.span
              initial={{ y: '105%' }}
              animate={{ y: 0 }}
              transition={{ duration: 1.1, delay: 0.25, ease: [0.19, 1, 0.22, 1] }}
              className="block"
            >
              With <em className="not-italic text-transparent [-webkit-text-stroke:1.3px_var(--color-cream)]">Thrivts</em>
            </motion.span>
          </span>
        </h1>

        <div className="mt-11 flex max-w-[1000px] flex-wrap items-end justify-between gap-9">
          <motion.p
            initial={{ opacity: 0, y: 18 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ duration: 1, delay: 0.55, ease: [0.19, 1, 0.22, 1] }}
            className="max-w-[34ch] text-[1.06rem] font-normal leading-relaxed text-[rgba(252,251,247,.92)]"
          >
            Source curated vintage inventory from 100+ verified suppliers worldwide. Transparent
            grading, secure transactions, and trusted wholesale relationships — all in one place.
          </motion.p>
          <motion.div
            initial={{ opacity: 0, y: 18 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ duration: 1, delay: 0.68, ease: [0.19, 1, 0.22, 1] }}
            className="flex gap-3"
          >
            <Link
              to="/login"
              data-cursor-hover
              className="inline-flex items-center gap-2.5 whitespace-nowrap rounded-full bg-[var(--color-cream)] px-8 py-4 text-xs font-bold uppercase tracking-widest text-[var(--color-ink)] transition-all hover:-translate-y-1 hover:shadow-[0_20px_40px_-16px_rgba(0,0,0,.4)]"
            >
              Start Sourcing <span className="transition-transform group-hover:translate-x-1">↗</span>
            </Link>
            <Link
              to="/login"
              data-cursor-hover
              className="inline-flex items-center gap-2.5 whitespace-nowrap rounded-full border border-white px-8 py-4 text-xs font-bold uppercase tracking-widest text-white transition-all hover:-translate-y-1 hover:border-[var(--color-cream)] hover:bg-[var(--color-cream)] hover:text-[var(--color-ink)]"
            >
              Start Selling ↗
            </Link>
          </motion.div>
        </div>
      </div>

      <motion.div
        initial={{ opacity: 0 }}
        animate={{ opacity: 1 }}
        transition={{ duration: 1, delay: 0.8 }}
        className="absolute right-[clamp(20px,5vw,64px)] top-[18vh] z-10 hidden text-right text-[0.68rem] uppercase leading-loose tracking-[0.2em] text-[rgba(252,251,247,.7)] lg:block"
      >
        <div>Source With Confidence</div>
        <div>Verified Suppliers · Transparent Grading</div>
        <div>Global Network</div>
      </motion.div>

      <div className="absolute bottom-8 left-1/2 z-10 flex -translate-x-1/2 flex-col items-center gap-2.5 text-[0.62rem] uppercase tracking-[0.32em] text-[rgba(252,251,247,.8)]">
        <span>Scroll</span>
        <span className="relative h-[50px] w-px overflow-hidden bg-gradient-to-b from-[rgba(252,251,247,.7)] to-transparent">
          <motion.span
            className="absolute inset-x-0 top-0 h-[38%] bg-[var(--color-cream)]"
            animate={{ y: ['0%', '300%'] }}
            transition={{ duration: 2, repeat: Infinity, ease: [0.19, 1, 0.22, 1] }}
          />
        </span>
      </div>
    </header>
  )
}
