import { forwardRef } from 'react'
import { cn } from '@/lib/utils'

export const Input = forwardRef<HTMLInputElement, React.InputHTMLAttributes<HTMLInputElement>>(
  ({ className, ...props }, ref) => (
    <input
      ref={ref}
      className={cn(
        'h-11 w-full rounded-[var(--radius-sm)] border border-[var(--color-line-strong)] bg-[var(--color-white)] px-4 text-sm text-[var(--color-ink)]',
        'transition-shadow duration-200 ease-[var(--ease-thrivts)] placeholder:text-[var(--color-ink-faint)]',
        'focus:outline-none focus:ring-2 focus:ring-[var(--color-accent)] focus:ring-offset-0',
        'disabled:cursor-not-allowed disabled:opacity-50',
        className,
      )}
      {...props}
    />
  ),
)
Input.displayName = 'Input'
