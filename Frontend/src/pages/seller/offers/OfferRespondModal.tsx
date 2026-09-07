import { useState } from 'react'
import { Modal } from '@/components/admin'
import { Button, Input, Textarea } from '@/components/ui'
import { formatUsd } from '@/lib/utils'
import { useRespondToOfferMutation } from '@/features/seller/sellerApi'
import type { SellerOffer } from '@/features/seller/sellerTypes'

export type OfferAction = 'accept' | 'decline' | 'counter' | 'message'

const TITLES: Record<OfferAction, string> = {
  accept: 'Accept offer',
  decline: 'Decline offer',
  counter: 'Counter offer',
  message: 'Send message',
}

function standingPrice(offer: SellerOffer): number {
  if (offer.currentPricePerPc != null) return offer.currentPricePerPc
  return offer.offerPricePerPc ?? 0
}

interface OfferRespondModalProps {
  offer: SellerOffer | null
  action: OfferAction | null
  onClose: () => void
}

export function OfferRespondModal({ offer, action, onClose }: OfferRespondModalProps) {
  if (!offer || !action) return null

  const standing = standingPrice(offer)

  return (
    <Modal
      open={!!offer}
      onClose={onClose}
      title={TITLES[action]}
      subtitle={`${offer.itemName ?? 'Offer'} · ${(offer.quantityPcs ?? 0).toLocaleString()} pcs · standing ${formatUsd(standing)}/pc`}
    >
      <OfferRespondForm key={`${offer.id}-${action}`} offer={offer} action={action} onClose={onClose} />
    </Modal>
  )
}

function OfferRespondForm({ offer, action, onClose }: { offer: SellerOffer; action: OfferAction; onClose: () => void }) {
  const [respond, { isLoading }] = useRespondToOfferMutation()
  const [price, setPrice] = useState(() => standingPrice(offer).toFixed(2))
  const [notes, setNotes] = useState('')
  const [error, setError] = useState<string | null>(null)
  const standing = standingPrice(offer)

  const submit = async () => {
    setError(null)
    if (action === 'counter' && !(Number(price) > 0)) { setError('Enter a valid counter price.'); return }
    if (action === 'message' && !notes.trim()) { setError('Write a message first.'); return }

    try {
      await respond({
        offerId: offer.id,
        action,
        pricePerPcUsd: action === 'counter' ? Number(price) : undefined,
        notes: action === 'counter' || action === 'message' ? (notes.trim() || undefined) : undefined,
      }).unwrap()
      onClose()
    } catch {
      setError('Could not save your response — please try again.')
    }
  }

  return (
    <div className="flex flex-col gap-4">
      {action === 'counter' && (
        <div>
          <label className="mb-1.5 block text-xs font-semibold uppercase tracking-wider text-[var(--color-ink-soft)]">Your counter price (USD/pc)</label>
          <Input type="number" step="0.01" min={0} value={price} onChange={(e) => setPrice(e.target.value)} />
        </div>
      )}
      {(action === 'counter' || action === 'message') && (
        <div>
          <label className="mb-1.5 block text-xs font-semibold uppercase tracking-wider text-[var(--color-ink-soft)]">
            {action === 'message' ? 'Message to Thrivts' : 'Notes (optional)'}
          </label>
          <Textarea rows={3} value={notes} onChange={(e) => setNotes(e.target.value)} />
        </div>
      )}
      {action === 'accept' && <p className="text-sm text-[var(--color-ink-soft)]">Accept the standing price of {formatUsd(standing)}/pc for this offer?</p>}
      {action === 'decline' && <p className="text-sm text-[var(--color-ink-soft)]">Decline this offer? This cannot be undone.</p>}

      {error && <div className="rounded-[var(--radius-sm)] bg-[var(--color-danger-soft)] px-3 py-2 text-xs text-[var(--color-danger)]">{error}</div>}

      <div className="flex justify-end gap-3">
        <Button variant="outline" onClick={onClose}>Cancel</Button>
        <Button variant={action === 'decline' ? 'danger' : 'primary'} disabled={isLoading} onClick={submit}>
          {isLoading ? 'Sending…' : action === 'accept' ? `Accept ${formatUsd(standing)}` : action === 'decline' ? 'Confirm decline' : action === 'message' ? 'Send' : 'Send counter'}
        </Button>
      </div>
    </div>
  )
}
