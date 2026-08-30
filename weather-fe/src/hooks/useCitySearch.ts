import { keepPreviousData, useQuery } from '@tanstack/react-query'
import { weatherApi } from '../api/weather'
import { useDebouncedValue } from './useDebouncedValue'

const MIN_LENGTH = 2

export function useCitySearch(input: string) {
  const query = useDebouncedValue(input.trim(), 300)
  const enabled = query.length >= MIN_LENGTH

  const result = useQuery({
    queryKey: ['cities', query.toLowerCase()],
    queryFn: () => weatherApi.cities(query),
    enabled,
    staleTime: 12 * 60 * 60_000,
    placeholderData: keepPreviousData,
  })

  return { ...result, enabled }
}
