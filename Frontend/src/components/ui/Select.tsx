import { forwardRef } from 'react'
import { ChevronDown } from 'lucide-react'
import { cn } from '@/lib/utils'

export const Select = forwardRef<HTMLSelectElement, React.SelectHTMLAttributes<HTMLSelectElement>>(
  ({ className, children, ...props }, ref) => (
    <div className="relative">
      <select
        ref={ref}
        className={cn(
          'h-11 w-full appearance-none rounded-[var(--radius-sm)] border border-[var(--color-line-strong)] bg-[var(--color-white)] px-4 pr-10 text-sm text-[var(--color-ink)]',
          'transition-shadow duration-200 ease-[var(--ease-thrivts)]',
          'focus:outline-none focus:ring-2 focus:ring-[var(--color-accent)] focus:ring-offset-0',
          'disabled:cursor-not-allowed disabled:opacity-50',
          className,
        )}
        {...props}
      >
        {children}
      </select>
      <ChevronDown
        size={16}
        className="pointer-events-none absolute right-3.5 top-1/2 -translate-y-1/2 text-[var(--color-ink-faint)]"
      />
    </div>
  ),
)
Select.displayName = 'Select'
