import { motion } from 'framer-motion'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { useLocation, useNavigate, type Location } from 'react-router-dom'
import { AlertCircle } from 'lucide-react'
import { Button, Card, Input } from '@/components/ui'
import { useAppDispatch, useAppSelector } from '@/app/hooks'
import { signIn } from '@/features/auth/authSlice'

const loginSchema = z.object({
  email: z.string().min(1, 'Email is required').email('Enter a valid email'),
  password: z.string().min(1, 'Password is required'),
})

type LoginForm = z.infer<typeof loginSchema>

export function Login() {
  const dispatch = useAppDispatch()
  const navigate = useNavigate()
  const location = useLocation()
  const { status, error } = useAppSelector((state) => state.auth)

  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<LoginForm>({ resolver: zodResolver(loginSchema) })

  const onSubmit = async (values: LoginForm) => {
    const result = await dispatch(signIn(values))
    if (signIn.fulfilled.match(result)) {
      const from = (location.state as { from?: Location })?.from?.pathname ?? '/'
      navigate(from, { replace: true })
    }
  }

  const submitting = status === 'loading'

  return (
    <div className="flex flex-1 items-center justify-center px-6 py-16">
      <motion.div
        initial={{ opacity: 0, y: 12 }}
        animate={{ opacity: 1, y: 0 }}
        transition={{ duration: 0.4, ease: [0.19, 1, 0.22, 1] }}
        className="w-full max-w-sm"
      >
        <Card>
          <h1 className="mb-1 text-xl font-semibold">Sign in to Thrivts</h1>
          <p className="mb-6 text-sm text-[var(--color-ink-soft)]">
            Buyer, seller, or admin — one account, routed to your portal.
          </p>

          <form className="flex flex-col gap-4" onSubmit={handleSubmit(onSubmit)} noValidate>
            <div>
              <label htmlFor="email" className="mb-1.5 block text-xs font-medium text-[var(--color-ink-soft)]">
                Email
              </label>
              <Input
                id="email"
                type="email"
                placeholder="you@company.com"
                autoComplete="email"
                {...register('email')}
              />
              {errors.email && (
                <p className="mt-1.5 text-xs text-[var(--color-danger)]">{errors.email.message}</p>
              )}
            </div>
            <div>
              <label htmlFor="password" className="mb-1.5 block text-xs font-medium text-[var(--color-ink-soft)]">
                Password
              </label>
              <Input
                id="password"
                type="password"
                placeholder="••••••••"
                autoComplete="current-password"
                {...register('password')}
              />
              {errors.password && (
                <p className="mt-1.5 text-xs text-[var(--color-danger)]">{errors.password.message}</p>
              )}
            </div>

            {error && (
              <div className="flex items-center gap-2 rounded-[var(--radius-sm)] bg-[var(--color-danger-soft)] px-3 py-2 text-xs text-[var(--color-danger)]">
                <AlertCircle size={14} className="shrink-0" />
                {error}
              </div>
            )}

            <Button type="submit" variant="accent" className="mt-2 w-full" disabled={submitting}>
              {submitting ? 'Signing in…' : 'Continue'}
            </Button>
          </form>
        </Card>
      </motion.div>
    </div>
  )
}
