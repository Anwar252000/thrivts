import { createContext, useCallback, useContext, useRef, useState, type ReactNode } from 'react'
import { isApiError } from '../api/http'

type Toast = (message: string, error?: boolean, durationMs?: number) => void

const ToastContext = createContext<Toast>(() => undefined)

/** The legacy one-line toast (bottom centre). Styled by each app's `.toast` CSS. */
export function ToastProvider({ children }: { children: ReactNode }) {
  const [state, setState] = useState<{ message: string; error: boolean; id: number } | null>(null)
  const timer = useRef<ReturnType<typeof setTimeout>>(undefined)

  const show = useCallback<Toast>((message, error = false, durationMs) => {
    clearTimeout(timer.current)
    setState({ message, error, id: Date.now() })
    timer.current = setTimeout(() => setState(null), durationMs ?? (error ? 4500 : 3000))
  }, [])

  return (
    <ToastContext.Provider value={show}>
      {children}
      <div key={state?.id} role="status" aria-live="polite" className={`toast${state ? ' show' : ''}${state?.error ? ' err' : ''}`}>
        {state?.message}
      </div>
    </ToastContext.Provider>
  )
}

export function useToast() {
  return useContext(ToastContext)
}

/** Shows any thrown error as a red toast with the server's message. */
export function useErrorToast() {
  const toast = useToast()
  return useCallback((error: unknown) => {
    toast(isApiError(error) ? error.firstMessage : error instanceof Error ? error.message : 'Something went wrong — please try again.', true)
  }, [toast])
}
