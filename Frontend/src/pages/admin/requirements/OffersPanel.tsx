import { useMemo, useState } from 'react'
import { Send, MessageSquare, Check, X, Trash2, Handshake } from 'lucide-react'
import { Badge, Button, Input, Select, Spinner } from '@/components/ui'
import { statusTone, humanizeStatus } from '@/features/admin/statusTone'
import { formatUsd } from '@/lib/utils'
import {
  useGetOffersQuery, useGetSellersQuery, useCreateOfferMutation, useGetOfferRoundsQuery,
  usePostOfferRoundMutation, useAcceptOfferMutation, useDeclineOfferMutation, useDeleteOfferMutation,
} from '@/features/admin/adminApi'
import { CreateDealModal } from './CreateDealModal'

interface OffersPanelProps {
  requirementId: string
  requirementSellerCost?: number | null
  requirementQuantityPcs?: number
}

const TERMINAL: string[] = ['Accepted', 'Declined', 'Expired', 'Withdrawn']

/** Replaces admin.html's "Direct Offers Sent" section, embedded in the requirement detail view. */
export function OffersPanel({ requirementId, requirementSellerCost, requirementQuantityPcs }: OffersPanelProps) {
  const { data: offers, isLoading } = useGetOffersQuery({ requirementId })
  const { data: sellersPage } = useGetSellersQuery({ pageSize: 200 })
  const [createOffer, { isLoading: sending }] = useCreateOfferMutation()
  const [deleteOffer] = useDeleteOfferMutation()

  const [sendingOpen, setSendingOpen] = useState(false)
  const [sellerId, setSellerId] = useState('')
  const [qty, setQty] = useState(String(requirementQuantityPcs ?? ''))
  const [price, setPrice] = useState('')
  const [notes, setNotes] = useState('')
  const [expiresInDays, setExpiresInDays] = useState('7')
  const [negotiatingId, setNegotiatingId] = useState<string | null>(null)
  const [dealCtx, setDealCtx] = useState<{ requirementId: string; sellerId: string; defaultQty: number; defaultSellerCost: number; sourceOfferId: string } | null>(null)

  const sellerNameById = useMemo(() => {
    const map = new Map<string, string>()
    sellersPage?.items.forEach((s) => map.set(s.id, s.companyName ?? s.publicAlias))
    return map
  }, [sellersPage])

  const submitOffer = async () => {
    if (!sellerId || !qty || Number(qty) <= 0 || !price || Number(price) <= 0) return
    await createOffer({
      requirementId, sellerId, quantityPcs: Number(qty), offerPricePerPc: Number(price),
      notes: notes.trim() || undefined, expiresInDays: Number(expiresInDays) || 7,
    })
    setSendingOpen(false)
    setSellerId('')
    setQty(String(requirementQuantityPcs ?? ''))
    setPrice('')
    setNotes('')
    setExpiresInDays('7')
  }

  return (
    <div>
      <div className="mb-2 flex items-center justify-between">
        <p className="text-xs font-semibold uppercase tracking-wider text-[var(--color-ink-faint)]">Direct offers sent</p>
        <Button size="sm" variant="outline" onClick={() => setSendingOpen((v) => !v)}>
          <Send size={14} /> Send offer
        </Button>
      </div>

      {sendingOpen && (
        <div className="mb-3 flex flex-wrap items-end gap-2 rounded-lg border border-[var(--color-line)] p-3">
          <div className="min-w-[220px] flex-1">
            <label className="mb-1 block text-xs font-medium text-[var(--color-ink-soft)]">Seller</label>
            <Select value={sellerId} onChange={(e) => setSellerId(e.target.value)}>
              <option value="">— select —</option>
              {sellersPage?.items.map((s) => (
                <option key={s.id} value={s.id}>
                  {s.companyName ?? s.publicAlias} · {s.tier} · {s.locationCity}, {s.locationCountry}
                </option>
              ))}
            </Select>
          </div>
          <div className="w-28">
            <label className="mb-1 block text-xs font-medium text-[var(--color-ink-soft)]">Quantity (pcs)</label>
            <Input type="number" min="1" value={qty} onChange={(e) => setQty(e.target.value)} />
          </div>
          <div className="w-36">
            <label className="mb-1 block text-xs font-medium text-[var(--color-ink-soft)]">Offer price/pc (USD)</label>
            <Input type="number" step="0.01" min="0.01" placeholder="What you'll pay seller" value={price} onChange={(e) => setPrice(e.target.value)} />
          </div>
          <div className="w-28">
            <label className="mb-1 block text-xs font-medium text-[var(--color-ink-soft)]">Expires (days)</label>
            <Input type="number" min="1" max="30" value={expiresInDays} onChange={(e) => setExpiresInDays(e.target.value)} />
          </div>
          <div className="min-w-[220px] flex-1">
            <label className="mb-1 block text-xs font-medium text-[var(--color-ink-soft)]">Notes to seller (optional)</label>
            <Input value={notes} onChange={(e) => setNotes(e.target.value)} />
          </div>
          <Button size="sm" onClick={submitOffer} disabled={sending || !sellerId || !qty || !price}>
            {sending ? 'Sending…' : 'Send'}
          </Button>
        </div>
      )}

      {isLoading ? (
        <div className="flex justify-center py-6"><Spinner /></div>
      ) : !offers || offers.length === 0 ? (
        <p className="text-sm text-[var(--color-ink-faint)]">No direct offers sent.</p>
      ) : (
        <div className="flex flex-col gap-2">
          {offers.map((o) => {
            const standing = o.currentPricePerPc ?? o.offerPricePerPc ?? 0
            const terminal = TERMINAL.includes(o.status)
            return (
              <div key={o.id} className="rounded-lg border border-[var(--color-line)] p-3 text-sm">
                <div className="flex items-start justify-between">
                  <div>
                    <div className="font-medium">{sellerNameById.get(o.sellerId) ?? 'Seller'}</div>
                    <div className="text-xs text-[var(--color-ink-faint)]">{o.offerNumber ?? ''}</div>
                  </div>
                  <Badge tone={statusTone(o.status)}>{humanizeStatus(o.status)}</Badge>
                </div>
                <div className="mt-2 grid grid-cols-2 gap-3">
                  <Field label="Quantity" value={o.quantityPcs != null ? `${o.quantityPcs.toLocaleString()} pcs` : '—'} />
                  <Field label="Expires" value={o.expiresAt ? new Date(o.expiresAt).toLocaleDateString() : '—'} />
                  <Field label="Offered/pc" value={formatUsd(o.offerPricePerPc ?? 0)} />
                  <Field label="Standing/pc" value={formatUsd(standing)} />
                </div>
                {o.adminNotes && <p className="mt-2 rounded bg-[var(--color-sage-mist)] px-2.5 py-1.5 text-xs text-[var(--color-ink-soft)]">{o.adminNotes}</p>}
                <div className="mt-2 flex flex-wrap justify-end gap-2">
                  {!terminal && (
                    <Button size="sm" variant="outline" onClick={() => setNegotiatingId(negotiatingId === o.id ? null : o.id)}>
                      <Handshake size={14} /> Negotiate / chat
                    </Button>
                  )}
                  {o.status === 'Accepted' && !o.dealId && (
                    <Button
                      size="sm"
                      onClick={() =>
                        setDealCtx({
                          requirementId,
                          sellerId: o.sellerId,
                          defaultQty: o.quantityPcs ?? 1,
                          defaultSellerCost: requirementSellerCost ?? standing,
                          sourceOfferId: o.id,
                        })
                      }
                    >
                      Create deal →
                    </Button>
                  )}
                  {!o.dealId && (
                    <Button size="sm" variant="danger" onClick={() => deleteOffer(o.id)}>
                      <Trash2 size={14} />
                    </Button>
                  )}
                </div>
                {negotiatingId === o.id && <NegotiationThread offerId={o.id} standing={standing} terminal={terminal} />}
              </div>
            )
          })}
        </div>
      )}

      <CreateDealModal ctx={dealCtx} onClose={() => setDealCtx(null)} />
    </div>
  )
}

function NegotiationThread({ offerId, standing, terminal }: { offerId: string; standing: number; terminal: boolean }) {
  const { data: rounds, isLoading } = useGetOfferRoundsQuery(offerId)
  const [postRound, { isLoading: posting }] = usePostOfferRoundMutation()
  const [acceptOffer] = useAcceptOfferMutation()
  const [declineOffer] = useDeclineOfferMutation()
  const [counterPrice, setCounterPrice] = useState(String(standing))
  const [notes, setNotes] = useState('')

  return (
    <div className="mt-3 border-t border-[var(--color-line)] pt-3">
      {isLoading ? (
        <Spinner className="size-4" />
      ) : (
        <div className="mb-2 flex max-h-48 flex-col gap-1.5 overflow-y-auto">
          {(rounds ?? []).length === 0 && <p className="text-xs text-[var(--color-ink-faint)]">No messages yet.</p>}
          {rounds?.map((r) => (
            <div key={r.id} className={`rounded-lg border border-[var(--color-line)] px-2.5 py-1.5 text-xs ${r.party === 'Admin' ? 'ml-8 bg-[var(--color-cream)]' : 'mr-8 bg-white'}`}>
              <div className="mb-0.5 text-[var(--color-ink-faint)]">
                {r.party === 'Admin' ? 'You (Thrivts)' : 'Seller'} · {r.kind === 'counter' && r.pricePerPcUsd != null ? formatUsd(r.pricePerPcUsd) : ''}
              </div>
              {r.notes && <div>{r.notes}</div>}
            </div>
          ))}
        </div>
      )}

      {terminal ? (
        <p className="text-xs text-[var(--color-ink-faint)]">This offer is closed.</p>
      ) : (
        <div className="flex flex-col gap-2">
          <div className="flex gap-2">
            <Input type="number" step="0.0001" className="w-32" value={counterPrice} onChange={(e) => setCounterPrice(e.target.value)} />
            <Input placeholder="Message / notes (optional)" value={notes} onChange={(e) => setNotes(e.target.value)} />
          </div>
          <div className="flex flex-wrap justify-end gap-2">
            <Button size="sm" variant="danger" onClick={() => declineOffer({ offerId, reason: notes || 'No reason given' })}>
              <X size={14} /> Decline
            </Button>
            <Button size="sm" variant="outline" disabled={!notes || posting} onClick={() => postRound({ offerId, kind: 'message', notes })}>
              <MessageSquare size={14} /> Send message
            </Button>
            <Button size="sm" variant="outline" disabled={!counterPrice || posting} onClick={() => postRound({ offerId, kind: 'counter', pricePerPcUsd: Number(counterPrice), notes: notes || undefined })}>
              Send counter
            </Button>
            <Button size="sm" onClick={() => acceptOffer(offerId)}>
              <Check size={14} /> Accept {formatUsd(standing)}
            </Button>
          </div>
        </div>
      )}
    </div>
  )
}

function Field({ label, value }: { label: string; value: string }) {
  return (
    <div>
      <p className="text-[10px] font-semibold uppercase tracking-wider text-[var(--color-ink-faint)]">{label}</p>
      <p className="font-medium">{value}</p>
    </div>
  )
}
