/** Mirrors the backend's Thrivts.Domain.Enums exactly — JSON now serializes enums as these
 * PascalCase member names (see Program.cs's global JsonStringEnumConverter), never raw ints. */

export type ApprovalStatus = 'Pending' | 'Approved' | 'Rejected' | 'Suspended'
export type ProfileApprovalAction = 'Approve' | 'Reject' | 'Block' | 'Unblock'
export type UserRoleEnum = 'Buyer' | 'Seller' | 'Agency' | 'Admin'
export type SellerTier = 'Bronze' | 'Silver' | 'Gold' | 'Platinum'
export type GradeType = 'A' | 'AB' | 'B' | 'Mixed'
export type CurrencyType = 'USD' | 'GBP' | 'EUR' | 'PKR'

export type RequirementStatus =
  | 'PendingReview' | 'Posted' | 'Matching' | 'ReadyToOrder' | 'Confirmed' | 'AwaitingPayment'
  | 'Paid' | 'InFulfillment' | 'SellersPaid' | 'Dispatched' | 'Delivered' | 'Disputed'
  | 'Settled' | 'Cancelled' | 'Expired' | 'Stale'

export type DealStatus =
  | 'Draft' | 'Confirmed' | 'AwaitingPayment' | 'Paid' | 'InFulfillment' | 'Dispatched'
  | 'Delivered' | 'Settled' | 'Cancelled' | 'Disputed' | 'Pending' | 'Accrued' | 'Released' | 'Reversed'

export type BidStatus = 'Pending' | 'Accepted' | 'Rejected' | 'CancelledBackout' | 'Accrued' | 'Released' | 'Reversed'
export type CommissionStatus = 'Pending' | 'ReadyToRelease' | 'Released' | 'Cancelled' | 'Accrued' | 'Reversed'
export type DisputeStatus = 'Open' | 'Investigating' | 'Resolved' | 'Rejected' | 'Withdrawn'
export type OfferStatus = 'Sent' | 'Viewed' | 'Accepted' | 'Declined' | 'Countered' | 'Expired' | 'Withdrawn'
export type MessageSenderType = 'Admin' | 'Buyer' | 'Seller' | 'Agency'
export type NegotiationActor = 'Buyer' | 'Seller' | 'Admin'
export type NotificationChannel = 'Email' | 'Whatsapp' | 'InApp'

export interface PagedResult<T> {
  items: T[]
  totalCount: number
  page: number
  pageSize: number
}

// ---- Users (role-agnostic) ----
export interface UserListItem {
  id: string
  email: string
  fullName: string
  role: UserRoleEnum
  phone: string | null
  whatsApp: string | null
  approvalStatus: ApprovalStatus
  isActive: boolean
  createdAt: string
}

// ---- Buyers ----
export interface BuyerListItem {
  id: string
  email: string
  companyName: string
  country: string
  city: string | null
  approvalStatus: ApprovalStatus
  isActive: boolean
  isPremium: boolean
  totalOrders: number
  totalSpendUsd: number
  createdAt: string
}

export interface BuyerDetail {
  id: string
  email: string
  phone: string | null
  whatsApp: string | null
  companyName: string
  companyRegistration: string | null
  vatId: string | null
  country: string
  city: string | null
  website: string | null
  instagram: string | null
  estimatedMonthlyVolumePcs: number | null
  categoriesOfInterest: string[] | null
  attributedToAgency: string | null
  influencerId: string | null
  firstOrderAt: string | null
  approvalStatus: ApprovalStatus
  isActive: boolean
  isPremium: boolean
  totalOrders: number
  totalSpendUsd: number
  notes: string | null
  createdAt: string
}

// ---- Sellers ----
export interface SellerListItem {
  id: string
  email: string
  publicAlias: string
  companyName: string | null
  locationCity: string
  locationCountry: string
  tier: SellerTier
  kycVerified: boolean
  approvalStatus: ApprovalStatus
  isActive: boolean
  totalOrdersFulfilled: number
  totalPaidUsd: number
  createdAt: string
}

export interface SellerDetail {
  id: string
  email: string
  phone: string | null
  whatsApp: string | null
  publicAlias: string
  companyName: string | null
  locationCity: string
  locationCountry: string
  yearsInBusiness: number | null
  categoriesSupplied: string[]
  referenceContact: string | null
  tier: SellerTier
  tags: string[] | null
  sellerCode: string | null
  kycVerified: boolean
  kycVerifiedAt: string | null
  kycNotes: string | null
  approvalStatus: ApprovalStatus
  isActive: boolean
  totalOrdersFulfilled: number
  totalPcsSupplied: number
  totalPaidUsd: number
  disputeCount: number
  backoutCount: number
  notes: string | null
  createdAt: string
}

// ---- Agencies ----
export interface AgencyListItem {
  id: string
  agencyName: string
  agencyCode: string
  ownerFullName: string
  country: string
  commissionRate: number
  isActive: boolean
  totalBuyersReferred: number
  totalDealsClosed: number
  totalCommissionEarnedUsd: number
  totalCommissionPendingUsd: number
  createdAt: string
}

export interface AgencyDetail {
  id: string
  email: string
  agencyName: string
  agencyCode: string
  ownerFullName: string
  country: string
  city: string | null
  teamSize: number | null
  commissionRate: number
  totalBuyersReferred: number
  totalDealsClosed: number
  totalCommissionEarnedUsd: number
  totalCommissionPaidUsd: number
  totalCommissionPendingUsd: number
  isActive: boolean
  notes: string | null
  createdAt: string
}

// ---- Requirements ----
export interface RequirementListItem {
  id: string
  requirementNumber: string
  buyerId: string
  itemName: string
  quantityPcs: number
  grade: GradeType
  destinationCountry: string
  status: RequirementStatus
  createdAt: string
}

export interface RequirementDetail {
  id: string
  requirementNumber: string
  buyerId: string
  itemName: string
  quantityPcs: number
  grade: GradeType
  destinationCountry: string
  destinationPort: string | null
  buyerTargetPriceUsd: number
  buyerNotes: string | null
  adminNotes: string | null
  status: RequirementStatus
  publicDisplay: boolean
  createdAt: string
  minSellerTier: SellerTier
  restrictedToTags: string[] | null
}

export interface AdminBidBoardRow {
  requirementId: string
  requirementNumber: string
  itemName: string
  requiredPcs: number
  requirementStatus: RequirementStatus
  bidId: string
  bidPcs: number
  sellerPricePerPc: number | null
  feePerPc: number | null
  buyerSeesPerPc: number | null
  buyerTotal: number | null
  bidStatus: BidStatus
  respondedAt: string | null
  sellerId: string
  sellerCompanyName: string | null
  sellerAlias: string
  sellerTier: SellerTier
  kycVerified: boolean
}

// ---- Deals ----
export interface DealListItem {
  id: string
  dealNumber: string
  buyerId: string
  sellerId: string | null
  totalQuantityPcs: number
  totalInvoiceUsd: number
  totalSpreadUsd: number
  status: DealStatus
  hasDispute: boolean
  createdAt: string
}

export interface DealDetail {
  id: string
  dealNumber: string
  requirementId: string
  buyerId: string
  sellerId: string | null
  agencyId: string | null
  status: DealStatus
  totalQuantityPcs: number
  buyerPricePerPcUsd: number
  avgSellerPricePerPcUsd: number
  spreadPerPcUsd: number
  subtotalUsd: number
  shippingCostUsd: number | null
  totalInvoiceUsd: number
  totalSpreadUsd: number
  totalSellerPayoutUsd: number
  destinationCountry: string | null
  destinationPort: string | null
  containerNumber: string | null
  containerSize: string | null
  shippingLine: string | null
  vesselName: string | null
  billOfLading: string | null
  trackingNumber: string | null
  trackingUrl: string | null
  courier: string | null
  paymentMethod: string | null
  paymentReference: string | null
  paymentReceivedAt: string | null
  hasDispute: boolean
  disputeAmountUsd: number
  adminNotes: string | null
  confirmedAt: string | null
  paidAt: string | null
  dispatchedAt: string | null
  deliveredAt: string | null
  settledAt: string | null
  cancelledAt: string | null
  cancellationReason: string | null
  createdAt: string
}

export interface DealAllocation {
  id: string
  sellerId: string
  sellerCompanyName: string | null
  sellerCode: string | null
  sellerPhone: string | null
  sellerWhatsApp: string | null
  lotNumber: string
  allocatedQuantityPcs: number
  pricePerPcUsd: number
  totalPayoutUsd: number
  status: BidStatus
  payoutPaidAt: string | null
}

// ---- Commissions ----
export interface CommissionListItem {
  id: string
  dealId: string
  agencyId: string
  commissionRate: number
  commissionAmountUsd: number
  status: CommissionStatus
  releaseDueAt: string
  releasedAt: string | null
  createdAt: string
}

export interface PlatformFeeRevenueMonth {
  year: number
  month: number
  allocations: number
  pcs: number
  feeRevenueUsd: number
  grossPaidUsd: number
  netPaidToSellersUsd: number
}

// ---- Disputes ----
export interface DisputeListItem {
  id: string
  disputeNumber: string
  dealId: string
  raisedBy: string
  category: string | null
  description: string
  status: DisputeStatus
  refundAmountUsd: number
  createdAt: string
}

// ---- Messages ----
export interface MessageThreadListItem {
  id: string
  participantRole: UserRoleEnum
  participantId: string
  subject: string | null
  isOpen: boolean
  lastMessageAt: string | null
  unreadCount: number
}

export interface AdminMessage {
  id: string
  senderId: string
  senderType: MessageSenderType
  body: string
  isRead: boolean
  createdAt: string
}

// ---- Approvals ----
export interface PendingApproval {
  id: string
  role: UserRoleEnum
  email: string
  fullName: string
  createdAt: string
}

// ---- Audit ----
export interface AuditLogEntry {
  id: string
  actorId: string | null
  actorRole: UserRoleEnum | null
  action: string
  entityType: string
  entityId: string | null
  detailsJson: string | null
  createdAt: string
}

// ---- Settings: categories / shipping / exchange rates / influencers / partner apps / fee config ----
export interface Category {
  id: number
  name: string
  nameFr: string | null
  weightPerPieceKg: number | null
  displayOrder: number
  isActive: boolean
}

export interface ShippingRate {
  id: number
  destinationCountry: string
  rateUsdPerKg: number | null
  flatRateUsd: number | null
  transitDays: number | null
  isActive: boolean
}

export interface ExchangeRate {
  id: number
  currency: CurrencyType
  rateToUsd: number
  effectiveFrom: string
  notes: string | null
}

export interface NotificationItem {
  id: string
  channel: NotificationChannel
  title: string
  body: string
  refType: string | null
  refId: string | null
  isRead: boolean
  readAt: string | null
  createdAt: string
}

export interface InfluencerListItem {
  id: string
  influencerCode: string
  referralCode: string
  fullName: string | null
  email: string | null
  commissionRate: number
  status: string
  createdAt: string
}

export interface PartnerApplication {
  id: string
  fullName: string
  email: string
  instagram: string | null
  tiktok: string | null
  platform: string | null
  profileLink: string | null
  status: string
  createdAt: string
}

export interface PlatformFeeConfig {
  feePerPcUsd: number
  pkrReference: string
  updatedAt: string
}

// ---- Offers ----
export interface OfferListItem {
  id: string
  offerNumber: string | null
  requirementId: string
  sellerId: string
  offerPricePerPc: number | null
  currentPricePerPc: number | null
  status: OfferStatus
  dealId: string | null
  sentAt: string | null
  expiresAt: string | null
}

export interface OfferRound {
  id: string
  party: NegotiationActor
  kind: string
  pricePerPcUsd: number | null
  notes: string | null
  createdAt: string
}

// ---- Dashboard ----
// Mirrors admin.html's loadDashboard() exactly — every figure is a live count/sum against the
// real tables, computed on each request (there is no cached platform-stats row to go stale).
export interface DashboardStats {
  totalBuyers: number
  pendingBuyers: number
  totalSellers: number
  pendingSellers: number
  activeAgencies: number
  pendingAgencies: number
  liveRequirements: number
  requirementsAwaitingReview: number
  openDisputes: number
  totalVolumeUsd: number
  pendingCommissionsUsd: number
  commissionsReadyToRelease: number
}
