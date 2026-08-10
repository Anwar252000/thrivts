import { PageTransition, Card } from '@/components/ui'

export function Placeholder({ title }: { title: string }) {
  return (
    <PageTransition>
      <h1 className="mb-6 text-2xl font-semibold">{title}</h1>
      <Card className="text-sm text-[var(--color-ink-soft)]">
        This screen will be built once the matching API endpoint is wired up.
      </Card>
    </PageTransition>
  )
}
