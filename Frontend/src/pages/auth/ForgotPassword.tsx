import { useState } from 'react'
import { motion } from 'framer-motion'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { Link } from 'react-router-dom'
import { ArrowLeft, MailCheck, AlertCircle } from 'lucide-react'
import { Button, Card, Input } from '@/components/ui'
import { Logo } from '@/components/marketing/Logo'
import { apiClient, ApiError } from '@/lib/apiClient'

const schema = z.object({
  email: z.string().min(1, 'Email is required').email('Enter a valid email'),
})
type FormValues = z.infer<typeof schema>

export function ForgotPassword() {
  const [sent, setSent] = useState(false)
  const [submitting, setSubmitting] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const { register, handleSubmit, formState: { errors } } = useForm<FormValues>({ resolver: zodResolver(schema) })

  const onSubmit = async (values: FormValues) => {
    setSubmitting(true)
    setError(null)
    try {
      await apiClient.post('/api/v1/auth/forgot-password', values)
      setSent(true)
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Something went wrong. Please try again.')
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
            <p className="mt-3 text-sm text-[var(--color-ink-soft)]">Reset your password</p>
          </div>

          {sent ? (
            <div className="flex flex-col items-center gap-3 text-center">
              <span className="flex size-12 items-center justify-center rounded-full bg-[var(--color-success-soft)] text-[var(--color-success)]">
                <MailCheck size={22} />
              </span>
              <p className="text-sm text-[var(--color-ink)]">
                If an account exists for that email, a reset link is on its way. Check your inbox (and spam folder).
              </p>
              <Link to="/login" className="mt-2 inline-flex items-center gap-1.5 text-sm font-medium text-[var(--color-accent)]">
                <ArrowLeft size={14} /> Back to sign in
              </Link>
            </div>
          ) : (
            <form className="flex flex-col gap-4" onSubmit={handleSubmit(onSubmit)} noValidate>
              <div>
                <label htmlFor="email" className="mb-1.5 block text-xs font-semibold uppercase tracking-wider text-[var(--color-ink-soft)]">
                  Email
                </label>
                <Input id="email" type="email" placeholder="you@thrivts.com" autoComplete="email" {...register('email')} />
                {errors.email && <p className="mt-1.5 text-xs text-[var(--color-danger)]">{errors.email.message}</p>}
              </div>

              {error && (
                <div className="flex items-center gap-2 rounded-[var(--radius-sm)] bg-[var(--color-danger-soft)] px-3 py-2 text-xs text-[var(--color-danger)]">
                  <AlertCircle size={14} className="shrink-0" />
                  {error}
                </div>
              )}

              <Button type="submit" variant="accent" className="mt-2 w-full" disabled={submitting}>
                {submitting ? 'Sending…' : 'Send reset link'}
              </Button>

              <Link to="/login" className="mt-1 inline-flex items-center justify-center gap-1.5 text-sm text-[var(--color-ink-soft)] hover:text-[var(--color-ink)]">
                <ArrowLeft size={14} /> Back to sign in
              </Link>
            </form>
          )}
        </Card>
      </motion.div>
    </div>
  )
}
