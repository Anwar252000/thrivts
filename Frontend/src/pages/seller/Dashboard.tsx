import { PackageSearch, Gavel, Handshake } from 'lucide-react'
import { StatCard } from '@/components/dashboard/StatCard'
import { Card, Badge, PageTransition } from '@/components/ui'

export function SellerDashboard() {
  return (
    <PageTransition>
      <h1 className="mb-6 text-2xl font-semibold">Welcome back</h1>

      <div className="grid gap-4 sm:grid-cols-3">
        <StatCard label="Open requirements" value="12" icon={PackageSearch} tone="info" />
        <StatCard label="Quotes awaiting buyer" value="4" icon={Gavel} tone="warning" />
        <StatCard label="Confirmed deals" value="7" icon={Handshake} tone="success" />
      </div>

      <Card className="mt-6">
        <div className="mb-4 flex items-center justify-between">
          <h2 className="font-semibold">Your public identity</h2>
          <Badge tone="neutral">Seller_7fx3 · Gold</Badge>
        </div>
        <p className="text-sm text-[var(--color-ink-soft)]">
          Buyers only ever see this alias and tier — never your company name.
        </p>
      </Card>
    </PageTransition>
  )
}
