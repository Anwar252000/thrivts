import { LogOut } from 'lucide-react'
import { useNavigate } from 'react-router-dom'
import { PageTransition, Card, Button, Spinner } from '@/components/ui'
import { useAppDispatch } from '@/app/hooks'
import { signOut } from '@/features/auth/authSlice'
import { useGetMyProfileQuery } from '@/features/buyer/buyerApi'

export function BuyerProfile() {
  const dispatch = useAppDispatch()
  const navigate = useNavigate()
  const { data: profile, isLoading } = useGetMyProfileQuery()

  const handleSignOut = async () => {
    await dispatch(signOut())
    navigate('/login')
  }

  return (
    <PageTransition>
      <h1 className="mb-1 text-2xl font-semibold">Your profile</h1>
      <p className="mb-6 text-sm text-[var(--color-ink-faint)]">Company details and account settings.</p>

      <Card className="max-w-2xl">
        {isLoading || !profile ? (
          <div className="flex justify-center py-10">
            <Spinner />
          </div>
        ) : (
          <>
            <table className="w-full text-sm">
              <tbody>
                <Row label="Name" value={profile.fullName} />
                <Row label="Email" value={profile.email} />
                <Row label="Phone" value={profile.phone ?? '—'} />
                <Row label="WhatsApp" value={profile.whatsApp ?? '—'} />
                <Row label="Language" value={profile.language === 'Fr' ? 'Français' : 'English'} />
                <Row label="Company" value={profile.companyName} />
                <Row
                  label="Website"
                  value={
                    profile.website ? (
                      <a href={profile.website} target="_blank" rel="noopener noreferrer" className="text-[var(--color-accent)] underline">
                        {profile.website}
                      </a>
                    ) : (
                      '—'
                    )
                  }
                />
                <Row label="Country" value={profile.country} />
                <Row label="City" value={profile.city ?? '—'} />
                <Row label="Instagram" value={profile.instagram ?? '—'} />
                <Row label="Monthly volume" value={profile.estimatedMonthlyVolumePcs ? `${profile.estimatedMonthlyVolumePcs.toLocaleString()} pcs` : '—'} />
                <Row
                  label="Support"
                  value={
                    <a href="https://wa.me/447988595541" target="_blank" rel="noopener noreferrer" className="text-[var(--color-accent)] underline">
                      WhatsApp +44 7988 595541
                    </a>
                  }
                />
              </tbody>
            </table>

            <div className="mt-4 flex justify-end border-t border-[var(--color-line)] pt-4">
              <Button size="sm" variant="outline" onClick={handleSignOut}>
                <LogOut size={14} /> Sign out
              </Button>
            </div>
          </>
        )}
      </Card>
    </PageTransition>
  )
}

function Row({ label, value }: { label: string; value: React.ReactNode }) {
  return (
    <tr className="border-b border-[var(--color-line)] last:border-b-0">
      <th className="w-1/3 py-2.5 pr-3 text-left text-xs font-semibold uppercase tracking-wider text-[var(--color-ink-faint)]">{label}</th>
      <td className="py-2.5">{value}</td>
    </tr>
  )
}
