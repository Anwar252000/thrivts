import type { SellerTier, GradeType, CurrencyType, RequirementStatus, DealStatus, BidStatus, Category, PoStatus, PurchaseOrder } from '@/features/admin/adminTypes'

export type { Category, SellerTier, GradeType, CurrencyType, RequirementStatus, DealStatus, BidStatus, PoStatus, PurchaseOrder }
export type NegotiationState = 'Open' | 'CounteredByBuyer' | 'CounteredBySeller' | 'Accepted' | 'Declined'
export type LanguagePref = 'En' | 'Fr'

export interface BuyerProfile {
  fullName: string
  email: string
  phone: string | null
  whatsApp: string | null
  language: LanguagePref
  companyName: string
  website: string | null
  country: string
  city: string | null
  instagram: string | null
  estimatedMonthlyVolumePcs: number | null
  typicalRequirementType: string | null
  approvalStatus: 'Pending' | 'Approved' | 'Rejected' | 'Suspended'
  createdAt: string
}

export interface RecentRequirement {
  id: string
  requirementNumber: string
  itemName: string
  quantityPcs: number
  grade: GradeType
  destinationCountry: string
  status: string
  createdAt: string
}

export interface BuyerDashboard {
  liveRequirementsCount: number
  pendingReviewCount: number
  openDealsCount: number
  inTransitCount: number
  settledCount: number
  totalSpentUsd: number
  recentRequirements: RecentRequirement[]
}

export interface MyRequirementListItem {
  id: string
  requirementNumber: string
  itemName: string
  categoryName: string | null
  quantityPcs: number
  grade: GradeType
  destinationCountry: string
  targetPricePerPc: number
  currency: CurrencyType
  status: RequirementStatus
  createdAt: string
}

export interface BuyerVisibleBid {
  bidId: string
  availableQuantityPcs: number
  buyerPricePerPcUsd: number
  buyerTotalUsd: number
  status: BidStatus
  negotiationState: NegotiationState
  roundCount: number
  sellerAlias: string
  sellerTier: SellerTier
  sellerVerified: boolean
  bidTime: string | null
}

export interface MyDealListItem {
  id: string
  dealNumber: string
  requirementNumber: string
  itemName: string
  grade: GradeType
  destinationCountry: string
  shippingMode: string | null
  totalQuantityPcs: number
  totalInvoiceUsd: number
  status: DealStatus
  hasDispute: boolean
  createdAt: string
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
  fulfillmentDueAt: string | null
}

export interface MyMessageThread {
  id: string
  subject: string | null
  isOpen: boolean
  unread: boolean
  lastMessageAt: string | null
}

export interface PublicExchangeRate {
  currency: CurrencyType
  rateToUsd: number
}

export interface PublicActivityItem {
  itemName: string
  quantityPcs: number
  grade: GradeType
  destinationCountry: string
  activityType: string
  activityTime: string
}
