import { useCallback, useMemo } from 'react'
import { useSearchParams } from 'react-router-dom'
import type { CityRef } from '../../api/weather'
import { METRICS, PARTS_OF_DAY, type ForecastFilters, type Metric, type PartOfDay } from './filters'

// City and filters live in the query string so a view is shareable and survives reload.
export function useForecastParams() {
  const [params, setParams] = useSearchParams()

  const city = params.get('city')?.trim() || null
  const ref = useMemo<CityRef | null>(
    () => (city ? { city, countryCode: params.get('country') ?? undefined } : null),
    [city, params],
  )

  const filters = useMemo<ForecastFilters>(
    () => ({
      from: params.get('from') ?? undefined,
      to: params.get('to') ?? undefined,
      partOfDay: oneOf(PARTS_OF_DAY, params.get('part'), 'all'),
      metric: oneOf(METRICS, params.get('metric'), 'temperature'),
    }),
    [params],
  )

  const selectCity = useCallback(
    (next: CityRef) =>
      setParams((prev) => {
        const p = new URLSearchParams(prev)
        p.set('city', next.city)
        if (next.countryCode) p.set('country', next.countryCode)
        else p.delete('country')
        p.delete('from')
        p.delete('to')
        return p
      }),
    [setParams],
  )

  const setFilters = useCallback(
    (patch: Partial<ForecastFilters>) =>
      setParams(
        (prev) => {
          const p = new URLSearchParams(prev)
          const put = (key: string, value: string | undefined, dflt?: string) =>
            value && value !== dflt ? p.set(key, value) : p.delete(key)
          if ('from' in patch) put('from', patch.from)
          if ('to' in patch) put('to', patch.to)
          if (patch.partOfDay) put('part', patch.partOfDay, 'all')
          if (patch.metric) put('metric', patch.metric, 'temperature')
          return p
        },
        { replace: true },
      ),
    [setParams],
  )

  return { ref, filters, selectCity, setFilters }
}

function oneOf<T extends string>(allowed: readonly T[], value: string | null, fallback: T): T {
  return allowed.includes(value as T) ? (value as T) : fallback
}

export type { Metric, PartOfDay }
