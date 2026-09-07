import { useState } from 'react'
import { Modal } from '@/components/admin'
import { Button, Input, Textarea } from '@/components/ui'
import { formatUsd } from '@/lib/utils'
import { useGetMyQuotesQuery, useSubmitQuoteMutation } from '@/features/seller/sellerApi'
import type { MyQuote, OpenRequirement } from '@/features/seller/sellerTypes'

/** Matches seller.html's THRIVTS_FEE_PER_PC_USD reference — the backend resolves the seller's
 * actual frozen fee (which may differ via a per-seller override), so this is a preview only. */
const FEE_PER_PC_USD = 0.7

interface QuoteModalProps {
  requirement: OpenRequirement | null
  onClose: () => void
}

export function QuoteModal({ requirement, onClose }: QuoteModalProps) {
  const { data: quotes } = useGetMyQuotesQuery()
  const existing = requirement ? quotes?.find((q) => q.requirementId === requirement.id) : undefined

  return (
    <Modal
      open={!!requirement}
      onClose={onClose}
      title={existing ? 'Update quote' : 'Submit quote'}
      subtitle={requirement ? `${requirement.itemName} · ${requirement.quantityPcs.toLocaleString()} pcs needed` : undefined}
    >
      {requirement && (
        <QuoteForm
          key={`${requirement.id}-${existing?.bidId ?? 'new'}`}
          requirement={requirement}
          existing={existing ?? null}
          onClose={onClose}
        />
      )}
    </Modal>
  )
}

function QuoteForm({ requirement, existing, onClose }: { requirement: OpenRequirement; existing: MyQuote | null; onClose: () => void }) {
  const [submitQuote, { isLoading }] = useSubmitQuoteMutation()
  const [qty, setQty] = useState(() => String(existing ? existing.availableQuantityPcs : requirement.quantityPcs))
  const [price, setPrice] = useState(() => (existing ? existing.yourPricePerPcUsd.toFixed(2) : ''))
  const [notes, setNotes] = useState(() => existing?.sellerNotes ?? '')
  const [error, setError] = useState<string | null>(null)

  const qtyNum = Number(qty) || 0
  const priceNum = Number(price) || 0
  const netPerPc = Math.max(priceNum - FEE_PER_PC_USD, 0)
  const netTotal = netPerPc * qtyNum

  const handleSubmit = async () => {
    if (!(qtyNum >= 1)) { setError('Quantity must be at least 1.'); return }
    if (!(priceNum > 0)) { setError('Enter a valid price.'); return }
    setError(null)
    try {
      await submitQuote({ requirementId: requirement.id, availableQuantityPcs: qtyNum, pricePerPcUsd: priceNum, sellerNotes: notes || undefined }).unwrap()
      onClose()
    } catch {
      setError('Could not save your quote — please try again.')
    }
  }

  return (
    <div className="flex flex-col gap-4">
      <div className="grid grid-cols-2 gap-3">
        <div>
          <label className="mb-1.5 block text-xs font-semibold uppercase tracking-wider text-[var(--color-ink-soft)]">Quantity you can supply</label>
          <Input type="number" min={1} value={qty} onChange={(e) => setQty(e.target.value)} />
        </div>
        <div>
          <label className="mb-1.5 block text-xs font-semibold uppercase tracking-wider text-[var(--color-ink-soft)]">Your price per piece (USD)</label>
          <Input type="number" step="0.01" min={0} value={price} onChange={(e) => setPrice(e.target.value)} />
        </div>
      </div>

      <div>
        <label className="mb-1.5 block text-xs font-semibold uppercase tracking-wider text-[var(--color-ink-soft)]">Notes (optional)</label>
        <Textarea rows={3} maxLength={500} value={notes} onChange={(e) => setNotes(e.target.value)} />
        <p className="mt-1 text-right text-xs text-[var(--color-ink-faint)]">{notes.length} / 500</p>
      </div>

      {priceNum > 0 && qtyNum > 0 && (
        <div className="rounded-[var(--radius-sm)] border border-[var(--color-line)] bg-[var(--color-sage-mist)] p-3 text-sm">
          <Row label="Your price" value={`${formatUsd(priceNum)}/pc`} />
          <Row label="Platform fee (reference)" value={`– ${formatUsd(FEE_PER_PC_USD)}/pc`} muted />
          <Row label="You receive" value={`${formatUsd(netPerPc)}/pc`} strong />
          <Row label={`Net total (${qtyNum.toLocaleString()} pcs)`} value={formatUsd(netTotal)} muted />
        </div>
      )}

      {error && <div className="rounded-[var(--radius-sm)] bg-[var(--color-danger-soft)] px-3 py-2 text-xs text-[var(--color-danger)]">{error}</div>}

      <div className="flex justify-end gap-3">
        <Button variant="outline" onClick={onClose}>Cancel</Button>
        <Button disabled={isLoading} onClick={handleSubmit}>{isLoading ? 'Saving…' : existing ? 'Update quote' : 'Submit quote'}</Button>
      </div>
    </div>
  )
}

function Row({ label, value, strong, muted }: { label: string; value: string; strong?: boolean; muted?: boolean }) {
  return (
    <div className={`flex justify-between ${strong ? 'mt-1.5 border-t border-[var(--color-line)] pt-1.5 font-bold' : ''}`}>
      <span className={muted ? 'text-[var(--color-ink-faint)]' : ''}>{label}</span>
      <span>{value}</span>
    </div>
  )
}
