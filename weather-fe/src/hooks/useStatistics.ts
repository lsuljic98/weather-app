import { useQuery } from '@tanstack/react-query'
import { statisticsApi } from '../api/statistics'

// Always served from the database: staleTime 0 so a new search shows up on the next visit.
export const statisticsKeys = { all: ['statistics'] as const }

export function useTopCities(take = 3) {
  return useQuery({
    queryKey: [...statisticsKeys.all, 'top-cities', take],
    queryFn: () => statisticsApi.topCities(take),
    staleTime: 0,
  })
}

export function useRecentSearches(take = 3) {
  return useQuery({
    queryKey: [...statisticsKeys.all, 'recent', take],
    queryFn: () => statisticsApi.recent(take),
    staleTime: 0,
  })
}

export function useConditionDistribution() {
  return useQuery({
    queryKey: [...statisticsKeys.all, 'conditions'],
    queryFn: statisticsApi.conditions,
    staleTime: 0,
  })
}
