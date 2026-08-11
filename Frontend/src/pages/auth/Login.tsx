import { useState } from 'react'
import { motion } from 'framer-motion'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { useLocation, useNavigate, type Location } from 'react-router-dom'
import { AlertCircle, Eye, EyeOff } from 'lucide-react'
import { Button, Card, Input } from '@/components/ui'
import { Logo } from '@/components/marketing/Logo'
import { useAppDispatch, useAppSelector } from '@/app/hooks'
import { signIn } from '@/features/auth/authSlice'

const loginSchema = z.object({
  email: z.string().min(1, 'Email is required').email('Enter a valid email'),
  password: z.string().min(1, 'Password is required'),
})

type LoginForm = z.infer<typeof loginSchema>

const roleHome: Record<string, string> = { admin: '/admin', seller: '/seller', buyer: '/buyer' }

export function Login() {
  const dispatch = useAppDispatch()
  const navigate = useNavigate()
  const location = useLocation()
  const { status, error } = useAppSelector((state) => state.auth)
  const [showPassword, setShowPassword] = useState(false)

  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<LoginForm>({ resolver: zodResolver(loginSchema) })

  const onSubmit = async (values: LoginForm) => {
    const result = await dispatch(signIn(values))
    if (signIn.fulfilled.match(result)) {
      const from = (location.state as { from?: Location })?.from?.pathname
      const role = result.payload?.user.role
      navigate(from ?? (role ? roleHome[role] : undefined) ?? '/', { replace: true })
    }
  }

  const submitting = status === 'loading'

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
            <p className="mt-3 text-sm text-[var(--color-ink-soft)]">
              Buyer, seller, or admin — one account, routed to your portal.
            </p>
          </div>

          <form className="flex flex-col gap-4" onSubmit={handleSubmit(onSubmit)} noValidate>
            <div>
              <label htmlFor="email" className="mb-1.5 block text-xs font-semibold uppercase tracking-wider text-[var(--color-ink-soft)]">
                Email
              </label>
              <Input
                id="email"
                type="email"
                placeholder="you@thrivts.com"
                autoComplete="email"
                {...register('email')}
              />
              {errors.email && <p className="mt-1.5 text-xs text-[var(--color-danger)]">{errors.email.message}</p>}
            </div>

            <div>
              <label htmlFor="password" className="mb-1.5 block text-xs font-semibold uppercase tracking-wider text-[var(--color-ink-soft)]">
                Password
              </label>
              <div className="relative">
                <Input
                  id="password"
                  type={showPassword ? 'text' : 'password'}
                  placeholder="••••••••"
                  autoComplete="current-password"
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

            {error && (
              <div className="flex items-center gap-2 rounded-[var(--radius-sm)] bg-[var(--color-danger-soft)] px-3 py-2 text-xs text-[var(--color-danger)]">
                <AlertCircle size={14} className="shrink-0" />
                {error}
              </div>
            )}

            <Button type="submit" variant="accent" className="mt-2 w-full" disabled={submitting}>
              {submitting ? 'Signing in…' : 'Sign in'}
            </Button>
          </form>
        </Card>
      </motion.div>
    </div>
  )
}
