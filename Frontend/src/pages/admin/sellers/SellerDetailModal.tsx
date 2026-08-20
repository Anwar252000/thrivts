import { Fragment, useEffect, useState } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { Check, X, ShieldOff, ShieldCheck, Trash2, BadgeCheck } from 'lucide-react'
import { Modal, ConfirmDialog } from '@/components/admin'
import { Badge, Button, Input, Select, Spinner } from '@/components/ui'
import { statusTone } from '@/features/admin/statusTone'
import { formatDate, formatUsd, formatNumber } from '@/lib/utils'
import {
  useGetSellerByIdQuery, useSetSellerApprovalMutation, useSetSellerKycMutation, useUpdateSellerMutation, useDeleteSellerMutation,
} from '@/features/admin/adminApi'
import type { SellerTier } from '@/features/admin/adminTypes'
import { ApproveSellerModal } from './ApproveSellerModal'

const editSchema = z.object({
  tier: z.enum(['Bronze', 'Silver', 'Gold', 'Platinum']),
  phone: z.string().optional(),
  whatsApp: z.string().optional(),
  referenceContact: z.string().optional(),
  tags: z.string().optional(),
})
type EditForm = z.infer<typeof editSchema>

interface SellerDetailModalProps {
  sellerId: string | null
  onClose: () => void
}

export function SellerDetailModal({ sellerId, onClose }: SellerDetailModalProps) {
  const { data: seller, isLoading } = useGetSellerByIdQuery(sellerId!, { skip: !sellerId })
  const [setApproval] = useSetSellerApprovalMutation()
  const [setKyc] = useSetSellerKycMutation()
  const [updateSeller, { isLoading: saving }] = useUpdateSellerMutation()
  const [deleteSeller] = useDeleteSellerMutation()
  const [rejecting, setRejecting] = useState(false)
  const [confirmDelete, setConfirmDelete] = useState(false)
  const [approving, setApproving] = useState(false)

  const { register, handleSubmit, reset, formState: { isDirty } } = useForm<EditForm>({ resolver: zodResolver(editSchema) })

  useEffect(() => {
    if (seller) {
      reset({
        tier: seller.tier,
        phone: seller.phone ?? '',
        whatsApp: seller.whatsApp ?? '',
        referenceContact: seller.referenceContact ?? '',
        tags: (seller.tags ?? []).join(', '),
      })
    }
  }, [seller, reset])

  if (!sellerId) return null

  const onSave = (values: EditForm) => {
    const tags = values.tags?.split(',').map((t) => t.trim()).filter(Boolean)
    updateSeller({ sellerId, tier: values.tier as SellerTier, phone: values.phone, whatsApp: values.whatsApp, referenceContact: values.referenceContact, tags })
  }

  const handleDelete = async () => {
    await deleteSeller(sellerId)
    setConfirmDelete(false)
    onClose()
  }

  return (
    <Fragment>
    <Modal open={!!sellerId} onClose={onClose} title={seller?.companyName ?? seller?.publicAlias ?? 'Seller'} subtitle={seller?.email}>
      {isLoading || !seller ? (
        <div className="flex justify-center py-10">
          <Spinner />
        </div>
      ) : (
        <div className="flex flex-col gap-6">
          <div className="flex flex-wrap items-center gap-2">
            <Badge tone={statusTone(seller.approvalStatus)}>{seller.approvalStatus}</Badge>
            {!seller.isActive && <Badge tone="danger">Blocked</Badge>}
            <Badge tone="accent">{seller.tier}</Badge>
            <Badge tone={seller.kycVerified ? 'success' : 'warning'}>{seller.kycVerified ? 'KYC verified' : 'KYC pending'}</Badge>
          </div>

          <div className="grid grid-cols-2 gap-4 text-sm">
            <Field label="Public alias" value={seller.publicAlias} />
            <Field label="Seller code" value={seller.sellerCode ?? '—'} />
            <Field label="Location" value={`${seller.locationCity}, ${seller.locationCountry}`} />
            <Field label="Years in business" value={seller.yearsInBusiness ? String(seller.yearsInBusiness) : '—'} />
            <Field label="Orders fulfilled" value={formatNumber(seller.totalOrdersFulfilled)} />
            <Field label="Total paid" value={formatUsd(seller.totalPaidUsd)} />
            <Field label="Pcs supplied" value={formatNumber(seller.totalPcsSupplied)} />
            <Field label="Disputes / backouts" value={`${seller.disputeCount} / ${seller.backoutCount}`} />
            <Field label="Joined" value={formatDate(seller.createdAt)} />
          </div>

          {seller.categoriesSupplied.length > 0 && (
            <div>
              <p className="mb-1.5 text-xs font-semibold uppercase tracking-wider text-[var(--color-ink-faint)]">Categories supplied</p>
              <div className="flex flex-wrap gap-1.5">
                {seller.categoriesSupplied.map((c) => (
                  <Badge key={c} tone="neutral">{c}</Badge>
                ))}
              </div>
            </div>
          )}

          <form onSubmit={handleSubmit(onSave)} className="flex flex-col gap-3 border-t border-[var(--color-line)] pt-5">
            <p className="text-xs font-semibold uppercase tracking-wider text-[var(--color-ink-faint)]">Edit details</p>
            <div className="grid grid-cols-2 gap-3">
              <div>
                <label className="mb-1 block text-xs font-medium text-[var(--color-ink-soft)]">Tier</label>
                <Select {...register('tier')}>
                  <option value="Bronze">Bronze</option>
                  <option value="Silver">Silver</option>
                  <option value="Gold">Gold</option>
                  <option value="Platinum">Platinum</option>
                </Select>
              </div>
              <LabeledInput label="Phone" {...register('phone')} />
              <LabeledInput label="WhatsApp" {...register('whatsApp')} />
              <LabeledInput label="Reference contact" {...register('referenceContact')} />
              <div className="col-span-2">
                <LabeledInput label="Tags (comma-separated — controls which requirements this seller sees)" {...register('tags')} />
              </div>
            </div>
            <Button type="submit" size="sm" className="self-start" disabled={!isDirty || saving}>
              {saving ? 'Saving…' : 'Save changes'}
            </Button>
          </form>

          <div className="flex flex-wrap gap-2 border-t border-[var(--color-line)] pt-5">
            {seller.approvalStatus === 'Pending' && (
              <>
                <Button size="sm" onClick={() => setApproving(true)}>
                  <Check size={14} /> Approve
                </Button>
                <Button size="sm" variant="outline" onClick={() => setRejecting(true)}>
                  <X size={14} /> Reject
                </Button>
              </>
            )}
            {seller.kycVerified ? (
              <Button size="sm" variant="outline" onClick={() => setKyc({ sellerId, verified: false })}>
                <BadgeCheck size={14} /> Un-verify KYC
              </Button>
            ) : (
              <Button size="sm" variant="outline" onClick={() => setKyc({ sellerId, verified: true })}>
                <BadgeCheck size={14} /> Verify KYC
              </Button>
            )}
            {seller.isActive ? (
              <Button size="sm" variant="outline" onClick={() => setApproval({ sellerId, action: 'Block' })}>
                <ShieldOff size={14} /> Block
              </Button>
            ) : (
              <Button size="sm" variant="outline" onClick={() => setApproval({ sellerId, action: 'Unblock' })}>
                <ShieldCheck size={14} /> Unblock
              </Button>
            )}
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
          setApproval({ sellerId, action: 'Reject', reason: reason ?? 'No reason given' })
          setRejecting(false)
        }}
        title="Reject this seller?"
        description="They'll be notified the application was rejected."
        requireReason
        confirmLabel="Reject"
      />
      <ConfirmDialog
        open={confirmDelete}
        onClose={() => setConfirmDelete(false)}
        onConfirm={handleDelete}
        title="Delete this seller?"
        description="This removes the seller profile permanently. This cannot be undone."
        confirmLabel="Delete"
      />
    </Modal>
    <ApproveSellerModal sellerId={approving ? sellerId : null} onClose={() => setApproving(false)} />
    </Fragment>
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
