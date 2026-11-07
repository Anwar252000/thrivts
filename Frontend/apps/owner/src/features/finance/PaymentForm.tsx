import { useRef, useState } from 'react'
import { rs, todayKarachi } from '@shared/format'
import { useErrorToast, useToast } from '@shared/ui/toast'
import type { PaymentMethod, SupplierAccount } from '@shared/api/ownerTypes'
import { useRecordPayment } from '../../api/owner'
import { Modal } from '../../ui'

export function PaymentForm({ supplier, onClose }: { supplier: SupplierAccount; onClose: () => void }) {
  const record = useRecordPayment()
  const toast = useToast()
  const showError = useErrorToast()
  const key = useRef(crypto.randomUUID())
  const [amount, setAmount] = useState('')
  const [method, setMethod] = useState<PaymentMethod>('Bank')
  const [paidOn, setPaidOn] = useState(todayKarachi)
  const [invoiceId, setInvoiceId] = useState('')
  const [note, setNote] = useState('')

  const submit = async () => {
    const value = parseFloat(amount)
    if (!(value > 0)) return toast('Enter an amount', true)
    try {
      await record.mutateAsync({ supplierId: supplier.id, amount: value, method, paidOn, note: note.trim() || null, invoiceId: invoiceId || null, idempotencyKey: key.current })
      toast(`Payment of ${rs(value)} logged for ${supplier.name}.`)
      onClose()
    } catch (e) {
      showError(e)
    }
  }

  return (
    <Modal onClose={onClose} width={460}>
      <div style={{ fontSize: 17, fontWeight: 800 }}>Log a payment to {supplier.name}</div>
      <div className="muted" style={{ fontSize: 12, margin: '2px 0 12px' }}>Balance due: {rs(supplier.due)}</div>
      <div className="form-grid">
        <div className="fld"><label>Amount (Rs)</label><input autoFocus type="number" min={1} value={amount} onChange={(e) => setAmount(e.target.value)} placeholder="0" /></div>
        <div className="fld"><label>Method</label>
          <select value={method} onChange={(e) => setMethod(e.target.value as PaymentMethod)}>
            <option>Bank</option><option>Cash</option><option>Hundi</option>
          </select></div>
        <div className="fld"><label>Paid on</label><input type="date" value={paidOn} max={todayKarachi()} onChange={(e) => setPaidOn(e.target.value || todayKarachi())} /></div>
        <div className="fld"><label>Against invoice (optional)</label>
          <select value={invoiceId} onChange={(e) => setInvoiceId(e.target.value)}>
            <option value="">— general payment —</option>
            {supplier.invoices.map((i) => <option key={i.id} value={i.id}>{i.number} · {i.lotSerial} · {rs(i.total)}</option>)}
          </select></div>
      </div>
      <div className="fld" style={{ marginTop: 11 }}><label>Note</label><input value={note} maxLength={300} onChange={(e) => setNote(e.target.value)} /></div>
      <div className="row-actions" style={{ marginTop: 14 }}>
        <button className="btn" onClick={onClose}>Cancel</button>
        <button className="btn p" disabled={record.isPending} onClick={() => void submit()}>{record.isPending ? 'Saving…' : '💷 Log payment'}</button>
      </div>
    </Modal>
  )
}
