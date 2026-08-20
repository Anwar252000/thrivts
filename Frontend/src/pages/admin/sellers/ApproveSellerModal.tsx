import { useState } from 'react'
import { Check } from 'lucide-react'
import { Modal } from '@/components/admin'
import { Button, Spinner } from '@/components/ui'
import { useGetCategoriesQuery, useGetSellerByIdQuery, useSetSellerApprovalMutation } from '@/features/admin/adminApi'
import type { Category, SellerDetail } from '@/features/admin/adminTypes'

interface ApproveSellerModalProps {
  sellerId: string | null
  onClose: () => void
  onApproved?: () => void
}

/** Category-tag picker for seller approval — without tags a seller sees zero requirements. */
export function ApproveSellerModal({ sellerId, onClose, onApproved }: ApproveSellerModalProps) {
  const { data: seller } = useGetSellerByIdQuery(sellerId!, { skip: !sellerId })
  const { data: categories } = useGetCategoriesQuery()

  if (!sellerId) return null

  return (
    <Modal open={!!sellerId} onClose={onClose} title="Approve seller & assign tags" subtitle={seller?.companyName ?? seller?.publicAlias}>
      {!seller || !categories ? (
        <div className="flex justify-center py-10">
          <Spinner />
        </div>
      ) : (
        <ApproveSellerForm key={seller.id} sellerId={sellerId} seller={seller} categories={categories} onClose={onClose} onApproved={onApproved} />
      )}
    </Modal>
  )
}

function ApproveSellerForm({
  sellerId, seller, categories, onClose, onApproved,
}: {
  sellerId: string; seller: SellerDetail; categories: Category[]; onClose: () => void; onApproved?: () => void
}) {
  const [setApproval, { isLoading: approving }] = useSetSellerApprovalMutation()
  const [selected, setSelected] = useState<Set<string>>(() => new Set(seller.categoriesSupplied ?? []))

  const activeCategories = categories.filter((c) => c.isActive)

  const toggle = (name: string) => {
    setSelected((prev) => {
      const next = new Set(prev)
      if (next.has(name)) next.delete(name)
      else next.add(name)
      return next
    })
  }

  const confirm = async () => {
    const tags = Array.from(selected)
    if (tags.length === 0 && !window.confirm('No tags selected — seller will see zero requirements. Approve anyway?')) return
    await setApproval({ sellerId, action: 'Approve', tags })
    onApproved?.()
    onClose()
  }

  return (
    <div className="flex flex-col gap-4">
      <div className="text-sm text-[var(--color-ink-soft)]">
        {seller.locationCity}, {seller.locationCountry} · Phone: {seller.phone ?? '—'} · Years: {seller.yearsInBusiness ?? '—'}
      </div>

      <div>
        <div className="mb-2 flex items-center justify-between">
          <p className="text-xs font-semibold uppercase tracking-wider text-[var(--color-ink-faint)]">
            Assign tags (controls which requirements this seller sees)
          </p>
          <div className="flex gap-1.5">
            <Button size="sm" variant="outline" onClick={() => setSelected(new Set(activeCategories.map((c) => c.name)))}>
              Select all
            </Button>
            <Button size="sm" variant="outline" onClick={() => setSelected(new Set())}>
              Clear
            </Button>
          </div>
        </div>
        <div className="flex flex-wrap gap-1.5">
          {activeCategories.map((c) => {
            const isSelected = selected.has(c.name)
            return (
              <button
                key={c.id}
                type="button"
                onClick={() => toggle(c.name)}
                className={`rounded-full border px-3 py-1 text-xs font-medium transition-colors ${
                  isSelected
                    ? 'border-[var(--color-accent)] bg-[var(--color-accent)] text-white'
                    : 'border-[var(--color-line)] bg-transparent text-[var(--color-ink-soft)] hover:border-[var(--color-accent)]'
                }`}
              >
                {c.name}
              </button>
            )
          })}
        </div>
        <p className="mt-2 text-xs text-[var(--color-ink-faint)]">
          Use "Select all" to give this seller access to every category. Without tags, they see zero requirements.
        </p>
      </div>

      <Button className="self-end" onClick={confirm} disabled={approving}>
        <Check size={14} /> {approving ? 'Approving…' : `Approve & assign ${selected.size} tag${selected.size === 1 ? '' : 's'}`}
      </Button>
    </div>
  )
}
