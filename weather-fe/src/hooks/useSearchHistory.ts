import { keepPreviousData, useQuery } from '@tanstack/react-query'
import { searchesApi } from '../api/searches'

export const searchesKeys = { all: ['searches'] as const }

export function useSearchHistory(page: number, pageSize = 20) {
  return useQuery({
    queryKey: [...searchesKeys.all, page, pageSize],
    queryFn: () => searchesApi.history(page, pageSize),
    staleTime: 0,
    placeholderData: keepPreviousData,
  })
}
