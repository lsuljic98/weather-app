import { useQuery, useQueryClient } from '@tanstack/react-query'
import { forecastApi, type CityRef } from '../api/weather'
import { statisticsKeys } from './useStatistics'

const THIRTY_MINUTES = 30 * 60_000

// Fetching a forecast is recorded as a search server-side, so this query never
// refetches on its own; a new city (or a fresh visit) is what triggers a search.
export function useForecast(ref: CityRef | null) {
  const queryClient = useQueryClient()

  return useQuery({
    queryKey: ['forecast', ref?.city.toLowerCase(), ref?.countryCode?.toUpperCase() ?? ''],
    queryFn: async () => {
      const forecast = await forecastApi.get(ref!)
      void queryClient.invalidateQueries({ queryKey: statisticsKeys.all })
      return forecast
    },
    enabled: ref !== null,
    staleTime: Infinity,
    gcTime: THIRTY_MINUTES,
    refetchOnMount: false,
    refetchOnReconnect: false,
  })
}
