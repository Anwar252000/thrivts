import { motion } from 'framer-motion'
import { Check } from 'lucide-react'
import { ACCENT_PRESETS, useTheme } from '@/lib/theme-context'
import { cn } from '@/lib/utils'

/** Lets a user (e.g. admin) swap the accent color live — no reload, no rebuild. */
export function AccentPicker() {
  const { accent, setAccent } = useTheme()

  return (
    <div className="flex items-center gap-2">
      {ACCENT_PRESETS.map((preset) => {
        const active = preset.value.toLowerCase() === accent.toLowerCase()
        return (
          <button
            key={preset.value}
            type="button"
            aria-label={`Use ${preset.name} accent`}
            onClick={() => setAccent(preset.value)}
            className={cn(
              'relative flex size-7 items-center justify-center rounded-full ring-2 ring-offset-2 ring-offset-[var(--color-cream)] transition-transform hover:scale-110',
              active ? 'ring-[var(--color-ink)]' : 'ring-transparent',
            )}
            style={{ backgroundColor: preset.value }}
          >
            {active && (
              <motion.span
                initial={{ scale: 0 }}
                animate={{ scale: 1 }}
                transition={{ duration: 0.2, ease: [0.19, 1, 0.22, 1] }}
              >
                <Check size={14} className="text-white" />
              </motion.span>
            )}
          </button>
        )
      })}
    </div>
  )
}
