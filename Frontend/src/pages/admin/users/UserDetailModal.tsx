import { useEffect, useState } from 'react'
import { useForm } from 'react-hook-form'
import { Trash2 } from 'lucide-react'
import { Modal, ConfirmDialog } from '@/components/admin'
import { Badge, Button, Input } from '@/components/ui'
import { statusTone } from '@/features/admin/statusTone'
import { formatDate } from '@/lib/utils'
import { useUpdateUserMutation, useDeleteUserMutation } from '@/features/admin/adminApi'
import type { UserListItem } from '@/features/admin/adminTypes'

interface UserDetailModalProps {
  user: UserListItem | null
  onClose: () => void
}

export function UserDetailModal({ user, onClose }: UserDetailModalProps) {
  const [updateUser, { isLoading: saving }] = useUpdateUserMutation()
  const [deleteUser] = useDeleteUserMutation()
  const [confirmDelete, setConfirmDelete] = useState(false)

  const { register, handleSubmit, reset, formState: { isDirty } } = useForm<{ fullName: string; phone: string; whatsApp: string }>()

  useEffect(() => {
    if (user) reset({ fullName: user.fullName, phone: user.phone ?? '', whatsApp: user.whatsApp ?? '' })
  }, [user, reset])

  if (!user) return null

  const onSave = handleSubmit((values) => {
    updateUser({ userId: user.id, fullName: values.fullName, phone: values.phone, whatsApp: values.whatsApp })
  })

  const handleDelete = async () => {
    await deleteUser(user.id)
    setConfirmDelete(false)
    onClose()
  }

  return (
    <Modal open={!!user} onClose={onClose} title={user.fullName} subtitle={user.email}>
      <div className="flex flex-col gap-6">
        <div className="flex flex-wrap items-center gap-2">
          <Badge tone="neutral">{user.role}</Badge>
          <Badge tone={statusTone(user.approvalStatus)}>{user.approvalStatus}</Badge>
          {!user.isActive && <Badge tone="danger">Blocked</Badge>}
        </div>

        <div className="grid grid-cols-2 gap-4 text-sm">
          <Field label="Joined" value={formatDate(user.createdAt)} />
        </div>

        <form onSubmit={onSave} className="flex flex-col gap-3 border-t border-[var(--color-line)] pt-5">
          <p className="text-xs font-semibold uppercase tracking-wider text-[var(--color-ink-faint)]">Edit details</p>
          <div className="grid grid-cols-2 gap-3">
            <div className="col-span-2">
              <label className="mb-1 block text-xs font-medium text-[var(--color-ink-soft)]">Full name</label>
              <Input {...register('fullName', { required: true })} />
            </div>
            <div>
              <label className="mb-1 block text-xs font-medium text-[var(--color-ink-soft)]">Phone</label>
              <Input {...register('phone')} />
            </div>
            <div>
              <label className="mb-1 block text-xs font-medium text-[var(--color-ink-soft)]">WhatsApp</label>
              <Input {...register('whatsApp')} />
            </div>
          </div>
          <Button type="submit" size="sm" className="self-start" disabled={!isDirty || saving}>
            {saving ? 'Saving…' : 'Save changes'}
          </Button>
        </form>

        <p className="text-xs text-[var(--color-ink-faint)]">
          Role-specific details (company info, tier, KYC, commission rate) are edited from the {user.role} page.
        </p>

        <div className="flex border-t border-[var(--color-line)] pt-5">
          <Button size="sm" variant="danger" className="ml-auto" onClick={() => setConfirmDelete(true)}>
            <Trash2 size={14} /> Delete user
          </Button>
        </div>
      </div>

      <ConfirmDialog
        open={confirmDelete}
        onClose={() => setConfirmDelete(false)}
        onConfirm={handleDelete}
        title="Delete this user?"
        description="Removes their profile and role-specific record permanently. Their login will no longer work. This cannot be undone."
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
