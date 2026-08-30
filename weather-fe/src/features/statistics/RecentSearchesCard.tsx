import { WeatherIcon } from '../../components/WeatherIcon'
import { useRecentSearches } from '../../hooks/useStatistics'
import { formatDateTime } from '../../lib/format'
import { StatCard } from './StatCard'

export function RecentSearchesCard() {
  const { data = [], isPending, isError } = useRecentSearches()

  return (
    <StatCard
      title="Recent searches"
      isPending={isPending}
      isError={isError}
      isEmpty={data.length === 0}
      emptyMessage="No searches yet."
    >
      <ul className="divide-y divide-slate-100 dark:divide-slate-800">
        {data.map((s) => (
          <li key={s.id} className="flex items-center gap-3 py-2 text-sm">
            {s.icon && <WeatherIcon code={s.icon} size={36} />}
            <div className="min-w-0 flex-1">
              <p className="truncate">
                {s.city}
                <span className="ml-1 text-slate-500">{s.country}</span>
              </p>
              <p className="truncate text-xs capitalize text-slate-500">
                {s.description} · {s.humidity}% · {s.windSpeed.toFixed(1)} m/s
              </p>
            </div>
            <div className="shrink-0 text-right">
              <p className="font-medium tabular-nums">{Math.round(s.temperatureC)}°</p>
              <p className="text-xs text-slate-500">{formatDateTime(s.searchedAt)}</p>
            </div>
          </li>
        ))}
      </ul>
    </StatCard>
  )
}
