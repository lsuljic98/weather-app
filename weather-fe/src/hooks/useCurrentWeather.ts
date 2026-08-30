import { useQuery } from '@tanstack/react-query'
import { weatherApi, type Coordinates } from '../api/weather'

const TEN_MINUTES = 10 * 60_000

export function useCurrentWeather(coords: Coordinates | null) {
  return useQuery({
    // Rounded to account for GPS drift
    queryKey: ['weather', 'current', coords?.latitude.toFixed(2), coords?.longitude.toFixed(2)],
    queryFn: () => weatherApi.currentAt(coords!),
    enabled: coords !== null,
    staleTime: TEN_MINUTES,
    refetchInterval: TEN_MINUTES,
  })
}
