import { useState } from 'react'
import { AlertTriangle } from 'lucide-react'
import { Modal } from '@/components/admin'
import { Badge, Button, Select, Textarea, Spinner } from '@/components/ui'
import { statusTone, humanizeStatus } from '@/features/admin/statusTone'
import { formatDate, formatUsd } from '@/lib/utils'
import { useGetMyDealsQuery, useRaiseDisputeMutation } from '@/features/buyer/buyerApi'

const CLOSED_STATUSES = new Set(['Cancelled'])

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

function Field({ label, value }: { label: string; value: React.ReactNode }) {
  return (
    <div>
      <p className="text-xs font-semibold uppercase tracking-wider text-[var(--color-ink-faint)]">{label}</p>
      <p className="mt-0.5 font-medium">{value}</p>
    </div>
  )
}
