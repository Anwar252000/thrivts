import { useState } from 'react'
import { AlertTriangle } from 'lucide-react'
import { Modal } from '@/components/admin'
import { Badge, Button, Input, Select, Textarea, Spinner } from '@/components/ui'
import { statusTone, humanizeStatus } from '@/features/admin/statusTone'
import { formatDate, formatUsd } from '@/lib/utils'
import {
  useGetMyDealsQuery, useRaiseDisputeMutation,
  useGetPurchaseOrderQuery, useRequestPaymentLinkMutation, useMarkPoPaidMutation,
} from '@/features/buyer/buyerApi'
import type { MyDealListItem } from '@/features/buyer/buyerTypes'

const CLOSED_STATUSES = new Set(['Cancelled'])

const TIMELINE_STAGES: { key: MyDealListItem['status']; label: string; ts: (d: MyDealListItem) => string | null }[] = [
  { key: 'Confirmed', label: 'Confirmed', ts: (d) => d.confirmedAt },
  { key: 'Paid', label: 'Payment received', ts: (d) => d.paidAt },
  { key: 'InFulfillment', label: 'In fulfillment', ts: (d) => d.inFulfillmentAt },
  { key: 'Dispatched', label: 'Dispatched', ts: (d) => d.dispatchedAt },
  { key: 'Delivered', label: 'Delivered', ts: (d) => d.deliveredAt },
  { key: 'Settled', label: 'Settled', ts: (d) => d.settledAt },
]
const TIMELINE_ORDER = TIMELINE_STAGES.map((s) => s.key)

const DISPUTE_CATEGORIES = [
  { value: 'quality', label: 'Quality issue' },
  { value: 'quantity', label: 'Quantity / count issue' },
  { value: 'damage', label: 'Damage in transit' },
  { value: 'wrong_item', label: 'Wrong item received' },
  { value: 'late_delivery', label: 'Late delivery' },
  { value: 'other', label: 'Other' },
]

interface DealDetailModalProps {
  dealId: string | null
  onClose: () => void
}

export function DealDetailModal({ dealId, onClose }: DealDetailModalProps) {
  const { data: deals } = useGetMyDealsQuery()
  const [raisingIssue, setRaisingIssue] = useState(false)
  const deal = deals?.find((d) => d.id === dealId)

  if (!dealId) return null

  return (
    <Modal open={!!dealId} onClose={onClose} title={deal ? `Deal ${deal.dealNumber}` : 'Deal'}>
      {!deal ? (
        <div className="flex justify-center py-10">
          <Spinner />
        </div>
      ) : raisingIssue ? (
        <DisputeForm dealId={deal.id} onDone={onClose} onCancel={() => setRaisingIssue(false)} />
      ) : (
        <div className="flex flex-col gap-6">
          <div className="grid grid-cols-2 gap-4 text-sm">
            <Field label="Status" value={<Badge tone={statusTone(deal.status)}>{humanizeStatus(deal.status)}</Badge>} />
            <Field label="Requirement" value={deal.requirementNumber} />
            <Field label="Item" value={deal.itemName} />
            <Field label="Grade" value={deal.grade === 'AB' ? 'A/B' : deal.grade} />
            <Field label="Quantity" value={`${deal.totalQuantityPcs.toLocaleString()} pcs`} />
            <Field label="Destination" value={deal.destinationCountry} />
            <Field label="Shipping" value={deal.shippingMode ? humanizeStatus(deal.shippingMode) : 'Sea freight'} />
            <Field label="Total invoice" value={formatUsd(deal.totalInvoiceUsd)} />
            <Field label="Confirmed" value={formatDate(deal.createdAt)} />
          </div>

          {deal.status !== 'Cancelled' && (
            <div className="border-t border-[var(--color-line)] pt-4">
              <p className="mb-2 text-xs font-semibold uppercase tracking-wider text-[var(--color-ink-faint)]">Fulfilment timeline</p>
              <div className="flex flex-col gap-1">
                {TIMELINE_STAGES.map((s) => {
                  const ts = s.ts(deal)
                  const currentIndex = TIMELINE_ORDER.indexOf(deal.status)
                  const stageIndex = TIMELINE_ORDER.indexOf(s.key)
                  const done = !!ts || stageIndex < currentIndex
                  const current = s.key === deal.status
                  return (
                    <div key={s.key} className={`flex items-center gap-2 py-0.5 text-sm ${current ? 'font-semibold' : ''}`}>
                      <span className={done ? 'text-[var(--color-success)]' : current ? 'text-[var(--color-accent)]' : 'text-[var(--color-ink-faint)]'}>
                        {done ? '●' : current ? '◐' : '○'}
                      </span>
                      <span className="flex-1">{s.label}</span>
                      <span className="text-xs text-[var(--color-ink-faint)]">{ts ? formatDate(ts) : ''}</span>
                    </div>
                  )
                })}
              </div>
            </div>
          )}

          {deal.trackingNumber && (
            <div className="rounded-[var(--radius-sm)] bg-[var(--color-sage-mist)] p-2.5 text-sm">
              <strong>Shipping:</strong> {deal.courier} · {deal.trackingNumber}
              {deal.trackingUrl && (
                <>
                  <br />
                  <a href={deal.trackingUrl} target="_blank" rel="noopener noreferrer" className="text-xs text-[var(--color-accent)] underline">Open tracking →</a>
                </>
              )}
            </div>
          )}

          <PurchaseOrderPanel dealId={deal.id} />

          <div className="flex justify-end border-t border-[var(--color-line)] pt-4">
            {deal.hasDispute ? (
              <Badge tone="danger">Issue under review</Badge>
            ) : CLOSED_STATUSES.has(deal.status) ? null : (
              <Button size="sm" variant="danger" onClick={() => setRaisingIssue(true)}>
                <AlertTriangle size={14} /> Raise an issue
              </Button>
            )}
          </div>
        </div>
      )}
    </Modal>
  )
}

function DisputeForm({ dealId, onDone, onCancel }: { dealId: string; onDone: () => void; onCancel: () => void }) {
  const [raiseDispute, { isLoading }] = useRaiseDisputeMutation()
  const [category, setCategory] = useState('quality')
  const [description, setDescription] = useState('')
  const [resolution, setResolution] = useState('')
  const [error, setError] = useState<string | null>(null)

  const submit = async () => {
    if (!description.trim()) {
      setError('Please describe the issue.')
      return
    }
    setError(null)
    const result = await raiseDispute({ dealId, description, category, requestedResolution: resolution || undefined })
    if ('error' in result && result.error) {
      setError('Could not submit — please try again.')
      return
    }
    onDone()
  }

  return (
    <div className="flex flex-col gap-4">
      <p className="text-sm text-[var(--color-ink-soft)]">Tell us what's wrong with this deal. We'll review and respond within 24 hours.</p>

      <div>
        <label className="mb-1 block text-xs font-medium text-[var(--color-ink-soft)]">Category</label>
        <Select value={category} onChange={(e) => setCategory(e.target.value)}>
          {DISPUTE_CATEGORIES.map((c) => (
            <option key={c.value} value={c.value}>{c.label}</option>
          ))}
        </Select>
      </div>

      <div>
        <label className="mb-1 block text-xs font-medium text-[var(--color-ink-soft)]">Describe the issue *</label>
        <Textarea rows={4} placeholder="What happened?" value={description} onChange={(e) => setDescription(e.target.value)} />
      </div>

      <div>
        <label className="mb-1 block text-xs font-medium text-[var(--color-ink-soft)]">What outcome are you looking for?</label>
        <Textarea rows={2} placeholder="Refund, replacement, credit, etc." value={resolution} onChange={(e) => setResolution(e.target.value)} />
      </div>

      {error && <p className="text-xs text-[var(--color-danger)]">{error}</p>}

      <div className="flex justify-end gap-2">
        <Button variant="outline" onClick={onCancel}>Cancel</Button>
        <Button onClick={submit} disabled={isLoading}>{isLoading ? 'Submitting…' : 'Submit issue'}</Button>
      </div>
    </div>
  )
}

/** Mirrors buyer.html's PO panel: shown regardless of deal status once a PO exists (issued
 * automatically the moment the deal is confirmed). Lets the buyer request a payment link or
 * upload a receipt while the PO is still open, and shows where things stand otherwise. */
function PurchaseOrderPanel({ dealId }: { dealId: string }) {
  const { data: po, isLoading } = useGetPurchaseOrderQuery(dealId)
  const [requestLink, { isLoading: requestingLink }] = useRequestPaymentLinkMutation()
  const [markPaid, { isLoading: uploading }] = useMarkPoPaidMutation()
  const [receiptFile, setReceiptFile] = useState<File | null>(null)
  const [error, setError] = useState<string | null>(null)

  if (isLoading || !po) return null

  const uploadReceipt = async () => {
    if (!receiptFile) {
      setError('Choose a receipt file first.')
      return
    }
    setError(null)
    const result = await markPaid({ dealId, receipt: receiptFile })
    if ('error' in result && result.error) setError('Could not upload — please try again.')
    else setReceiptFile(null)
  }

  return (
    <div className="rounded-[var(--radius-md)] border border-[var(--color-line)] p-4">
      <div className="mb-2 flex items-center justify-between">
        <p className="text-xs font-semibold uppercase tracking-wider text-[var(--color-ink-faint)]">Purchase order {po.poNumber}</p>
        <Badge tone={po.status === 'Verified' ? 'success' : po.status === 'PaymentSubmitted' ? 'info' : 'warning'}>{humanizeStatus(po.status)}</Badge>
      </div>
      <div className="grid grid-cols-2 gap-3 text-sm">
        <Field label="Amount due" value={formatUsd(po.amountUsd)} />
        <Field label="Due by" value={formatDate(po.dueAt)} />
      </div>

      {po.status === 'Issued' && (
        <div className="mt-3 flex flex-col gap-3 border-t border-[var(--color-line)] pt-3">
          {po.paymentLinkUrl ? (
            <a href={po.paymentLinkUrl} target="_blank" rel="noopener noreferrer">
              <Button size="sm" variant="outline" type="button">Open payment link →</Button>
            </a>
          ) : (
            <Button size="sm" variant="outline" onClick={() => requestLink(dealId)} disabled={requestingLink}>
              {requestingLink ? 'Requesting…' : 'Request a payment link'}
            </Button>
          )}

          <div>
            <label className="mb-1 block text-xs font-medium text-[var(--color-ink-soft)]">Already paid? Upload your receipt</label>
            <div className="flex gap-2">
              <Input type="file" accept="image/*,application/pdf" onChange={(e) => setReceiptFile(e.target.files?.[0] ?? null)} />
              <Button size="sm" onClick={uploadReceipt} disabled={uploading}>{uploading ? 'Uploading…' : 'Upload'}</Button>
            </div>
            {error && <p className="mt-1 text-xs text-[var(--color-danger)]">{error}</p>}
          </div>
        </div>
      )}

      {po.status === 'PaymentSubmitted' && (
        <p className="mt-3 border-t border-[var(--color-line)] pt-3 text-sm text-[var(--color-ink-soft)]">
          Payment submitted — awaiting verification from our team.
        </p>
      )}

      {po.status === 'Verified' && (
        <p className="mt-3 border-t border-[var(--color-line)] pt-3 text-sm text-[var(--color-success)]">✓ Payment verified.</p>
      )}
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
