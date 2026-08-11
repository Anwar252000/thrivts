import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { Plus, Check } from 'lucide-react'
import { Card, Button, Input, Select } from '@/components/ui'
import { useGetExchangeRatesQuery, useSetExchangeRateMutation } from '@/features/admin/adminApi'
import type { CurrencyType, ExchangeRate } from '@/features/admin/adminTypes'
import { formatDate } from '@/lib/utils'

const currencies: CurrencyType[] = ['GBP', 'EUR', 'PKR']

export function ExchangeRatesPanel() {
  const { data: rates } = useGetExchangeRatesQuery()
  const [setRate, { isLoading }] = useSetExchangeRateMutation()
  const [adding, setAdding] = useState(false)
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
        <Button size="sm" variant="ghost" onClick={() => setAdding((v) => !v)}>
          <Plus size={14} /> Add
        </Button>
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
    </Card>
  )
}
