import { useEffect, useRef, useState } from 'react'
import { AnimatePresence, motion } from 'framer-motion'
import { Bell, CheckCheck } from 'lucide-react'
import { useGetNotificationsQuery, useMarkNotificationsReadMutation } from '@/features/admin/adminApi'
import { formatDateTime } from '@/lib/utils'
import { cn } from '@/lib/utils'

/** Admin's own notification bell — same GET /api/v1/notifications every role's bell uses,
 * scoped server-side to the caller. Matches admin.html's header bell affordance. */
export function NotificationBell() {
  const [open, setOpen] = useState(false)
  const ref = useRef<HTMLDivElement>(null)
  const { data: notifications } = useGetNotificationsQuery({ take: 30 }, { pollingInterval: 30_000 })
  const [markRead] = useMarkNotificationsReadMutation()

  const unreadCount = notifications?.filter((n) => !n.isRead).length ?? 0

  useEffect(() => {
    if (!open) return
    const onClickOutside = (e: MouseEvent) => {
      if (ref.current && !ref.current.contains(e.target as Node)) setOpen(false)
    }
    document.addEventListener('mousedown', onClickOutside)
    return () => document.removeEventListener('mousedown', onClickOutside)
  }, [open])

  return (
    <div className="relative" ref={ref}>
      <button
        onClick={() => setOpen((v) => !v)}
        aria-label="Notifications"
        className="relative inline-flex h-9 w-9 items-center justify-center rounded-[var(--radius-sm)] border border-[var(--color-line-strong)] bg-[var(--color-white)] text-[var(--color-ink)] transition-colors hover:bg-[var(--color-sage-mist)]"
      >
        <Bell size={16} />
        {unreadCount > 0 && (
          <span className="absolute -right-1 -top-1 flex h-[16px] min-w-[16px] items-center justify-center rounded-full bg-[var(--color-danger)] px-1 text-[0.62rem] font-bold text-white">
            {unreadCount > 9 ? '9+' : unreadCount}
          </span>
        )}
      </button>

      <AnimatePresence>
        {open && (
          <motion.div
            initial={{ opacity: 0, y: -8 }}
            animate={{ opacity: 1, y: 0 }}
            exit={{ opacity: 0, y: -8 }}
            transition={{ duration: 0.15 }}
            className="absolute right-0 top-11 z-[1400] w-80 overflow-hidden rounded-[var(--radius-md)] border border-[var(--color-line)] bg-[var(--color-white)] shadow-[var(--shadow-xl)]"
          >
            <div className="flex items-center justify-between border-b border-[var(--color-line)] px-4 py-3">
              <p className="text-sm font-semibold">Notifications</p>
              {unreadCount > 0 && (
                <button
                  onClick={() => markRead(undefined)}
                  className="flex items-center gap-1 text-xs font-medium text-[var(--color-accent)] hover:underline"
                >
                  <CheckCheck size={13} /> Mark all read
                </button>
              )}
            </div>

            <div className="max-h-96 overflow-y-auto">
              {!notifications || notifications.length === 0 ? (
                <p className="px-4 py-6 text-center text-sm text-[var(--color-ink-faint)]">No notifications yet.</p>
              ) : (
                notifications.map((n) => (
                  <button
                    key={n.id}
                    onClick={() => !n.isRead && markRead([n.id])}
                    className={cn(
                      'flex w-full flex-col gap-0.5 border-b border-[var(--color-line)] px-4 py-3 text-left transition-colors last:border-b-0 hover:bg-[var(--color-sage-mist)]',
                      !n.isRead && 'bg-[var(--color-cream)]',
                    )}
                  >
                    <div className="flex items-center gap-1.5">
                      {!n.isRead && <span className="size-1.5 shrink-0 rounded-full bg-[var(--color-accent)]" />}
                      <p className="truncate text-[0.82rem] font-semibold">{n.title}</p>
                    </div>
                    <p className="line-clamp-2 text-xs text-[var(--color-ink-soft)]">{n.body}</p>
                    <p className="text-[0.68rem] text-[var(--color-ink-faint)]">{formatDateTime(n.createdAt)}</p>
                  </button>
                ))
              )}
            </div>
          </motion.div>
        )}
      </AnimatePresence>
    </div>
  )
}
