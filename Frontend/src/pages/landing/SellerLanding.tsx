import { Link } from 'react-router-dom'
import { motion } from 'framer-motion'
import { Button } from '@/components/ui'
import { Logo } from '@/components/marketing/Logo'
import { LiveActivityFeed } from './LiveActivityFeed'

export function SellerLanding() {
  return (
    <div
      className="min-h-screen"
      style={{ background: 'linear-gradient(165deg, var(--color-sage-mist) 0%, var(--color-sage-paper) 55%, var(--color-cream) 100%)' }}
    >
      <header className="flex items-center justify-between px-6 py-6 sm:px-10">
        <Link to="/"><Logo className="text-2xl text-[var(--color-ink)]" /></Link>
        <div className="flex items-center gap-3">
          <Link to="/login" className="rounded-full border border-[var(--color-line-strong)] bg-[var(--color-white)]/70 px-5 py-2.5 text-xs font-bold uppercase tracking-widest text-[var(--color-ink)] transition-colors hover:bg-[var(--color-white)]">
            Sign In
          </Link>
          <Link to="/apply-seller" className="rounded-full bg-[var(--color-sage-darker)] px-6 py-2.5 text-xs font-bold uppercase tracking-widest text-[var(--color-cream)] transition-colors hover:bg-[var(--color-sage-dark)]">
            Become a Seller
          </Link>
        </div>
      </header>

      <main className="mx-auto w-[min(1100px,92vw)] pb-24 pt-10 text-center sm:pt-16">
        <motion.h1
          initial={{ opacity: 0, y: 16 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ duration: 0.6, ease: [0.19, 1, 0.22, 1] }}
          className="mx-auto max-w-[20ch] text-[clamp(2.4rem,7.5vw,5.2rem)] font-black uppercase leading-[0.98] tracking-tighter text-[var(--color-ink)]"
        >
          Live B2B vintage wholesale
        </motion.h1>

        <motion.p
          initial={{ opacity: 0, y: 12 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ duration: 0.6, delay: 0.12 }}
          className="mx-auto mt-6 max-w-[52ch] text-[1.05rem] leading-relaxed text-[var(--color-ink-soft)]"
        >
          Buyers from across Europe source containers of graded vintage stock. Sellers worldwide
          bid for direct deals. See what's moving right now.
        </motion.p>

        <motion.div
          initial={{ opacity: 0, y: 12 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ duration: 0.6, delay: 0.22 }}
          className="mt-8 flex flex-wrap justify-center gap-3"
        >
          <Button asChild size="lg" className="rounded-full bg-[var(--color-sage-darker)] text-[var(--color-cream)] hover:bg-[var(--color-sage-dark)]">
            <Link to="/apply-seller">Become a seller</Link>
          </Button>
          <Button asChild variant="outline" size="lg" className="rounded-full bg-[var(--color-white)]/70">
            <a href="#seller-live-activity">See live activity</a>
          </Button>
        </motion.div>
      </main>

      <section id="seller-live-activity" className="border-t border-[var(--color-line)] bg-[var(--color-cream)] px-6 py-16 sm:px-10">
        <div className="mx-auto w-[min(1100px,92vw)]">
          <p className="flex items-center gap-2 text-xs font-bold uppercase tracking-[0.2em] text-[var(--color-ink-faint)]">
            <span className="inline-block size-2 rounded-full bg-[var(--color-success)]" /> Live activity
          </p>
          <h2 className="mt-3 text-2xl font-black uppercase tracking-tight sm:text-3xl">What's moving right now</h2>
          <p className="mt-2 text-sm text-[var(--color-ink-soft)]">Open requirements and deals flowing through the platform, anonymized.</p>
          <div className="mt-8">
            <LiveActivityFeed emptyLabel="Feed will populate as activity goes live." />
          </div>
        </div>
      </section>
    </div>
  )
}
