import { useQuery, useQueryClient } from '@tanstack/react-query'
import { forecastApi, type CityRef } from '../api/weather'
import { searchesKeys } from './useSearchHistory'
import { statisticsKeys } from './useStatistics'

const THIRTY_MINUTES = 30 * 60_000

export function useForecast(ref: CityRef | null) {
  const queryClient = useQueryClient()

  return useQuery({
    queryKey: ['forecast', ref?.city.toLowerCase(), ref?.countryCode?.toUpperCase() ?? ''],
    queryFn: async () => {
      const forecast = await forecastApi.get(ref!)
      void queryClient.invalidateQueries({ queryKey: statisticsKeys.all })
      void queryClient.invalidateQueries({ queryKey: searchesKeys.all })
      return forecast
    },
    enabled: ref !== null,
    staleTime: Infinity,
    gcTime: THIRTY_MINUTES,
    refetchOnMount: false,
    refetchOnReconnect: false,
  })
}
