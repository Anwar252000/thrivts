import { FileText, Gavel, Handshake } from 'lucide-react'
import { StatCard } from '@/components/dashboard/StatCard'
import { Card, Badge, PageTransition } from '@/components/ui'

export function BuyerDashboard() {
  return (
    <PageTransition>
      <h1 className="mb-6 text-2xl font-semibold">Welcome back</h1>

      <div className="grid gap-4 sm:grid-cols-3">
        <StatCard label="Open requirements" value="3" icon={FileText} tone="info" />
        <StatCard label="Bids awaiting you" value="5" icon={Gavel} tone="warning" />
        <StatCard label="Active deals" value="2" icon={Handshake} tone="success" />
      </div>

      <Card className="mt-6">
        <div className="mb-4 flex items-center justify-between">
          <h2 className="font-semibold">Recent activity</h2>
          <Badge tone="accent">Live</Badge>
        </div>
        <p className="text-sm text-[var(--color-ink-soft)]">
          Connect the API to populate requirements, bids, and deals here.
        </p>
      </Card>
    </PageTransition>
  )
}
