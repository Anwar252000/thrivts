import { useState } from 'react'
import { usePinLogin } from '@shared/auth/usePinLogin'
import { initial } from '@shared/format'
import type { LoginUserCard, UserRole } from '@shared/api/types'

const ROLE_LABEL: Partial<Record<UserRole, string>> = {
  GeneralManager: 'GM',
  Supervisor: 'Supervisor',
  InventoryManager: 'Inventory',
  GateGuard: 'Guard',
}

const KEYS = ['1', '2', '3', '4', '5', '6', '7', '8', '9']

/** "Who are you?" → PIN, exactly as the legacy staff app. The PIN is checked by the API. */
export function LoginScreen() {
  const login = usePinLogin()
  const [picked, setPicked] = useState<LoginUserCard | null>(null)

  if (login.selected) return <PinScreen login={login} />

  return (
    <div className="screen active" id="selectScreen">
      <div className="login-logo">
        <div className="vh">VH · Staff Portal</div>
        <h1>Vintage Hub</h1>
        <p>Who are you?</p>
      </div>

      {login.usersLoading && <div className="loading-line">Loading the team…</div>}
      {login.usersError && (
        <div className="error-line">
          {login.usersError}
          <button className="logout-btn" onClick={() => void login.retryUsers()}>Retry</button>
        </div>
      )}

      <div className="person-grid">
        {login.users.map((u) => (
          <button key={u.id} className={`person-btn${picked?.id === u.id ? ' selected' : ''}`} onClick={() => setPicked(u)}>
            <div className="role">{ROLE_LABEL[u.role] ?? u.role}</div>
            <div className="name">{u.displayName}</div>
            <div className="dept">{u.title}</div>
          </button>
        ))}
      </div>

      <button className="next-btn" disabled={!picked} onClick={() => login.choose(picked)}>
        {picked ? `Continue as ${picked.displayName} →` : 'Enter PIN →'}
      </button>
    </div>
  )
}

function PinScreen({ login }: { login: ReturnType<typeof usePinLogin> }) {
  const user = login.selected!
  return (
    <div className="screen active" id="pinScreen">
      <div className="pin-card">
        <button className="pin-back" onClick={() => login.choose(null)}>← Back</button>
        <div className="pin-avatar" style={{ background: user.color }}>{initial(user.displayName)}</div>
        <div className="pin-name">{user.displayName}</div>
        <div className="pin-role">{user.title}</div>
        <div className="pin-dots" aria-label={`${login.pin.length} of ${login.length} digits entered`}>
          {Array.from({ length: login.length }, (_, i) => (
            <div key={i} className={`pin-dot${i < login.pin.length ? ' filled' : ''}${login.shake ? ' error' : ''}`} />
          ))}
        </div>
        <div className={`pin-keypad${login.busy ? ' btn-busy' : ''}`}>
          {KEYS.map((k) => (
            <button key={k} className="pin-key" onClick={() => login.press(k)}>{k}</button>
          ))}
          <button className="pin-key zero" onClick={() => login.press('0')}>0</button>
          <button className="pin-key del" onClick={login.back} aria-label="Delete">⌫</button>
        </div>
        <div className="pin-error">{login.busy ? 'Checking…' : login.error}</div>
      </div>
    </div>
  )
}
