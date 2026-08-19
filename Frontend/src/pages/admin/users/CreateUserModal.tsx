import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { Modal } from '@/components/admin'
import { Button, Input, Select } from '@/components/ui'
import { useCreateUserMutation } from '@/features/admin/adminApi'
import type { UserRoleEnum } from '@/features/admin/adminTypes'

const schema = z.object({
  email: z.string().email(),
  password: z.string().min(8, 'At least 8 characters'),
  fullName: z.string().min(1, 'Required'),
  role: z.enum(['Buyer', 'Seller', 'Agency', 'Admin']),
  phone: z.string().optional(),
  whatsApp: z.string().optional(),
  companyName: z.string().optional(),
  country: z.string().optional(),
  publicAlias: z.string().optional(),
  locationCity: z.string().optional(),
  locationCountry: z.string().optional(),
  agencyName: z.string().optional(),
  commissionRate: z.coerce.number().min(0).max(100).optional(),
})
type FormInput = z.input<typeof schema>
type FormValues = z.infer<typeof schema>

interface CreateUserModalProps {
  open: boolean
  onClose: () => void
}

export function CreateUserModal({ open, onClose }: CreateUserModalProps) {
  const [createUser, { isLoading, error }] = useCreateUserMutation()
  const { register, handleSubmit, watch, reset, formState: { errors } } = useForm<FormInput, unknown, FormValues>({
    resolver: zodResolver(schema),
    defaultValues: { role: 'Buyer' as UserRoleEnum },
  })
  const role = watch('role')

  const onSubmit = async (values: FormValues) => {
    await createUser(values).unwrap()
    reset()
    onClose()
  }

  const errorMessage = error && 'data' in error ? (error.data as { title?: string })?.title : undefined

  return (
    <Modal
      open={open}
      onClose={onClose}
      title="Add new user"
      subtitle="Creates a real login (Supabase Auth account) directly — no signup or approval step needed."
      footer={
        <>
          <Button variant="outline" onClick={onClose}>Cancel</Button>
          <Button onClick={handleSubmit(onSubmit)} disabled={isLoading}>
            {isLoading ? 'Creating…' : 'Create user'}
          </Button>
        </>
      }
    >
      <form className="grid grid-cols-2 gap-3" onSubmit={handleSubmit(onSubmit)}>
        <Field label="Email" full error={errors.email?.message} {...register('email')} />
        <Field label="Password" full type="password" error={errors.password?.message} {...register('password')} />
        <Field label="Full name" error={errors.fullName?.message} {...register('fullName')} />
        <div>
          <label className="mb-1 block text-xs font-medium text-[var(--color-ink-soft)]">Role</label>
          <Select {...register('role')}>
            <option value="Buyer">Buyer</option>
            <option value="Seller">Seller</option>
            <option value="Agency">Agency</option>
            <option value="Admin">Admin</option>
          </Select>
        </div>
        <Field label="Phone" {...register('phone')} />
        <Field label="WhatsApp" {...register('whatsApp')} />

        {role === 'Buyer' && (
          <>
            <Field label="Company name" error={errors.companyName?.message} {...register('companyName')} />
            <Field label="Country" error={errors.country?.message} {...register('country')} />
          </>
        )}

        {role === 'Seller' && (
          <>
            <Field label="Public alias (optional)" {...register('publicAlias')} />
            <Field label="City" error={errors.locationCity?.message} {...register('locationCity')} />
            <Field label="Country" {...register('locationCountry')} />
          </>
        )}

        {role === 'Agency' && (
          <>
            <Field label="Agency name" error={errors.agencyName?.message} {...register('agencyName')} />
            <Field label="Country" error={errors.country?.message} {...register('country')} />
            <Field label="Commission rate %" type="number" step="0.01" {...register('commissionRate')} />
          </>
        )}

        {errorMessage && <p className="col-span-2 text-xs text-[var(--color-danger)]">{errorMessage}</p>}
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
