import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { Plus, Check, X } from 'lucide-react'
import { Card, Badge, Button, Input } from '@/components/ui'
import { useGetCategoriesQuery, useCreateCategoryMutation, useUpdateCategoryMutation } from '@/features/admin/adminApi'

export function CategoriesPanel() {
  const { data: categories } = useGetCategoriesQuery()
  const [createCategory, { isLoading: creating }] = useCreateCategoryMutation()
  const [updateCategory] = useUpdateCategoryMutation()
  const [adding, setAdding] = useState(false)
  const { register, handleSubmit, reset } = useForm<{ name: string; weightPerPieceKg?: number }>()

  const onAdd = handleSubmit(async (values) => {
    await createCategory({ name: values.name, weightPerPieceKg: values.weightPerPieceKg, displayOrder: (categories?.length ?? 0) + 1 })
    reset()
    setAdding(false)
  })

  return (
    <Card>
      <div className="mb-4 flex items-center justify-between">
        <p className="font-semibold">Categories</p>
        <Button size="sm" variant="ghost" onClick={() => setAdding((v) => !v)}>
          <Plus size={14} /> Add
        </Button>
      </div>

      {adding && (
        <form onSubmit={onAdd} className="mb-4 flex gap-2">
          <Input placeholder="Category name" className="flex-1" {...register('name', { required: true })} />
          <Input type="number" step="0.01" placeholder="kg/pc" className="w-24" {...register('weightPerPieceKg')} />
          <Button type="submit" size="sm" disabled={creating}>
            <Check size={14} />
          </Button>
        </form>
      )}

      <div className="flex max-h-80 flex-col gap-2 overflow-y-auto">
        {categories?.map((c) => (
          <div key={c.id} className="flex items-center justify-between rounded-[var(--radius-sm)] border border-[var(--color-line)] px-3 py-2 text-sm">
            <span>{c.name}</span>
            <div className="flex items-center gap-2">
              <Badge tone={c.isActive ? 'success' : 'neutral'}>{c.isActive ? 'Active' : 'Inactive'}</Badge>
              <button
                onClick={() => updateCategory({ categoryId: c.id, name: c.name, nameFr: c.nameFr ?? undefined, isActive: !c.isActive })}
                className="text-[var(--color-ink-faint)] hover:text-[var(--color-ink)]"
                title={c.isActive ? 'Deactivate' : 'Activate'}
              >
                {c.isActive ? <X size={14} /> : <Check size={14} />}
              </button>
            </div>
          </div>
        ))}
      </div>
    </Card>
  )
}
