// Owner-only wire types (prices, money, client values). Only the owner app imports these; the
// API refuses these endpoints to staff logins regardless.
import type { DateOnly, Guid, Instant, ItemCriteria, ItemRequestStatus, LotStage } from './types'

export type Grade = 'A' | 'C'

// ---------- floor ----------

export type FloorStage = 'Gate' | 'Counting' | 'Grading' | 'Cosign' | 'Pricing' | 'Finalized'

export interface FloorLot {
  id: Guid
  serial: string
  supplierName: string
  sacks: number
  pieces: number
  stage: FloorStage
  gatedAt: Instant
}

export interface Floor {
  piecesOnFloor: number
  lots: number
  waitingForPrice: number
  finalized: number
  suppliers: number
  items: FloorLot[]
}

// ---------- catalogue with prices ----------

export interface OwnerItem {
  id: Guid
  name: string
  category: string | null
  skuCategory: string | null
  gradeNote: string | null
  weightKg: number | null
  criteria: ItemCriteria
  rejectRule: string | null
  isArchived: boolean
  buyPrice: number | null
  sellPrice: number | null
  sellA: number | null
  sellC: number | null
  marginPercent: number | null
}

export interface SaveOwnerItemInput {
  id: Guid | null
  name: string
  category: string | null
  skuCategory: string | null
  gradeNote: string | null
  weightKg: number | null
  criteria: ItemCriteria | null
  rejectRule: string | null
  buyPrice: number | null
  sellPrice: number | null
  sellA: number | null
  sellC: number | null
}

export interface OwnerItemRequest {
  id: Guid
  proposedName: string
  note: string | null
  requestedBy: string
  status: ItemRequestStatus
  createdAt: Instant
  decisionNote: string | null
}

// ---------- hisab / invoices / payments ----------

export type InvoiceMode = 'NonGraded' | 'Final'
export type InvoiceStatus = 'Generated' | 'Sent'
export type PaymentMethod = 'Bank' | 'Cash' | 'Hundi'

export interface HisabRow {
  supplierId: Guid
  name: string
  city: string | null
  phone: string | null
  invoices: number
  newLots: number
  billed: number
  paid: number
  due: number
}

export interface Hisab {
  totalDue: number
  suppliers: number
  withBalance: number
  rows: HisabRow[]
}

export interface InvoiceSummary {
  id: Guid
  number: string
  lotSerial: string | null
  mode: InvoiceMode
  status: InvoiceStatus
  total: number
  rejected: number
  accepted: number
  returned: number
  rejectionReasons: string | null
  generatedAt: Instant
}

export interface Payment {
  id: Guid
  amount: number
  method: PaymentMethod
  paidOn: DateOnly
  note: string | null
  invoiceNumber: string | null
}

export interface NewLot {
  lotId: Guid
  serial: string
  pieces: number
  stage: LotStage
  arrivalDate: DateOnly
}

export interface SupplierAccount {
  id: Guid
  name: string
  phone: string | null
  city: string | null
  since: string | null
  notes: string | null
  billed: number
  paid: number
  due: number
  newLots: NewLot[]
  invoices: InvoiceSummary[]
  payments: Payment[]
}

export interface InvoiceDraftLine {
  itemName: string
  pieces: number
  defaultBuyPrice: number | null
}

export interface InvoiceDraftRejection {
  itemName: string
  rejected: number
  reason: string | null
}

export interface InvoiceDraft {
  lotId: Guid
  lotSerial: string
  supplierId: Guid
  supplierName: string
  suggestedMode: InvoiceMode
  totalPieces: number
  lines: InvoiceDraftLine[]
  rejections: InvoiceDraftRejection[]
}

export interface InvoiceLine {
  itemName: string
  pieces: number
  buyPrice: number
  lineTotal: number
  isAcceptedRejection: boolean
  mergedFrom: string[] | null
}

export interface InvoiceRejection {
  itemName: string
  rejected: number
  reason: string | null
  accepted: number
  acceptPrice: number
  returned: number
}

export interface Invoice {
  id: Guid
  number: string
  supplierId: Guid
  supplierName: string
  lotSerial: string | null
  mode: InvoiceMode
  status: InvoiceStatus
  generatedAt: Instant
  total: number
  lines: InvoiceLine[]
  rejections: InvoiceRejection[]
}

export interface Receivable {
  orderId: Guid
  orderName: string
  clientName: string | null
  status: string
  value: number
  paid: number
  outstanding: number
}

export interface Dues {
  youOwe: number
  clientsOwe: number
  suppliersWithBalance: number
  payables: HisabRow[]
  receivables: Receivable[]
}

export interface ValuationRow {
  itemName: string
  category: string | null
  grade: Grade
  lotSerial: string
  supplierName: string
  quantity: number
  pricePerPiece: number | null
  value: number
}

export interface Valuation {
  pieces: number
  value: number
  lines: number
  rows: ValuationRow[]
}

// ---------- client orders ----------

export type ClientOrderStatus = 'InProcess' | 'BeingGraded' | 'WaitingForPayment' | 'ReadyToShip' | 'Completed' | 'Shipped' | 'OnHold'
export type OrderLineStatus = 'Pending' | 'BeingGraded' | 'PartialReady' | 'Ready' | 'OnHold' | 'Shipped'

export interface OwnerOrderLine {
  id: Guid
  itemName: string
  grade: string | null
  category: string | null
  required: number
  ready: number
  clientReady: number
  status: OrderLineStatus
  note: string | null
  seq: number
}

export interface OrderPayment {
  id: Guid
  label: string | null
  amount: number
  isPaid: boolean
  paidOn: DateOnly | null
}

export interface OwnerOrder {
  id: Guid
  name: string
  clientName: string | null
  status: ClientOrderStatus
  deadline: DateOnly | null
  isCompleted: boolean
  loginCode: string | null
  totalValue: number
  required: number
  clientReady: number
  paid: number
  lines: OwnerOrderLine[]
  payments: OrderPayment[]
  destinationCountry: string | null
  orderReceivedDate: DateOnly | null
  dispatchDate: DateOnly | null
  boxes: number | null
  weightKg: number | null
}

export type ClientMessageType = 'Request' | 'Call' | 'Video'

export interface ClientMessage {
  id: Guid
  clientOrderId: Guid
  orderName: string
  clientName: string | null
  type: ClientMessageType
  message: string
  requestedWhen: string | null
  isDone: boolean
  doneAt: Instant | null
  createdAt: Instant
}

// ---------- approvals & goals ----------

export type ApprovalStatus = 'Pending' | 'Approved' | 'Rejected'
export type GoalPeriod = 'Monthly' | 'SixMonth' | 'Yearly'

export interface Approval {
  id: Guid
  fromName: string
  description: string
  valueText: string | null
  status: ApprovalStatus
  createdAt: Instant
}

export interface Goal {
  id: Guid
  period: GoalPeriod
  label: string
  target: string | null
  progress: number
}

export interface ApprovalsAndGoals {
  approvals: Approval[]
  goals: Goal[]
}

// ---------- assistant ----------

export type AssistantRole = 'User' | 'Assistant'

export interface AssistantTurn {
  role: AssistantRole
  content: string
}
