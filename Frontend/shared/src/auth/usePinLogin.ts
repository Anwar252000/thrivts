import { useCallback, useEffect, useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { useApi } from '../api/context'
import { isApiError } from '../api/http'
import type { LoginUserCard } from '../api/types'

/**
 * The shared "pick your name → enter PIN" logic. Each app draws its own screen; this owns the
 * user list, the digits, submitting when the PIN is complete, and showing the server's answer
 * (wrong PIN, locked for 30 s…) — attempts are counted and locked server-side, never here.
 */
export function usePinLogin({ autoSelectSingle = false } = {}) {
  const api = useApi()
  const users = useQuery({ queryKey: ['login-users', api.app], queryFn: api.auth.users, staleTime: 60_000 })
  const [picked, setSelected] = useState<LoginUserCard | null>(null)
  const [pin, setPin] = useState('')
  const [error, setError] = useState('')
  const [shake, setShake] = useState(false)
  const [busy, setBusy] = useState(false)

  // With only one person on the list there is nothing to pick — go straight to the PIN.
  const only = autoSelectSingle && users.data?.length === 1 ? users.data[0] : null
  const selected = picked ?? only

  // Before anyone is picked, show the PIN length this app uses (owner 6, staff 4).
  const length = selected?.pinLength ?? users.data?.[0]?.pinLength ?? (api.app === 'Owner' ? 6 : 4)

  const submit = useCallback(async (user: LoginUserCard, value: string) => {
    setBusy(true)
    try {
      await api.auth.login(user.id, value)
    } catch (e) {
      setError(isApiError(e) ? e.message : 'Could not sign in — please try again.')
      setShake(true)
      setTimeout(() => {
        setShake(false)
        setPin('')
      }, 600)
    } finally {
      setBusy(false)
    }
  }, [api])

  const press = useCallback((digit: string) => {
    if (!selected || busy) return
    setError('')
    setPin((current) => {
      if (current.length >= length) return current
      const next = current + digit
      if (next.length === length) void submit(selected, next)
      return next
    })
  }, [busy, length, selected, submit])

  const back = useCallback(() => {
    if (busy) return
    setError('')
    setPin((p) => p.slice(0, -1))
  }, [busy])

  const choose = useCallback((user: LoginUserCard | null) => {
    setSelected(user)
    setPin('')
    setError('')
  }, [])

  // Physical keyboard works too (desktop / tablets with keyboards).
  useEffect(() => {
    if (!selected) return
    const onKey = (e: KeyboardEvent) => {
      if (/^\d$/.test(e.key)) press(e.key)
      else if (e.key === 'Backspace') back()
    }
    window.addEventListener('keydown', onKey)
    return () => window.removeEventListener('keydown', onKey)
  }, [back, press, selected])

  return {
    users: users.data ?? [],
    usersLoading: users.isLoading,
    usersError: users.error ? (isApiError(users.error) ? users.error.message : 'Could not load the team list.') : '',
    retryUsers: users.refetch,
    selected,
    choose,
    pin,
    length,
    press,
    back,
    error,
    shake,
    busy,
  }
}
