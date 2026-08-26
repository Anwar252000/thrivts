import { MessageSquare } from 'lucide-react'
import { PageTransition, Badge, Card, Spinner } from '@/components/ui'
import { EmptyState } from '@/components/admin'
import { formatDate } from '@/lib/utils'
import { useGetMessageThreadsQuery } from '@/features/buyer/buyerApi'

export function BuyerMessages() {
  const { data: threads, isLoading } = useGetMessageThreadsQuery()

  return (
    <PageTransition>
      <h1 className="mb-1 text-2xl font-semibold">Conversations with Thrivts</h1>
      <p className="mb-6 text-sm text-[var(--color-ink-faint)]">Our team responds within working hours.</p>

      {isLoading ? (
        <div className="flex justify-center py-10"><Spinner /></div>
      ) : !threads || threads.length === 0 ? (
        <EmptyState icon={MessageSquare} title="No messages yet" description="When the Thrivts team has anything to share about your requirements or deals, it appears here." />
      ) : (
        <div className="flex flex-col gap-2">
          {threads.map((t) => (
            <Card key={t.id} className="flex items-center justify-between gap-4">
              <div>
                <p className="font-semibold">{t.subject ?? 'Message'}</p>
                <p className="text-xs text-[var(--color-ink-faint)]">{t.lastMessageAt ? formatDate(t.lastMessageAt) : '—'}</p>
              </div>
              {t.unread && <Badge tone="warning">New</Badge>}
            </Card>
          ))}
        </div>
      )}
    </PageTransition>
  )
}
