import { useEffect, useState } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { Check, X, ShieldOff, ShieldCheck } from 'lucide-react'
import { Modal, ConfirmDialog } from '@/components/admin'
import { Badge, Button, Input, Spinner } from '@/components/ui'
import { formatDate, formatUsd, formatNumber } from '@/lib/utils'
import { useGetAgencyByIdQuery, useSetAgencyApprovalMutation, useUpdateAgencyMutation } from '@/features/admin/adminApi'

const editSchema = z.object({
  commissionRate: z.coerce.number().min(0).max(100),
})
type EditFormInput = z.input<typeof editSchema>
type EditForm = z.infer<typeof editSchema>

interface AgencyDetailModalProps {
  agencyId: string | null
  onClose: () => void
}

export function AgencyDetailModal({ agencyId, onClose }: AgencyDetailModalProps) {
  const { data: agency, isLoading } = useGetAgencyByIdQuery(agencyId!, { skip: !agencyId })
  const [setApproval] = useSetAgencyApprovalMutation()
  const [updateAgency, { isLoading: saving }] = useUpdateAgencyMutation()
  const [rejecting, setRejecting] = useState(false)

  const { register, handleSubmit, reset, formState: { errors, isDirty } } = useForm<EditFormInput, unknown, EditForm>({ resolver: zodResolver(editSchema) })

  useEffect(() => {
    if (agency) reset({ commissionRate: agency.commissionRate })
  }, [agency, reset])

  if (!agencyId) return null

  const onSave = (values: EditForm) => {
    updateAgency({ agencyId, commissionRate: values.commissionRate })
  }

  return (
    <Modal open={!!agencyId} onClose={onClose} title={agency?.agencyName ?? 'Agency'} subtitle={agency?.email}>
      {isLoading || !agency ? (
        <div className="flex justify-center py-10">
          <Spinner />
        </div>
      ) : (
        <div className="flex flex-col gap-6">
          <div className="flex flex-wrap items-center gap-2">
            <Badge tone="neutral">{agency.agencyCode}</Badge>
            {!agency.isActive && <Badge tone="danger">Blocked</Badge>}
          </div>

          <div className="grid grid-cols-2 gap-4 text-sm">
            <Field label="Owner" value={agency.ownerFullName} />
            <Field label="Location" value={[agency.city, agency.country].filter(Boolean).join(', ')} />
            <Field label="Team size" value={agency.teamSize ? String(agency.teamSize) : '—'} />
            <Field label="Commission rate" value={`${agency.commissionRate}%`} />
            <Field label="Buyers referred" value={formatNumber(agency.totalBuyersReferred)} />
            <Field label="Deals closed" value={formatNumber(agency.totalDealsClosed)} />
            <Field label="Commission earned" value={formatUsd(agency.totalCommissionEarnedUsd)} />
            <Field label="Commission paid" value={formatUsd(agency.totalCommissionPaidUsd)} />
            <Field label="Commission pending" value={formatUsd(agency.totalCommissionPendingUsd)} />
            <Field label="Joined" value={formatDate(agency.createdAt)} />
          </div>

          {agency.notes && (
            <div>
              <p className="mb-1 text-xs font-semibold uppercase tracking-wider text-[var(--color-ink-faint)]">Notes</p>
              <p className="text-sm text-[var(--color-ink-soft)]">{agency.notes}</p>
            </div>
          )}

          <form onSubmit={handleSubmit(onSave)} className="flex flex-col gap-3 border-t border-[var(--color-line)] pt-5">
            <p className="text-xs font-semibold uppercase tracking-wider text-[var(--color-ink-faint)]">Edit commission rate</p>
            <div className="flex items-end gap-3">
              <div className="w-40">
                <label className="mb-1 block text-xs font-medium text-[var(--color-ink-soft)]">Commission rate %</label>
                <Input type="number" step="0.01" {...register('commissionRate')} />
                {errors.commissionRate && <p className="mt-1 text-xs text-[var(--color-danger)]">{errors.commissionRate.message}</p>}
              </div>
              <Button type="submit" size="sm" disabled={!isDirty || saving}>
                {saving ? 'Saving…' : 'Save'}
              </Button>
            </div>
          </form>

          <div className="flex flex-wrap gap-2 border-t border-[var(--color-line)] pt-5">
            <Button size="sm" onClick={() => setApproval({ agencyId, action: 'Approve' })}>
              <Check size={14} /> Approve
            </Button>
            <Button size="sm" variant="outline" onClick={() => setRejecting(true)}>
              <X size={14} /> Reject
            </Button>
            {agency.isActive ? (
              <Button size="sm" variant="outline" onClick={() => setApproval({ agencyId, action: 'Block' })}>
                <ShieldOff size={14} /> Block
              </Button>
            ) : (
              <Button size="sm" variant="outline" onClick={() => setApproval({ agencyId, action: 'Unblock' })}>
                <ShieldCheck size={14} /> Unblock
              </Button>
            )}
          </div>
        </div>
      )}

      <ConfirmDialog
        open={rejecting}
        onClose={() => setRejecting(false)}
        onConfirm={(reason) => {
          setApproval({ agencyId, action: 'Reject', reason: reason ?? 'No reason given' })
          setRejecting(false)
        }}
        title="Reject this agency?"
        description="They'll be notified the application was rejected."
        requireReason
        confirmLabel="Reject"
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
