import type { ReactNode } from 'react'
import { AnimatePresence, motion } from 'framer-motion'
import { X } from 'lucide-react'
import { cn } from '@/lib/utils'

interface ModalProps {
  open: boolean
  onClose: () => void
  title: string
  subtitle?: string
  children: ReactNode
  footer?: ReactNode
  size?: 'md' | 'lg'
}

/** Modal chrome matching admin.html's .modal-backdrop/.modal exactly — blurred backdrop, rise-in
 * panel, sticky header/footer. */
export function Modal({ open, onClose, title, subtitle, children, footer, size = 'md' }: ModalProps) {
  return (
    <AnimatePresence>
      {open && (
        <motion.div
          initial={{ opacity: 0 }}
          animate={{ opacity: 1 }}
          exit={{ opacity: 0 }}
          transition={{ duration: 0.2 }}
          className="fixed inset-0 z-[1500] flex items-center justify-center bg-[rgb(34,40,37,0.55)] p-4 backdrop-blur-sm"
          onClick={(e) => e.target === e.currentTarget && onClose()}
        >
          <motion.div
            initial={{ opacity: 0, y: 20 }}
            animate={{ opacity: 1, y: 0 }}
            exit={{ opacity: 0, y: 12 }}
            transition={{ duration: 0.3, ease: [0.19, 1, 0.22, 1] }}
            className={cn(
              'flex max-h-[88vh] w-full flex-col overflow-hidden rounded-[var(--radius-lg)] bg-[var(--color-white)] shadow-[var(--shadow-xl)]',
              size === 'lg' ? 'max-w-3xl' : 'max-w-xl',
            )}
          >
            <div className="sticky top-0 z-[1] flex items-center justify-between gap-4 border-b border-[var(--color-line)] bg-[var(--color-white)] p-5">
              <div>
                <h3 className="text-xl font-semibold tracking-tight">{title}</h3>
                {subtitle && <p className="mt-1 text-sm text-[var(--color-ink-faint)]">{subtitle}</p>}
              </div>
              <button
                onClick={onClose}
                aria-label="Close"
                className="flex size-8 shrink-0 items-center justify-center rounded-full text-[var(--color-ink-soft)] transition-colors hover:bg-[var(--color-sage-mist)] hover:text-[var(--color-ink)]"
              >
                <X size={18} />
              </button>
            </div>

            <div className="overflow-y-auto p-5">{children}</div>

            {footer && (
              <div className="sticky bottom-0 flex justify-end gap-3 border-t border-[var(--color-line)] bg-[var(--color-white)] p-4 px-5">
                {footer}
              </div>
            )}
          </motion.div>
        </motion.div>
      )}
    </AnimatePresence>
  )
}
