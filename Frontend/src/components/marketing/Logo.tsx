import { motion } from 'framer-motion'
import { cn } from '@/lib/utils'

interface LogoProps {
  className?: string
  sparkClassName?: string
  animateSpark?: boolean
}

/** The "thriv✦ts" wordmark — used in the nav, footer, and loader at different sizes. */
export function Logo({ className, sparkClassName, animateSpark }: LogoProps) {
  return (
    <span className={cn('inline-flex items-start font-black lowercase leading-none tracking-tight', className)}>
      thriv
      {animateSpark ? (
        <motion.span
          className={cn('mt-[0.16em] text-[0.4em] text-[var(--color-sage)]', sparkClassName)}
          animate={{ rotate: [0, 180, 360], scale: [1, 1.25, 1] }}
          transition={{ duration: 2.4, repeat: Infinity, ease: [0.19, 1, 0.22, 1] }}
        >
          ✦
        </motion.span>
      ) : (
        <span className={cn('mt-[0.16em] text-[0.4em] text-[var(--color-sage)]', sparkClassName)}>✦</span>
      )}
      ts
    </span>
  )
}
