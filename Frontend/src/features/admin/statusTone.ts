import type { BadgeProps } from '@/components/ui'

type Tone = NonNullable<BadgeProps['tone']>

/** One shared label->tone lookup — status labels mean the same thing across every domain enum
 * (Pending, Cancelled, Confirmed, …), so a single map covers Deal/Requirement/Bid/Commission/
 * Dispute/Offer/Approval status badges without duplicating a mapping per enum. */
const TONE_BY_LABEL: Record<string, Tone> = {
  Pending: 'warning',
  PendingReview: 'warning',
  Approved: 'success',
  Rejected: 'danger',
  Suspended: 'danger',
  Draft: 'neutral',
  Posted: 'info',
  Matching: 'info',
  ReadyToOrder: 'accent',
  Confirmed: 'info',
  AwaitingPayment: 'warning',
  Paid: 'info',
  InFulfillment: 'info',
  SellersPaid: 'success',
  Dispatched: 'info',
  Delivered: 'success',
  Settled: 'success',
  Cancelled: 'danger',
  CancelledBackout: 'danger',
  Disputed: 'danger',
  Expired: 'neutral',
  Stale: 'neutral',
  Accepted: 'success',
  Accrued: 'neutral',
  ReadyToRelease: 'info',
  Released: 'success',
  Reversed: 'neutral',
  Open: 'danger',
  Investigating: 'warning',
  Resolved: 'success',
  Withdrawn: 'neutral',
  Sent: 'info',
  Viewed: 'info',
  Declined: 'danger',
  Countered: 'warning',
}

export function statusTone(status: string): Tone {
  return TONE_BY_LABEL[status] ?? 'neutral'
}

/** "AwaitingPayment" -> "Awaiting payment" — for display without a translation table per enum. */
export function humanizeStatus(status: string): string {
  const spaced = status.replace(/([a-z])([A-Z])/g, '$1 $2')
  return spaced.charAt(0) + spaced.slice(1).toLowerCase()
}
