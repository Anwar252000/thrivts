import { useState } from 'react'
import { motion } from 'framer-motion'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { Link } from 'react-router-dom'
import { AlertCircle, CheckCircle2, Eye, EyeOff } from 'lucide-react'
import { Button, Card, Input } from '@/components/ui'
import { Logo } from '@/components/marketing/Logo'
import { apiClient, ApiError } from '@/lib/apiClient'

const schema = z
  .object({
    password: z.string().min(8, 'Password must be at least 8 characters'),
    confirmPassword: z.string().min(1, 'Please confirm your password'),
  })
  .refine((v) => v.password === v.confirmPassword, { message: 'Passwords do not match', path: ['confirmPassword'] })
type FormValues = z.infer<typeof schema>

function parseRecoveryToken(): string | null {
  const raw = window.location.hash.startsWith('#') ? window.location.hash.slice(1) : window.location.hash
  const params = new URLSearchParams(raw || window.location.search)
  const accessToken = params.get('access_token')
  const type = params.get('type')
  return accessToken && type === 'recovery' ? accessToken : null
}

/** Supabase redirects here from the recovery email with access_token/type=recovery in the URL
 * hash fragment (never sent to any server) — see ForgotPasswordCommand's redirect_to. Read once,
 * lazily, at mount — the URL doesn't change while this page is open. */
function useRecoveryToken() {
  const [token] = useState(parseRecoveryToken)
  return { token, invalid: token === null }
}

export function ResetPassword() {
  const { token, invalid } = useRecoveryToken()
  const [done, setDone] = useState(false)
  const [submitting, setSubmitting] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [showPassword, setShowPassword] = useState(false)

  const { register, handleSubmit, formState: { errors } } = useForm<FormValues>({ resolver: zodResolver(schema) })

  const onSubmit = async (values: FormValues) => {
    if (!token) return
    setSubmitting(true)
    setError(null)
    try {
      await apiClient.post('/api/v1/auth/reset-password', { accessToken: token, newPassword: values.password })
      setDone(true)
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'The link may have expired — request a new one.')
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <div className="relative flex min-h-screen items-center justify-center overflow-hidden bg-[var(--color-sage-paper)] p-5">
      <div
        className="pointer-events-none absolute inset-0"
        style={{
          background:
            'radial-gradient(circle at 20% 30%, rgb(115 131 122 / 0.18), transparent 55%), radial-gradient(circle at 80% 70%, rgb(197 135 75 / 0.08), transparent 55%)',
        }}
      />

      <motion.div
        initial={{ opacity: 0, y: 16 }}
        animate={{ opacity: 1, y: 0 }}
        transition={{ duration: 0.5, ease: [0.19, 1, 0.22, 1] }}
        className="relative w-full max-w-[460px]"
      >
        <Card className="p-8 sm:p-10">
          <div className="mb-8 text-center">
            <Logo className="justify-center text-[2.4rem] sm:text-[2.8rem]" />
            <p className="mt-3 text-sm text-[var(--color-ink-soft)]">Set a new password</p>
          </div>

          {invalid ? (
            <div className="flex flex-col items-center gap-3 text-center">
              <span className="flex size-12 items-center justify-center rounded-full bg-[var(--color-danger-soft)] text-[var(--color-danger)]">
                <AlertCircle size={22} />
              </span>
              <p className="text-sm text-[var(--color-ink)]">This link is invalid or has expired.</p>
              <Link to="/forgot-password" className="text-sm font-medium text-[var(--color-accent)]">
                Request a new reset link
              </Link>
            </div>
          ) : done ? (
            <div className="flex flex-col items-center gap-3 text-center">
              <span className="flex size-12 items-center justify-center rounded-full bg-[var(--color-success-soft)] text-[var(--color-success)]">
                <CheckCircle2 size={22} />
              </span>
              <p className="text-sm text-[var(--color-ink)]">Your password has been updated.</p>
              <Link to="/login" className="text-sm font-medium text-[var(--color-accent)]">
                Continue to sign in
              </Link>
            </div>
          ) : (
            <form className="flex flex-col gap-4" onSubmit={handleSubmit(onSubmit)} noValidate>
              <div>
                <label htmlFor="password" className="mb-1.5 block text-xs font-semibold uppercase tracking-wider text-[var(--color-ink-soft)]">
                  New password
                </label>
                <div className="relative">
                  <Input
                    id="password"
                    type={showPassword ? 'text' : 'password'}
                    placeholder="••••••••"
                    autoComplete="new-password"
                    className="pr-11"
                    {...register('password')}
                  />
                  <button
                    type="button"
                    onClick={() => setShowPassword((v) => !v)}
                    aria-label={showPassword ? 'Hide password' : 'Show password'}
                    className="absolute right-3.5 top-1/2 -translate-y-1/2 text-[var(--color-ink-faint)] hover:text-[var(--color-ink)]"
                  >
                    {showPassword ? <EyeOff size={16} /> : <Eye size={16} />}
                  </button>
                </div>
                {errors.password && <p className="mt-1.5 text-xs text-[var(--color-danger)]">{errors.password.message}</p>}
              </div>

              <div>
                <label htmlFor="confirmPassword" className="mb-1.5 block text-xs font-semibold uppercase tracking-wider text-[var(--color-ink-soft)]">
                  Confirm password
                </label>
                <Input
                  id="confirmPassword"
                  type={showPassword ? 'text' : 'password'}
                  placeholder="••••••••"
                  autoComplete="new-password"
                  {...register('confirmPassword')}
                />
                {errors.confirmPassword && <p className="mt-1.5 text-xs text-[var(--color-danger)]">{errors.confirmPassword.message}</p>}
              </div>

              {error && (
                <div className="flex items-center gap-2 rounded-[var(--radius-sm)] bg-[var(--color-danger-soft)] px-3 py-2 text-xs text-[var(--color-danger)]">
                  <AlertCircle size={14} className="shrink-0" />
                  {error}
                </div>
              )}

              <Button type="submit" variant="accent" className="mt-2 w-full" disabled={submitting}>
                {submitting ? 'Saving…' : 'Save new password'}
              </Button>
            </form>
          )}
        </Card>
      </motion.div>
    </div>
  )
}
