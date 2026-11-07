import { useInfiniteQuery, useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useApi } from '@shared/api/context'
import { qs } from '@shared/api/http'
import type { CursorPage, DateOnly, GateDirection, GateEntry, Guid, NextGateSerials } from '@shared/api/types'

export const gateKeys = {
  next: ['gate', 'next'] as const,
  history: ['gate', 'history'] as const,
}

export function useNextGateSerials() {
  const { http } = useApi()
  return useQuery({ queryKey: gateKeys.next, queryFn: ({ signal }) => http.get<NextGateSerials>('/gate/next-serials', signal) })
}

export interface RecordGateInput {
  direction: GateDirection
  supplierId: Guid
  sacks: number
  date: DateOnly
  idempotencyKey: string
}

export function useRecordGateEntry() {
  const { http } = useApi()
  const qc = useQueryClient()
  return useMutation({
    mutationFn: ({ idempotencyKey, ...body }: RecordGateInput) => http.post<GateEntry>('/gate/entries', body, idempotencyKey),
    onSuccess: () => {
      void qc.invalidateQueries({ queryKey: ['gate'] })
      void qc.invalidateQueries({ queryKey: ['lots'] })
    },
  })
}

/** Gate history newest first, loaded a page at a time (keyset paging — fast at any depth). */
export function useGateHistory() {
  const { http } = useApi()
  return useInfiniteQuery({
    queryKey: gateKeys.history,
    queryFn: ({ pageParam, signal }) => http.get<CursorPage<GateEntry>>(`/gate/entries${qs({ cursor: pageParam, size: 50 })}`, signal),
    initialPageParam: undefined as string | undefined,
    getNextPageParam: (last) => last.nextCursor ?? undefined,
  })
}
