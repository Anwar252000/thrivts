import type { SellerTier, GradeType, RequirementStatus, DealStatus, BidStatus, OfferStatus, NegotiationActor } from '@/features/admin/adminTypes'

export type { SellerTier, GradeType, RequirementStatus, DealStatus, BidStatus, OfferStatus, NegotiationActor }
export type NegotiationState = 'Open' | 'CounteredByBuyer' | 'CounteredBySeller' | 'Accepted' | 'Declined'

export interface SellerProfile {
  sellerCode: string | null
  kycVerified: boolean
  companyName: string | null
  locationCity: string
  locationCountry: string
  phone: string | null
  whatsApp: string | null
  tier: SellerTier
  tags: string[] | null
  categoriesSupplied: string[]
  createdAt: string
}

export interface SellerDashboard {
  totalPcsFulfilled: number
  totalOrdersFulfilled: number
  totalPaidUsd: number
}

export interface OpenRequirement {
  id: string
  requirementNumber: string
  itemName: string
  quantityPcs: number
  grade: GradeType
  destinationCountry: string
  shippingMode: string | null
  deliveryTimelineDays: number | null
  status: RequirementStatus
  postedAt: string | null
  hasQuoted: boolean
}

export interface MyQuote {
  bidId: string
  requirementId: string
  requirementNumber: string
  itemName: string
  requirementQtyPcs: number
  grade: GradeType
  destinationCountry: string
  availableQuantityPcs: number
  yourPricePerPcUsd: number
  platformFeePerPcUsd: number
  youReceivePerPcUsd: number
  youReceiveTotalUsd: number
  buyerCounterPriceUsd: number | null
  counterYouReceivePerPc: number | null
  buyerCounterAt: string | null
  buyerCounterNote: string | null
  negotiationState: NegotiationState
  lastActor: NegotiationActor | null
  lastActionAt: string | null
  statusLabel: string
  status: BidStatus
  respondedAt: string | null
  sellerNotes: string | null
}

export interface OfferRound {
  id: string
  party: NegotiationActor
  kind: string
  pricePerPcUsd: number | null
  notes: string | null
  createdAt: string
}

export interface SellerOffer {
  id: string
  offerNumber: string | null
  requirementId: string
  itemName: string | null
  quantityPcs: number | null
  grade: string | null
  offerPricePerPc: number | null
  currentPricePerPc: number | null
  status: OfferStatus
  adminNotes: string | null
  sentAt: string | null
  expiresAt: string | null
  rounds: OfferRound[]
}

export interface SellerDeal {
  dealId: string
  dealNumber: string
  status: DealStatus
  allocatedQuantityPcs: number
  pricePerPcUsd: number
  totalPayoutUsd: number
  platformFeePerPcUsd: number
  payoutPaidAt: string | null
  confirmedAt: string | null
  paidAt: string | null
  inFulfillmentAt: string | null
  dispatchedAt: string | null
  deliveredAt: string | null
  settledAt: string | null
  cancelledAt: string | null
  trackingNumber: string | null
  trackingUrl: string | null
  courier: string | null
  itemName: string
  grade: GradeType
  destinationCountry: string
  shippingMode: string | null
  createdAt: string
}
