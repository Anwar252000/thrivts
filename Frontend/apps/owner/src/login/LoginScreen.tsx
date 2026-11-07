import { usePinLogin } from '@shared/auth/usePinLogin'
import { initial } from '@shared/format'

const KEYS = ['1', '2', '3', '4', '5', '6', '7', '8', '9', '', '0', '⌫']

/** The legacy owner login card: pick a person, then the keypad. Owner-app PINs are 6 digits and
 *  are checked (and attempts limited) by the API. */
export function LoginScreen() {
  const login = usePinLogin({ autoSelectSingle: true })

  const press = (k: string) => {
    if (!login.selected) return
    if (k === '⌫') login.back()
    else login.press(k)
  }

  return (
    <div id="login">
      <div className="loginbox">
        <div className="logo" />
        <h1>Vintage Hub — Owner</h1>
        <p>Pick your name, then your PIN</p>

        {login.usersLoading && <div className="loading-line">Loading…</div>}
        {login.usersError && (
          <div className="error-line">
            {login.usersError}
            <button className="btn" onClick={() => void login.retryUsers()}>Retry</button>
          </div>
        )}

        <div className="people">
          {login.users.map((u) => (
            <div key={u.id} className={`person${login.selected?.id === u.id ? ' on' : ''}`} role="button" tabIndex={0}
              onClick={() => login.choose(u)} onKeyDown={(e) => e.key === 'Enter' && login.choose(u)}>
              <div className="av" style={{ background: u.color }}>{initial(u.displayName)}</div>
              <div className="nm">{u.displayName}</div>
              <div className="rl">{u.title}</div>
            </div>
          ))}
        </div>

        <div className={`pin${login.shake ? ' shake' : ''}`}>
          {Array.from({ length: login.length }, (_, i) => <span key={i} className={`d${i < login.pin.length ? ' on' : ''}`} />)}
        </div>
        <div className={`keys${login.busy ? ' busy' : ''}`}>
          {KEYS.map((k, i) => (k === '' ? <span key={i} /> : (
            <button key={i} onClick={() => press(k)} disabled={login.busy} aria-label={k === '⌫' ? 'Delete' : undefined}>{k}</button>
          )))}
        </div>
        <div className="err">{login.busy ? 'Checking…' : login.selected ? login.error : login.users.length ? 'Pick a name first' : ''}</div>
      </div>
    </div>
  )
}
