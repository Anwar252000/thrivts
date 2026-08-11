import { useEffect, useState } from 'react'
import { useForm } from 'react-hook-form'
import { Send, MessageSquare } from 'lucide-react'
import { PageTransition, Badge, Button, Textarea, Spinner } from '@/components/ui'
import { EmptyState } from '@/components/admin'
import {
  useGetMessageThreadsQuery, useGetThreadMessagesQuery, useSendAdminMessageMutation, useMarkThreadReadMutation,
} from '@/features/admin/adminApi'
import { cn, formatDateTime } from '@/lib/utils'

export function AdminMessages() {
  const [selectedThread, setSelectedThread] = useState<string | null>(null)
  const { data: threads, isLoading } = useGetMessageThreadsQuery()
  const { data: messages, isLoading: messagesLoading } = useGetThreadMessagesQuery(selectedThread!, { skip: !selectedThread })
  const [sendMessage, { isLoading: sending }] = useSendAdminMessageMutation()
  const [markRead] = useMarkThreadReadMutation()

  const { register, handleSubmit, reset } = useForm<{ body: string }>()

  useEffect(() => {
    if (selectedThread) markRead(selectedThread)
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [selectedThread])

  const activeThread = threads?.find((t) => t.id === selectedThread)

  const onSend = handleSubmit(async (values) => {
    if (!selectedThread || !values.body.trim()) return
    await sendMessage({ threadId: selectedThread, body: values.body })
    reset()
  })

  return (
    <PageTransition>
      <h1 className="mb-1 text-2xl font-semibold">Messages</h1>
      <p className="mb-6 text-sm text-[var(--color-ink-faint)]">Support conversations with buyers, sellers, and agencies.</p>

      <div className="grid gap-4 lg:grid-cols-[320px_1fr]" style={{ height: 560 }}>
        <div className="overflow-y-auto rounded-[var(--radius-md)] border border-[var(--color-line)] bg-[var(--color-white)]">
          {isLoading ? (
            <div className="flex justify-center py-10">
              <Spinner />
            </div>
          ) : !threads || threads.length === 0 ? (
            <div className="p-4">
              <EmptyState icon={MessageSquare} title="No conversations yet" />
            </div>
          ) : (
            threads.map((t) => (
              <button
                key={t.id}
                onClick={() => setSelectedThread(t.id)}
                className={cn(
                  'flex w-full flex-col gap-1 border-b border-[var(--color-line-soft)] p-4 text-left transition-colors hover:bg-[var(--color-sage-mist)]',
                  selectedThread === t.id && 'bg-[var(--color-sage-mist)]',
                )}
              >
                <div className="flex items-center justify-between gap-2">
                  <span className="truncate text-sm font-semibold">{t.subject ?? `${t.participantRole} conversation`}</span>
                  {t.unreadCount > 0 && (
                    <span className="flex size-5 shrink-0 items-center justify-center rounded-full bg-[var(--color-accent)] text-[0.65rem] font-bold text-white">
                      {t.unreadCount}
                    </span>
                  )}
                </div>
                <div className="flex items-center gap-2 text-xs text-[var(--color-ink-faint)]">
                  <Badge tone="neutral">{t.participantRole}</Badge>
                  {t.lastMessageAt && <span>{formatDateTime(t.lastMessageAt)}</span>}
                </div>
              </button>
            ))
          )}
        </div>

        <div className="flex flex-col overflow-hidden rounded-[var(--radius-md)] border border-[var(--color-line)] bg-[var(--color-white)]">
          {!selectedThread ? (
            <div className="flex flex-1 items-center justify-center">
              <EmptyState icon={MessageSquare} title="Select a conversation" description="Pick a thread from the list to view messages." />
            </div>
          ) : (
            <>
              <div className="border-b border-[var(--color-line)] p-4">
                <p className="text-sm font-semibold">{activeThread?.subject ?? `${activeThread?.participantRole} conversation`}</p>
              </div>
              <div className="flex-1 space-y-3 overflow-y-auto p-4">
                {messagesLoading ? (
                  <div className="flex justify-center py-10">
                    <Spinner />
                  </div>
                ) : (
                  messages?.map((m) => (
                    <div key={m.id} className={cn('flex', m.senderType === 'Admin' ? 'justify-end' : 'justify-start')}>
                      <div
                        className={cn(
                          'max-w-[75%] rounded-[var(--radius-md)] px-4 py-2.5 text-sm',
                          m.senderType === 'Admin' ? 'bg-[var(--color-sage-dark)] text-[var(--color-cream)]' : 'bg-[var(--color-sage-mist)] text-[var(--color-ink)]',
                        )}
                      >
                        <p>{m.body}</p>
                        <p className={cn('mt-1 text-[0.68rem] opacity-70')}>{formatDateTime(m.createdAt)}</p>
                      </div>
                    </div>
                  ))
                )}
              </div>
              <form onSubmit={onSend} className="flex items-end gap-2 border-t border-[var(--color-line)] p-3">
                <Textarea rows={1} placeholder="Type a reply…" className="flex-1 resize-none" {...register('body')} />
                <Button type="submit" size="sm" disabled={sending}>
                  <Send size={14} />
                </Button>
              </form>
            </>
          )}
        </div>
      </div>
    </PageTransition>
  )
}
