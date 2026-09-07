import { useEffect, useRef, useState } from 'react'
import { PageTransition, Badge, Button, Spinner } from '@/components/ui'
import { EmptyState } from '@/components/admin'
import { formatDate, formatUsd } from '@/lib/utils'
import { statusTone } from '@/features/admin/statusTone'
import { useGetOffersQuery, useMarkOffersViewedMutation } from '@/features/seller/sellerApi'
import type { SellerOffer, OfferRound } from '@/features/seller/sellerTypes'
import { OfferRespondModal, type OfferAction } from './OfferRespondModal'

const TERMINAL: SellerOffer['status'][] = ['Accepted', 'Declined', 'Expired', 'Withdrawn']

function latestCounter(rounds: OfferRound[]): OfferRound | null {
  const counters = rounds.filter((r) => r.kind === 'counter')
  return counters.length ? counters[counters.length - 1] : null
}

/** Whose move it is. Before any counter, it's the seller's move; after one, it's the other party's. */
function isSellerTurn(rounds: OfferRound[]): boolean {
  const last = latestCounter(rounds)
  if (!last) return true
  return last.party !== 'Seller'
}

function standingPrice(offer: SellerOffer): number {
  if (offer.currentPricePerPc != null) return offer.currentPricePerPc
  const last = latestCounter(offer.rounds)
  if (last?.pricePerPcUsd != null) return last.pricePerPcUsd
  return offer.offerPricePerPc ?? 0
}

export function SellerOffers() {
  const { data, isLoading } = useGetOffersQuery()
  const [action, setAction] = useState<{ offer: SellerOffer; kind: OfferAction } | null>(null)
  const [markViewed] = useMarkOffersViewedMutation()
  const markedRef = useRef<Set<string>>(new Set())

  // Mirrors loadOffers()'s auto-mark-viewed side effect — flips still-unopened offers to
  // "Viewed" once the seller's board has loaded them. Guarded by markedRef so a re-render (or the
  // refetch this mutation itself triggers) never re-fires for the same offer twice.
  useEffect(() => {
    const unviewed = (data ?? []).filter((o) => o.status === 'Sent' && !markedRef.current.has(o.id)).map((o) => o.id)
    if (unviewed.length === 0) return
    unviewed.forEach((id) => markedRef.current.add(id))
    markViewed(unviewed)
  }, [data, markViewed])

  return (
    <PageTransition>
      <div className="mb-6">
        <h1 className="text-2xl font-semibold">Direct offers</h1>
        <p className="mt-1 text-sm text-[var(--color-ink-faint)]">Offers Thrivts has sent you directly, outside the open bid board.</p>
      </div>

      {isLoading ? (
        <div className="flex justify-center py-10"><Spinner /></div>
      ) : !data || data.length === 0 ? (
        <EmptyState title="No direct offers yet" />
      ) : (
        <div className="flex flex-col gap-3">
          {data.map((o) => (
            <OfferCard key={o.id} offer={o} onAct={(kind) => setAction({ offer: o, kind })} />
          ))}
        </div>
      )}

      <OfferRespondModal offer={action?.offer ?? null} action={action?.kind ?? null} onClose={() => setAction(null)} />
    </PageTransition>
  )
}

function OfferCard({ offer, onAct }: { offer: SellerOffer; onAct: (kind: OfferAction) => void }) {
  const expired = offer.expiresAt ? new Date(offer.expiresAt) < new Date() : false
  const terminal = TERMINAL.includes(offer.status)
  const myTurn = isSellerTurn(offer.rounds)
  const canRespond = !terminal && !expired && myTurn
  const standing = standingPrice(offer)

  return (
    <div className="rounded-[var(--radius-md)] border border-[var(--color-line)] bg-[var(--color-white)] p-4">
      <div className="flex items-start justify-between gap-3">
        <div>
          <p className="font-semibold">{offer.itemName ?? 'Offer'}</p>
          <p className="text-xs text-[var(--color-ink-faint)]">
            {offer.offerNumber ?? ''} · Sent {formatDate(offer.sentAt)}
            {expired && <span className="text-[var(--color-danger)]"> · expired</span>}
          </p>
        </div>
        <Badge tone={statusTone(offer.status)}>{offer.status}</Badge>
      </div>

      <div className="mt-3 grid grid-cols-2 gap-3 text-sm sm:grid-cols-4">
        <Field label="Quantity" value={`${(offer.quantityPcs ?? 0).toLocaleString()} pcs`} />
        <Field label="Grade" value={offer.grade ?? '—'} />
        <Field label={offer.rounds.some((r) => r.kind === 'counter') ? 'Standing price/pc' : 'Our offer/pc'} value={formatUsd(standing)} />
        <Field label="Expires" value={formatDate(offer.expiresAt)} />
      </div>

      {offer.adminNotes && (
        <p className="mt-3 rounded-[var(--radius-sm)] bg-[var(--color-sage-mist)] p-2.5 text-xs text-[var(--color-ink-faint)]">{offer.adminNotes}</p>
      )}

      {offer.rounds.length > 0 && <NegotiationThread rounds={offer.rounds} />}

      {offer.status === 'Accepted' && (
        <div className="mt-3 rounded-[var(--radius-sm)] bg-[var(--color-success-soft)] px-3 py-2 text-xs text-[var(--color-success)]">
          <strong>Match confirmed.</strong> The Thrivts team will contact you on WhatsApp within 24 hours to finalize the deal.
        </div>
      )}

      {!terminal && !expired && !myTurn && (
        <p className="mt-2 text-xs text-[var(--color-ink-faint)]">⏳ Waiting on Thrivts to respond to your counter.</p>
      )}

      {canRespond && (
        <div className="mt-3 flex flex-wrap justify-end gap-2">
          <Button size="sm" variant="outline" onClick={() => onAct('message')}>Message</Button>
          <Button size="sm" variant="outline" className="text-[var(--color-danger)]" onClick={() => onAct('decline')}>Decline</Button>
          <Button size="sm" variant="outline" onClick={() => onAct('counter')}>Counter</Button>
          <Button size="sm" onClick={() => onAct('accept')}>Accept {formatUsd(standing)}</Button>
        </div>
      )}
    </div>
  )
}

function NegotiationThread({ rounds }: { rounds: OfferRound[] }) {
  return (
    <div className="mt-3 border-t border-[var(--color-line)] pt-3">
      <p className="mb-2 text-[0.68rem] font-bold uppercase tracking-wider text-[var(--color-ink-faint)]">Negotiation</p>
      <div className="flex flex-col gap-1.5">
        {rounds.map((r) => {
          const mine = r.party === 'Seller'
          return (
            <div key={r.id} className={`flex ${mine ? 'justify-end' : ''}`}>
              <div className={`max-w-[80%] rounded-[var(--radius-sm)] border border-[var(--color-line)] px-2.5 py-1.5 text-sm ${mine ? 'bg-[var(--color-sage-mist)]' : 'bg-[var(--color-cream)]'}`}>
                <p className="text-[0.68rem] text-[var(--color-ink-faint)]">
                  {mine ? 'You' : 'Thrivts'} · {formatDate(r.createdAt)}
                  {r.kind === 'counter' && r.pricePerPcUsd != null && ` · ${formatUsd(r.pricePerPcUsd)}/pc`}
                </p>
                {r.notes && <p>{r.notes}</p>}
              </div>
            </div>
          )
        })}
      </div>
    </div>
  )
}

function Field({ label, value }: { label: string; value: string }) {
  return (
    <div>
      <p className="text-[0.68rem] font-semibold uppercase tracking-wider text-[var(--color-ink-faint)]">{label}</p>
      <p className="font-medium">{value}</p>
    </div>
  )
}
