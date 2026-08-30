import { Link, Navigate, useSearchParams } from 'react-router-dom'
import { ApiError } from '../../api/client'
import { Button } from '../../components/Button'
import { WeatherIcon } from '../../components/WeatherIcon'
import { useSearchHistory } from '../../hooks/useSearchHistory'
import { formatDateTime } from '../../lib/format'

export function HistoryPage() {
  const [params, setParams] = useSearchParams()
  const page = Math.max(1, Number(params.get('page')) || 1)
  const { data, isPending, isError, error, isPlaceholderData } = useSearchHistory(page)

  const goTo = (next: number) => setParams(next === 1 ? {} : { page: String(next) })

  if (isPending) {
    return <p className="text-sm text-slate-500">Loading history…</p>
  }

  if (isError || !data) {
    return (
      <p className="text-sm text-red-600 dark:text-red-400">
        {error instanceof ApiError && error.status === 400
          ? 'That page does not exist.'
          : "Couldn't load your search history."}
      </p>
    )
  }

  if (data.totalCount === 0) {
    return (
      <p className="text-sm text-slate-500">
        You haven't searched for a forecast yet.{' '}
        <Link to="/" className="text-sky-600 hover:underline">
          Search for a city
        </Link>
        .
      </p>
    )
  }

  // Past the last page (e.g. a stale link): send the user back to the last real one.
  if (data.items.length === 0 && data.totalPages > 0) {
    return <Navigate to={`/history?page=${data.totalPages}`} replace />
  }

  const first = (data.page - 1) * data.pageSize + 1
  const last = first + data.items.length - 1

  return (
    <div className="space-y-4">
      <div className="flex items-baseline justify-between">
        <h2 className="text-lg font-semibold">Search history</h2>
        <p className="text-sm text-slate-500">
          {first}–{last} of {data.totalCount}
        </p>
      </div>

      <div
        className={`overflow-x-auto rounded-xl border border-slate-200 transition-opacity dark:border-slate-800 ${
          isPlaceholderData ? 'opacity-60' : ''
        }`}
      >
        <table className="w-full text-sm">
          <thead className="bg-slate-50 text-left text-xs text-slate-500 dark:bg-slate-900">
            <tr>
              <th scope="col" className="px-3 py-2 font-medium">When</th>
              <th scope="col" className="px-3 py-2 font-medium">City</th>
              <th scope="col" className="px-3 py-2 font-medium">Conditions</th>
              <th scope="col" className="px-3 py-2 text-right font-medium">Temp</th>
              <th scope="col" className="px-3 py-2 text-right font-medium">Humidity</th>
              <th scope="col" className="px-3 py-2 text-right font-medium">Wind</th>
              <th scope="col" className="px-3 py-2"><span className="sr-only">Actions</span></th>
            </tr>
          </thead>
          <tbody className="divide-y divide-slate-100 dark:divide-slate-800">
            {data.items.map((s) => (
              <tr key={s.id}>
                <td className="px-3 py-2 whitespace-nowrap text-slate-500">{formatDateTime(s.searchedAt)}</td>
                <td className="px-3 py-2 whitespace-nowrap">
                  {s.city}
                  <span className="ml-1 text-slate-500">{s.country}</span>
                </td>
                <td className="px-3 py-2">
                  <span className="flex items-center gap-1 capitalize">
                    {s.icon && <WeatherIcon code={s.icon} size={22} />}
                    {s.description}
                  </span>
                </td>
                <td className="px-3 py-2 text-right tabular-nums">{Math.round(s.temperatureC)}°</td>
                <td className="px-3 py-2 text-right tabular-nums">{s.humidity}%</td>
                <td className="px-3 py-2 text-right tabular-nums whitespace-nowrap">{s.windSpeed.toFixed(1)} m/s</td>
                <td className="px-3 py-2 text-right">
                  <Link
                    to={`/?${new URLSearchParams({ city: s.city, country: s.country })}`}
                    className="whitespace-nowrap text-sky-600 hover:underline"
                  >
                    Search again
                  </Link>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {data.totalPages > 1 && (
        <nav className="flex items-center justify-between text-sm" aria-label="Pagination">
          <Button onClick={() => goTo(data.page - 1)} disabled={!data.hasPrevious}>
            Previous
          </Button>
          <span className="text-slate-500">
            Page {data.page} of {data.totalPages}
          </span>
          <Button onClick={() => goTo(data.page + 1)} disabled={!data.hasNext}>
            Next
          </Button>
        </nav>
      )}
    </div>
  )
}
