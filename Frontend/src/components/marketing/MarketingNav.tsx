import { useEffect, useRef, useState } from 'react'
import { AnimatePresence, motion, useMotionValueEvent, useScroll } from 'framer-motion'
import { Link } from 'react-router-dom'
import { Menu, X } from 'lucide-react'
import { Logo } from './Logo'

const NAV_LINKS = [
  { href: '#about', label: 'Buyers' },
  { href: '#vendors', label: 'Sellers' },
  { href: '#app', label: 'App soon' },
  { href: '#about', label: 'About' },
  { href: '#contact', label: 'Contact' },
]

export function MarketingNav() {
  const [scrolled, setScrolled] = useState(false)
  const [hidden, setHidden] = useState(false)
  const [menuOpen, setMenuOpen] = useState(false)
  const lastY = useRef(0)
  const { scrollY } = useScroll()

  useMotionValueEvent(scrollY, 'change', (y) => {
    setScrolled(y > 30)
    setHidden(y > 500 && y > lastY.current)
    lastY.current = y
  })

  useEffect(() => {
    document.body.classList.toggle('overflow-hidden', menuOpen)
  }, [menuOpen])

  return (
    <>
      <motion.nav
        animate={{ y: hidden ? '-130%' : 0 }}
        transition={{ duration: 0.5, ease: [0.19, 1, 0.22, 1] }}
        className="fixed inset-x-0 top-0 z-[1000] px-3 py-5"
      >
        <div className="mx-auto w-[min(1480px,94vw)]">
          <div
            className={`flex items-center justify-between gap-8 rounded-full border px-3 py-2.5 pl-6 transition-all duration-500 ${
              scrolled
                ? 'border-[var(--color-line-soft)] bg-[var(--color-cream)]/80 shadow-[var(--shadow-sm)] backdrop-blur-xl'
                : 'border-transparent bg-transparent'
            }`}
          >
            <a href="#top" data-cursor-hover>
              <Logo className="text-2xl text-[var(--color-ink)]" />
            </a>

            <div className="ml-auto mr-2 hidden items-center gap-8 md:flex">
              {NAV_LINKS.map((link, i) => (
                <a
                  key={link.label + i}
                  href={link.href}
                  data-cursor-hover
                  className="group relative py-1.5 text-xs font-semibold uppercase tracking-widest text-[var(--color-ink-soft)] transition-colors hover:text-[var(--color-ink)]"
                >
                  {link.label}
                  {link.label === 'App soon' && (
                    <span className="ml-1.5 rounded bg-[var(--color-sage)]/15 px-1.5 py-0.5 text-[0.58rem] text-[var(--color-sage)]">
                      soon
                    </span>
                  )}
                  <span className="absolute inset-x-0 -bottom-0.5 h-px w-0 bg-[var(--color-sage)] transition-all duration-500 group-hover:w-full" />
                </a>
              ))}
            </div>

            <Link
              to="/login"
              data-cursor-hover
              className="hidden shrink-0 whitespace-nowrap rounded-full bg-[var(--color-sage-dark)] px-6 py-3 text-xs font-bold uppercase tracking-widest text-[var(--color-cream)] transition-all hover:-translate-y-0.5 hover:bg-[var(--color-sage)] md:inline-block"
            >
              Log In
            </Link>

            <button
              type="button"
              aria-label="Menu"
              onClick={() => setMenuOpen((o) => !o)}
              className="flex size-8 items-center justify-center md:hidden"
            >
              {menuOpen ? <X size={22} /> : <Menu size={22} />}
            </button>
          </div>
        </div>
      </motion.nav>

      <AnimatePresence>
        {menuOpen && (
          <motion.div
            initial={{ clipPath: 'circle(0% at calc(100% - 40px) 40px)' }}
            animate={{ clipPath: 'circle(150% at calc(100% - 40px) 40px)' }}
            exit={{ clipPath: 'circle(0% at calc(100% - 40px) 40px)' }}
            transition={{ duration: 0.8, ease: [0.19, 1, 0.22, 1] }}
            className="fixed inset-0 z-[990] flex flex-col justify-center gap-1 bg-[var(--color-sage-darker)] px-[9vw] text-[var(--color-cream)]"
          >
            {NAV_LINKS.map((link, i) => (
              <motion.a
                key={link.label + i}
                href={link.href}
                onClick={() => setMenuOpen(false)}
                initial={{ opacity: 0, y: 18 }}
                animate={{ opacity: 1, y: 0 }}
                transition={{ duration: 0.6, delay: 0.2 + i * 0.07, ease: [0.19, 1, 0.22, 1] }}
                className="py-3 text-[clamp(2rem,8vw,3rem)] font-extrabold uppercase leading-none tracking-tight"
              >
                {link.label}
              </motion.a>
            ))}
            <motion.div
              initial={{ opacity: 0 }}
              animate={{ opacity: 1 }}
              transition={{ duration: 0.6, delay: 0.55 }}
              className="mt-10 text-sm uppercase tracking-[0.2em] text-[var(--color-sage-soft)]"
            >
              Source · Grade · Grow
            </motion.div>
          </motion.div>
        )}
      </AnimatePresence>
    </>
  )
}
