import { useState } from 'react'
import { useSession } from '@shared/api/context'
import { ago, initial } from '@shared/format'
import { useErrorToast, useToast } from '@shared/ui/toast'
import type { User, UserRole } from '@shared/api/types'
import { useSaveUser, useSetUserActive, useUsers } from '../../api/users'

const ROLES: { role: UserRole; label: string; app: 'Staff' | 'Owner' }[] = [
  { role: 'GateGuard', label: 'Gate guard', app: 'Staff' },
  { role: 'Supervisor', label: 'Supervisor (count & grade)', app: 'Staff' },
  { role: 'InventoryManager', label: 'Inventory manager', app: 'Staff' },
  { role: 'GeneralManager', label: 'General manager', app: 'Staff' },
  { role: 'Finance', label: 'Finance (owner app)', app: 'Owner' },
  { role: 'Sales', label: 'Sales (owner app)', app: 'Owner' },
  { role: 'Owner', label: 'Owner', app: 'Owner' },
]

const roleLabel = (r: UserRole) => ROLES.find((x) => x.role === r)?.label ?? r
const pinLength = (r: UserRole) => (ROLES.find((x) => x.role === r)?.app === 'Owner' ? 6 : 4)

interface Draft {
  id: string | null
  displayName: string
  title: string
  role: UserRole
  color: string
  pin: string
  sortOrder: number
}

const EMPTY: Draft = { id: null, displayName: '', title: '', role: 'GateGuard', color: '#1AA767', pin: '', sortOrder: 0 }

/** Who can sign in to which app, with their PINs. Only the owner sees this page (the API also
 *  restricts it). PINs are never shown — they can only be set or replaced. */
export function TeamPage() {
  const me = useSession()!.user
  const users = useUsers()
  const save = useSaveUser()
  const setActive = useSetUserActive()
  const toast = useToast()
  const showError = useErrorToast()
  const [draft, setDraft] = useState<Draft | null>(null)

  const edit = (u: User) => setDraft({ id: u.id, displayName: u.displayName, title: u.title, role: u.role, color: u.color, pin: '', sortOrder: u.sortOrder })

  const submit = async () => {
    if (!draft) return
    try {
      await save.mutateAsync({ ...draft, pin: draft.pin || null })
      toast(draft.id ? `✓ ${draft.displayName} updated${draft.pin ? ' — new PIN set, old sessions signed out' : ''}` : `✓ ${draft.displayName} can now sign in`)
      setDraft(null)
    } catch (e) {
      showError(e)
    }
  }

  const toggle = async (u: User) => {
    if (u.isActive && !confirm(`Turn off ${u.displayName}'s login? They can't sign in again, and open sessions end within 15 minutes.`)) return
    try {
      await setActive.mutateAsync({ id: u.id, active: !u.isActive })
      toast(u.isActive ? `${u.displayName} can no longer sign in` : `✓ ${u.displayName} can sign in again`)
    } catch (e) {
      showError(e)
    }
  }

  const groups: { app: 'Staff' | 'Owner'; title: string }[] = [
    { app: 'Staff', title: 'Staff app' },
    { app: 'Owner', title: 'Owner app' },
  ]

  return (
    <>
      <div style={{ display: 'flex', justifyContent: 'flex-end', marginBottom: 4 }}>
        <button className="btn p" onClick={() => setDraft({ ...EMPTY })}>＋ Add person</button>
      </div>

      {draft && (
        <div className="card pad" style={{ margin: '8px 0 4px' }}>
          <div className="k" style={{ marginBottom: 10 }}>{draft.id ? `Edit ${draft.displayName}` : 'New login'}</div>
          <div className="form-grid">
            <div className="fld"><label>Name</label>
              <input value={draft.displayName} maxLength={60} autoFocus onChange={(e) => setDraft({ ...draft, displayName: e.target.value })} placeholder="e.g. Ali" /></div>
            <div className="fld"><label>Title (shown on the login screen)</label>
              <input value={draft.title} maxLength={60} onChange={(e) => setDraft({ ...draft, title: e.target.value })} placeholder="e.g. Gate — In / Out" /></div>
            <div className="fld"><label>Role</label>
              <select value={draft.role} disabled={draft.id === me.id} onChange={(e) => setDraft({ ...draft, role: e.target.value as UserRole, pin: '' })}>
                {ROLES.map((r) => <option key={r.role} value={r.role}>{r.label}</option>)}
              </select></div>
            <div className="fld"><label>{draft.id ? `New PIN (${pinLength(draft.role)} digits, optional)` : `PIN (${pinLength(draft.role)} digits)`}</label>
              <input value={draft.pin} inputMode="numeric" autoComplete="new-password" maxLength={pinLength(draft.role)}
                onChange={(e) => setDraft({ ...draft, pin: e.target.value.replace(/\D/g, '') })} placeholder={'•'.repeat(pinLength(draft.role))} /></div>
            <div className="fld"><label>Colour</label>
              <input type="color" value={draft.color} onChange={(e) => setDraft({ ...draft, color: e.target.value.toUpperCase() })} /></div>
            <div className="fld"><label>Order on login screen</label>
              <input type="number" min={0} value={draft.sortOrder} onChange={(e) => setDraft({ ...draft, sortOrder: parseInt(e.target.value) || 0 })} /></div>
          </div>
          <div className="row-actions" style={{ marginTop: 12 }}>
            <button className="btn" onClick={() => setDraft(null)}>Cancel</button>
            <button className="btn p" disabled={save.isPending} onClick={() => void submit()}>{save.isPending ? 'Saving…' : draft.id ? 'Save changes' : 'Create login'}</button>
          </div>
          <div className="hint" style={{ marginTop: 8 }}>Staff PINs are 4 digits, owner-app PINs 6. Changing someone's PIN ends their sessions on every device (within 15 minutes).</div>
        </div>
      )}

      {users.isLoading && <div className="loading-line">Loading…</div>}
      {users.isError && <div className="error-line">Couldn't load the team.<button className="btn" onClick={() => void users.refetch()}>Retry</button></div>}

      {users.data && groups.map((g) => {
        const list = users.data.filter((u) => u.app === g.app)
        return (
          <div key={g.app}>
            <div className="sec-t">{g.title} · {list.length}</div>
            <div className="tablecard"><div className="tblscroll">
              <table>
                <thead><tr><th>Person</th><th>Role</th><th>Status</th><th>Last sign-in</th><th /></tr></thead>
                <tbody>
                  {list.length === 0 && <tr><td colSpan={5} className="muted">No one yet — add the first person above.</td></tr>}
                  {list.map((u) => (
                    <tr key={u.id} style={u.isActive ? undefined : { opacity: 0.55 }}>
                      <td>
                        <div style={{ display: 'flex', alignItems: 'center', gap: 9 }}>
                          <span style={{ width: 26, height: 26, borderRadius: 8, background: u.color, display: 'grid', placeItems: 'center', color: '#fff', fontWeight: 800, fontSize: 12 }}>{initial(u.displayName)}</span>
                          <div><b>{u.displayName}</b>{u.id === me.id && <span className="muted"> (you)</span>}<div className="hint">{u.title}</div></div>
                        </div>
                      </td>
                      <td>{roleLabel(u.role)}</td>
                      <td>
                        {!u.isActive ? <span className="tag t-warn">Off</span>
                          : u.isLockedOut ? <span className="tag t-price">Locked (wrong PINs)</span>
                            : <span className="tag t-good">Active</span>}
                      </td>
                      <td className="muted">{u.lastLoginAt ? ago(u.lastLoginAt) : 'never'}</td>
                      <td>
                        <div className="row-actions">
                          <button className="btn" onClick={() => edit(u)}>Edit</button>
                          {u.id !== me.id && (
                            <button className={`btn${u.isActive ? ' danger' : ''}`} disabled={setActive.isPending} onClick={() => void toggle(u)}>
                              {u.isActive ? 'Turn off' : 'Turn on'}
                            </button>
                          )}
                        </div>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div></div>
          </div>
        )
      })}
    </>
  )
}
