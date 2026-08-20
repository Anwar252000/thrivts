import { useEffect, useState } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { Check, X, ShieldOff, ShieldCheck, Trash2, Star } from 'lucide-react'
import { Modal, ConfirmDialog } from '@/components/admin'
import { Badge, Button, Input, Spinner } from '@/components/ui'
import { statusTone } from '@/features/admin/statusTone'
import { formatDate, formatUsd, formatNumber } from '@/lib/utils'
import {
  useGetBuyerByIdQuery, useSetBuyerApprovalMutation, useUpdateBuyerMutation, useDeleteBuyerMutation, useSetBuyerPremiumMutation,
} from '@/features/admin/adminApi'

const editSchema = z.object({
  companyName: z.string().min(1, 'Required'),
  country: z.string().min(1, 'Required'),
  city: z.string().optional(),
  website: z.string().optional(),
  instagram: z.string().optional(),
})
type EditForm = z.infer<typeof editSchema>

interface BuyerDetailModalProps {
  buyerId: string | null
  onClose: () => void
}

export function BuyerDetailModal({ buyerId, onClose }: BuyerDetailModalProps) {
  const { data: buyer, isLoading } = useGetBuyerByIdQuery(buyerId!, { skip: !buyerId })
  const [setApproval] = useSetBuyerApprovalMutation()
  const [updateBuyer, { isLoading: saving }] = useUpdateBuyerMutation()
  const [deleteBuyer] = useDeleteBuyerMutation()
  const [setPremium] = useSetBuyerPremiumMutation()
  const [rejecting, setRejecting] = useState(false)
  const [confirmDelete, setConfirmDelete] = useState(false)

  const { register, handleSubmit, reset, formState: { errors, isDirty } } = useForm<EditForm>({ resolver: zodResolver(editSchema) })

  useEffect(() => {
    if (buyer) reset({ companyName: buyer.companyName, country: buyer.country, city: buyer.city ?? '', website: buyer.website ?? '', instagram: buyer.instagram ?? '' })
  }, [buyer, reset])

  if (!buyerId) return null

  const onSave = (values: EditForm) => {
    updateBuyer({ buyerId, ...values })
  }

  const handleDelete = async () => {
    await deleteBuyer(buyerId)
    setConfirmDelete(false)
    onClose()
  }

  return (
    <Modal open={!!buyerId} onClose={onClose} title={buyer?.companyName ?? 'Buyer'} subtitle={buyer?.email}>
      {isLoading || !buyer ? (
        <div className="flex justify-center py-10">
          <Spinner />
        </div>
      ) : (
        <div className="flex flex-col gap-6">
          <div className="flex flex-wrap items-center gap-2">
            <Badge tone={statusTone(buyer.approvalStatus)}>{buyer.approvalStatus}</Badge>
            {!buyer.isActive && <Badge tone="danger">Blocked</Badge>}
            {buyer.isPremium && <Badge tone="accent">Premium</Badge>}
          </div>

          <div className="grid grid-cols-2 gap-4 text-sm">
            <Field label="Total orders" value={formatNumber(buyer.totalOrders)} />
            <Field label="Total spend" value={formatUsd(buyer.totalSpendUsd)} />
            <Field label="Phone" value={buyer.phone ?? '—'} />
            <Field label="WhatsApp" value={buyer.whatsApp ?? '—'} />
            <Field label="First order" value={formatDate(buyer.firstOrderAt)} />
            <Field label="Joined" value={formatDate(buyer.createdAt)} />
          </div>

          {buyer.categoriesOfInterest && buyer.categoriesOfInterest.length > 0 && (
            <div>
              <p className="mb-1.5 text-xs font-semibold uppercase tracking-wider text-[var(--color-ink-faint)]">Categories of interest</p>
              <div className="flex flex-wrap gap-1.5">
                {buyer.categoriesOfInterest.map((c) => (
                  <Badge key={c} tone="neutral">{c}</Badge>
                ))}
              </div>
            </div>
          )}

          <form onSubmit={handleSubmit(onSave)} className="flex flex-col gap-3 border-t border-[var(--color-line)] pt-5">
            <p className="text-xs font-semibold uppercase tracking-wider text-[var(--color-ink-faint)]">Edit details</p>
            <div className="grid grid-cols-2 gap-3">
              <LabeledInput label="Company name" error={errors.companyName?.message} {...register('companyName')} />
              <LabeledInput label="Country" error={errors.country?.message} {...register('country')} />
              <LabeledInput label="City" {...register('city')} />
              <LabeledInput label="Website" {...register('website')} />
              <LabeledInput label="Instagram" {...register('instagram')} />
            </div>
            <Button type="submit" size="sm" className="self-start" disabled={!isDirty || saving}>
              {saving ? 'Saving…' : 'Save changes'}
            </Button>
          </form>

          <div className="flex flex-wrap gap-2 border-t border-[var(--color-line)] pt-5">
            {buyer.approvalStatus === 'Pending' && (
              <>
                <Button size="sm" onClick={() => setApproval({ buyerId, action: 'Approve' })}>
                  <Check size={14} /> Approve
                </Button>
                <Button size="sm" variant="outline" onClick={() => setRejecting(true)}>
                  <X size={14} /> Reject
                </Button>
              </>
            )}
            {buyer.isActive ? (
              <Button size="sm" variant="outline" onClick={() => setApproval({ buyerId, action: 'Block' })}>
                <ShieldOff size={14} /> Block
              </Button>
            ) : (
              <Button size="sm" variant="outline" onClick={() => setApproval({ buyerId, action: 'Unblock' })}>
                <ShieldCheck size={14} /> Unblock
              </Button>
            )}
            <Button size="sm" variant="outline" onClick={() => setPremium({ buyerId, isPremium: !buyer.isPremium })}>
              <Star size={14} /> {buyer.isPremium ? 'Remove premium' : 'Mark premium'}
            </Button>
            <Button size="sm" variant="danger" className="ml-auto" onClick={() => setConfirmDelete(true)}>
              <Trash2 size={14} /> Delete
            </Button>
          </div>
        </div>
      )}

      <ConfirmDialog
        open={rejecting}
        onClose={() => setRejecting(false)}
        onConfirm={(reason) => {
          setApproval({ buyerId, action: 'Reject', reason: reason ?? 'No reason given' })
          setRejecting(false)
        }}
        title="Reject this buyer?"
        description="They'll be notified the application was rejected."
        requireReason
        confirmLabel="Reject"
      />
      <ConfirmDialog
        open={confirmDelete}
        onClose={() => setConfirmDelete(false)}
        onConfirm={handleDelete}
        title="Delete this buyer?"
        description="This removes the buyer profile permanently. This cannot be undone."
        confirmLabel="Delete"
      />
    </Modal>
  )
}

function Field({ label, value }: { label: string; value: string }) {
  return (
    <div>
      <p className="text-xs font-semibold uppercase tracking-wider text-[var(--color-ink-faint)]">{label}</p>
      <p className="mt-0.5 font-medium">{value}</p>
    </div>
  )
}

function LabeledInput({ label, error, ...props }: { label: string; error?: string } & React.InputHTMLAttributes<HTMLInputElement>) {
  return (
    <div>
      <label className="mb-1 block text-xs font-medium text-[var(--color-ink-soft)]">{label}</label>
      <Input {...props} />
      {error && <p className="mt-1 text-xs text-[var(--color-danger)]">{error}</p>}
    </div>
  )
}
