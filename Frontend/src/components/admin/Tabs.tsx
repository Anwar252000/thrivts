import { motion } from 'framer-motion'
import { cn } from '@/lib/utils'

export interface TabOption<T extends string> {
  value: T
  label: string
  count?: number
}

interface TabsProps<T extends string> {
  value: T
  onChange: (value: T) => void
  options: TabOption<T>[]
  className?: string
}

/** Segmented pill control matching admin.html's .tab-row/.tab-btn. */
export function Tabs<T extends string>({ value, onChange, options, className }: TabsProps<T>) {
  return (
    <div className={cn('inline-flex gap-0.5 rounded-full bg-[var(--color-sage-mist)] p-1', className)}>
      {options.map((option) => (
        <button
          key={option.value}
          onClick={() => onChange(option.value)}
          className={cn(
            'relative whitespace-nowrap rounded-full px-4 py-1.5 text-[0.7rem] font-semibold uppercase tracking-wider transition-colors',
            value === option.value ? 'text-[var(--color-ink)]' : 'text-[var(--color-ink-soft)] hover:text-[var(--color-ink)]',
          )}
        >
          {value === option.value && (
            <motion.span
              layoutId="active-tab-pill"
              className="absolute inset-0 rounded-full bg-[var(--color-white)] shadow-[var(--shadow-xs)]"
              transition={{ duration: 0.25, ease: [0.19, 1, 0.22, 1] }}
            />
          )}
          <span className="relative z-10">
            {option.label}
            {option.count !== undefined && <span className="ml-1.5 opacity-70">{option.count}</span>}
          </span>
        </button>
      ))}
    </div>
  )
}
