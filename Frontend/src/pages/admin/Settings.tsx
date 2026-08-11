import { PageTransition } from '@/components/ui'
import { FeeConfigPanel } from './settings/FeeConfigPanel'
import { CategoriesPanel } from './settings/CategoriesPanel'
import { ShippingRatesPanel } from './settings/ShippingRatesPanel'
import { ExchangeRatesPanel } from './settings/ExchangeRatesPanel'
import { InfluencersPanel } from './settings/InfluencersPanel'
import { PartnerApplicationsPanel } from './settings/PartnerApplicationsPanel'

export function AdminSettings() {
  return (
    <PageTransition>
      <h1 className="mb-1 text-2xl font-semibold">Settings</h1>
      <p className="mb-6 text-sm text-[var(--color-ink-faint)]">Platform-wide configuration.</p>

      <div className="mb-4">
        <FeeConfigPanel />
      </div>

      <div className="grid gap-4 md:grid-cols-2">
        <CategoriesPanel />
        <ShippingRatesPanel />
        <ExchangeRatesPanel />
        <InfluencersPanel />
        <div className="md:col-span-2">
          <PartnerApplicationsPanel />
        </div>
      </div>
    </PageTransition>
  )
}
