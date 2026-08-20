import { useMemo, useState } from 'react'
import { Check, X, UserCheck } from 'lucide-react'
import { Card, Badge, Button, PageTransition } from '@/components/ui'
import { Tabs, SearchField, ConfirmDialog, EmptyState } from '@/components/admin'
import {
  useGetPendingApprovalsQuery, useSetBuyerApprovalMutation, useSetSellerApprovalMutation, useSetAgencyApprovalMutation,
} from '@/features/admin/adminApi'
import type { UserRoleEnum } from '@/features/admin/adminTypes'
import { formatDate } from '@/lib/utils'
import { ApproveSellerModal } from './sellers/ApproveSellerModal'

type TabValue = 'all' | 'Buyer' | 'Seller' | 'Agency'

export function AdminApprovals() {
  const [tab, setTab] = useState<TabValue>('all')
  const [search, setSearch] = useState('')
  const [rejectTarget, setRejectTarget] = useState<{ id: string; role: UserRoleEnum } | null>(null)
  const [approveSellerId, setApproveSellerId] = useState<string | null>(null)

  const { data: approvals, isLoading } = useGetPendingApprovalsQuery()
  const [approveBuyer] = useSetBuyerApprovalMutation()
  const [approveSeller] = useSetSellerApprovalMutation()
  const [approveAgency] = useSetAgencyApprovalMutation()

  const filtered = useMemo(() => {
    if (!approvals) return []
    return approvals
      .filter((a) => tab === 'all' || a.role === tab)
      .filter((a) => !search || a.fullName.toLowerCase().includes(search.toLowerCase()) || a.email.toLowerCase().includes(search.toLowerCase()))
  }, [approvals, tab, search])

  const counts = useMemo(
    () => ({
      all: approvals?.length ?? 0,
      Buyer: approvals?.filter((a) => a.role === 'Buyer').length ?? 0,
      Seller: approvals?.filter((a) => a.role === 'Seller').length ?? 0,
      Agency: approvals?.filter((a) => a.role === 'Agency').length ?? 0,
    }),
    [approvals],
  )

  const approve = (id: string, role: UserRoleEnum) => {
    if (role === 'Buyer') approveBuyer({ buyerId: id, action: 'Approve' })
    else if (role === 'Seller') setApproveSellerId(id)
    else if (role === 'Agency') approveAgency({ agencyId: id, action: 'Approve' })
  }

  const reject = (id: string, role: UserRoleEnum, reason: string) => {
    if (role === 'Buyer') approveBuyer({ buyerId: id, action: 'Reject', reason })
    else if (role === 'Seller') approveSeller({ sellerId: id, action: 'Reject', reason })
    else if (role === 'Agency') approveAgency({ agencyId: id, action: 'Reject', reason })
    setRejectTarget(null)
  }

  return (
    <PageTransition>
      <h1 className="mb-1 text-2xl font-semibold">Pending approvals</h1>
      <p className="mb-6 text-sm text-[var(--color-ink-faint)]">New signups waiting for review before they can use the platform.</p>

      <div className="mb-4 flex flex-wrap items-center gap-3">
        <SearchField placeholder="Search pending approvals…" value={search} onChange={(e) => setSearch(e.target.value)} />
        <Tabs
          value={tab}
          onChange={setTab}
          options={[
            { value: 'all', label: 'All', count: counts.all },
            { value: 'Buyer', label: 'Buyers', count: counts.Buyer },
            { value: 'Seller', label: 'Sellers', count: counts.Seller },
            { value: 'Agency', label: 'Agencies', count: counts.Agency },
          ]}
        />
      </div>

      {!isLoading && filtered.length === 0 ? (
        <EmptyState icon={UserCheck} title="No pending approvals" description="Everyone is reviewed — new signups will appear here." />
      ) : (
        <div className="flex flex-col gap-2">
          {isLoading
            ? Array.from({ length: 3 }).map((_, i) => <Card key={i} className="h-20 animate-pulse" />)
            : filtered.map((a) => (
                <Card key={a.id} className="flex flex-wrap items-center justify-between gap-4 p-4">
                  <div className="flex min-w-0 flex-col gap-0.5">
                    <div className="flex items-center gap-2">
                      <b className="truncate text-[0.95rem] font-semibold">{a.fullName}</b>
                      <Badge tone="neutral">{a.role}</Badge>
                    </div>
                    <small className="truncate text-[0.78rem] font-normal text-[var(--color-ink-faint)]">
                      {a.email} · applied {formatDate(a.createdAt)}
                    </small>
                  </div>
                  <div className="flex shrink-0 gap-2">
                    <Button size="sm" variant="outline" onClick={() => setRejectTarget({ id: a.id, role: a.role })}>
                      <X size={14} /> Reject
                    </Button>
                    <Button size="sm" variant="primary" onClick={() => approve(a.id, a.role)}>
                      <Check size={14} /> Approve
                    </Button>
                  </div>
                </Card>
              ))}
        </div>
      )}

      <ConfirmDialog
        open={rejectTarget !== null}
        onClose={() => setRejectTarget(null)}
        onConfirm={(reason) => {
          if (rejectTarget) reject(rejectTarget.id, rejectTarget.role, reason ?? 'No reason given')
        }}
        title="Reject this application?"
        description="They'll be notified the application was rejected. Give a reason so they understand why."
        requireReason
        confirmLabel="Reject"
      />

      <ApproveSellerModal sellerId={approveSellerId} onClose={() => setApproveSellerId(null)} />
    </PageTransition>
  )
}
