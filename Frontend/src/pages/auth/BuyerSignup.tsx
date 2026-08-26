import { useEffect, useState } from 'react'
import { motion } from 'framer-motion'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { useSearchParams, Link } from 'react-router-dom'
import { UserCheck, MailCheck } from 'lucide-react'
import { Button, Card, Input, Select } from '@/components/ui'
import { Logo } from '@/components/marketing/Logo'
import { apiClient, ApiError } from '@/lib/apiClient'
import { useGetPublicCategoriesQuery, useGetAgencyNameQuery } from '@/features/buyer/buyerApi'

const schema = z.object({
  fullName: z.string().min(1, 'Required'),
  email: z.string().email('Enter a valid email'),
  password: z.string().min(8, 'At least 8 characters'),
  phone: z.string().optional(),
  whatsApp: z.string().optional(),
  language: z.enum(['En', 'Fr']),
  companyName: z.string().min(1, 'Required'),
  website: z.string().optional(),
  country: z.string().min(1, 'Required'),
  city: z.string().optional(),
  instagram: z.string().optional(),
  estimatedMonthlyVolumePcs: z.coerce.number().optional(),
  typicalRequirementType: z.string().optional(),
  referralCode: z.string().optional(),
})
type FormInput = z.input<typeof schema>
type FormValues = z.infer<typeof schema>

export function BuyerSignup() {
  const [searchParams] = useSearchParams()
  const { data: categories } = useGetPublicCategoriesQuery()
  const [selectedCategoryIds, setSelectedCategoryIds] = useState<Set<number>>(new Set())
  const [submitting, setSubmitting] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [submittedEmail, setSubmittedEmail] = useState<string | null>(null)

  const agencyRef = searchParams.get('ref')
  const prefillReferral = searchParams.get('code') ?? searchParams.get('partner') ?? ''
  const { data: agency } = useGetAgencyNameQuery(agencyRef ?? '', { skip: !agencyRef })

  const {
    register, handleSubmit, setValue, formState: { errors },
  } = useForm<FormInput, unknown, FormValues>({
    resolver: zodResolver(schema),
    defaultValues: { language: 'En', referralCode: prefillReferral },
  })

  useEffect(() => {
    if (prefillReferral) setValue('referralCode', prefillReferral)
  }, [prefillReferral, setValue])

  const toggleCategory = (id: number) => {
    setSelectedCategoryIds((prev) => {
      const next = new Set(prev)
      if (next.has(id)) next.delete(id)
      else next.add(id)
      return next
    })
  }

  const onSubmit = async (values: FormValues) => {
    setSubmitting(true)
    setError(null)
    try {
      await apiClient.post('/api/v1/auth/register/buyer', {
        ...values,
        categoryIds: Array.from(selectedCategoryIds),
        agencyRef: agencyRef ?? undefined,
        referralCode: values.referralCode || undefined,
      })
      setSubmittedEmail(values.email)
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Could not submit your application — please try again.')
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <div className="relative flex min-h-screen items-center justify-center overflow-hidden bg-[var(--color-sage-paper)] p-5 py-12">
      <div
        className="pointer-events-none absolute inset-0"
        style={{ background: 'radial-gradient(circle at 20% 30%, rgb(115 131 122 / 0.18), transparent 55%), radial-gradient(circle at 80% 70%, rgb(197 135 75 / 0.08), transparent 55%)' }}
      />

      <motion.div
        initial={{ opacity: 0, y: 16 }}
        animate={{ opacity: 1, y: 0 }}
        transition={{ duration: 0.5, ease: [0.19, 1, 0.22, 1] }}
        className="relative w-full max-w-2xl"
      >
        <Card className="p-8 sm:p-10">
          <div className="mb-6 text-center">
            <Logo className="justify-center text-[2rem]" />
            <h1 className="mt-4 text-xl font-bold uppercase tracking-tight">Apply as a buyer</h1>
            <p className="mt-2 text-sm text-[var(--color-ink-soft)]">Tell us about your business. We review every application — typically within 24 hours.</p>
          </div>

          {submittedEmail ? (
            <div className="flex flex-col items-center gap-3 py-6 text-center">
              <span className="flex size-14 items-center justify-center rounded-full bg-[var(--color-success-soft)] text-[var(--color-success)]">
                <MailCheck size={26} />
              </span>
              <p className="text-sm text-[var(--color-ink)]">
                We've sent a confirmation link to <strong>{submittedEmail}</strong>. Click it to verify your email — your application then
                goes to our team for review.
              </p>
              <p className="text-xs text-[var(--color-ink-faint)]">Didn't get it? Check spam, or wait a minute and check again.</p>
              <Link to="/login" className="text-sm font-medium text-[var(--color-accent)]">Back to sign in</Link>
            </div>
          ) : (
            <>
              {agency?.agencyName && (
                <div className="mb-6 flex items-center gap-2 rounded-[var(--radius-sm)] bg-[var(--color-sage-mist)] px-4 py-2.5 text-sm">
                  <UserCheck size={16} className="text-[var(--color-accent)]" />
                  Referred by <strong>{agency.agencyName}</strong>
                </div>
              )}

          <form onSubmit={handleSubmit(onSubmit)} className="flex flex-col gap-5" noValidate>
            <Section title="Account">
              <div className="grid grid-cols-2 gap-3">
                <Field label="Your full name *" error={errors.fullName?.message}>
                  <Input placeholder="Jane Doe" {...register('fullName')} />
                </Field>
                <Field label="Email *" error={errors.email?.message}>
                  <Input type="email" placeholder="you@company.com" {...register('email')} />
                </Field>
              </div>
              <Field label="Password *" error={errors.password?.message}>
                <Input type="password" placeholder="At least 8 characters" {...register('password')} />
              </Field>
              <div className="grid grid-cols-3 gap-3">
                <Field label="Phone"><Input placeholder="+44 ..." {...register('phone')} /></Field>
                <Field label="WhatsApp"><Input placeholder="+44 ..." {...register('whatsApp')} /></Field>
                <Field label="Language">
                  <Select {...register('language')}>
                    <option value="En">English</option>
                    <option value="Fr">Français</option>
                  </Select>
                </Field>
              </div>
            </Section>

            <Section title="Company">
              <div className="grid grid-cols-2 gap-3">
                <Field label="Company name *" error={errors.companyName?.message}>
                  <Input placeholder="Your Vintage Co. Ltd" {...register('companyName')} />
                </Field>
                <Field label="Website"><Input placeholder="https://yoursite.com" {...register('website')} /></Field>
              </div>
              <div className="grid grid-cols-2 gap-3">
                <Field label="Country *" error={errors.country?.message}>
                  <Input placeholder="United Kingdom" {...register('country')} />
                </Field>
                <Field label="City"><Input placeholder="London" {...register('city')} /></Field>
              </div>
            </Section>

            <Section title="Instagram (helps us verify you)">
              <Input placeholder="@yourbrand" {...register('instagram')} />
            </Section>

            <Section title="Business profile">
              <div className="grid grid-cols-2 gap-3">
                <Field label="Estimated monthly volume (pieces)">
                  <Input type="number" min={0} placeholder="10000" {...register('estimatedMonthlyVolumePcs')} />
                </Field>
                <Field label="Typically you buy">
                  <Select {...register('typicalRequirementType')}>
                    <option value="container_load">Container loads</option>
                    <option value="partial_load">Partial loads (smaller orders)</option>
                    <option value="both">Both</option>
                  </Select>
                </Field>
              </div>
              <div>
                <label className="mb-1.5 block text-xs font-semibold uppercase tracking-wider text-[var(--color-ink-soft)]">Categories you're interested in</label>
                <div className="flex flex-wrap gap-1.5">
                  {categories?.filter((c) => c.isActive).map((c) => {
                    const isSelected = selectedCategoryIds.has(c.id)
                    return (
                      <button
                        type="button"
                        key={c.id}
                        onClick={() => toggleCategory(c.id)}
                        className={`rounded-full border px-3 py-1 text-xs font-medium transition-colors ${
                          isSelected ? 'border-[var(--color-accent)] bg-[var(--color-accent)] text-white' : 'border-[var(--color-line)] text-[var(--color-ink-soft)] hover:border-[var(--color-accent)]'
                        }`}
                      >
                        {c.name}
                      </button>
                    )
                  })}
                </div>
              </div>
            </Section>

            <Section title="Have a partner code?">
              <Input placeholder="Enter code for 5% off your first order" className="uppercase" {...register('referralCode')} />
            </Section>

            {error && <div className="rounded-[var(--radius-sm)] bg-[var(--color-danger-soft)] px-4 py-3 text-sm text-[var(--color-danger)]">{error}</div>}

            <div className="flex justify-end gap-3">
              <Link to="/" className="inline-flex items-center px-4 text-sm text-[var(--color-ink-soft)] hover:text-[var(--color-ink)]">Cancel</Link>
              <Button type="submit" variant="accent" disabled={submitting}>{submitting ? 'Submitting…' : 'Submit application'}</Button>
            </div>

            <p className="text-center text-xs text-[var(--color-ink-faint)]">By submitting, you agree to our terms and privacy policy.</p>
            <p className="text-center text-sm">
              Already have an account? <Link to="/login" className="text-[var(--color-accent)] underline">Sign in</Link>
            </p>
          </form>
            </>
          )}
        </Card>
      </motion.div>
    </div>
  )
}

function Section({ title, children }: { title: string; children: React.ReactNode }) {
  return (
    <div className="flex flex-col gap-3 border-t border-[var(--color-line)] pt-5 first:border-t-0 first:pt-0">
      <h4 className="text-xs font-semibold uppercase tracking-wider text-[var(--color-ink-faint)]">{title}</h4>
      {children}
    </div>
  )
}

function Field({ label, error, children }: { label: string; error?: string; children: React.ReactNode }) {
  return (
    <div>
      <label className="mb-1.5 block text-xs font-semibold uppercase tracking-wider text-[var(--color-ink-soft)]">{label}</label>
      {children}
      {error && <p className="mt-1 text-xs text-[var(--color-danger)]">{error}</p>}
    </div>
  )
}
