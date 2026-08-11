import { useEffect, useState } from 'react'
import { useForm } from 'react-hook-form'
import { AlertCircle, Ban } from 'lucide-react'
import { Modal, ConfirmDialog, DataTable, type Column } from '@/components/admin'
import { Badge, Button, Input, Select, Spinner } from '@/components/ui'
import { statusTone } from '@/features/admin/statusTone'
import { formatUsd, formatDate, formatDateTime } from '@/lib/utils'
import {
  useGetDealByIdQuery, useAdvanceDealStatusMutation, useRecordDealPaymentMutation,
  useSetDealShippingMutation, useSetDealTrackingMutation, useCancelDealMutation, useGetDealAllocationsQuery,
} from '@/features/admin/adminApi'
import type { DealAllocation, DealStatus } from '@/features/admin/adminTypes'

const FORWARD_STATUSES: DealStatus[] = ['Confirmed', 'AwaitingPayment', 'Paid', 'InFulfillment', 'Dispatched', 'Delivered', 'Settled']

interface DealDetailModalProps {
  dealId: string | null
  onClose: () => void
}

function errorMessage(err: unknown): string | null {
  if (!err || typeof err !== 'object') return null
  const data = (err as { data?: { title?: string } }).data
  return data?.title ?? 'Something went wrong.'
}

export function DealDetailModal({ dealId, onClose }: DealDetailModalProps) {
  const { data: deal, isLoading } = useGetDealByIdQuery(dealId!, { skip: !dealId })
  const { data: allocations, isLoading: allocationsLoading } = useGetDealAllocationsQuery(dealId!, { skip: !dealId })

  const [advanceStatus, advanceState] = useAdvanceDealStatusMutation()
  const [recordPayment, paymentState] = useRecordDealPaymentMutation()
  const [setShipping, shippingState] = useSetDealShippingMutation()
  const [setTracking, trackingState] = useSetDealTrackingMutation()
  const [cancelDeal] = useCancelDealMutation()

  const [nextStatus, setNextStatus] = useState<DealStatus>('Confirmed')
  const [confirmCancel, setConfirmCancel] = useState(false)

  const paymentForm = useForm<{ paymentMethod: string; paymentReference: string }>()
  const shippingForm = useForm<{ containerNumber: string; containerSize: string; shippingLine: string; vesselName: string; billOfLading: string }>()
  const trackingForm = useForm<{ trackingNumber: string; trackingUrl: string; courier: string }>()

  useEffect(() => {
    if (deal) {
      paymentForm.reset({ paymentMethod: deal.paymentMethod ?? '', paymentReference: deal.paymentReference ?? '' })
      shippingForm.reset({
        containerNumber: deal.containerNumber ?? '', containerSize: deal.containerSize ?? '', shippingLine: deal.shippingLine ?? '',
        vesselName: deal.vesselName ?? '', billOfLading: deal.billOfLading ?? '',
      })
      trackingForm.reset({ trackingNumber: deal.trackingNumber ?? '', trackingUrl: deal.trackingUrl ?? '', courier: deal.courier ?? '' })
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [deal])

  if (!dealId) return null

  const allocationColumns: Column<DealAllocation>[] = [
    { header: 'Seller', accessor: (a) => <span className="font-semibold">{a.sellerCompanyName ?? a.sellerCode ?? a.sellerId}</span> },
    { header: 'Lot', accessor: (a) => a.lotNumber },
    { header: 'Qty', accessor: (a) => a.allocatedQuantityPcs, numeric: true },
    { header: 'Price/pc', accessor: (a) => formatUsd(a.pricePerPcUsd), numeric: true },
    { header: 'Payout', accessor: (a) => formatUsd(a.totalPayoutUsd), numeric: true },
    { header: 'Status', accessor: (a) => <Badge tone={statusTone(a.status)}>{a.status}</Badge> },
    { header: 'Paid', accessor: (a) => formatDate(a.payoutPaidAt) },
  ]

  return (
    <Modal open={!!dealId} onClose={onClose} title={deal?.dealNumber ?? 'Deal'} subtitle={deal ? `${deal.totalQuantityPcs.toLocaleString()} pcs` : undefined} size="lg">
      {isLoading || !deal ? (
        <div className="flex justify-center py-10">
          <Spinner />
        </div>
      ) : (
        <div className="flex flex-col gap-6">
          <div className="flex flex-wrap items-center gap-2">
            <Badge tone={statusTone(deal.status)}>{deal.status}</Badge>
            {deal.hasDispute && <Badge tone="danger">Disputed</Badge>}
          </div>

          <div className="grid grid-cols-3 gap-4 text-sm">
            <Field label="Buyer price/pc" value={formatUsd(deal.buyerPricePerPcUsd)} />
            <Field label="Seller price/pc" value={formatUsd(deal.avgSellerPricePerPcUsd)} />
            <Field label="Spread/pc" value={formatUsd(deal.spreadPerPcUsd)} />
            <Field label="Subtotal" value={formatUsd(deal.subtotalUsd)} />
            <Field label="Shipping" value={deal.shippingCostUsd != null ? formatUsd(deal.shippingCostUsd) : '—'} />
            <Field label="Total invoice" value={formatUsd(deal.totalInvoiceUsd)} />
            <Field label="Total spread (fee)" value={formatUsd(deal.totalSpreadUsd)} />
            <Field label="Seller payout" value={formatUsd(deal.totalSellerPayoutUsd)} />
            <Field label="Created" value={formatDate(deal.createdAt)} />
          </div>

          {deal.status !== 'Cancelled' && deal.status !== 'Settled' && (
            <div className="border-t border-[var(--color-line)] pt-5">
              <p className="mb-2 text-xs font-semibold uppercase tracking-wider text-[var(--color-ink-faint)]">Advance status</p>
              <div className="flex flex-wrap items-end gap-3">
                <Select className="w-56" value={nextStatus} onChange={(e) => setNextStatus(e.target.value as DealStatus)}>
                  {FORWARD_STATUSES.map((s) => (
                    <option key={s} value={s}>{s}</option>
                  ))}
                </Select>
                <Button size="sm" onClick={() => advanceStatus({ dealId, newStatus: nextStatus })} disabled={advanceState.isLoading}>
                  {advanceState.isLoading ? 'Advancing…' : 'Advance'}
                </Button>
                <Button size="sm" variant="outline" onClick={() => setConfirmCancel(true)}>
                  <Ban size={14} /> Cancel deal
                </Button>
              </div>
              {errorMessage(advanceState.error) && (
                <p className="mt-2 flex items-center gap-1.5 text-xs text-[var(--color-danger)]">
                  <AlertCircle size={13} /> {errorMessage(advanceState.error)}
                </p>
              )}
            </div>
          )}

          <div className="grid gap-5 border-t border-[var(--color-line)] pt-5 sm:grid-cols-2">
            <form
              className="flex flex-col gap-2"
              onSubmit={paymentForm.handleSubmit((v) => recordPayment({ dealId, paymentMethod: v.paymentMethod, paymentReference: v.paymentReference }))}
            >
              <p className="text-xs font-semibold uppercase tracking-wider text-[var(--color-ink-faint)]">Payment</p>
              <Input placeholder="Payment method" {...paymentForm.register('paymentMethod')} />
              <Input placeholder="Reference" {...paymentForm.register('paymentReference')} />
              <Button type="submit" size="sm" className="self-start" disabled={paymentState.isLoading}>
                {paymentState.isLoading ? 'Saving…' : 'Save payment'}
              </Button>
              {deal.paymentReceivedAt && <p className="text-xs text-[var(--color-ink-faint)]">Received {formatDateTime(deal.paymentReceivedAt)}</p>}
            </form>

            <form
              className="flex flex-col gap-2"
              onSubmit={trackingForm.handleSubmit((v) => setTracking({ dealId, ...v }))}
            >
              <p className="text-xs font-semibold uppercase tracking-wider text-[var(--color-ink-faint)]">Tracking</p>
              <Input placeholder="Tracking number" {...trackingForm.register('trackingNumber')} />
              <Input placeholder="Tracking URL" {...trackingForm.register('trackingUrl')} />
              <Input placeholder="Courier" {...trackingForm.register('courier')} />
              <Button type="submit" size="sm" className="self-start" disabled={trackingState.isLoading}>
                {trackingState.isLoading ? 'Saving…' : 'Save tracking'}
              </Button>
            </form>
          </div>

          <form
            className="grid gap-3 border-t border-[var(--color-line)] pt-5 sm:grid-cols-2"
            onSubmit={shippingForm.handleSubmit((v) => setShipping({ dealId, ...v }))}
          >
            <p className="col-span-2 text-xs font-semibold uppercase tracking-wider text-[var(--color-ink-faint)]">Shipping details</p>
            <Input placeholder="Container number" {...shippingForm.register('containerNumber')} />
            <Input placeholder="Container size" {...shippingForm.register('containerSize')} />
            <Input placeholder="Shipping line" {...shippingForm.register('shippingLine')} />
            <Input placeholder="Vessel name" {...shippingForm.register('vesselName')} />
            <Input placeholder="Bill of lading" {...shippingForm.register('billOfLading')} />
            <Button type="submit" size="sm" className="col-span-2 self-start" disabled={shippingState.isLoading}>
              {shippingState.isLoading ? 'Saving…' : 'Save shipping'}
            </Button>
          </form>

          <div className="border-t border-[var(--color-line)] pt-5">
            <p className="mb-2 text-xs font-semibold uppercase tracking-wider text-[var(--color-ink-faint)]">Seller allocations</p>
            <DataTable columns={allocationColumns} rows={allocations ?? []} keyFor={(a) => a.id} loading={allocationsLoading} emptyTitle="No allocations" />
          </div>
        </div>
      )}

      <ConfirmDialog
        open={confirmCancel}
        onClose={() => setConfirmCancel(false)}
        onConfirm={async (reason) => {
          await cancelDeal({ dealId, reason: reason ?? 'No reason given' })
          setConfirmCancel(false)
        }}
        title="Cancel this deal?"
        description="This frees the seller's bid back onto the board, reverts the requirement if nothing else is committed, and reverses any accrued commission."
        requireReason
        confirmLabel="Cancel deal"
      />
    </Modal>
  )
}

function Field({ label, value }: { label: string; value: string }) {
  return (
    <div>
      <p className="text-xs font-semibold uppercase tracking-wider text-[var(--color-ink-faint)]">{label}</p>
      <p className="mt-0.5 font-medium">{value}</p>
    </div>
  )
}
