import type { LucideIcon } from 'lucide-react'
import { motion } from 'framer-motion'
import { Card } from '@/components/ui'

interface StatCardProps {
  label: string
  value: string
  icon: LucideIcon
  tone?: 'accent' | 'success' | 'warning' | 'info'
}

const toneClasses: Record<NonNullable<StatCardProps['tone']>, string> = {
  accent: 'bg-[var(--color-accent-soft)] text-[var(--color-accent)]',
  success: 'bg-[var(--color-success-soft)] text-[var(--color-success)]',
  warning: 'bg-[var(--color-warning-soft)] text-[var(--color-warning)]',
  info: 'bg-[var(--color-info-soft)] text-[var(--color-info)]',
}

export function StatCard({ label, value, icon: Icon, tone = 'accent' }: StatCardProps) {
  return (
    <Card hover className="flex items-center gap-4">
      <span className={`flex size-11 shrink-0 items-center justify-center rounded-[var(--radius-sm)] ${toneClasses[tone]}`}>
        <Icon size={20} />
      </span>
      <div>
        <motion.p
          key={value}
          initial={{ opacity: 0, y: 4 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ duration: 0.3 }}
          className="text-2xl font-semibold leading-none"
        >
          {value}
        </motion.p>
        <p className="mt-1 text-xs text-[var(--color-ink-faint)]">{label}</p>
      </div>
    </Card>
  )
}
