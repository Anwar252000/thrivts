import { useMutation, useQuery, useQueryClient, keepPreviousData } from '@tanstack/react-query'
import { useApi } from '@shared/api/context'
import { qs } from '@shared/api/http'
import type { CursorPage, Escalation, IdResponse, ItemRequestStatus, LotDetail } from '@shared/api/types'
import type {
  ApprovalsAndGoals, AssistantTurn, ClientMessage, Dues, Floor, GoalPeriod, Hisab, Invoice, InvoiceDraft, InvoiceMode,
  OwnerItem, OwnerItemRequest, OwnerOrder, PaymentMethod, SaveOwnerItemInput, SupplierAccount, Valuation,
} from '@shared/api/ownerTypes'

/** A mutation that refreshes the given query-key prefixes when it succeeds. */
function useAction<TInput, TResult = void>(fn: (input: TInput) => Promise<TResult>, refresh: string[]) {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: fn,
    onSuccess: () => Promise.all(refresh.map((p) => qc.invalidateQueries({ queryKey: [p] }))),
  })
}

// ---------- floor & lots ----------

export function useFloor() {
  const { http } = useApi()
  return useQuery({ queryKey: ['floor'], queryFn: ({ signal }) => http.get<Floor>('/owner/floor', signal) })
}

export function useLotDetail(id: string | null) {
  const { http } = useApi()
  return useQuery({ queryKey: ['lots', 'detail', id], queryFn: ({ signal }) => http.get<LotDetail>(`/lots/${id}`, signal), enabled: !!id })
}

export function useDeleteLot() {
  const { http } = useApi()
  return useAction((id: string) => http.del<void>(`/owner/lots/${id}`), ['floor', 'lots', 'finance', 'stock'])
}

// ---------- catalogue ----------

export function useOwnerItems(includeArchived: boolean) {
  const { http } = useApi()
  return useQuery({
    queryKey: ['catalog', 'owner-items', includeArchived],
    queryFn: ({ signal }) => http.get<OwnerItem[]>(`/owner/items${qs({ includeArchived })}`, signal),
    placeholderData: keepPreviousData,
  })
}

export function useSaveItem() {
  const { http } = useApi()
  return useAction(async ({ id, ...body }: SaveOwnerItemInput) => {
    if (id) await http.put<void>(`/owner/items/${id}`, { ...body, id })
    else await http.post<IdResponse>('/owner/items', { ...body, id: null })
  }, ['catalog', 'finance'])
}

export function useSetItemArchived() {
  const { http } = useApi()
  return useAction(({ id, archived }: { id: string; archived: boolean }) => http.put<void>(`/owner/items/${id}/archived`, { value: archived }), ['catalog'])
}

export function useItemRequests(status: ItemRequestStatus | null) {
  const { http } = useApi()
  return useQuery({ queryKey: ['catalog', 'item-requests', status], queryFn: ({ signal }) => http.get<OwnerItemRequest[]>(`/owner/item-requests${qs({ status })}`, signal) })
}

export interface ItemDecision {
  id: string
  approve: boolean
  category: string | null
  weightKg: number | null
  buyPrice: number | null
  sellPrice: number | null
  note: string | null
}

export function useDecideItemRequest() {
  const { http } = useApi()
  return useAction(({ id, ...body }: ItemDecision) => http.post<void>(`/owner/item-requests/${id}/decision`, { ...body, id }), ['catalog'])
}

// ---------- finance ----------

export function useHisab(search: string) {
  const { http } = useApi()
  return useQuery({
    queryKey: ['finance', 'hisab', search],
    queryFn: ({ signal }) => http.get<Hisab>(`/owner/finance/hisab${qs({ search })}`, signal),
    placeholderData: keepPreviousData,
  })
}

export function useSupplierAccount(id: string | null) {
  const { http } = useApi()
  return useQuery({ queryKey: ['finance', 'supplier', id], queryFn: ({ signal }) => http.get<SupplierAccount>(`/owner/finance/suppliers/${id}`, signal), enabled: !!id })
}

export interface SupplierInput {
  id: string | null
  name: string
  phone: string | null
  city: string | null
  since: string | null
  notes: string | null
}

export function useSaveSupplier() {
  const { http } = useApi()
  return useAction(async ({ id, ...body }: SupplierInput) => {
    if (id) {
      await http.put<void>(`/owner/finance/suppliers/${id}`, { ...body, id })
      return id
    }
    return (await http.post<IdResponse>('/owner/finance/suppliers', { ...body, id: null })).id
  }, ['finance', 'catalog'])
}

export function useInvoiceDraft(lotId: string | null) {
  const { http } = useApi()
  return useQuery({ queryKey: ['finance', 'draft', lotId], queryFn: ({ signal }) => http.get<InvoiceDraft>(`/owner/finance/lots/${lotId}/invoice-draft`, signal), enabled: !!lotId, staleTime: 0 })
}

export interface CreateInvoiceInput {
  lotId: string
  mode: InvoiceMode
  lines: { itemName: string; pieces: number; buyPrice: number; mergedFrom: string[] | null }[]
  rejections: { itemName: string; rejected: number; reason: string | null; accepted: number; acceptPrice: number }[]
  idempotencyKey: string
}

export function useCreateInvoice() {
  const { http } = useApi()
  return useAction(({ idempotencyKey, ...body }: CreateInvoiceInput) => http.post<IdResponse>('/owner/finance/invoices', body, idempotencyKey), ['finance', 'floor'])
}

export function useInvoice(id: string | null) {
  const { http } = useApi()
  return useQuery({ queryKey: ['finance', 'invoice', id], queryFn: ({ signal }) => http.get<Invoice>(`/owner/finance/invoices/${id}`, signal), enabled: !!id })
}

export function useDeleteInvoice() {
  const { http } = useApi()
  return useAction((id: string) => http.del<void>(`/owner/finance/invoices/${id}`), ['finance', 'floor'])
}

export interface PaymentInput {
  supplierId: string
  amount: number
  method: PaymentMethod
  paidOn: string | null
  note: string | null
  invoiceId: string | null
  idempotencyKey: string
}

export function useRecordPayment() {
  const { http } = useApi()
  return useAction(({ idempotencyKey, ...body }: PaymentInput) => http.post<IdResponse>('/owner/finance/payments', body, idempotencyKey), ['finance'])
}

export function useDeletePayment() {
  const { http } = useApi()
  return useAction((id: string) => http.del<void>(`/owner/finance/payments/${id}`), ['finance'])
}

export function useDues() {
  const { http } = useApi()
  return useQuery({ queryKey: ['finance', 'dues'], queryFn: ({ signal }) => http.get<Dues>('/owner/finance/dues', signal) })
}

export function useValuation() {
  const { http } = useApi()
  return useQuery({ queryKey: ['finance', 'valuation'], queryFn: ({ signal }) => http.get<Valuation>('/owner/finance/valuation', signal) })
}

// ---------- client orders ----------

export function useOwnerOrders(completed: boolean) {
  const { http } = useApi()
  return useQuery({
    queryKey: ['orders', 'owner', completed],
    queryFn: ({ signal }) => http.get<OwnerOrder[]>(`/owner/orders${qs({ completed })}`, signal),
    placeholderData: keepPreviousData,
  })
}

const ORDER_KEYS = ['orders', 'finance', 'client-messages']

export function useOrderActions() {
  const { http } = useApi()
  return {
    create: useAction((b: { name: string; clientName: string | null; deadline: string | null }) => http.post<IdResponse>('/orders', b), ORDER_KEYS),
    update: useAction((o: OwnerOrder) => http.put<void>(`/orders/${o.id}`, {
      id: o.id, name: o.name, clientName: o.clientName, status: o.status, deadline: o.deadline, destinationCountry: o.destinationCountry,
      orderReceivedDate: o.orderReceivedDate, dispatchDate: o.dispatchDate, boxes: o.boxes, weightKg: o.weightKg,
    }), ORDER_KEYS),
    ownerFields: useAction(({ id, totalValue, loginCode }: { id: string; totalValue: number; loginCode: string | null }) =>
      http.put<void>(`/owner/orders/${id}/owner-fields`, { id, totalValue, loginCode }), ORDER_KEYS),
    setCompleted: useAction(({ id, value }: { id: string; value: boolean }) => http.put<void>(`/orders/${id}/completed`, { value }), ORDER_KEYS),
    remove: useAction((id: string) => http.del<void>(`/orders/${id}`), ORDER_KEYS),
    addLine: useAction(({ orderId, itemName }: { orderId: string; itemName: string }) =>
      http.post<IdResponse>(`/orders/${orderId}/lines`, { orderId, lineId: null, itemName, grade: null, category: null, required: 0, ready: 0, status: 'Pending', note: null }), ORDER_KEYS),
    saveLine: useAction(({ orderId, line }: { orderId: string; line: OwnerOrder['lines'][number] }) =>
      http.put<void>(`/orders/${orderId}/lines/${line.id}`, {
        orderId, lineId: line.id, itemName: line.itemName, grade: line.grade, category: line.category,
        required: line.required, ready: line.ready, status: line.status, note: line.note,
      }), ORDER_KEYS),
    clientReady: useAction(({ orderId, lineId, clientReady }: { orderId: string; lineId: string; clientReady: number }) =>
      http.put<void>(`/owner/orders/${orderId}/lines/${lineId}/client-ready`, { clientReady }), ORDER_KEYS),
    removeLine: useAction(({ orderId, lineId }: { orderId: string; lineId: string }) => http.del<void>(`/orders/${orderId}/lines/${lineId}`), ORDER_KEYS),
    addPayment: useAction(({ orderId, ...b }: { orderId: string; label: string | null; amount: number; isPaid: boolean; paidOn: string | null }) =>
      http.post<IdResponse>(`/owner/orders/${orderId}/payments`, { orderId, paymentId: null, ...b }), ORDER_KEYS),
    savePayment: useAction(({ orderId, paymentId, ...b }: { orderId: string; paymentId: string; label: string | null; amount: number; isPaid: boolean; paidOn: string | null }) =>
      http.put<void>(`/owner/orders/${orderId}/payments/${paymentId}`, { orderId, paymentId, ...b }), ORDER_KEYS),
    removePayment: useAction(({ orderId, paymentId }: { orderId: string; paymentId: string }) => http.del<void>(`/owner/orders/${orderId}/payments/${paymentId}`), ORDER_KEYS),
  }
}

export function useClientMessages(openOnly: boolean) {
  const { http } = useApi()
  return useQuery({ queryKey: ['client-messages', openOnly], queryFn: ({ signal }) => http.get<ClientMessage[]>(`/owner/client-messages${qs({ openOnly })}`, signal) })
}

export function useSetMessageDone() {
  const { http } = useApi()
  return useAction(({ id, done }: { id: string; done: boolean }) => http.put<void>(`/owner/client-messages/${id}/done`, { value: done }), ['client-messages'])
}

// ---------- escalations ----------

export function useEscalations() {
  const { http } = useApi()
  return useQuery({ queryKey: ['escalations', 'all'], queryFn: ({ signal }) => http.get<CursorPage<Escalation>>(`/escalations${qs({ openOnly: false, size: 100 })}`, signal) })
}

export function useEscalationActions() {
  const { http } = useApi()
  return {
    reply: useAction(({ id, reply, resolve }: { id: string; reply: string; resolve: boolean }) => http.post<void>(`/escalations/${id}/reply`, { reply, resolve }), ['escalations']),
    resolve: useAction(({ id, resolved }: { id: string; resolved: boolean }) => http.put<void>(`/escalations/${id}/resolved`, { value: resolved }), ['escalations']),
  }
}

// ---------- approvals & goals ----------

export function useApprovalsAndGoals() {
  const { http } = useApi()
  return useQuery({ queryKey: ['operations', 'approvals'], queryFn: ({ signal }) => http.get<ApprovalsAndGoals>('/owner/approvals', signal) })
}

export interface GoalInput {
  id: string | null
  period: GoalPeriod
  label: string
  target: string | null
  progress: number
}

export function useManageActions() {
  const { http } = useApi()
  return {
    decide: useAction(({ id, approve }: { id: string; approve: boolean }) => http.post<void>(`/owner/approvals/${id}/decision`, { approve }), ['operations']),
    saveGoal: useAction(async ({ id, ...b }: GoalInput) => {
      if (id) await http.put<void>(`/owner/goals/${id}`, { ...b, id })
      else await http.post<IdResponse>('/owner/goals', { ...b, id: null })
    }, ['operations']),
    deleteGoal: useAction((id: string) => http.del<void>(`/owner/goals/${id}`), ['operations']),
  }
}

// ---------- assistant ----------

export function useAskAssistant() {
  const { http } = useApi()
  return useMutation({ mutationFn: (messages: AssistantTurn[]) => http.post<{ text: string }>('/owner/assistant', { messages }) })
}
