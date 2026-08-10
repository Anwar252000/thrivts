import { forwardRef } from 'react'
import { motion, type HTMLMotionProps } from 'framer-motion'
import { cn } from '@/lib/utils'

export interface CardProps extends HTMLMotionProps<'div'> {
  hover?: boolean
}

/** The soft-shadow, large-radius card shell used everywhere in the thrivts portals. */
export const Card = forwardRef<HTMLDivElement, CardProps>(
  ({ className, hover = false, children, ...props }, ref) => (
    <motion.div
      ref={ref}
      initial={{ opacity: 0, y: 8 }}
      animate={{ opacity: 1, y: 0 }}
      transition={{ duration: 0.4, ease: [0.19, 1, 0.22, 1] }}
      whileHover={hover ? { y: -3, boxShadow: 'var(--shadow-lg)' } : undefined}
      className={cn(
        'rounded-[var(--radius-lg)] border border-[var(--color-line)] bg-[var(--color-white)] p-6 shadow-[var(--shadow-sm)] text-[var(--color-ink)]',
        className,
      )}
      {...props}
    >
      {children}
    </motion.div>
  ),
)
Card.displayName = 'Card'
