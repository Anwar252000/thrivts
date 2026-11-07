import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useApi } from '@shared/api/context'
import type { IdResponse, SaveUserInput, User } from '@shared/api/types'

const usersKey = ['users'] as const

export function useUsers() {
  const { http } = useApi()
  return useQuery({ queryKey: usersKey, queryFn: ({ signal }) => http.get<User[]>('/owner/users', signal) })
}

export function useSaveUser() {
  const { http } = useApi()
  const qc = useQueryClient()
  return useMutation({
    mutationFn: async ({ id, ...body }: SaveUserInput): Promise<string> => {
      if (id) {
        await http.put<void>(`/owner/users/${id}`, { ...body, id })
        return id
      }
      return (await http.post<IdResponse>('/owner/users', { ...body, id: null })).id
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: usersKey }),
  })
}

export function useSetUserActive() {
  const { http } = useApi()
  const qc = useQueryClient()
  return useMutation({
    mutationFn: ({ id, active }: { id: string; active: boolean }) => http.put<void>(`/owner/users/${id}/active`, { value: active }),
    onSuccess: () => qc.invalidateQueries({ queryKey: usersKey }),
  })
}
