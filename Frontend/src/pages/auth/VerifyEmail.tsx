import { useEffect, useState } from 'react'
import { motion } from 'framer-motion'
import { Link } from 'react-router-dom'
import { AlertCircle, CheckCircle2 } from 'lucide-react'
import { Card, Spinner } from '@/components/ui'
import { Logo } from '@/components/marketing/Logo'
import { useAppDispatch } from '@/app/hooks'
import { completeBuyerRegistration, type AuthSession } from '@/features/auth/authSlice'

function parseSignupTokens(): AuthSession | null {
  const raw = window.location.hash.startsWith('#') ? window.location.hash.slice(1) : window.location.hash
  const params = new URLSearchParams(raw || window.location.search)
  const accessToken = params.get('access_token')
  const refreshToken = params.get('refresh_token')
  const expiresIn = params.get('expires_in')
  const type = params.get('type')
  if (!accessToken || !refreshToken || type !== 'signup') return null

  return { accessToken, refreshToken, expiresIn: Number(expiresIn) || 3600, userId: '', email: '' }
}

/** Supabase redirects here from the confirmation email with access_token/refresh_token/type=signup
 * in the URL hash (never sent to any server) — see RegisterBuyerCommand's redirect_to. Completing
 * registration here (not at signup time) is what actually creates the Profile+Buyer row. */
export function VerifyEmail() {
  const dispatch = useAppDispatch()
  const [session] = useState(parseSignupTokens)
  const [status, setStatus] = useState<'working' | 'done' | 'invalid' | 'error'>(session ? 'working' : 'invalid')
  const [errorMessage, setErrorMessage] = useState<string | null>(null)

  useEffect(() => {
    if (!session) return

    dispatch(completeBuyerRegistration(session))
      .unwrap()
      .then(() => setStatus('done'))
      .catch((err: unknown) => {
        setStatus('error')
        setErrorMessage(err instanceof Error ? err.message : 'Could not complete your registration.')
      })
  }, [dispatch, session])

  return (
    <div className="relative flex min-h-screen items-center justify-center overflow-hidden bg-[var(--color-sage-paper)] p-5">
      <div
        className="pointer-events-none absolute inset-0"
        style={{ background: 'radial-gradient(circle at 20% 30%, rgb(115 131 122 / 0.18), transparent 55%), radial-gradient(circle at 80% 70%, rgb(197 135 75 / 0.08), transparent 55%)' }}
      />

      <motion.div
        initial={{ opacity: 0, y: 16 }}
        animate={{ opacity: 1, y: 0 }}
        transition={{ duration: 0.5, ease: [0.19, 1, 0.22, 1] }}
        className="relative w-full max-w-[460px]"
      >
        <Card className="p-8 text-center sm:p-10">
          <Logo className="mb-6 justify-center text-[2.4rem] sm:text-[2.8rem]" />

          {status === 'working' && (
            <div className="flex flex-col items-center gap-3">
              <Spinner />
              <p className="text-sm text-[var(--color-ink-soft)]">Confirming your email…</p>
            </div>
          )}

          {status === 'done' && (
            <div className="flex flex-col items-center gap-3">
              <span className="flex size-12 items-center justify-center rounded-full bg-[var(--color-success-soft)] text-[var(--color-success)]">
                <CheckCircle2 size={22} />
              </span>
              <p className="text-sm text-[var(--color-ink)]">Your email is confirmed and your application has been submitted.</p>
              <Link to="/buyer" className="text-sm font-medium text-[var(--color-accent)]">Continue to your account</Link>
            </div>
          )}

          {(status === 'invalid' || status === 'error') && (
            <div className="flex flex-col items-center gap-3">
              <span className="flex size-12 items-center justify-center rounded-full bg-[var(--color-danger-soft)] text-[var(--color-danger)]">
                <AlertCircle size={22} />
              </span>
              <p className="text-sm text-[var(--color-ink)]">
                {status === 'invalid' ? 'This confirmation link is invalid or has expired.' : errorMessage}
              </p>
              <Link to="/apply" className="text-sm font-medium text-[var(--color-accent)]">Apply again</Link>
            </div>
          )}
        </Card>
      </motion.div>
    </div>
  )
}
