import { useState } from 'react'
import { PageTransition, Badge, Button, Input, Spinner } from '@/components/ui'
import { EmptyState } from '@/components/admin'
import { formatDate, formatUsd } from '@/lib/utils'
import { useGetMyQuotesQuery, useRespondToBuyerCounterMutation } from '@/features/seller/sellerApi'
import type { MyQuote, NegotiationState } from '@/features/seller/sellerTypes'

const STATE_TONE: Record<NegotiationState, 'accent' | 'neutral' | 'danger' | 'success'> = {
  Open: 'neutral',
  CounteredByBuyer: 'accent',
  CounteredBySeller: 'neutral',
  Declined: 'danger',
  Accepted: 'success',
}

export function SellerQuotes() {
  const { data, isLoading } = useGetMyQuotesQuery()

  return (
    <PageTransition>
      <div className="mb-6">
        <h1 className="text-2xl font-semibold">My quotes</h1>
        <p className="mt-1 text-sm text-[var(--color-ink-faint)]">Every bid you've placed, and the buyer's counters if any are on the table.</p>
      </div>

      {isLoading ? (
        <div className="flex justify-center py-10"><Spinner /></div>
      ) : !data || data.length === 0 ? (
        <EmptyState title="No bids submitted yet" />
      ) : (
        <div className="flex flex-col gap-3">
          {data.map((q) => <QuoteCard key={q.bidId} quote={q} />)}
        </div>
      )}
    </PageTransition>
  )
}

function QuoteCard({ quote }: { quote: MyQuote }) {
  const buyerCountered = quote.negotiationState === 'CounteredByBuyer' && quote.buyerCounterPriceUsd != null

  return (
    <div className="rounded-[var(--radius-md)] border border-[var(--color-line)] bg-[var(--color-white)] p-4">
      <div className="flex items-start justify-between gap-3">
        <div>
          <p className="font-semibold">{quote.itemName}</p>
          <p className="text-xs text-[var(--color-ink-faint)]">{quote.requirementNumber} · Bid {formatDate(quote.respondedAt)}</p>
        </div>
        <Badge tone={STATE_TONE[quote.negotiationState]}>{quote.statusLabel}</Badge>
      </div>

      <div className="mt-3 grid grid-cols-2 gap-3 text-sm sm:grid-cols-4">
        <Field label="Your qty" value={`${quote.availableQuantityPcs.toLocaleString()} of ${quote.requirementQtyPcs.toLocaleString()}`} />
        <Field label="Your price/pc" value={formatUsd(quote.yourPricePerPcUsd)} />
        <Field label="You receive/pc" value={formatUsd(quote.youReceivePerPcUsd)} strong />
        <Field label="Net total" value={formatUsd(quote.youReceiveTotalUsd)} />
      </div>

      {buyerCountered && <BuyerCounterPanel quote={quote} />}

      {quote.negotiationState === 'CounteredBySeller' && (
        <div className="mt-3 rounded-[var(--radius-sm)] bg-[var(--color-sage-mist)] px-3 py-2 text-xs text-[var(--color-ink-faint)]">
          You countered at <strong className="text-[var(--color-ink)]">{formatUsd(quote.yourPricePerPcUsd)}/pc</strong> — waiting for the buyer to respond.
        </div>
      )}

      {quote.negotiationState === 'Declined' && (
        <div className="mt-3 rounded-[var(--radius-sm)] bg-[var(--color-danger-soft)] px-3 py-2 text-xs text-[var(--color-danger)]">
          You declined the buyer's counter. Your original bid is no longer active.
        </div>
      )}

      {quote.status === 'Accepted' && (
        <div className="mt-3 rounded-[var(--radius-sm)] bg-[var(--color-success-soft)] px-3 py-2 text-xs text-[var(--color-success)]">
          <strong>Deal confirmed.</strong> The Thrivts team will contact you on WhatsApp within 24 hours to finalize.
        </div>
      )}
    </div>
  )
}

function BuyerCounterPanel({ quote }: { quote: MyQuote }) {
  const [respond, { isLoading }] = useRespondToBuyerCounterMutation()
  const [countering, setCountering] = useState(false)
  const [price, setPrice] = useState(String(quote.buyerCounterPriceUsd ?? 0))

  return (
    <div className="mt-3 rounded-[var(--radius-sm)] border border-[var(--color-accent-soft)] bg-[var(--color-accent-soft)] p-3">
      <p className="mb-1.5 text-[0.68rem] font-bold uppercase tracking-wider text-[var(--color-accent)]">Buyer countered</p>
      <div className="flex justify-between text-sm">
        <span className="text-[var(--color-ink-faint)]">Their offer (your side)</span>
        <span className="font-bold">{formatUsd(quote.buyerCounterPriceUsd ?? 0)}/pc</span>
      </div>
      <div className="flex justify-between text-sm">
        <span className="text-[var(--color-ink-faint)]">You would receive</span>
        <span className="font-bold">{formatUsd(quote.counterYouReceivePerPc ?? 0)}/pc</span>
      </div>
      {quote.buyerCounterNote && <p className="mt-2 text-xs italic text-[var(--color-ink-faint)]">"{quote.buyerCounterNote}"</p>}

      {countering ? (
        <div className="mt-3 flex items-center gap-2">
          <Input type="number" step="0.01" className="w-32" value={price} onChange={(e) => setPrice(e.target.value)} />
          <Button size="sm" variant="outline" onClick={() => setCountering(false)}>Cancel</Button>
          <Button
            size="sm"
            disabled={isLoading}
            onClick={async () => {
              await respond({ bidId: quote.bidId, action: 'counter', newPriceUsd: Number(price) }).unwrap()
              setCountering(false)
            }}
          >
            {isLoading ? 'Sending…' : 'Send counter'}
          </Button>
        </div>
      ) : (
        <div className="mt-3 flex flex-wrap justify-end gap-2">
          <Button size="sm" variant="outline" disabled={isLoading} onClick={() => respond({ bidId: quote.bidId, action: 'decline' })}>Decline</Button>
          <Button size="sm" variant="outline" disabled={isLoading} onClick={() => setCountering(true)}>Counter back</Button>
          <Button size="sm" disabled={isLoading} onClick={() => respond({ bidId: quote.bidId, action: 'accept' })}>Accept counter</Button>
        </div>
      )}
    </div>
  )
}

function Field({ label, value, strong }: { label: string; value: string; strong?: boolean }) {
  return (
    <div>
      <p className="text-[0.68rem] font-semibold uppercase tracking-wider text-[var(--color-ink-faint)]">{label}</p>
      <p className={strong ? 'font-bold' : 'font-medium'}>{value}</p>
    </div>
  )
}
