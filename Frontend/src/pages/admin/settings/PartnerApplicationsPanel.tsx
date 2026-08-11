import { useState } from 'react'
import { Check, X, Handshake } from 'lucide-react'
import { Card, Button } from '@/components/ui'
import { EmptyState, ConfirmDialog } from '@/components/admin'
import { useGetPartnerApplicationsQuery, useSetPartnerApplicationStatusMutation } from '@/features/admin/adminApi'
import type { PartnerApplication } from '@/features/admin/adminTypes'
import { CreateInfluencerModal } from './CreateInfluencerModal'

export function PartnerApplicationsPanel() {
  const { data: applications } = useGetPartnerApplicationsQuery({ status: 'pending' })
  const [setStatus] = useSetPartnerApplicationStatusMutation()
  const [approving, setApproving] = useState<PartnerApplication | null>(null)
  const [rejecting, setRejecting] = useState<PartnerApplication | null>(null)

  return (
    <Card>
      <p className="mb-1 font-semibold">Partner applications</p>
      <p className="mb-4 text-sm text-[var(--color-ink-faint)]">
        People who applied through the partner landing page. Approving creates their influencer profile.
      </p>

      {!applications || applications.length === 0 ? (
        <EmptyState icon={Handshake} title="No pending applications" />
      ) : (
        <div className="flex max-h-80 flex-col gap-2 overflow-y-auto">
          {applications.map((a) => (
            <div key={a.id} className="flex items-center justify-between gap-3 rounded-[var(--radius-sm)] border border-[var(--color-line)] px-3 py-2 text-sm">
              <div className="min-w-0">
                <p className="truncate font-medium">{a.fullName}</p>
                <p className="truncate text-xs text-[var(--color-ink-faint)]">{a.email}{a.instagram ? ` · @${a.instagram}` : ''}</p>
              </div>
              <div className="flex shrink-0 gap-1.5">
                <Button size="sm" variant="outline" onClick={() => setRejecting(a)}>
                  <X size={14} />
                </Button>
                <Button size="sm" onClick={() => setApproving(a)}>
                  <Check size={14} />
                </Button>
              </div>
            </div>
          ))}
        </div>
      )}

      <CreateInfluencerModal
        open={!!approving}
        onClose={() => setApproving(null)}
        prefill={approving ? { fullName: approving.fullName, email: approving.email, instagram: approving.instagram, tiktok: approving.tiktok } : undefined}
        onCreated={(influencerId) => {
          if (approving) setStatus({ applicationId: approving.id, approve: true, influencerId })
        }}
      />

      <ConfirmDialog
        open={!!rejecting}
        onClose={() => setRejecting(null)}
        onConfirm={() => {
          if (rejecting) setStatus({ applicationId: rejecting.id, approve: false })
          setRejecting(null)
        }}
        title="Reject this application?"
        description="The applicant will not be onboarded as an influencer."
        confirmLabel="Reject"
      />
    </Card>
  )
}
