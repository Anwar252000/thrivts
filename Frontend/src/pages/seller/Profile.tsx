import { LogOut } from 'lucide-react'
import { useNavigate } from 'react-router-dom'
import { PageTransition, Card, Button, Badge, Spinner } from '@/components/ui'
import { formatDate } from '@/lib/utils'
import { useAppDispatch } from '@/app/hooks'
import { signOut } from '@/features/auth/authSlice'
import { useGetMyProfileQuery } from '@/features/seller/sellerApi'

export function SellerProfile() {
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
      <p className="mb-6 text-sm text-[var(--color-ink-faint)]">Company details, tier, and account settings.</p>

      <Card className="max-w-2xl">
        {isLoading || !profile ? (
          <div className="flex justify-center py-10">
            <Spinner />
          </div>
        ) : (
          <>
            <table className="w-full text-sm">
              <tbody>
                <Row label="Seller code" value={<span className="font-mono">{profile.sellerCode ?? '—'}</span>} />
                <Row label="KYC status" value={profile.kycVerified ? <span className="text-[var(--color-success)]">✓ Verified</span> : <span className="text-[var(--color-warning)]">Not verified — pending</span>} />
                <Row label="Company" value={profile.companyName ?? '—'} />
                <Row label="Location" value={`${profile.locationCity}, ${profile.locationCountry}`} />
                <Row label="Phone" value={profile.phone ?? '—'} />
                <Row label="WhatsApp" value={profile.whatsApp ?? '—'} />
                <Row label="Current tier" value={<Badge tone="accent">{profile.tier}</Badge>} />
                <Row label="Tags (set by admin)" value={profile.tags?.length ? profile.tags.join(', ') : '—'} />
                <Row label="Categories supplied" value={profile.categoriesSupplied.length ? profile.categoriesSupplied.join(', ') : '—'} />
                <Row label="Member since" value={formatDate(profile.createdAt)} />
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

            {!profile.kycVerified && (
              <p className="mt-4 rounded-[var(--radius-sm)] bg-[var(--color-warning-soft)] p-3 text-xs text-[var(--color-warning)]">
                Your account is approved and you can submit quotes, but the "Verified" badge appears only after our team completes KYC checks. Contact us via WhatsApp to expedite.
              </p>
            )}

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
