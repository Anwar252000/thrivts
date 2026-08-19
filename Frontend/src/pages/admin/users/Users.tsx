import { useState } from 'react'
import { Plus } from 'lucide-react'
import { PageTransition, Badge, Button, Select } from '@/components/ui'
import { DataTable, SearchField, Pagination, type Column } from '@/components/admin'
import { useGetUsersQuery } from '@/features/admin/adminApi'
import type { UserListItem, UserRoleEnum } from '@/features/admin/adminTypes'
import { statusTone } from '@/features/admin/statusTone'
import { formatDate } from '@/lib/utils'
import { UserDetailModal } from './UserDetailModal'
import { CreateUserModal } from './CreateUserModal'

const ROLE_OPTIONS: UserRoleEnum[] = ['Buyer', 'Seller', 'Agency', 'Admin']

export function AdminUsers() {
  const [page, setPage] = useState(1)
  const [role, setRole] = useState<UserRoleEnum | ''>('')
  const [search, setSearch] = useState('')
  const [selected, setSelected] = useState<UserListItem | null>(null)
  const [creating, setCreating] = useState(false)

  const { data, isLoading } = useGetUsersQuery({ role: role || undefined, search: search || undefined, page, pageSize: 25 })

  const columns: Column<UserListItem>[] = [
    { header: 'Name', accessor: (u) => <span className="font-semibold">{u.fullName}</span> },
    { header: 'Email', accessor: (u) => u.email },
    { header: 'Role', accessor: (u) => <Badge tone="neutral">{u.role}</Badge> },
    { header: 'Status', accessor: (u) => <Badge tone={statusTone(u.approvalStatus)}>{u.approvalStatus}</Badge> },
    { header: 'Active', accessor: (u) => <Badge tone={u.isActive ? 'success' : 'danger'}>{u.isActive ? 'Yes' : 'No'}</Badge> },
    { header: 'Joined', accessor: (u) => formatDate(u.createdAt) },
  ]

  return (
    <PageTransition>
      <div className="mb-6 flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-semibold">Users</h1>
          <p className="mt-1 text-sm text-[var(--color-ink-faint)]">{data?.totalCount ?? 0} accounts across every role.</p>
        </div>
        <Button size="sm" onClick={() => setCreating(true)}>
          <Plus size={14} /> Add user
        </Button>
      </div>

      <div className="mb-4 flex flex-wrap gap-3">
        <SearchField placeholder="Search by name or email…" value={search} onChange={(e) => { setSearch(e.target.value); setPage(1) }} />
        <Select className="w-44" value={role} onChange={(e) => { setRole(e.target.value as UserRoleEnum | ''); setPage(1) }}>
          <option value="">All roles</option>
          {ROLE_OPTIONS.map((r) => (
            <option key={r} value={r}>{r}</option>
          ))}
        </Select>
      </div>

      <DataTable
        columns={columns}
        rows={data?.items ?? []}
        keyFor={(u) => u.id}
        onRowClick={setSelected}
        loading={isLoading}
        emptyTitle="No users found"
      />

      {data && data.totalCount > data.pageSize && (
        <Pagination page={page} pageSize={data.pageSize} total={data.totalCount} onChange={setPage} />
      )}

      <UserDetailModal user={selected} onClose={() => setSelected(null)} />
      <CreateUserModal open={creating} onClose={() => setCreating(false)} />
    </PageTransition>
  )
}
