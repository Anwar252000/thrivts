import { forwardRef } from 'react'
import { Slot } from '@radix-ui/react-slot'
import { motion, type HTMLMotionProps } from 'framer-motion'
import { cva, type VariantProps } from 'class-variance-authority'
import { cn } from '@/lib/utils'

const buttonVariants = cva(
  'inline-flex items-center justify-center gap-2 whitespace-nowrap rounded-[var(--radius-sm)] font-medium transition-colors duration-200 ease-[var(--ease-thrivts)] disabled:pointer-events-none disabled:opacity-50 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-[var(--color-accent)]',
  {
    variants: {
      variant: {
        primary: 'bg-[var(--color-sage-dark)] text-[var(--color-cream)] shadow-[var(--shadow-xs)] hover:bg-[var(--color-sage-darker)]',
        accent: 'bg-[var(--color-accent)] text-white shadow-[var(--shadow-xs)] hover:brightness-95',
        outline: 'border border-[var(--color-line-strong)] text-[var(--color-ink)] hover:bg-[var(--color-sage-mist)]',
        ghost: 'text-[var(--color-ink-soft)] hover:bg-[var(--color-sage-mist)] hover:text-[var(--color-ink)]',
        danger: 'bg-[var(--color-danger)] text-white shadow-[var(--shadow-xs)] hover:brightness-95',
      },
      size: {
        sm: 'h-9 px-3 text-sm',
        md: 'h-11 px-5 text-sm',
        lg: 'h-13 px-7 text-base',
      },
    },
    defaultVariants: {
      variant: 'primary',
      size: 'md',
    },
  },
)

export interface ButtonProps
  extends Omit<HTMLMotionProps<'button'>, 'children'>,
    VariantProps<typeof buttonVariants> {
  children?: React.ReactNode
  /** Render as the single child element (e.g. a react-router `Link`) instead of a `<button>`. */
  asChild?: boolean
}

export const Button = forwardRef<HTMLButtonElement, ButtonProps>(
  ({ className, variant, size, asChild, children, ...props }, ref) => {
    if (asChild) {
      return (
        <Slot ref={ref} className={cn(buttonVariants({ variant, size }), className)}>
          {children}
        </Slot>
      )
    }

    return (
      <motion.button
        ref={ref}
        whileHover={{ scale: 1.015 }}
        whileTap={{ scale: 0.98 }}
        transition={{ duration: 0.15, ease: [0.19, 1, 0.22, 1] }}
        className={cn(buttonVariants({ variant, size }), className)}
        {...props}
      >
        {children}
      </motion.button>
    )
  },
)
Button.displayName = 'Button'
