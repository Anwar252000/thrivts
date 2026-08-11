import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { Search, Check, X } from 'lucide-react'
import { Modal } from '@/components/admin'
import { Badge, Button, Input, Textarea } from '@/components/ui'
import { statusTone } from '@/features/admin/statusTone'
import { formatUsd, formatDate } from '@/lib/utils'
import {
  useBeginDisputeInvestigationMutation, useResolveDisputeMutation, useRejectDisputeMutation,
} from '@/features/admin/adminApi'
import type { DisputeListItem } from '@/features/admin/adminTypes'

interface DisputeDetailModalProps {
  dispute: DisputeListItem | null
  onClose: () => void
}

export function DisputeDetailModal({ dispute, onClose }: DisputeDetailModalProps) {
  const [beginInvestigation, investigateState] = useBeginDisputeInvestigationMutation()
  const [resolveDispute, resolveState] = useResolveDisputeMutation()
  const [rejectDispute, rejectState] = useRejectDisputeMutation()
  const [mode, setMode] = useState<'view' | 'resolve' | 'reject'>('view')

  const resolveForm = useForm<{ resolution: string; resolutionNotes: string; refundAmountUsd: number }>({
    defaultValues: { refundAmountUsd: dispute?.refundAmountUsd ?? 0 },
  })
  const rejectForm = useForm<{ resolutionNotes: string }>()

  if (!dispute) return null

  const handleResolve = resolveForm.handleSubmit(async (values) => {
    await resolveDispute({ disputeId: dispute.id, resolution: values.resolution, resolutionNotes: values.resolutionNotes, refundAmountUsd: Number(values.refundAmountUsd) })
    setMode('view')
    onClose()
  })

  const handleReject = rejectForm.handleSubmit(async (values) => {
    await rejectDispute({ disputeId: dispute.id, resolutionNotes: values.resolutionNotes })
    setMode('view')
    onClose()
  })

  return (
    <Modal open={!!dispute} onClose={onClose} title={dispute.disputeNumber} subtitle={dispute.category ?? undefined}>
      <div className="flex flex-col gap-5">
        <div className="flex items-center gap-2">
          <Badge tone={statusTone(dispute.status)}>{dispute.status}</Badge>
          {dispute.refundAmountUsd > 0 && <Badge tone="warning">Requested refund {formatUsd(dispute.refundAmountUsd)}</Badge>}
        </div>

        <div>
          <p className="mb-1 text-xs font-semibold uppercase tracking-wider text-[var(--color-ink-faint)]">Description</p>
          <p className="text-sm text-[var(--color-ink-soft)]">{dispute.description}</p>
        </div>

        <p className="text-xs text-[var(--color-ink-faint)]">Raised {formatDate(dispute.createdAt)}</p>

        {mode === 'view' && dispute.status !== 'Resolved' && dispute.status !== 'Rejected' && dispute.status !== 'Withdrawn' && (
          <div className="flex flex-wrap gap-2 border-t border-[var(--color-line)] pt-4">
            {dispute.status === 'Open' && (
              <Button size="sm" variant="outline" onClick={() => beginInvestigation(dispute.id)} disabled={investigateState.isLoading}>
                <Search size={14} /> Begin investigation
              </Button>
            )}
            <Button size="sm" onClick={() => setMode('resolve')}>
              <Check size={14} /> Resolve
            </Button>
            <Button size="sm" variant="outline" onClick={() => setMode('reject')}>
              <X size={14} /> Reject
            </Button>
          </div>
        )}

        {mode === 'resolve' && (
          <form onSubmit={handleResolve} className="flex flex-col gap-3 border-t border-[var(--color-line)] pt-4">
            <p className="text-xs font-semibold uppercase tracking-wider text-[var(--color-ink-faint)]">Resolve dispute</p>
            <Input placeholder="Resolution summary" {...resolveForm.register('resolution', { required: true })} />
            <Textarea rows={2} placeholder="Resolution notes" {...resolveForm.register('resolutionNotes')} />
            <div>
              <label className="mb-1 block text-xs font-medium text-[var(--color-ink-soft)]">Refund amount (USD)</label>
              <Input type="number" step="0.01" {...resolveForm.register('refundAmountUsd')} />
            </div>
            <div className="flex gap-2">
              <Button type="submit" size="sm" disabled={resolveState.isLoading}>{resolveState.isLoading ? 'Saving…' : 'Confirm resolution'}</Button>
              <Button type="button" size="sm" variant="outline" onClick={() => setMode('view')}>Cancel</Button>
            </div>
          </form>
        )}

        {mode === 'reject' && (
          <form onSubmit={handleReject} className="flex flex-col gap-3 border-t border-[var(--color-line)] pt-4">
            <p className="text-xs font-semibold uppercase tracking-wider text-[var(--color-ink-faint)]">Reject dispute</p>
            <Textarea rows={2} placeholder="Reason" {...rejectForm.register('resolutionNotes')} />
            <div className="flex gap-2">
              <Button type="submit" size="sm" variant="danger" disabled={rejectState.isLoading}>{rejectState.isLoading ? 'Saving…' : 'Confirm rejection'}</Button>
              <Button type="button" size="sm" variant="outline" onClick={() => setMode('view')}>Cancel</Button>
            </div>
          </form>
        )}
      </div>
    </Modal>
  )
}
