import { useState } from 'react'
import { motion } from 'framer-motion'
import { Mail, Phone, MapPin } from 'lucide-react'
import { Reveal } from '@/components/marketing/Reveal'

const revealProps = {
  initial: { opacity: 0, y: 26 },
  whileInView: { opacity: 1, y: 0 },
  viewport: { once: true, amount: 0.2 },
  transition: { duration: 1.1, ease: [0.19, 1, 0.22, 1] as const },
}

const FIELD_CLASS =
  'peer w-full rounded-[var(--radius-md)] border border-transparent bg-[var(--color-sage-paper)] px-4.5 py-4.5 text-base text-[var(--color-ink)] transition-all duration-300 placeholder:text-transparent focus:border-[var(--color-sage)] focus:bg-white focus:shadow-[var(--shadow-sm)] focus:outline-none'

const LABEL_CLASS =
  'pointer-events-none absolute left-4.5 top-4.5 text-base uppercase tracking-wide text-[var(--color-sage-deep)] transition-all duration-300 peer-focus:-top-2.5 peer-focus:left-3 peer-focus:rounded peer-focus:bg-[var(--color-cream)] peer-focus:px-1.5 peer-focus:text-[0.64rem] peer-focus:tracking-[0.16em] peer-focus:text-[var(--color-sage)] peer-not-placeholder-shown:-top-2.5 peer-not-placeholder-shown:left-3 peer-not-placeholder-shown:rounded peer-not-placeholder-shown:bg-[var(--color-cream)] peer-not-placeholder-shown:px-1.5 peer-not-placeholder-shown:text-[0.64rem] peer-not-placeholder-shown:tracking-[0.16em]'

const INFO = [
  { icon: Mail, label: 'Email', value: 'support@thrivts.com' },
  { icon: Phone, label: 'Phone', value: '+44 7427 754233' },
  { icon: MapPin, label: 'Location', value: 'Pakistan · UK · Indonesia' },
]

export function Contact() {
  const [sent, setSent] = useState(false)

  const onSubmit = (e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault()
    setSent(true)
    e.currentTarget.reset()
    setTimeout(() => setSent(false), 2600)
  }

  return (
    <section id="contact" className="bg-[var(--color-cream)] py-[clamp(90px,13vh,170px)]">
      <div className="w-[min(1240px,90vw)] mx-auto">
        <Reveal className="mb-16">
          <h2 className="text-[clamp(2.4rem,6.4vw,5.4rem)] font-black leading-[0.94] tracking-tight text-[var(--color-ink)]">
            Let's
            <br />
            <em className="not-italic text-[var(--color-sage)]">Talk Stock</em>
          </h2>
        </Reveal>

        <div className="grid gap-14 lg:grid-cols-2">
          <motion.form onSubmit={onSubmit} className="grid gap-4" {...revealProps}>
            <div className="relative">
              <input id="cn" required placeholder=" " className={FIELD_CLASS} />
              <label htmlFor="cn" className={LABEL_CLASS}>Your Name</label>
            </div>
            <div className="relative">
              <input id="ce" type="email" required placeholder=" " className={FIELD_CLASS} />
              <label htmlFor="ce" className={LABEL_CLASS}>Email Address</label>
            </div>
            <div className="relative">
              <input id="cc" placeholder=" " className={FIELD_CLASS} />
              <label htmlFor="cc" className={LABEL_CLASS}>Company</label>
            </div>
            <div className="relative">
              <textarea id="cm" required placeholder=" " rows={4} className={`${FIELD_CLASS} resize-y`} />
              <label htmlFor="cm" className={LABEL_CLASS}>What are you sourcing?</label>
            </div>
            <motion.button
              type="submit"
              whileHover={{ scale: 1.01 }}
              whileTap={{ scale: 0.98 }}
              animate={{ backgroundColor: sent ? 'var(--color-sage)' : 'var(--color-sage-dark)' }}
              className="mt-1 flex items-center justify-center gap-2.5 rounded-full px-8 py-4.5 text-xs font-bold uppercase tracking-widest text-[var(--color-cream)]"
            >
              {sent ? 'Message Sent ✦' : (
                <>
                  Send Message <span>↗</span>
                </>
              )}
            </motion.button>
          </motion.form>

          <div className="grid content-start gap-3.5">
            <Reveal delay={0.1} className="rounded-[var(--radius-lg)] bg-[var(--color-sage-darker)] p-7 text-[var(--color-cream)]">
              <b className="text-base uppercase tracking-wide">Sourcing at scale?</b>
              <p className="mt-2 text-sm leading-relaxed text-[var(--color-sage-soft)]">
                Tell us what you need and our team will connect you with the right suppliers from our network.
              </p>
            </Reveal>
            {INFO.map((item, i) => (
              <Reveal
                key={item.label}
                delay={0.15 + i * 0.08}
                className="group flex items-center gap-4 rounded-[var(--radius-lg)] bg-[var(--color-sage-paper)] p-6 transition-all duration-500 hover:translate-x-2 hover:bg-[var(--color-sage-darker)]"
              >
                <div className="flex size-12 shrink-0 items-center justify-center rounded-2xl bg-[var(--color-sage)] text-[var(--color-cream)]">
                  <item.icon size={20} />
                </div>
                <div>
                  <h4 className="text-xs font-bold uppercase tracking-[0.14em] text-[var(--color-sage-deep)] transition-colors group-hover:text-[var(--color-sage-soft)]">
                    {item.label}
                  </h4>
                  <p className="mt-1 text-base font-medium text-[var(--color-ink)] transition-colors group-hover:text-[var(--color-cream)]">
                    {item.value}
                  </p>
                </div>
              </Reveal>
            ))}
          </div>
        </div>
      </div>
    </section>
  )
}
