import { Link } from 'react-router-dom'
import { motion } from 'framer-motion'
import { Button } from '@/components/ui'
import { Logo } from '@/components/marketing/Logo'
import { LiveActivityFeed } from './LiveActivityFeed'

const STEPS = [
  { n: '01', title: 'Post your requirement', body: "Tell us the item, quantity, grade, and destination. Takes a minute." },
  { n: '02', title: 'We match you', body: 'We match you with the best fit from 100+ verified, vetted sellers.' },
  { n: '03', title: 'Secure & protected', body: 'Safe, secure payments with full dispute handling on every order.' },
]

export function BuyerLanding() {
  return (
    <div className="min-h-screen bg-[var(--color-cream)]">
      <header className="flex items-center justify-between px-6 py-6 sm:px-10">
        <Link to="/"><Logo className="text-2xl text-[var(--color-ink)]" /></Link>
        <div className="flex items-center gap-3">
          <Link to="/apply" className="rounded-full border border-[var(--color-line-strong)] px-5 py-2.5 text-xs font-bold uppercase tracking-widest text-[var(--color-ink)] transition-colors hover:bg-[var(--color-sage-mist)]">
            Sign Up
          </Link>
          <Link to="/login" className="rounded-full border border-[var(--color-line-strong)] px-5 py-2.5 text-xs font-bold uppercase tracking-widest text-[var(--color-ink)] transition-colors hover:bg-[var(--color-sage-mist)]">
            Sign In
          </Link>
        </div>
      </header>

      <main className="mx-auto w-[min(1100px,92vw)] pb-24 pt-6 sm:pt-14">
        <motion.p
          initial={{ opacity: 0, y: 8 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ duration: 0.5 }}
          className="mb-5 flex items-center gap-2 text-xs font-bold uppercase tracking-[0.2em] text-[var(--color-ink-faint)]"
        >
          <span className="h-px w-6 bg-[var(--color-ink-faint)]" /> By invitation · Vetted buyers only
        </motion.p>

        <motion.h1
          initial={{ opacity: 0, y: 16 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ duration: 0.6, delay: 0.05, ease: [0.19, 1, 0.22, 1] }}
          className="max-w-[16ch] text-[clamp(2.2rem,6.2vw,4.6rem)] font-black uppercase leading-[0.98] tracking-tighter text-[var(--color-ink)]"
        >
          The B2B vintage wholesale platform built for serious buyers.
        </motion.h1>

        <motion.p
          initial={{ opacity: 0, y: 12 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ duration: 0.6, delay: 0.15 }}
          className="mt-6 max-w-[52ch] text-[1.05rem] leading-relaxed text-[var(--color-ink-soft)]"
        >
          Tell us what you need. We'll source it from our vetted supplier network, grade it, and
          ship it. No middlemen. No mystery boxes. Just clean vintage, by the container or by the order.
        </motion.p>

        <motion.div
          initial={{ opacity: 0, y: 12 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ duration: 0.6, delay: 0.25 }}
          className="mt-8 flex flex-wrap gap-3"
        >
          <Button asChild variant="primary" size="lg" className="rounded-full">
            <Link to="/apply">Apply as a buyer</Link>
          </Button>
          <Button asChild variant="outline" size="lg" className="rounded-full bg-[var(--color-white)]">
            <Link to="/login">I have an account</Link>
          </Button>
        </motion.div>

        <div className="mt-16 grid gap-4 sm:grid-cols-3">
          {STEPS.map((s) => (
            <div key={s.n} className="rounded-[var(--radius-lg)] border border-[var(--color-line)] bg-[var(--color-white)] p-6">
              <p className="text-xs font-bold text-[var(--color-ink-faint)]">{s.n}</p>
              <p className="mt-3 text-sm font-extrabold uppercase tracking-wide">{s.title}</p>
              <p className="mt-2 text-sm leading-relaxed text-[var(--color-ink-soft)]">{s.body}</p>
            </div>
          ))}
        </div>
      </main>

      <section className="border-t border-[var(--color-line)] bg-[var(--color-sage-paper)] px-6 py-16 sm:px-10">
        <div className="mx-auto w-[min(1100px,92vw)]">
          <p className="flex items-center gap-2 text-xs font-bold uppercase tracking-[0.2em] text-[var(--color-ink-faint)]">
            <span className="h-px w-6 bg-[var(--color-ink-faint)]" /> Live on Thrivts
          </p>
          <h2 className="mt-3 text-2xl font-black uppercase tracking-tight sm:text-3xl">What's moving right now</h2>
          <p className="mt-2 text-sm text-[var(--color-ink-soft)]">Real buyer requirements flowing through our verified network.</p>
          <div className="mt-8">
            <LiveActivityFeed emptyLabel="New activity will appear here as requirements are posted." />
          </div>
        </div>
      </section>
    </div>
  )
}
