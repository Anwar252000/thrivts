import { useEffect } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { Modal } from '@/components/admin'
import { Button, Input } from '@/components/ui'
import { useCreateInfluencerMutation } from '@/features/admin/adminApi'

const schema = z.object({
  fullName: z.string().min(1, 'Required'),
  email: z.string().email(),
  phone: z.string().optional(),
  instagram: z.string().optional(),
  tiktok: z.string().optional(),
  referralCode: z.string().optional(),
  commissionRate: z.coerce.number().min(0).max(1),
})
type FormInput = z.input<typeof schema>
type FormValues = z.infer<typeof schema>

interface CreateInfluencerModalProps {
  open: boolean
  onClose: () => void
  /** Pre-fills from an approved partner application, and reports back the new influencer id
   * so the caller can complete the two-step approve flow (create influencer -> link application). */
  prefill?: { fullName: string; email: string; instagram?: string | null; tiktok?: string | null }
  onCreated?: (influencerId: string) => void
}

export function CreateInfluencerModal({ open, onClose, prefill, onCreated }: CreateInfluencerModalProps) {
  const [createInfluencer, { isLoading }] = useCreateInfluencerMutation()
  const { register, handleSubmit, reset, formState: { errors } } = useForm<FormInput, unknown, FormValues>({
    resolver: zodResolver(schema),
    defaultValues: { commissionRate: 0.05 },
  })

  useEffect(() => {
    if (open) {
      reset({
        fullName: prefill?.fullName ?? '', email: prefill?.email ?? '', instagram: prefill?.instagram ?? '',
        tiktok: prefill?.tiktok ?? '', commissionRate: 0.05,
      })
    }
  }, [open, prefill, reset])

  const onSubmit = async (values: FormValues) => {
    const result = await createInfluencer(values).unwrap()
    onCreated?.(result.id)
    onClose()
  }

  return (
    <Modal
      open={open}
      onClose={onClose}
      title="Add new influencer"
      subtitle="They earn a recurring commission on every order from buyers who join with their code."
      footer={
        <>
          <Button variant="outline" onClick={onClose}>Cancel</Button>
          <Button onClick={handleSubmit(onSubmit)} disabled={isLoading}>
            {isLoading ? 'Creating…' : 'Create influencer'}
          </Button>
        </>
      }
    >
      <form className="grid grid-cols-2 gap-3" onSubmit={handleSubmit(onSubmit)}>
        <Field label="Full name" full error={errors.fullName?.message} {...register('fullName')} />
        <Field label="Email" full error={errors.email?.message} {...register('email')} />
        <Field label="Phone" {...register('phone')} />
        <Field label="Referral code (optional)" {...register('referralCode')} />
        <Field label="Instagram" {...register('instagram')} />
        <Field label="TikTok" {...register('tiktok')} />
        <Field label="Commission rate (fraction, e.g. 0.05 = 5%)" full type="number" step="0.01" error={errors.commissionRate?.message} {...register('commissionRate')} />
      </form>
    </Modal>
  )
}

function Field({
  label, error, full, ...props
}: { label: string; error?: string; full?: boolean } & React.InputHTMLAttributes<HTMLInputElement>) {
  return (
    <div className={full ? 'col-span-2' : undefined}>
      <label className="mb-1 block text-xs font-medium text-[var(--color-ink-soft)]">{label}</label>
      <Input {...props} />
      {error && <p className="mt-1 text-xs text-[var(--color-danger)]">{error}</p>}
    </div>
  )
}
