import { useState } from 'react'
import { Plus, Megaphone } from 'lucide-react'
import { Card, Badge, Button } from '@/components/ui'
import { EmptyState } from '@/components/admin'
import { useGetInfluencersQuery } from '@/features/admin/adminApi'
import { CreateInfluencerModal } from './CreateInfluencerModal'

export function InfluencersPanel() {
  const { data: influencers } = useGetInfluencersQuery()
  const [creating, setCreating] = useState(false)

  return (
    <Card>
      <div className="mb-4 flex items-center justify-between">
        <p className="font-semibold">Influencers</p>
        <Button size="sm" variant="ghost" onClick={() => setCreating(true)}>
          <Plus size={14} /> Add
        </Button>
      </div>

      {!influencers || influencers.length === 0 ? (
        <EmptyState icon={Megaphone} title="No influencers yet" />
      ) : (
        <div className="flex max-h-80 flex-col gap-2 overflow-y-auto">
          {influencers.map((i) => (
            <div key={i.id} className="flex items-center justify-between rounded-[var(--radius-sm)] border border-[var(--color-line)] px-3 py-2 text-sm">
              <div>
                <p className="font-medium">{i.fullName ?? i.influencerCode}</p>
                <p className="text-xs text-[var(--color-ink-faint)]">Code {i.referralCode} · {(i.commissionRate * 100).toFixed(0)}%</p>
              </div>
              <Badge tone="neutral">{i.status}</Badge>
            </div>
          ))}
        </div>
      )}

      <CreateInfluencerModal open={creating} onClose={() => setCreating(false)} />
    </Card>
  )
}
