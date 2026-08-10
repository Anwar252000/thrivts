import { motion } from 'framer-motion'
import { Reveal } from '@/components/marketing/Reveal'
import appShot from '@/assets/marketing/app-screenshot.jpg'

export function AppTeaser() {
  return (
    <section id="app" className="relative overflow-hidden bg-[var(--color-sage-darker)] py-[clamp(90px,13vh,170px)] text-[var(--color-cream)]">
      <div
        aria-hidden
        className="pointer-events-none absolute inset-0"
        style={{ background: 'radial-gradient(80% 60% at 78% 25%, rgba(174,184,177,.16), transparent 60%)' }}
      />

      <div className="relative z-10 grid w-[min(1240px,90vw)] mx-auto items-center gap-12 lg:grid-cols-[1.15fr_0.85fr]">
        <div>
          <Reveal>
            <span className="text-xs font-bold uppercase tracking-[0.34em] text-[var(--color-sage-soft)]">Coming Soon</span>
          </Reveal>
          <Reveal delay={0.1} className="mt-4.5">
            <h2 className="text-[clamp(2.5rem,6vw,5rem)] font-black leading-[0.96] tracking-tight">
              Chat, Handpick
              <br />
              &amp; Order — In
              <br />
              One <em className="not-italic text-[var(--color-sage-soft)]">App</em>
            </h2>
          </Reveal>
          <Reveal delay={0.2} className="my-7 max-w-[40ch] text-[1.08rem] leading-relaxed text-[rgba(252,251,247,.86)]">
            Browse inventory, chat directly with suppliers, handpick stock through live video
            calls, build wishlists, and manage orders from one place. Your whole sourcing
            operation, in your pocket.
          </Reveal>
          <Reveal delay={0.3} className="inline-flex items-center gap-2.5 rounded-full border border-white/40 px-5.5 py-2.5 text-[0.95rem] font-bold">
            <motion.span
              className="size-2.5 rounded-full bg-[var(--color-sage-soft)]"
              animate={{ scale: [1, 1.6, 1], opacity: [1, 0.4, 1] }}
              transition={{ duration: 1.6, repeat: Infinity, ease: 'easeInOut' }}
            />
            In development
            <span className="inline-flex gap-0.5">
              {[0, 1, 2].map((i) => (
                <motion.i
                  key={i}
                  className="not-italic"
                  animate={{ opacity: [0.2, 1, 0.2] }}
                  transition={{ duration: 1.4, repeat: Infinity, delay: i * 0.2 }}
                >
                  .
                </motion.i>
              ))}
            </span>
          </Reveal>
          <Reveal delay={0.3} className="relative mt-5.5 h-1 w-[240px] max-w-full overflow-hidden rounded bg-white/[0.18]">
            <motion.span
              className="absolute top-0 h-full w-2/5 rounded"
              style={{ background: 'linear-gradient(90deg, transparent, rgba(252,251,247,.92), transparent)' }}
              animate={{ left: ['-40%', '100%'] }}
              transition={{ duration: 1.6, repeat: Infinity, ease: 'easeInOut' }}
            />
          </Reveal>
          <Reveal delay={0.3} className="mt-4 max-w-[42ch] text-sm opacity-70">
            Our iOS &amp; Android apps are on the way. For now, source and sell right here on the web.
          </Reveal>
        </div>

        <Reveal delay={0.2} className="relative mx-auto max-w-fit">
          <div
            aria-hidden
            className="absolute -inset-[12%] -z-10 blur-3xl"
            style={{ background: 'radial-gradient(circle, rgba(174,184,177,.3), transparent 65%)' }}
          />
          <div className="relative aspect-[9/19] w-[min(270px,68vw)] overflow-hidden rounded-[42px] border-[9px] border-[#15191680] bg-[var(--color-ink)] shadow-[var(--shadow-xl)]">
            <div className="absolute left-1/2 top-3 z-10 h-[22px] w-[84px] -translate-x-1/2 rounded-full bg-black" />
            <img src={appShot} alt="Thrivts app" className="h-full w-full object-cover object-top" />
          </div>
        </Reveal>
      </div>
    </section>
  )
}
