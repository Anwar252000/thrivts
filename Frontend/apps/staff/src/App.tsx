import { useBootstrap } from '@shared/api/context'
import { LoginScreen } from './login/LoginScreen'
import { Shell } from './shell/Shell'

export function App() {
  const state = useBootstrap()

  if (state === 'checking')
    return (
      <div className="boot">
        <div className="nav-dot" />
      </div>
    )

  return state === 'signed-in' ? <Shell /> : <LoginScreen />
}
