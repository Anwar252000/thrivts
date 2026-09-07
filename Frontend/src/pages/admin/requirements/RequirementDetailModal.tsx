import { useEffect, useState } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { Rocket, Eye, EyeOff, Trash2, Check } from 'lucide-react'
import { Modal, ConfirmDialog, DataTable, type Column } from '@/components/admin'
import { Badge, Button, Input, Select, Textarea, Spinner } from '@/components/ui'
import { statusTone } from '@/features/admin/statusTone'
import { formatUsd } from '@/lib/utils'
import {
  useGetRequirementByIdQuery, useUpdateRequirementMutation, useDeleteRequirementMutation,
  usePostRequirementLiveMutation, useSetRequirementPublicDisplayMutation, useSetRequirementFiltersMutation,
  useSetSellerTargetPriceMutation, useGetBidBoardQuery, useAcceptSellerResponseMutation,
} from '@/features/admin/adminApi'
import type { AdminBidBoardRow, GradeType, SellerTier } from '@/features/admin/adminTypes'
import { OffersPanel } from './OffersPanel'

const editSchema = z.object({
  itemName: z.string().min(1, 'Required'),
  quantityPcs: z.coerce.number().min(1),
  grade: z.enum(['A', 'AB', 'B', 'Mixed']),
  destinationCountry: z.string().min(1, 'Required'),
  buyerTargetPriceUsd: z.coerce.number().min(0),
  adminNotes: z.string().optional(),
})
type EditFormInput = z.input<typeof editSchema>
type EditForm = z.infer<typeof editSchema>

const filtersSchema = z.object({
  minSellerTier: z.enum(['Bronze', 'Silver', 'Gold', 'Platinum']),
  restrictedToTags: z.string().optional(),
})
type FiltersForm = z.infer<typeof filtersSchema>

const sellerTargetSchema = z.object({
  sellerTargetPriceUsd: z.coerce.number().min(0.01, 'Must be greater than 0'),
})
type SellerTargetFormInput = z.input<typeof sellerTargetSchema>
type SellerTargetForm = z.infer<typeof sellerTargetSchema>

interface RequirementDetailModalProps {
  requirementId: string | null
  onClose: () => void
}

export function RequirementDetailModal({ requirementId, onClose }: RequirementDetailModalProps) {
  const { data: requirement } = useGetRequirementByIdQuery(requirementId!, { skip: !requirementId })
  const { data: bidBoard, isLoading: bidsLoading } = useGetBidBoardQuery({ requirementId: requirementId ?? undefined }, { skip: !requirementId })

  const [updateRequirement, { isLoading: saving }] = useUpdateRequirementMutation()
  const [deleteRequirement] = useDeleteRequirementMutation()
  const [postLive] = usePostRequirementLiveMutation()
  const [setPublicDisplay] = useSetRequirementPublicDisplayMutation()
  const [setFilters, { isLoading: savingFilters }] = useSetRequirementFiltersMutation()
  const [setSellerTargetPrice, { isLoading: savingSellerTarget }] = useSetSellerTargetPriceMutation()
  const [acceptBid] = useAcceptSellerResponseMutation()
  const [confirmDelete, setConfirmDelete] = useState(false)

  const { register, handleSubmit, reset, formState: { errors, isDirty } } = useForm<EditFormInput, unknown, EditForm>({ resolver: zodResolver(editSchema) })
  const {
    register: registerFilters, handleSubmit: handleSubmitFilters, reset: resetFilters, formState: { isDirty: filtersDirty },
  } = useForm<FiltersForm>({ resolver: zodResolver(filtersSchema) })
  const {
    register: registerSellerTarget, handleSubmit: handleSubmitSellerTarget, reset: resetSellerTarget,
    formState: { errors: sellerTargetErrors, isDirty: sellerTargetDirty },
  } = useForm<SellerTargetFormInput, unknown, SellerTargetForm>({ resolver: zodResolver(sellerTargetSchema) })

  useEffect(() => {
    if (requirement) {
      reset({
        itemName: requirement.itemName,
        quantityPcs: requirement.quantityPcs,
        grade: requirement.grade,
        destinationCountry: requirement.destinationCountry,
        buyerTargetPriceUsd: requirement.buyerTargetPriceUsd,
        adminNotes: requirement.adminNotes ?? '',
      })
      resetFilters({
        minSellerTier: requirement.minSellerTier,
        restrictedToTags: (requirement.restrictedToTags ?? []).join(', '),
      })
      resetSellerTarget({ sellerTargetPriceUsd: requirement.sellerTargetPriceUsd ?? 0 })
    }
  }, [requirement, reset, resetFilters, resetSellerTarget])

  if (!requirementId) return null

  const onSave = (values: EditForm) => {
    updateRequirement({ requirementId, ...values, grade: values.grade as GradeType })
  }

  const onSaveFilters = (values: FiltersForm) => {
    const restrictedToTags = values.restrictedToTags?.split(',').map((t) => t.trim()).filter(Boolean)
    setFilters({ requirementId, minSellerTier: values.minSellerTier as SellerTier, restrictedToTags: restrictedToTags?.length ? restrictedToTags : null })
  }

  const onSaveSellerTarget = (values: SellerTargetForm) => {
    setSellerTargetPrice({ requirementId, sellerTargetPriceUsd: values.sellerTargetPriceUsd })
  }

  const handleDelete = async (confirmCascade?: boolean) => {
    const result = await deleteRequirement({ requirementId, confirm: !!confirmCascade })
    if ('error' in result && result.error) {
      // Backend returns a validation error explaining the cascade — surfacing the confirm flow.
      setConfirmDelete(true)
      return
    }
    setConfirmDelete(false)
    onClose()
  }

  const bidColumns: Column<AdminBidBoardRow>[] = [
    { header: 'Seller', accessor: (b) => <span className="font-semibold">{b.sellerCompanyName ?? b.sellerAlias}</span> },
    { header: 'Tier', accessor: (b) => <Badge tone="accent">{b.sellerTier}</Badge> },
    { header: 'Qty', accessor: (b) => b.bidPcs, numeric: true },
    { header: 'Buyer sees/pc', accessor: (b) => (b.buyerSeesPerPc != null ? formatUsd(b.buyerSeesPerPc) : '—'), numeric: true },
    { header: 'Status', accessor: (b) => <Badge tone={statusTone(b.bidStatus)}>{b.bidStatus}</Badge> },
    {
      header: '',
      accessor: (b) =>
        b.bidStatus === 'Pending' ? (
          <Button size="sm" onClick={() => acceptBid(b.bidId)}>
            <Check size={14} /> Accept
          </Button>
        ) : null,
    },
  ]

  return (
    <Modal open={!!requirementId} onClose={onClose} title={requirement?.itemName ?? 'Requirement'} subtitle={requirement?.requirementNumber} size="lg">
      {!requirement ? (
        <div className="flex justify-center py-10">
          <Spinner />
        </div>
      ) : (
        <div className="flex flex-col gap-6">
          <div className="flex flex-wrap items-center gap-2">
            <Badge tone={statusTone(requirement.status)}>{requirement.status}</Badge>
          </div>

          <form onSubmit={handleSubmit(onSave)} className="flex flex-col gap-3">
            <p className="text-xs font-semibold uppercase tracking-wider text-[var(--color-ink-faint)]">Edit details</p>
            <div className="grid grid-cols-2 gap-3">
              <Field label="Item name" error={errors.itemName?.message} {...register('itemName')} />
              <Field label="Quantity (pcs)" type="number" error={errors.quantityPcs?.message} {...register('quantityPcs')} />
              <div>
                <label className="mb-1 block text-xs font-medium text-[var(--color-ink-soft)]">Grade</label>
                <Select {...register('grade')}>
                  <option value="A">A</option>
                  <option value="AB">A/B</option>
                  <option value="B">B</option>
                  <option value="Mixed">Mixed</option>
                </Select>
              </div>
              <Field label="Destination country" error={errors.destinationCountry?.message} {...register('destinationCountry')} />
              <Field label="Buyer target price/pc (USD)" type="number" step="0.01" {...register('buyerTargetPriceUsd')} />
            </div>
            <div>
              <label className="mb-1 block text-xs font-medium text-[var(--color-ink-soft)]">Admin notes</label>
              <Textarea rows={2} {...register('adminNotes')} />
            </div>
            <Button type="submit" size="sm" className="self-start" disabled={!isDirty || saving}>
              {saving ? 'Saving…' : 'Save changes'}
            </Button>
          </form>

          <form onSubmit={handleSubmitFilters(onSaveFilters)} className="flex flex-col gap-3 border-t border-[var(--color-line)] pt-5">
            <p className="text-xs font-semibold uppercase tracking-wider text-[var(--color-ink-faint)]">Matching filters (who can see this requirement)</p>
            <div className="grid grid-cols-2 gap-3">
              <div>
                <label className="mb-1 block text-xs font-medium text-[var(--color-ink-soft)]">Minimum seller tier</label>
                <Select {...registerFilters('minSellerTier')}>
                  <option value="Bronze">Bronze</option>
                  <option value="Silver">Silver</option>
                  <option value="Gold">Gold</option>
                  <option value="Platinum">Platinum</option>
                </Select>
              </div>
              <Field label="Restricted to tags (comma-separated, blank = tier only)" {...registerFilters('restrictedToTags')} />
            </div>
            <Button type="submit" size="sm" className="self-start" disabled={!filtersDirty || savingFilters}>
              {savingFilters ? 'Saving…' : 'Save matching filters'}
            </Button>
          </form>

          <form onSubmit={handleSubmitSellerTarget(onSaveSellerTarget)} className="flex flex-col gap-3 border-t border-[var(--color-line)] pt-5">
            <p className="text-xs font-semibold uppercase tracking-wider text-[var(--color-ink-faint)]">Seller target price (max-pay-to-seller — sellers never see this)</p>
            <div className="w-48">
              <Field label="Target price/pc (USD)" type="number" step="0.01" error={sellerTargetErrors.sellerTargetPriceUsd?.message} {...registerSellerTarget('sellerTargetPriceUsd')} />
            </div>
            <Button type="submit" size="sm" className="self-start" disabled={!sellerTargetDirty || savingSellerTarget}>
              {savingSellerTarget ? 'Saving…' : 'Save seller target price'}
            </Button>
          </form>

          <div>
            <p className="mb-2 text-xs font-semibold uppercase tracking-wider text-[var(--color-ink-faint)]">Bid board</p>
            <DataTable columns={bidColumns} rows={bidBoard ?? []} keyFor={(b) => b.bidId} loading={bidsLoading} emptyTitle="No bids yet" />
          </div>

          <div className="border-t border-[var(--color-line)] pt-5">
            <OffersPanel
              requirementId={requirementId}
              requirementSellerCost={requirement.sellerTargetPriceUsd ?? requirement.buyerTargetPriceUsd}
              requirementQuantityPcs={requirement.quantityPcs}
            />
          </div>

          <div className="flex flex-wrap gap-2 border-t border-[var(--color-line)] pt-5">
            {requirement.status === 'PendingReview' && (
              <Button size="sm" onClick={() => postLive(requirementId)}>
                <Rocket size={14} /> Post live
              </Button>
            )}
            <Button size="sm" variant="outline" onClick={() => setPublicDisplay({ requirementId, public: true })}>
              <Eye size={14} /> Show on public feed
            </Button>
            <Button size="sm" variant="outline" onClick={() => setPublicDisplay({ requirementId, public: false })}>
              <EyeOff size={14} /> Hide from public feed
            </Button>
            <Button size="sm" variant="danger" className="ml-auto" onClick={() => setConfirmDelete(true)}>
              <Trash2 size={14} /> Delete
            </Button>
          </div>
        </div>
      )}

      <ConfirmDialog
        open={confirmDelete}
        onClose={() => setConfirmDelete(false)}
        onConfirm={() => handleDelete(true)}
        title="Delete this requirement?"
        description="If deals already exist for this requirement, they — and their allocations, commissions, and disputes — will be permanently deleted too. This cannot be undone."
        confirmLabel="Delete permanently"
      />
    </Modal>
  )
}

function Field({
  label, error, ...props
}: { label: string; error?: string } & React.InputHTMLAttributes<HTMLInputElement>) {
  return (
    <div>
      <label className="mb-1 block text-xs font-medium text-[var(--color-ink-soft)]">{label}</label>
      <Input {...props} />
      {error && <p className="mt-1 text-xs text-[var(--color-danger)]">{error}</p>}
    </div>
  )
}
