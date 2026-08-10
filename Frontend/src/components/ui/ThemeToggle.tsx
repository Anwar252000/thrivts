import { Moon, Sun } from 'lucide-react'
import { motion } from 'framer-motion'
import { useTheme } from '@/lib/theme-context'

export function ThemeToggle() {
  const { mode, toggleMode } = useTheme()
  const isDark = mode === 'dark'

  return (
    <button
      type="button"
      onClick={toggleMode}
      aria-label="Toggle theme"
      className="relative flex size-10 items-center justify-center rounded-full border border-[var(--color-line-strong)] text-[var(--color-ink-soft)] transition-colors hover:bg-[var(--color-sage-mist)]"
    >
      <motion.span
        key={mode}
        initial={{ rotate: -90, opacity: 0 }}
        animate={{ rotate: 0, opacity: 1 }}
        transition={{ duration: 0.25, ease: [0.19, 1, 0.22, 1] }}
      >
        {isDark ? <Moon size={18} /> : <Sun size={18} />}
      </motion.span>
    </button>
  )
}
