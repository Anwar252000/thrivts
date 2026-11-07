// Wire types for Thrivts.Api (camelCase JSON, enums as strings, dates as ISO strings).
// Keep these in step with the C# DTOs — they are the contract between the apps and the API.

export type Guid = string
/** yyyy-MM-dd (a business date in Karachi time). */
export type DateOnly = string
/** ISO-8601 instant. */
export type Instant = string

// ---------- auth ----------

export type AppKind = 'Staff' | 'Owner'

export type UserRole = 'Owner' | 'Finance' | 'Sales' | 'GeneralManager' | 'Supervisor' | 'InventoryManager' | 'GateGuard'

export interface LoginUserCard {
  id: Guid
  displayName: string
  title: string
  color: string
  role: UserRole
  pinLength: number
}

export interface SessionUser {
  id: Guid
  displayName: string
  title: string
  color: string
  role: UserRole
  app: AppKind
  permissions: string[]
}

export interface SessionResponse {
  accessToken: string
  accessTokenExpiresAt: Instant
  refreshToken: string
  refreshTokenExpiresAt: Instant
  user: SessionUser
}

// ---------- paging ----------

export interface CursorPage<T> {
  items: T[]
  nextCursor: string | null
}

export interface PagedResult<T> {
  items: T[]
  totalCount: number
  page: number
  pageSize: number
}

export interface IdResponse {
  id: Guid
}

// ---------- catalog ----------

export interface ItemCriteria {
  brands: string | null
  grades: string | null
  rejectIf: string | null
  notes: string | null
  style: string | null
}

export interface CatalogItem {
  id: Guid
  name: string
  category: string | null
  skuCategory: string | null
  gradeNote: string | null
  weightKg: number | null
  criteria: ItemCriteria
  rejectRule: string | null
}

export interface Supplier {
  id: Guid
  name: string
}

export interface SkuCategory {
  id: Guid
  name: string
  sackSize: number
}

export interface Labourer {
  id: Guid
  name: string
}

export type ItemRequestStatus = 'Pending' | 'Approved' | 'Rejected'

export interface ItemRequest {
  id: Guid
  proposedName: string
  note: string | null
  requestedBy: string
  status: ItemRequestStatus
  decisionNote: string | null
  createdAt: Instant
  decidedAt: Instant | null
}

// ---------- gate ----------

export type GateDirection = 'In' | 'Out'

export interface GateEntry {
  id: Guid
  direction: GateDirection
  serial: string
  supplierId: Guid
  supplierName: string
  sacks: number
  date: DateOnly
  createdAt: Instant
}

export interface NextGateSerials {
  nextLot: string
  nextExit: string
}

// ---------- lots ----------

export type LotStage = 'Counting' | 'Grading' | 'AwaitingCosign' | 'Finalized'

export type LotScope = 'Open' | 'AwaitingCosign' | 'InProgress' | 'Finalized'

export type RejectionReason = 'PakkaDaagh' | 'KataPhataFake' | 'WrongStock'

export interface LotSummary {
  id: Guid
  serial: string
  supplierId: Guid
  supplierName: string
  sacks: number
  arrivalDate: DateOnly
  stage: LotStage
  itemCount: number
  received: number
  qtyA: number
  qtyB: number
  qtyC: number
  rejected: number
  gatedAt: Instant
  countDueAt: Instant
  countFinalizedAt: Instant | null
  gradeDueAt: Instant | null
  gradingDays: number
  supervisorSignedAt: Instant | null
  cosignedAt: Instant | null
  sentBackAt: Instant | null
  sentBackReason: string | null
}

export interface LotItem {
  id: Guid
  itemId: Guid | null
  itemName: string
  received: number
  qtyA: number
  qtyB: number
  qtyC: number
  rejected: number
  rejectionReason: RejectionReason | null
  returnedRejected: number
  unaccounted: number
  sortOrder: number
  washRejected: number
}

export interface LotDetail {
  summary: LotSummary
  items: LotItem[]
  gradeDraftSavedAt: Instant | null
  supervisorSignatureId: Guid | null
  supervisorSignedBy: string | null
  cosignSignatureId: Guid | null
  cosignedBy: string | null
  sentBackBy: string | null
}

export interface GradeLine {
  lineId: Guid
  qtyA: number
  qtyB: number
  qtyC: number
  rejected: number
  rejectionReason: RejectionReason | null
}

// ---------- communication ----------

export type EscalationSource = 'Message' | 'Blocker'

export interface Escalation {
  id: Guid
  fromName: string
  source: EscalationSource
  message: string
  department: string | null
  date: DateOnly
  reply: string | null
  repliedAt: Instant | null
  hasUnreadReply: boolean
  isResolved: boolean
  createdAt: Instant
}

// ---------- users (owner) ----------

export interface User {
  id: Guid
  displayName: string
  title: string
  role: UserRole
  app: AppKind
  color: string
  isActive: boolean
  isLockedOut: boolean
  lastLoginAt: Instant | null
  sortOrder: number
}

export interface SaveUserInput {
  id: Guid | null
  displayName: string
  title: string
  role: UserRole
  color: string
  pin: string | null
  sortOrder: number
}
