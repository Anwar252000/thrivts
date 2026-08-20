import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { Plus, Check, RefreshCw } from 'lucide-react'
import { Card, Button, Input, Select } from '@/components/ui'
import { Modal } from '@/components/admin'
import { useGetExchangeRatesQuery, useSetExchangeRateMutation } from '@/features/admin/adminApi'
import type { CurrencyType, ExchangeRate } from '@/features/admin/adminTypes'
import { formatDate } from '@/lib/utils'

const currencies: CurrencyType[] = ['GBP', 'EUR', 'PKR']

export function ExchangeRatesPanel() {
  const { data: rates } = useGetExchangeRatesQuery()
  const [setRate, { isLoading }] = useSetExchangeRateMutation()
  const [adding, setAdding] = useState(false)
  const [bulkOpen, setBulkOpen] = useState(false)
  const { register, handleSubmit, reset } = useForm<{ currency: CurrencyType; rateToUsd: number }>()

  // Show only the latest row per currency — history is kept server-side but the panel is a snapshot.
  const latestByCurrency = new Map<CurrencyType, ExchangeRate>()
  rates?.forEach((r) => {
    if (!latestByCurrency.has(r.currency)) latestByCurrency.set(r.currency, r)
  })

  const onAdd = handleSubmit(async (values) => {
    await setRate({ currency: values.currency, rateToUsd: Number(values.rateToUsd) })
    reset()
    setAdding(false)
  })

  return (
    <Card>
      <div className="mb-4 flex items-center justify-between">
        <p className="font-semibold">Exchange rates</p>
        <div className="flex gap-2">
          <Button size="sm" variant="ghost" onClick={() => setBulkOpen(true)}>
            <RefreshCw size={14} /> Update all
          </Button>
          <Button size="sm" variant="ghost" onClick={() => setAdding((v) => !v)}>
            <Plus size={14} /> Add
          </Button>
        </div>
      </div>

      {adding && (
        <form onSubmit={onAdd} className="mb-4 flex gap-2">
          <Select className="w-28" {...register('currency', { required: true })}>
            {currencies.map((c) => (
              <option key={c} value={c}>{c}</option>
            ))}
          </Select>
          <Input type="number" step="0.0001" placeholder="Rate to USD" className="flex-1" {...register('rateToUsd', { required: true })} />
          <Button type="submit" size="sm" disabled={isLoading}>
            <Check size={14} />
          </Button>
        </form>
      )}

      <div className="flex flex-col gap-2">
        {Array.from(latestByCurrency.values()).map((r) => (
          <div key={r.currency} className="flex items-center justify-between rounded-[var(--radius-sm)] border border-[var(--color-line)] px-3 py-2 text-sm">
            <span className="font-medium">{r.currency}</span>
            <span>{r.rateToUsd}</span>
            <span className="text-xs text-[var(--color-ink-faint)]">{formatDate(r.effectiveFrom)}</span>
          </div>
        ))}
      </div>

      <BulkUpdateModal open={bulkOpen} onClose={() => setBulkOpen(false)} latestByCurrency={latestByCurrency} />
    </Card>
  )
}

function BulkUpdateModal({
  open, onClose, latestByCurrency,
}: { open: boolean; onClose: () => void; latestByCurrency: Map<CurrencyType, ExchangeRate> }) {
  const [setRate, { isLoading }] = useSetExchangeRateMutation()
  const [values, setValues] = useState<Record<CurrencyType, string>>(() => ({
    GBP: String(latestByCurrency.get('GBP')?.rateToUsd ?? ''),
    EUR: String(latestByCurrency.get('EUR')?.rateToUsd ?? ''),
    PKR: String(latestByCurrency.get('PKR')?.rateToUsd ?? ''),
    USD: '1',
  }))

  if (!open) return null

  const save = async () => {
    const entries = currencies
      .map((c) => ({ currency: c, rateToUsd: Number(values[c]) }))
      .filter((e) => !Number.isNaN(e.rateToUsd) && e.rateToUsd > 0)
    if (entries.length === 0) return
    await Promise.all(entries.map((e) => setRate(e)))
    onClose()
  }

  return (
    <Modal open={open} onClose={onClose} title="Update exchange rates" subtitle="1 unit of currency per USD">
      <div className="flex flex-col gap-4">
        <div className="grid grid-cols-3 gap-3">
          {currencies.map((c) => (
            <div key={c}>
              <label className="mb-1 block text-xs font-medium text-[var(--color-ink-soft)]">{c} per 1 USD</label>
              <Input
                type="number"
                step="0.000001"
                placeholder="0.000000"
                value={values[c]}
                onChange={(e) => setValues((prev) => ({ ...prev, [c]: e.target.value }))}
              />
            </div>
          ))}
        </div>
        <div className="flex justify-end gap-2">
          <Button variant="outline" onClick={onClose}>Cancel</Button>
          <Button onClick={save} disabled={isLoading}>{isLoading ? 'Saving…' : 'Save rates'}</Button>
        </div>
      </div>
    </Modal>
  )
}
