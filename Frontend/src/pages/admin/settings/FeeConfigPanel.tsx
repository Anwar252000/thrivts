import { useEffect } from 'react'
import { useForm } from 'react-hook-form'
import { Card, Button, Input } from '@/components/ui'
import { useGetFeeConfigQuery, useUpdateFeeConfigMutation } from '@/features/admin/adminApi'
import { formatDateTime } from '@/lib/utils'

export function FeeConfigPanel() {
  const { data: config } = useGetFeeConfigQuery()
  const [updateFeeConfig, { isLoading }] = useUpdateFeeConfigMutation()
  const { register, handleSubmit, reset, formState: { isDirty } } = useForm<{ feePerPcUsd: number; pkrReference: string }>()

  useEffect(() => {
    if (config) reset({ feePerPcUsd: config.feePerPcUsd, pkrReference: config.pkrReference })
  }, [config, reset])

  return (
    <Card>
      <p className="mb-1 font-semibold">Platform fee</p>
      <p className="mb-4 text-sm text-[var(--color-ink-faint)]">The per-piece fee added to every seller price the buyer sees.</p>
      <form
        className="flex flex-wrap items-end gap-3"
        onSubmit={handleSubmit((v) => updateFeeConfig({ feePerPcUsd: Number(v.feePerPcUsd), pkrReference: v.pkrReference }))}
      >
        <div>
          <label className="mb-1 block text-xs font-medium text-[var(--color-ink-soft)]">Fee per piece (USD)</label>
          <Input type="number" step="0.01" className="w-36" {...register('feePerPcUsd')} />
        </div>
        <div>
          <label className="mb-1 block text-xs font-medium text-[var(--color-ink-soft)]">PKR reference</label>
          <Input className="w-40" {...register('pkrReference')} />
        </div>
        <Button type="submit" size="sm" disabled={!isDirty || isLoading}>
          {isLoading ? 'Saving…' : 'Save'}
        </Button>
      </form>
      {config && <p className="mt-3 text-xs text-[var(--color-ink-faint)]">Last updated {formatDateTime(config.updatedAt)}</p>}
    </Card>
  )
}
