import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { Modal } from '@/components/admin'
import { Button, Input } from '@/components/ui'
import { useCreateAgencyMutation } from '@/features/admin/adminApi'

const schema = z.object({
  userId: z.string().uuid('Must be a valid Supabase auth user id'),
  email: z.string().email(),
  agencyName: z.string().min(1, 'Required'),
  ownerFullName: z.string().min(1, 'Required'),
  country: z.string().min(1, 'Required'),
  city: z.string().optional(),
  phone: z.string().optional(),
  whatsApp: z.string().optional(),
  commissionRate: z.coerce.number().min(0).max(100),
  teamSize: z.coerce.number().min(1).optional(),
  notes: z.string().optional(),
})
type FormInput = z.input<typeof schema>
type FormValues = z.infer<typeof schema>

interface CreateAgencyModalProps {
  open: boolean
  onClose: () => void
}

export function CreateAgencyModal({ open, onClose }: CreateAgencyModalProps) {
  const [createAgency, { isLoading }] = useCreateAgencyMutation()
  const { register, handleSubmit, reset, formState: { errors } } = useForm<FormInput, unknown, FormValues>({
    resolver: zodResolver(schema),
    defaultValues: { commissionRate: 30 },
  })

  const onSubmit = async (values: FormValues) => {
    await createAgency(values).unwrap()
    reset()
    onClose()
  }

  return (
    <Modal
      open={open}
      onClose={onClose}
      title="Add new agency"
      subtitle="Create an agency partner account. The Supabase auth user must already exist — create their login first, then paste its user id here."
      footer={
        <>
          <Button variant="outline" onClick={onClose}>Cancel</Button>
          <Button onClick={handleSubmit(onSubmit)} disabled={isLoading}>
            {isLoading ? 'Creating…' : 'Create agency'}
          </Button>
        </>
      }
    >
      <form className="grid grid-cols-2 gap-3" onSubmit={handleSubmit(onSubmit)}>
        <Field label="Auth user id (UUID)" error={errors.userId?.message} full {...register('userId')} />
        <Field label="Email" error={errors.email?.message} full {...register('email')} />
        <Field label="Agency name" error={errors.agencyName?.message} {...register('agencyName')} />
        <Field label="Owner full name" error={errors.ownerFullName?.message} {...register('ownerFullName')} />
        <Field label="Country" error={errors.country?.message} {...register('country')} />
        <Field label="City" {...register('city')} />
        <Field label="Phone" {...register('phone')} />
        <Field label="WhatsApp" {...register('whatsApp')} />
        <Field label="Commission rate %" type="number" step="0.01" error={errors.commissionRate?.message} {...register('commissionRate')} />
        <Field label="Team size" type="number" {...register('teamSize')} />
        <Field label="Notes" full {...register('notes')} />
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
