import { useEffect, useState } from 'react'
import { AnimatePresence, motion } from 'framer-motion'
import { Logo } from './Logo'

/** Full-screen splash — logo rises in, a line draws under it, then it fades to reveal the hero. */
export function Loader({ onDone }: { onDone?: () => void }) {
  const [done, setDone] = useState(false)

  useEffect(() => {
    const timer = setTimeout(() => {
      setDone(true)
      onDone?.()
    }, 1100)
    return () => clearTimeout(timer)
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [])

  return (
    <AnimatePresence>
      {!done && (
        <motion.div
          className="fixed inset-0 z-[10000] grid place-items-center bg-[var(--color-sage-darker)] text-[var(--color-cream)]"
          exit={{ opacity: 0 }}
          transition={{ duration: 0.8, ease: [0.19, 1, 0.22, 1] }}
        >
          <div className="text-center">
            <motion.div
              initial={{ y: '110%' }}
              animate={{ y: 0 }}
              transition={{ duration: 1, delay: 0.15, ease: [0.19, 1, 0.22, 1] }}
              className="overflow-hidden"
            >
              <Logo animateSpark className="text-[clamp(2.2rem,7vw,4.5rem)] text-[var(--color-cream)]" sparkClassName="text-[var(--color-sage-soft)]" />
            </motion.div>
            <motion.div
              initial={{ width: 0 }}
              animate={{ width: 200 }}
              transition={{ duration: 1.6, delay: 0.3, ease: [0.19, 1, 0.22, 1] }}
              className="mx-auto mt-6 h-0.5 bg-gradient-to-r from-transparent via-[var(--color-sage-soft)] to-transparent"
            />
          </div>
        </motion.div>
      )}
    </AnimatePresence>
  )
}
