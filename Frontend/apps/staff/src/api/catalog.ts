import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useApi } from '@shared/api/context'
import type { CatalogItem, Labourer, SkuCategory, Supplier } from '@shared/api/types'

export const catalogKeys = {
  suppliers: ['catalog', 'suppliers'] as const,
  items: ['catalog', 'items'] as const,
  labourers: ['catalog', 'labourers'] as const,
  skuCategories: ['catalog', 'sku-categories'] as const,
}

// Reference data changes rarely; the "catalog" live signal refreshes it when it does.
const REFERENCE_STALE = 5 * 60_000

export function useSuppliers() {
  const { http } = useApi()
  return useQuery({ queryKey: catalogKeys.suppliers, queryFn: ({ signal }) => http.get<Supplier[]>('/catalog/suppliers', signal), staleTime: REFERENCE_STALE })
}

export function useCatalogItems() {
  const { http } = useApi()
  return useQuery({ queryKey: catalogKeys.items, queryFn: ({ signal }) => http.get<CatalogItem[]>('/catalog/items', signal), staleTime: REFERENCE_STALE })
}

export function useLabourers() {
  const { http } = useApi()
  return useQuery({ queryKey: catalogKeys.labourers, queryFn: ({ signal }) => http.get<Labourer[]>('/catalog/labourers', signal), staleTime: REFERENCE_STALE })
}

export function useSkuCategories() {
  const { http } = useApi()
  return useQuery({ queryKey: catalogKeys.skuCategories, queryFn: ({ signal }) => http.get<SkuCategory[]>('/catalog/sku-categories', signal), staleTime: REFERENCE_STALE })
}

/** Adding a supplier that already exists (any casing) returns the existing one. */
export function useCreateSupplier() {
  const { http } = useApi()
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (name: string) => http.post<Supplier>('/catalog/suppliers', { name }),
    onSuccess: () => qc.invalidateQueries({ queryKey: catalogKeys.suppliers }),
  })
}
