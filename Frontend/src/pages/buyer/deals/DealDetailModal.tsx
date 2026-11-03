import { useState } from 'react'
import { AlertTriangle } from 'lucide-react'
import { Modal } from '@/components/admin'
import { Badge, Button, Input, Select, Textarea, Spinner } from '@/components/ui'
import { statusTone, humanizeStatus } from '@/features/admin/statusTone'
import { formatDate, formatUsd } from '@/lib/utils'
import {
  useGetMyDealsQuery, useRaiseDisputeMutation, useGetMyProfileQuery,
  useGetPurchaseOrderQuery, useRequestPaymentLinkMutation, useMarkPoPaidMutation,
} from '@/features/buyer/buyerApi'
import type { MyDealListItem, PurchaseOrder } from '@/features/buyer/buyerTypes'

const WHATSAPP_NUMBER = '447988595541'
const BANK_NAME = 'THE THRIVTS MARKETPL'
const BANK_ACCOUNT = '1660007766470 (USD)'
const BANK_SWIFT = 'BMRIIDJA'
const BANK_BANK = 'Bank Mandiri'
const BANK_ACCOUNT_ADDRESS = 'jl tomang raya blk MM kav 557 lt 3 no 51E, Grogol Petamburan, Grogol, Jakarta Barat, DKI Jakarta, Indonesia 11440'
const BANK_BANK_ADDRESS = 'jl teratai putih raya no 19, Malaka Sari, Duren Sawit, Jakarta Timur, DKI Jakarta, Indonesia 13460'

function escapeHtml(value: string): string {
  return value.replace(/[&<>"']/g, (c) => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' })[c]!)
}

function poWhatsappHref(deal: MyDealListItem, po?: PurchaseOrder | null) {
  const ref = po?.poNumber || deal.dealNumber
  const amt = formatUsd(po?.amountUsd ?? deal.totalInvoiceUsd)
  const msg = `Hi Thrivts, I have paid for my order (${ref}) — amount ${amt}. Here is my payment receipt:`
  return `https://wa.me/${WHATSAPP_NUMBER}?text=${encodeURIComponent(msg)}`
}

/** Mirrors buyer.html's generateInvoice(): a printable invoice opened in a new tab, with the same
 * bank-transfer remittance details so the buyer can pay by wire straight from it. */
function generateInvoice(deal: MyDealListItem, buyerName: string) {
  const qty = deal.totalQuantityPcs
  const total = deal.totalInvoiceUsd
  const unit = qty ? total / qty : 0
  const invoiceNumber = 'INV-' + deal.dealNumber.replace(/[^0-9A-Za-z-]/g, '')
  const today = new Date().toLocaleDateString('en-GB', { day: 'numeric', month: 'short', year: 'numeric' })
  const money = (n: number) => '$' + n.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })
  const grade = deal.grade === 'AB' ? 'A/B' : deal.grade

  const html = `<!doctype html><html><head><meta charset="utf-8"><title>${escapeHtml(invoiceNumber)}</title><style>
body{font-family:-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#222825;max-width:720px;margin:40px auto;padding:0 24px;}
h1{font-size:26px;letter-spacing:-.02em;margin:0;}.muted{color:#4A524C;font-size:13px;}
.rowx{display:flex;justify-content:space-between;gap:24px;}
table{width:100%;border-collapse:collapse;margin-top:24px;}th,td{text-align:left;padding:10px 8px;border-bottom:1px solid #e5e2da;font-size:14px;}
th{text-transform:uppercase;font-size:11px;letter-spacing:.08em;color:#4A524C;}
.tot{text-align:right;font-size:20px;font-weight:800;margin-top:16px;}
.box{background:#f6f4ee;border-radius:10px;padding:16px;margin-top:24px;font-size:13px;line-height:1.7;}
@media print{.noprint{display:none;}}</style></head><body>
<div class="rowx" style="align-items:flex-start;"><div><h1>THRIVTS</h1><div class="muted">B2B Vintage Wholesale</div></div>
<div style="text-align:right;"><div style="font-weight:700;">INVOICE</div><div class="muted">${escapeHtml(invoiceNumber)}<br>${today}</div></div></div>
<div class="rowx" style="margin-top:28px;"><div><div class="muted">Billed to</div><div style="font-weight:600;">${escapeHtml(buyerName)}</div></div>
<div style="text-align:right;"><div class="muted">Order</div><div style="font-weight:600;">${escapeHtml(deal.dealNumber)} · ${escapeHtml(deal.requirementNumber)}</div></div></div>
<table><thead><tr><th>Item</th><th>Grade</th><th>Destination</th><th style="text-align:right;">Qty</th><th style="text-align:right;">Unit</th><th style="text-align:right;">Amount</th></tr></thead>
<tbody><tr><td>${escapeHtml(deal.itemName)}</td><td>${escapeHtml(grade)}</td><td>${escapeHtml(deal.destinationCountry)}</td>
<td style="text-align:right;">${qty.toLocaleString()}</td><td style="text-align:right;">${money(unit)}</td><td style="text-align:right;">${money(total)}</td></tr></tbody></table>
<div class="tot">Total: ${money(total)} USD</div>
<div class="box"><div style="font-weight:700;margin-bottom:6px;">Remittance — Bank transfer (USD)</div>
Name: ${BANK_NAME}<br>Account number: ${BANK_ACCOUNT}<br>SWIFT: ${BANK_SWIFT}<br>Bank: ${BANK_BANK}<br>
Account address: ${BANK_ACCOUNT_ADDRESS}<br>
Bank address: ${BANK_BANK_ADDRESS}</div>
<button class="noprint" onclick="window.print()" style="margin-top:24px;padding:10px 18px;background:#3A443E;color:#fff;border:none;border-radius:8px;cursor:pointer;font:inherit;">Print / Save as PDF</button>
</body></html>`

  const w = window.open('', '_blank')
  if (!w) return
  w.document.write(html)
  w.document.close()
}

function BankTransferBox() {
  return (
    <div className="rounded-[var(--radius-sm)] bg-[var(--color-sage-mist)] p-3 text-xs leading-relaxed">
      <p className="mb-1 font-bold">Bank transfer (USD)</p>
      Name: {BANK_NAME}<br />
      Account: {BANK_ACCOUNT}<br />
      SWIFT: {BANK_SWIFT}<br />
      Bank: {BANK_BANK}
    </div>
  )
}

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
  const { data: profile } = useGetMyProfileQuery()
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
              {(deal.status === 'Paid' || deal.status === 'InFulfillment') && <FulfillmentDeadlineBlock deal={deal} />}
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

          {(deal.trackingNumber || deal.vesselName || deal.containerNumber || deal.estimatedArrivalDate) && (
            <div className="rounded-[var(--radius-sm)] bg-[var(--color-sage-mist)] p-2.5 text-sm">
              <p className="mb-1 font-bold">Shipment tracking</p>
              <TrackingRow label="Tracking #" value={deal.trackingNumber} />
              <TrackingRow label="Courier" value={deal.courier} />
              <TrackingRow label="Vessel" value={deal.vesselName} />
              <TrackingRow label="Container" value={deal.containerNumber} />
              <TrackingRow label="Est. arrival" value={deal.estimatedArrivalDate ? formatDate(deal.estimatedArrivalDate) : null} />
              {deal.trackingUrl && (
                <a href={deal.trackingUrl} target="_blank" rel="noopener noreferrer" className="mt-1 inline-block text-xs text-[var(--color-accent)] underline">Open tracking →</a>
              )}
            </div>
          )}

          <div className="flex justify-start">
            <Button size="sm" variant="outline" onClick={() => generateInvoice(deal, profile?.companyName ?? profile?.fullName ?? 'Valued buyer')}>
              View invoice ↗
            </Button>
          </div>

          <PurchaseOrderPanel dealId={deal.id} deal={deal} buyerName={profile?.companyName ?? profile?.fullName ?? 'Valued buyer'} />

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

/** Mirrors buyer.html's poCountdown(): colored "Due in Xd Xh" / "Payment overdue" next to the PO
 * number, instead of a plain due date. */
function PoCountdown({ dueAt }: { dueAt: string }) {
  const ms = new Date(dueAt).getTime() - new Date().getTime()
  if (ms <= 0) return <span className="text-xs font-bold text-[var(--color-danger)]">Payment overdue</span>
  const days = Math.floor(ms / 86_400_000)
  const hours = Math.floor((ms % 86_400_000) / 3_600_000)
  return <span className="text-xs font-bold">Due in {days > 0 ? `${days}d ` : ''}{hours}h</span>
}

/** Mirrors buyer.html's PO panel: shown regardless of deal status once a PO exists (issued
 * automatically the moment the deal is confirmed). Lets the buyer request a payment link, upload
 * a receipt, or share it on WhatsApp instead, while the PO is still open. */
function PurchaseOrderPanel({ dealId, deal, buyerName }: { dealId: string; deal: MyDealListItem; buyerName: string }) {
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
        <div className="flex items-center gap-2">
          {po.status === 'Issued' && <PoCountdown dueAt={po.dueAt} />}
          <Badge tone={po.status === 'Verified' ? 'success' : po.status === 'PaymentSubmitted' ? 'info' : 'warning'}>{humanizeStatus(po.status)}</Badge>
        </div>
      </div>
      <div className="grid grid-cols-2 gap-3 text-sm">
        <Field label="Amount due" value={formatUsd(po.amountUsd)} />
        <Field label="Due by" value={formatDate(po.dueAt)} />
      </div>

      {po.status === 'Issued' && (
        <div className="mt-3 flex flex-col gap-3 border-t border-[var(--color-line)] pt-3">
          <div className="flex flex-wrap gap-2">
            <Button size="sm" variant="outline" type="button" onClick={() => generateInvoice(deal, buyerName)}>View invoice ↗</Button>
            {po.paymentLinkUrl ? (
              <a href={po.paymentLinkUrl} target="_blank" rel="noopener noreferrer">
                <Button size="sm" variant="outline" type="button">Open payment link →</Button>
              </a>
            ) : (
              <Button size="sm" variant="outline" onClick={() => requestLink(dealId)} disabled={requestingLink}>
                {requestingLink ? 'Requesting…' : 'Request a payment link'}
              </Button>
            )}
          </div>

          <BankTransferBox />

          <div>
            <label className="mb-1 block text-xs font-medium text-[var(--color-ink-soft)]">Already paid? Upload your receipt</label>
            <div className="flex gap-2">
              <Input type="file" accept="image/*,application/pdf" onChange={(e) => setReceiptFile(e.target.files?.[0] ?? null)} />
              <Button size="sm" onClick={uploadReceipt} disabled={uploading}>{uploading ? 'Uploading…' : 'Upload'}</Button>
            </div>
            {error && <p className="mt-1 text-xs text-[var(--color-danger)]">{error}</p>}
          </div>

          <div className="border-t border-[var(--color-line)] pt-3">
            <p className="mb-1.5 text-xs font-semibold">Paid already? Share your receipt to confirm</p>
            <a href={poWhatsappHref(deal, po)} target="_blank" rel="noopener noreferrer">
              <Button size="sm" type="button">Share receipt on WhatsApp ↗</Button>
            </a>
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

function TrackingRow({ label, value }: { label: string; value: string | null }) {
  if (!value) return null
  return (
    <div className="flex items-center justify-between gap-3 py-0.5 text-sm">
      <span className="text-[var(--color-ink-faint)]">{label}</span>
      <span className="font-medium">{value}</span>
    </div>
  )
}

/** Mirrors buyer.html's deadlineBlock: a countdown to fulfillmentDueAt (paid_at + the
 * requirement's delivery_timeline_days), coloring red once the seller is overdue. */
function FulfillmentDeadlineBlock({ deal }: { deal: MyDealListItem }) {
  if (!deal.fulfillmentDueAt) return null
  const ms = new Date(deal.fulfillmentDueAt).getTime() - new Date().getTime()
  const overdue = ms <= 0
  const urgent = !overdue && ms < 86_400_000
  const days = Math.floor(ms / 86_400_000)
  const hours = Math.floor((ms % 86_400_000) / 3_600_000)

  const label = overdue
    ? 'Seller is late — fulfillment overdue'
    : days < 1
      ? `${hours}h left for seller to fulfil`
      : `${days} day${days === 1 ? '' : 's'} ${hours}h left for seller to fulfil`

  const tone = overdue ? 'bg-[var(--color-danger-soft)] text-[var(--color-danger)]'
    : urgent ? 'bg-[var(--color-warning-soft)] text-[var(--color-warning)]'
    : 'bg-[var(--color-sage-mist)] text-[var(--color-ink-soft)]'

  return (
    <div className={`mb-2 flex items-center justify-between gap-2 rounded-[var(--radius-sm)] px-3 py-2 text-sm ${tone}`}>
      <strong>{label}</strong>
      {deal.deliveryTimelineDays && <span className="text-xs text-[var(--color-ink-faint)]">{deal.deliveryTimelineDays}-day window</span>}
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
