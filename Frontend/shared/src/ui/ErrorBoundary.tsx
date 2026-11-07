import { Component, type ErrorInfo, type ReactNode } from 'react'

interface Props {
  /** Changing this resets the boundary (e.g. the page key), so moving to another page recovers. */
  resetKey?: string
  children: ReactNode
}

interface State {
  error: Error | null
}

/**
 * Keeps one broken screen from blanking the whole app: the rest of the shell (navigation,
 * sign-out) keeps working and the user can retry. Styled with inline values so it works in both
 * apps' themes.
 */
export class ErrorBoundary extends Component<Props, State> {
  state: State = { error: null }

  static getDerivedStateFromError(error: Error): State {
    return { error }
  }

  componentDidCatch(error: Error, info: ErrorInfo) {
    console.error('Screen crashed', error, info.componentStack)
  }

  componentDidUpdate(prev: Props) {
    if (this.state.error && prev.resetKey !== this.props.resetKey) this.setState({ error: null })
  }

  render() {
    if (!this.state.error) return this.props.children
    return (
      <div role="alert" style={{ border: '1px solid rgba(221,107,107,.35)', background: 'rgba(221,107,107,.08)', color: 'inherit', borderRadius: 14, padding: '18px 20px', margin: '8px 0' }}>
        <div style={{ fontWeight: 800, fontSize: 15, marginBottom: 6 }}>Something went wrong on this screen.</div>
        <div style={{ fontSize: 13, opacity: 0.8, marginBottom: 12 }}>Your data is safe — nothing was lost. Try again; if it keeps happening, reload the page.</div>
        <button onClick={() => this.setState({ error: null })}
          style={{ border: '1px solid currentColor', background: 'transparent', color: 'inherit', borderRadius: 9, padding: '7px 14px', fontWeight: 700, cursor: 'pointer', marginRight: 8 }}>Try again</button>
        <button onClick={() => window.location.reload()}
          style={{ border: '1px solid currentColor', background: 'transparent', color: 'inherit', borderRadius: 9, padding: '7px 14px', fontWeight: 700, cursor: 'pointer' }}>Reload</button>
      </div>
    )
  }
}
