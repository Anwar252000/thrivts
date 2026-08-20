import { useState } from 'react'
import { Modal } from '@/components/admin'
import { Button, Input, Spinner, Textarea } from '@/components/ui'
import { formatUsd } from '@/lib/utils'
import { useCreateDealFromMatchMutation, useGetRequirementByIdQuery, useGetSellerByIdQuery } from '@/features/admin/adminApi'
import type { RequirementDetail, SellerDetail } from '@/features/admin/adminTypes'

interface DealMatchContext {
  requirementId: string
  sellerId: string
  defaultQty: number
  defaultSellerCost: number
  sourceResponseId?: string | null
  sourceOfferId?: string | null
}

interface CreateDealModalProps {
  ctx: DealMatchContext | null
  onClose: () => void
  onCreated?: (dealId: string) => void
}

/** Manual "create deal from match" override — used when accepting a direct offer doesn't itself
 * create a deal (AdminAcceptOfferCommand only marks the offer accepted; matches admin.html's
 * two-step accept → openCreateDealFromOffer → submitCreateDeal flow). */
export function CreateDealModal({ ctx, onClose, onCreated }: CreateDealModalProps) {
  const { data: requirement } = useGetRequirementByIdQuery(ctx?.requirementId ?? '', { skip: !ctx })
  const { data: seller } = useGetSellerByIdQuery(ctx?.sellerId ?? '', { skip: !ctx })

  if (!ctx) return null

  return (
    <Modal open={!!ctx} onClose={onClose} title="Create deal from match" subtitle={requirement?.itemName}>
      {!requirement || !seller ? (
        <div className="flex justify-center py-10">
          <Spinner />
        </div>
      ) : (
        <CreateDealForm key={`${ctx.requirementId}:${ctx.sellerId}`} ctx={ctx} requirement={requirement} seller={seller} onClose={onClose} onCreated={onCreated} />
      )}
    </Modal>
  )
}

function CreateDealForm({
  ctx, requirement, seller, onClose, onCreated,
}: {
  ctx: DealMatchContext; requirement: RequirementDetail; seller: SellerDetail; onClose: () => void; onCreated?: (dealId: string) => void
}) {
  const [createDeal, { isLoading }] = useCreateDealFromMatchMutation()

  const [qty, setQty] = useState(ctx.defaultQty)
  const [buyerPrice, setBuyerPrice] = useState(requirement.buyerTargetPriceUsd)
  const [sellerCost, setSellerCost] = useState(ctx.defaultSellerCost)
  const [shipping, setShipping] = useState(0)
  const [dispatchDate, setDispatchDate] = useState('')
  const [notes, setNotes] = useState('')
  const [error, setError] = useState<string | null>(null)

  const subtotal = buyerPrice * qty
  const invoiceTotal = subtotal + shipping
  const spread = (buyerPrice - sellerCost) * qty - shipping
  const commission = spread * 0.3

  const submit = async () => {
    setError(null)
    const result = await createDeal({
      requirementId: ctx.requirementId,
      sellerId: ctx.sellerId,
      finalQuantityPcs: qty,
      buyerPricePerPcUsd: buyerPrice,
      sellerCostPerPcUsd: sellerCost,
      shippingCostUsd: shipping,
      sourceResponseId: ctx.sourceResponseId ?? null,
      sourceOfferId: ctx.sourceOfferId ?? null,
      estimatedDispatchDate: dispatchDate || null,
      adminNotes: notes || null,
    })
    if ('error' in result && result.error) {
      setError('Could not create the deal — check the values and try again.')
      return
    }
    if ('data' in result && result.data) onCreated?.(result.data.id)
    onClose()
  }

  return (
    <div className="flex flex-col gap-4">
      <div className="rounded-lg bg-[var(--color-cream)] p-3 text-sm">
        <div className="font-medium">
          {requirement.itemName} · {requirement.grade} → {requirement.destinationCountry}
        </div>
        <div className="text-xs text-[var(--color-ink-faint)]">Seller: {seller.companyName ?? seller.publicAlias}</div>
      </div>

      <div className="grid grid-cols-2 gap-3">
        <LabeledInput label="Final quantity (pcs)" type="number" min={1} value={qty} onChange={(e) => setQty(Number(e.target.value))} />
        <LabeledInput label="Buyer pays / pc (USD)" type="number" step="0.0001" value={buyerPrice} onChange={(e) => setBuyerPrice(Number(e.target.value))} />
        <LabeledInput label="Seller cost / pc (USD)" type="number" step="0.0001" value={sellerCost} onChange={(e) => setSellerCost(Number(e.target.value))} />
        <LabeledInput label="Shipping cost (USD)" type="number" step="0.01" value={shipping} onChange={(e) => setShipping(Number(e.target.value))} />
        <div className="col-span-2">
          <label className="mb-1 block text-xs font-medium text-[var(--color-ink-soft)]">Estimated dispatch date</label>
          <Input type="date" value={dispatchDate} onChange={(e) => setDispatchDate(e.target.value)} />
        </div>
        <div className="col-span-2">
          <label className="mb-1 block text-xs font-medium text-[var(--color-ink-soft)]">Admin notes (optional)</label>
          <Textarea rows={2} value={notes} onChange={(e) => setNotes(e.target.value)} />
        </div>
      </div>

      <div className="rounded-lg bg-[var(--color-warning-soft,#FFF8E7)] p-3 text-sm">
        <div>Subtotal: <span className="font-medium">{formatUsd(subtotal)}</span></div>
        <div>Total invoice (incl. shipping): <span className="font-medium">{formatUsd(invoiceTotal)}</span></div>
        <div>Spread (your margin): <span className="font-medium text-[var(--color-success)]">{formatUsd(spread)}</span></div>
        <div>Commission to agency (30%): <span className="font-medium">{formatUsd(commission)}</span></div>
      </div>

      {error && <p className="text-xs text-[var(--color-danger)]">{error}</p>}

      <div className="flex justify-end gap-2">
        <Button variant="outline" onClick={onClose}>Cancel</Button>
        <Button onClick={submit} disabled={isLoading}>{isLoading ? 'Creating…' : 'Create deal'}</Button>
      </div>
    </div>
  )
}

function LabeledInput({ label, ...props }: { label: string } & React.InputHTMLAttributes<HTMLInputElement>) {
  return (
    <div>
      <label className="mb-1 block text-xs font-medium text-[var(--color-ink-soft)]">{label}</label>
      <Input {...props} />
    </div>
  )
}
