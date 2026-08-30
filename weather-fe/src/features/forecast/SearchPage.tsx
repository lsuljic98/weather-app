import { useMemo } from 'react'
import { ApiError } from '../../api/client'
import { useForecast } from '../../hooks/useForecast'
import { CitySearch } from './CitySearch'
import { DayStrip } from './DayStrip'
import { ForecastChart } from './ForecastChart'
import { ForecastFilters } from './ForecastFilters'
import { ForecastGrid } from './ForecastGrid'
import { applyFilters, METRIC_LABELS } from './filters'
import { useForecastParams } from './useForecastParams'

export function SearchPage() {
  const { ref, filters, selectCity, setFilters } = useForecastParams()
  const { data, isPending, isError, error } = useForecast(ref)

  // Grid and chart both render this one array, so a filter change moves both.
  const filtered = useMemo(() => (data ? applyFilters(data.points, filters) : []), [data, filters])

  const selectedDay = filters.from && filters.from === filters.to ? filters.from : undefined

  return (
    <div className="space-y-6">
      <CitySearch onSelect={selectCity} />

      {!ref ? (
        <p className="text-sm text-slate-500">Search for a city to see its 5-day forecast.</p>
      ) : isPending ? (
        <p className="text-sm text-slate-500">Loading forecast for {ref.city}…</p>
      ) : isError || !data ? (
        <p className="text-sm text-red-600 dark:text-red-400">
          {error instanceof ApiError && error.status === 404
            ? `No forecast found for "${ref.city}".`
            : "Couldn't load the forecast. Please try again."}
        </p>
      ) : (
        <>
          <h2 className="text-lg font-semibold">
            {data.city}
            <span className="ml-1 font-normal text-slate-500">{data.country}</span>
          </h2>

          <DayStrip
            days={data.days}
            selected={selectedDay}
            onSelect={(date) =>
              setFilters(selectedDay === date ? { from: undefined, to: undefined } : { from: date, to: date })
            }
          />

          <ForecastFilters days={data.days} filters={filters} onChange={setFilters} />

          {filtered.length === 0 ? (
            <p className="text-sm text-slate-500">No readings match these filters.</p>
          ) : (
            <>
              <section>
                <h3 className="mb-2 text-sm font-semibold text-slate-500">
                  {METRIC_LABELS[filters.metric]} · {filtered.length} readings
                </h3>
                <ForecastChart points={filtered} metric={filters.metric} />
              </section>
              <ForecastGrid points={filtered} metric={filters.metric} />
            </>
          )}
        </>
      )}
    </div>
  )
}
