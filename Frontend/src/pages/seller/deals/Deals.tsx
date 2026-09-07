import { PageTransition, Badge, Spinner } from '@/components/ui'
import { EmptyState } from '@/components/admin'
import { statusTone, humanizeStatus } from '@/features/admin/statusTone'
import { formatDate, formatUsd } from '@/lib/utils'
import { useGetMyDealsQuery } from '@/features/seller/sellerApi'
import type { SellerDeal } from '@/features/seller/sellerTypes'

const STAGES: { key: SellerDeal['status']; label: string; ts: (d: SellerDeal) => string | null }[] = [
  { key: 'Confirmed', label: 'Confirmed', ts: (d) => d.confirmedAt },
  { key: 'Paid', label: 'Payment received', ts: (d) => d.paidAt },
  { key: 'InFulfillment', label: 'In fulfillment', ts: (d) => d.inFulfillmentAt },
  { key: 'Dispatched', label: 'Dispatched', ts: (d) => d.dispatchedAt },
  { key: 'Delivered', label: 'Delivered', ts: (d) => d.deliveredAt },
  { key: 'Settled', label: 'Settled (paid out)', ts: (d) => d.settledAt },
]
const STAGE_ORDER = STAGES.map((s) => s.key)

export function SellerDeals() {
  const { data, isLoading } = useGetMyDealsQuery()

  return (
    <PageTransition>
      <div className="mb-6">
        <h1 className="text-2xl font-semibold">My deals</h1>
        <p className="mt-1 text-sm text-[var(--color-ink-faint)]">Accepted quotes and offers turn into deals once Thrivts confirms the buyer details.</p>
      </div>

      {isLoading ? (
        <div className="flex justify-center py-10"><Spinner /></div>
      ) : !data || data.length === 0 ? (
        <EmptyState title="No deals yet" description="Accepted offers turn into deals once Thrivts confirms the buyer details." />
      ) : (
        <div className="flex flex-col gap-3">
          {data.map((d) => <DealRow key={d.dealId} deal={d} />)}
        </div>
      )}
    </PageTransition>
  )
}

function DealRow({ deal }: { deal: SellerDeal }) {
  const currentIndex = STAGE_ORDER.indexOf(deal.status)
  const gross = deal.allocatedQuantityPcs * deal.pricePerPcUsd
  const feeTotal = deal.allocatedQuantityPcs * deal.platformFeePerPcUsd

  return (
    <div className="rounded-[var(--radius-md)] border border-[var(--color-line)] bg-[var(--color-white)] p-4">
      <div className="mb-3 flex items-start justify-between gap-3">
        <div>
          <p className="font-semibold">{deal.itemName}</p>
          <p className="text-xs text-[var(--color-ink-faint)]">{deal.dealNumber} · {deal.allocatedQuantityPcs.toLocaleString()} pcs · {deal.destinationCountry}</p>
        </div>
        <Badge tone={statusTone(deal.status)}>{humanizeStatus(deal.status)}</Badge>
      </div>

      {deal.status !== 'Cancelled' && (
        <div className="flex flex-col gap-1">
          {STAGES.map((s, i) => {
            const ts = s.ts(deal)
            const done = !!ts || i < currentIndex
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
      )}

      <div className="mt-3 border-t border-[var(--color-line)] pt-3 text-sm">
        <div className="flex justify-between">
          <span className="text-[var(--color-ink-faint)]">Gross ({deal.allocatedQuantityPcs.toLocaleString()} × {formatUsd(deal.pricePerPcUsd)})</span>
          <span>{formatUsd(gross)}</span>
        </div>
        <div className="mt-0.5 flex justify-between">
          <span className="text-[var(--color-ink-faint)]">Platform fee · {formatUsd(deal.platformFeePerPcUsd)}/pc</span>
          <span className="text-[var(--color-accent)]">– {formatUsd(feeTotal)}</span>
        </div>
        <div className="mt-1.5 flex justify-between border-t border-[var(--color-line)] pt-1.5 font-bold">
          <span>Your net payout</span>
          <span>{formatUsd(deal.totalPayoutUsd)}</span>
        </div>
      </div>

      {deal.trackingNumber && (
        <div className="mt-2 rounded-[var(--radius-sm)] bg-[var(--color-sage-mist)] p-2.5 text-sm">
          <strong>Shipping:</strong> {deal.courier} · {deal.trackingNumber}
          {deal.trackingUrl && (
            <>
              <br />
              <a href={deal.trackingUrl} target="_blank" rel="noopener noreferrer" className="text-xs text-[var(--color-accent)] underline">Open tracking →</a>
            </>
          )}
        </div>
      )}
    </div>
  )
}
