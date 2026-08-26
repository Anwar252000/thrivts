import { useState } from 'react'
import { Trash2 } from 'lucide-react'
import { Modal, ConfirmDialog } from '@/components/admin'
import { Badge, Button, Input, Spinner } from '@/components/ui'
import { statusTone, humanizeStatus } from '@/features/admin/statusTone'
import { formatDate, formatUsd } from '@/lib/utils'
import { useGetMyRequirementsQuery, useDeleteRequirementMutation, useGetBidsQuery, useCounterBidMutation, useAcceptBidMutation } from '@/features/buyer/buyerApi'

const DELETABLE_STATUSES = new Set(['PendingReview', 'Posted', 'Matching', 'ReadyToOrder', 'Cancelled'])

interface RequirementDetailModalProps {
  requirementId: string | null
  onClose: () => void
}

export function RequirementDetailModal({ requirementId, onClose }: RequirementDetailModalProps) {
  const { data: requirements } = useGetMyRequirementsQuery()
  const [deleteRequirement] = useDeleteRequirementMutation()
  const [confirmDelete, setConfirmDelete] = useState(false)

  const requirement = requirements?.find((r) => r.id === requirementId)

  if (!requirementId) return null

  const handleDelete = async () => {
    await deleteRequirement(requirementId)
    setConfirmDelete(false)
    onClose()
  }

  return (
    <Modal open={!!requirementId} onClose={onClose} title={requirement ? `Requirement ${requirement.requirementNumber}` : 'Requirement'} size="lg">
      {!requirement ? (
        <div className="flex justify-center py-10">
          <Spinner />
        </div>
      ) : (
        <div className="flex flex-col gap-6">
          <div className="grid grid-cols-2 gap-4 text-sm">
            <Field label="Status" value={<Badge tone={statusTone(requirement.status)}>{humanizeStatus(requirement.status)}</Badge>} />
            <Field label="Item" value={requirement.itemName} />
            <Field label="Category" value={requirement.categoryName ?? '—'} />
            <Field label="Quantity" value={`${requirement.quantityPcs.toLocaleString()} pcs`} />
            <Field label="Grade" value={requirement.grade === 'AB' ? 'A/B' : requirement.grade} />
            <Field label="Target price" value={`${requirement.targetPricePerPc.toFixed(2)} ${requirement.currency} / pc`} />
            <Field label="Destination" value={requirement.destinationCountry} />
            <Field label="Posted" value={formatDate(requirement.createdAt)} />
          </div>

          <BidBoard requirementId={requirement.id} />

          {DELETABLE_STATUSES.has(requirement.status) && (
            <div className="flex justify-end border-t border-[var(--color-line)] pt-4">
              <Button size="sm" variant="danger" onClick={() => setConfirmDelete(true)}>
                <Trash2 size={14} /> Delete requirement
              </Button>
            </div>
          )}
        </div>
      )}

      <ConfirmDialog
        open={confirmDelete}
        onClose={() => setConfirmDelete(false)}
        onConfirm={handleDelete}
        title="Delete this requirement?"
        description="This cannot be undone."
        confirmLabel="Delete"
      />
    </Modal>
  )
}

function BidBoard({ requirementId }: { requirementId: string }) {
  const { data: bids, isLoading } = useGetBidsQuery(requirementId)
  const [counteringId, setCounteringId] = useState<string | null>(null)

  return (
    <div className="border-t border-[var(--color-line)] pt-5">
      <p className="mb-3 text-xs font-semibold uppercase tracking-wider text-[var(--color-ink-faint)]">Live bids</p>
      {isLoading ? (
        <div className="flex justify-center py-6"><Spinner /></div>
      ) : !bids || bids.length === 0 ? (
        <p className="rounded-[var(--radius-sm)] bg-[var(--color-sage-mist)] p-4 text-center text-sm text-[var(--color-ink-faint)]">
          No bids yet — sellers are reviewing your requirement. This board updates on refresh.
        </p>
      ) : (
        <div className="flex flex-col gap-2">
          {bids.map((b) => {
            const isAccepted = b.status === 'Accepted'
            const canAct = b.status === 'Pending'
            return (
              <div key={b.bidId} className={`rounded-[var(--radius-sm)] border p-3 text-sm ${isAccepted ? 'border-[var(--color-accent)]' : 'border-[var(--color-line)]'}`}>
                <div className="flex items-center justify-between gap-3">
                  <div>
                    <div className="flex items-center gap-2">
                      <span className="font-semibold">{b.sellerAlias}</span>
                      <span className="text-xs font-semibold uppercase tracking-wider text-[var(--color-accent)]">{b.sellerTier}</span>
                      {b.sellerVerified && <span className="text-xs font-semibold text-[var(--color-success)]">✓ Verified</span>}
                      {isAccepted && <Badge tone="success">Accepted</Badge>}
                    </div>
                    <p className="mt-0.5 text-xs text-[var(--color-ink-faint)]">
                      {b.availableQuantityPcs.toLocaleString()} pcs · {b.bidTime ? formatDate(b.bidTime) : ''}
                    </p>
                  </div>
                  <div className="text-right">
                    <p className="text-base font-bold">{formatUsd(b.buyerPricePerPcUsd)}<span className="text-xs font-normal text-[var(--color-ink-faint)]">/pc</span></p>
                    <p className="text-xs text-[var(--color-ink-faint)]">{formatUsd(b.buyerTotalUsd)} total</p>
                  </div>
                </div>

                {b.negotiationState !== 'Open' && (
                  <div className="mt-2 rounded-[var(--radius-sm)] bg-[var(--color-warning-soft)] px-3 py-1.5 text-xs text-[var(--color-warning)]">
                    {humanizeStatus(b.negotiationState)} {b.roundCount ? `· round ${b.roundCount}` : ''}
                  </div>
                )}

                {canAct && counteringId !== b.bidId && (
                  <div className="mt-2 flex justify-end gap-2">
                    <Button size="sm" variant="outline" onClick={() => setCounteringId(b.bidId)}>Counter</Button>
                    <AcceptButton bidId={b.bidId} requirementId={requirementId} />
                  </div>
                )}
                {canAct && counteringId === b.bidId && (
                  <CounterForm bidId={b.bidId} requirementId={requirementId} currentPrice={b.buyerPricePerPcUsd} onDone={() => setCounteringId(null)} />
                )}
              </div>
            )
          })}
        </div>
      )}
    </div>
  )
}

function AcceptButton({ bidId, requirementId }: { bidId: string; requirementId: string }) {
  const [acceptBid, { isLoading }] = useAcceptBidMutation()
  return (
    <Button size="sm" disabled={isLoading} onClick={() => acceptBid({ bidId, requirementId })}>
      {isLoading ? 'Accepting…' : 'Accept'}
    </Button>
  )
}

function CounterForm({ bidId, requirementId, currentPrice, onDone }: { bidId: string; requirementId: string; currentPrice: number; onDone: () => void }) {
  const [counterBid, { isLoading }] = useCounterBidMutation()
  const [price, setPrice] = useState(currentPrice.toFixed(2))

  const submit = async () => {
    const value = Number(price)
    if (!(value > 0)) return
    await counterBid({ bidId, requirementId, counterBuyerPriceUsd: value })
    onDone()
  }

  return (
    <div className="mt-2 flex items-center gap-2">
      <Input type="number" step="0.01" className="w-32" value={price} onChange={(e) => setPrice(e.target.value)} />
      <Button size="sm" variant="outline" onClick={onDone}>Cancel</Button>
      <Button size="sm" disabled={isLoading} onClick={submit}>{isLoading ? 'Sending…' : 'Send counter'}</Button>
    </div>
  )
}

function Field({ label, value }: { label: string; value: React.ReactNode }) {
  return (
    <div>
      <p className="text-xs font-semibold uppercase tracking-wider text-[var(--color-ink-faint)]">{label}</p>
      <p className="mt-0.5 font-medium">{value}</p>
    </div>
  )
}
