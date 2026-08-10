import { Users, Store, Handshake, AlertTriangle } from 'lucide-react'
import { StatCard } from '@/components/dashboard/StatCard'
import { Card, Badge, PageTransition } from '@/components/ui'

export function AdminDashboard() {
  return (
    <PageTransition>
      <h1 className="mb-6 text-2xl font-semibold">Platform overview</h1>

      <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
        <StatCard label="Pending sellers" value="6" icon={Store} tone="warning" />
        <StatCard label="Pending buyers" value="2" icon={Users} tone="warning" />
        <StatCard label="Deals in progress" value="14" icon={Handshake} tone="info" />
        <StatCard label="Open disputes" value="0" icon={AlertTriangle} tone="success" />
      </div>

      <Card className="mt-6">
        <div className="mb-4 flex items-center justify-between">
          <h2 className="font-semibold">Moat status</h2>
          <Badge tone="success">Enforced</Badge>
        </div>
        <p className="text-sm text-[var(--color-ink-soft)]">
          Seller anonymity and fee opacity are enforced at the API layer — role-specific DTOs,
          never a shared response shape.
        </p>
      </Card>
    </PageTransition>
  )
}
