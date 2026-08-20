import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { Plus, Check, Trash2, Pencil, X } from 'lucide-react'
import { Card, Badge, Button, Input } from '@/components/ui'
import { useGetShippingRatesQuery, useCreateShippingRateMutation, useUpdateShippingRateMutation, useDeleteShippingRateMutation } from '@/features/admin/adminApi'
import type { ShippingRate } from '@/features/admin/adminTypes'
import { formatUsd } from '@/lib/utils'

export function ShippingRatesPanel() {
  const { data: rates } = useGetShippingRatesQuery()
  const [createRate, { isLoading: creating }] = useCreateShippingRateMutation()
  const [deleteRate] = useDeleteShippingRateMutation()
  const [adding, setAdding] = useState(false)
  const [editingId, setEditingId] = useState<number | null>(null)
  const { register, handleSubmit, reset } = useForm<{ destinationCountry: string; rateUsdPerKg?: number; flatRateUsd?: number; transitDays?: number }>()

  const onAdd = handleSubmit(async (values) => {
    await createRate(values)
    reset()
    setAdding(false)
  })

  return (
    <Card>
      <div className="mb-4 flex items-center justify-between">
        <p className="font-semibold">Shipping rates</p>
        <Button size="sm" variant="ghost" onClick={() => setAdding((v) => !v)}>
          <Plus size={14} /> Add
        </Button>
      </div>

      {adding && (
        <form onSubmit={onAdd} className="mb-4 grid grid-cols-2 gap-2">
          <Input placeholder="Destination country" className="col-span-2" {...register('destinationCountry', { required: true })} />
          <Input type="number" step="0.01" placeholder="USD/kg" {...register('rateUsdPerKg')} />
          <Input type="number" step="0.01" placeholder="Flat USD" {...register('flatRateUsd')} />
          <Input type="number" placeholder="Transit days" {...register('transitDays')} />
          <Button type="submit" size="sm" disabled={creating} className="self-start">
            <Check size={14} />
          </Button>
        </form>
      )}

      <div className="flex max-h-80 flex-col gap-2 overflow-y-auto">
        {rates?.map((r) =>
          editingId === r.id ? (
            <ShippingRateEditRow key={r.id} rate={r} onDone={() => setEditingId(null)} />
          ) : (
            <div key={r.id} className="flex items-center justify-between rounded-[var(--radius-sm)] border border-[var(--color-line)] px-3 py-2 text-sm">
              <div>
                <p className="font-medium">{r.destinationCountry}</p>
                <p className="text-xs text-[var(--color-ink-faint)]">
                  {r.rateUsdPerKg ? `${formatUsd(r.rateUsdPerKg)}/kg` : r.flatRateUsd ? `Flat ${formatUsd(r.flatRateUsd)}` : '—'}
                  {r.transitDays ? ` · ${r.transitDays}d` : ''}
                </p>
              </div>
              <div className="flex items-center gap-2">
                <Badge tone={r.isActive ? 'success' : 'neutral'}>{r.isActive ? 'Active' : 'Inactive'}</Badge>
                <button onClick={() => setEditingId(r.id)} className="text-[var(--color-ink-faint)] hover:text-[var(--color-accent)]">
                  <Pencil size={14} />
                </button>
                <button onClick={() => deleteRate(r.id)} className="text-[var(--color-ink-faint)] hover:text-[var(--color-danger)]">
                  <Trash2 size={14} />
                </button>
              </div>
            </div>
          ),
        )}
      </div>
    </Card>
  )
}

function ShippingRateEditRow({ rate, onDone }: { rate: ShippingRate; onDone: () => void }) {
  const [updateRate, { isLoading: saving }] = useUpdateShippingRateMutation()
  const [rateUsdPerKg, setRateUsdPerKg] = useState(rate.rateUsdPerKg ?? '')
  const [flatRateUsd, setFlatRateUsd] = useState(rate.flatRateUsd ?? '')
  const [isActive, setIsActive] = useState(rate.isActive)

  const save = async () => {
    await updateRate({
      shippingRateId: rate.id,
      rateUsdPerKg: rateUsdPerKg === '' ? undefined : Number(rateUsdPerKg),
      flatRateUsd: flatRateUsd === '' ? undefined : Number(flatRateUsd),
      isActive,
    })
    onDone()
  }

  return (
    <div className="rounded-[var(--radius-sm)] border border-[var(--color-accent)] px-3 py-2 text-sm">
      <p className="mb-2 font-medium">{rate.destinationCountry}</p>
      <div className="mb-2 grid grid-cols-2 gap-2">
        <Input type="number" step="0.01" placeholder="USD/kg" value={rateUsdPerKg} onChange={(e) => setRateUsdPerKg(e.target.value === '' ? '' : Number(e.target.value))} />
        <Input type="number" step="0.01" placeholder="Flat USD" value={flatRateUsd} onChange={(e) => setFlatRateUsd(e.target.value === '' ? '' : Number(e.target.value))} />
      </div>
      <div className="flex items-center justify-between">
        <button onClick={() => setIsActive((v) => !v)}>
          <Badge tone={isActive ? 'success' : 'neutral'}>{isActive ? 'Active' : 'Inactive'}</Badge>
        </button>
        <div className="flex gap-2">
          <Button size="sm" variant="outline" onClick={onDone}>
            <X size={14} />
          </Button>
          <Button size="sm" onClick={save} disabled={saving}>
            <Check size={14} />
          </Button>
        </div>
      </div>
    </div>
  )
}
