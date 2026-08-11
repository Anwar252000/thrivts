import { useState } from 'react'
import { Modal } from './Modal'
import { Button, Textarea } from '@/components/ui'

interface ConfirmDialogProps {
  open: boolean
  onClose: () => void
  onConfirm: (reason?: string) => void | Promise<void>
  title: string
  description: string
  /** When set, shows a required reason textarea and passes its value to onConfirm. */
  requireReason?: boolean
  confirmLabel?: string
  tone?: 'danger' | 'primary'
  loading?: boolean
}

/** Destructive-action confirmation — reject/cancel/delete flows across the admin portal. */
export function ConfirmDialog({
  open, onClose, onConfirm, title, description, requireReason, confirmLabel = 'Confirm', tone = 'danger', loading,
}: ConfirmDialogProps) {
  const [reason, setReason] = useState('')

  const handleConfirm = () => {
    onConfirm(requireReason ? reason : undefined)
  }

  return (
    <Modal
      open={open}
      onClose={onClose}
      title={title}
      footer={
        <>
          <Button variant="outline" onClick={onClose}>
            Cancel
          </Button>
          <Button
            variant={tone === 'danger' ? 'danger' : 'primary'}
            onClick={handleConfirm}
            disabled={loading || (requireReason && !reason.trim())}
          >
            {loading ? 'Working…' : confirmLabel}
          </Button>
        </>
      }
    >
      <p className="text-sm text-[var(--color-ink-soft)]">{description}</p>
      {requireReason && (
        <Textarea
          className="mt-4"
          rows={3}
          placeholder="Reason…"
          value={reason}
          onChange={(e) => setReason(e.target.value)}
          autoFocus
        />
      )}
    </Modal>
  )
}
