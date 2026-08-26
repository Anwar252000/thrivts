import { useMemo } from 'react'
import { useNavigate } from 'react-router-dom'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { Card, Button, Input, Select, Textarea, PageTransition } from '@/components/ui'
import { useGetPublicCategoriesQuery, useGetPublicExchangeRatesQuery, usePostRequirementMutation } from '@/features/buyer/buyerApi'

const DESTINATION_COUNTRIES = [
  'United Kingdom', 'France', 'Italy', 'Spain', 'Germany', 'Netherlands', 'Belgium', 'Poland',
  'Portugal', 'Ireland', 'USA', 'Canada', 'UAE', 'Saudi Arabia', 'Other',
]

const schema = z.object({
  itemName: z.string().min(1, 'Required'),
  categoryId: z.coerce.number().min(1, 'Select a category'),
  grade: z.enum(['A', 'AB', 'B']),
  quantityPcs: z.coerce.number().min(30, 'Minimum 30 pieces'),
  shippingMode: z.enum(['sea_freight', 'air_freight']),
  deliveryTimelineDays: z.coerce.number(),
  destinationCountry: z.string().min(1, 'Required'),
  currency: z.enum(['USD', 'GBP', 'EUR']),
  pricePerPc: z.coerce.number().min(0.01, 'Required'),
  notes: z.string().optional(),
})
type FormInput = z.input<typeof schema>
type FormValues = z.infer<typeof schema>

const CURRENCY_SYMBOLS: Record<string, string> = { USD: '$', GBP: '£', EUR: '€' }

export function PostRequirement() {
  const navigate = useNavigate()
  const { data: categories } = useGetPublicCategoriesQuery()
  const { data: rates } = useGetPublicExchangeRatesQuery()
  const [postRequirement, { isLoading, error }] = usePostRequirementMutation()

  const {
    register, handleSubmit, watch, reset, formState: { errors },
  } = useForm<FormInput, unknown, FormValues>({
    resolver: zodResolver(schema),
    defaultValues: { grade: 'AB', shippingMode: 'sea_freight', deliveryTimelineDays: 10, currency: 'GBP' },
  })

  const quantityPcs = watch('quantityPcs')
  const pricePerPc = watch('pricePerPc')
  const currency = watch('currency')
  const categoryId = watch('categoryId')

  const preview = useMemo(() => {
    const qty = Number(quantityPcs) || 0
    const price = Number(pricePerPc) || 0
    if (!qty || !price) return null

    const totalLocal = qty * price
    const rate = currency === 'USD' ? 1 : rates?.find((r) => r.currency === currency)?.rateToUsd
    const totalUsd = currency === 'USD' || !rate ? totalLocal : totalLocal / rate
    const weightPerPc = categories?.find((c) => c.id === Number(categoryId))?.weightPerPieceKg ?? 0.45
    const weightKg = qty * weightPerPc

    return { totalLocal, totalUsd, weightKg }
  }, [quantityPcs, pricePerPc, currency, rates, categories, categoryId])

  const onSubmit = async (values: FormValues) => {
    const result = await postRequirement(values)
    if ('data' in result) {
      reset()
      navigate('/buyer/requirements')
    }
  }

  return (
    <PageTransition>
      <h1 className="mb-1 text-2xl font-semibold">Post a requirement</h1>
      <p className="mb-6 text-sm text-[var(--color-ink-faint)]">Tell us what you need. We'll source it from our verified partner network.</p>

      <form onSubmit={handleSubmit(onSubmit)} className="grid gap-6 lg:grid-cols-[1fr_320px]">
        <div className="flex flex-col gap-4">
          <Card>
            <h4 className="mb-3 text-xs font-semibold uppercase tracking-wider text-[var(--color-ink-faint)]">What you need</h4>
            <div className="mb-3">
              <label className="mb-1 block text-xs font-medium text-[var(--color-ink-soft)]">Item name *</label>
              <Input placeholder="e.g. Vintage Levi's 501, Knitwear, Branded Polos" {...register('itemName')} />
              {errors.itemName && <p className="mt-1 text-xs text-[var(--color-danger)]">{errors.itemName.message}</p>}
            </div>
            <div className="mb-3">
              <label className="mb-1 block text-xs font-medium text-[var(--color-ink-soft)]">Category *</label>
              <Select {...register('categoryId')}>
                <option value="">Select category...</option>
                {categories?.filter((c) => c.isActive).map((c) => (
                  <option key={c.id} value={c.id}>{c.name}</option>
                ))}
              </Select>
              {errors.categoryId && <p className="mt-1 text-xs text-[var(--color-danger)]">{errors.categoryId.message}</p>}
            </div>
            <div className="grid grid-cols-2 gap-3">
              <div>
                <label className="mb-1 block text-xs font-medium text-[var(--color-ink-soft)]">Grade *</label>
                <Select {...register('grade')}>
                  <option value="A">A — Premium</option>
                  <option value="AB">A/B — Standard</option>
                  <option value="B">B — Budget</option>
                </Select>
              </div>
              <div>
                <label className="mb-1 block text-xs font-medium text-[var(--color-ink-soft)]">Quantity (pieces) *</label>
                <Input type="number" min={30} placeholder="5000" {...register('quantityPcs')} />
                {errors.quantityPcs && <p className="mt-1 text-xs text-[var(--color-danger)]">{errors.quantityPcs.message}</p>}
              </div>
            </div>
            <div className="mt-3 grid grid-cols-2 gap-3">
              <div>
                <label className="mb-1 block text-xs font-medium text-[var(--color-ink-soft)]">Shipping mode</label>
                <Select {...register('shippingMode')}>
                  <option value="sea_freight">Sea freight</option>
                  <option value="air_freight">Air freight</option>
                </Select>
              </div>
              <div>
                <label className="mb-1 block text-xs font-medium text-[var(--color-ink-soft)]">Timeline</label>
                <Select {...register('deliveryTimelineDays')}>
                  <option value={5}>Within 5 days</option>
                  <option value={10}>Within 10 days</option>
                  <option value={15}>Within 15 days</option>
                  <option value={30}>Within 30 days</option>
                </Select>
              </div>
            </div>
          </Card>

          <Card>
            <h4 className="mb-3 text-xs font-semibold uppercase tracking-wider text-[var(--color-ink-faint)]">Destination</h4>
            <label className="mb-1 block text-xs font-medium text-[var(--color-ink-soft)]">Country *</label>
            <Select {...register('destinationCountry')}>
              <option value="">Select country...</option>
              {DESTINATION_COUNTRIES.map((c) => (
                <option key={c} value={c}>{c}</option>
              ))}
            </Select>
            {errors.destinationCountry && <p className="mt-1 text-xs text-[var(--color-danger)]">{errors.destinationCountry.message}</p>}
          </Card>

          <Card>
            <h4 className="mb-3 text-xs font-semibold uppercase tracking-wider text-[var(--color-ink-faint)]">Your target price</h4>
            <div className="grid grid-cols-2 gap-3">
              <div>
                <label className="mb-1 block text-xs font-medium text-[var(--color-ink-soft)]">Currency</label>
                <Select {...register('currency')}>
                  <option value="USD">USD</option>
                  <option value="GBP">GBP</option>
                  <option value="EUR">EUR</option>
                </Select>
              </div>
              <div>
                <label className="mb-1 block text-xs font-medium text-[var(--color-ink-soft)]">Price per piece *</label>
                <Input type="number" step="0.01" placeholder="2.50" {...register('pricePerPc')} />
                {errors.pricePerPc && <p className="mt-1 text-xs text-[var(--color-danger)]">{errors.pricePerPc.message}</p>}
              </div>
            </div>
            <p className="mt-2 text-xs text-[var(--color-ink-faint)]">Your max price target. Per-piece cost you're willing to pay.</p>
          </Card>

          <Card>
            <h4 className="mb-3 text-xs font-semibold uppercase tracking-wider text-[var(--color-ink-faint)]">Additional notes</h4>
            <Textarea rows={3} placeholder="Brand preferences, packing requirements, season, anything else..." {...register('notes')} />
          </Card>

          {error && (
            <div className="rounded-[var(--radius-sm)] bg-[var(--color-danger-soft)] px-4 py-3 text-sm text-[var(--color-danger)]">
              {'data' in error && typeof error.data === 'object' && error.data && 'title' in error.data
                ? String((error.data as { title?: string }).title)
                : 'Could not submit — please check the values and try again.'}
            </div>
          )}

          <div className="flex justify-end gap-3">
            <Button type="button" variant="outline" onClick={() => reset()}>Reset</Button>
            <Button type="submit" disabled={isLoading}>{isLoading ? 'Submitting…' : 'Submit requirement'}</Button>
          </div>
        </div>

        <aside>
          <div className="sticky top-24 rounded-[var(--radius-lg)] bg-[var(--color-sage-darker)] p-6 text-white">
            <p className="text-xs font-semibold uppercase tracking-wider text-[var(--color-sidebar-text-muted)]">Estimated total</p>
            <p className="mt-1 text-2xl font-bold">
              {preview ? `${CURRENCY_SYMBOLS[currency] ?? ''}${preview.totalLocal.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })}` : '—'}
            </p>
            <div className="mt-3 flex justify-between border-t border-white/10 pt-3 text-sm text-[var(--color-sidebar-text-muted)]">
              <span>In USD</span>
              <span>{preview ? `$${preview.totalUsd.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })}` : '—'}</span>
            </div>
            <div className="mt-1 flex justify-between text-sm text-[var(--color-sidebar-text-muted)]">
              <span>Est. weight</span>
              <span>{preview ? `${preview.weightKg.toLocaleString(undefined, { maximumFractionDigits: 0 })} kg` : '—'}</span>
            </div>
            <p className="mt-4 border-t border-white/10 pt-4 text-xs leading-relaxed text-[var(--color-sidebar-text-muted)]">
              Shipping calculated separately at deal confirmation. Final pricing locked when offers come back from sellers.
            </p>
          </div>
        </aside>
      </form>
    </PageTransition>
  )
}
