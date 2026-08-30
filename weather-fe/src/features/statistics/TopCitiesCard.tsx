import { useTopCities } from '../../hooks/useStatistics'
import { StatCard } from './StatCard'

export function TopCitiesCard() {
  const { data = [], isPending, isError } = useTopCities()
  const max = Math.max(1, ...data.map((c) => c.count))

  return (
    <StatCard
      title="Most searched cities"
      isPending={isPending}
      isError={isError}
      isEmpty={data.length === 0}
      emptyMessage="Search for a forecast and your top cities will show up here."
    >
      <ol className="space-y-3">
        {data.map((c, i) => (
          <li key={`${c.city}-${c.country}`} className="text-sm">
            <div className="flex items-baseline justify-between">
              <span>
                <span className="mr-2 text-slate-400">{i + 1}.</span>
                {c.city}
                <span className="ml-1 text-slate-500">{c.country}</span>
              </span>
              <span className="tabular-nums text-slate-500">
                {c.count} {c.count === 1 ? 'search' : 'searches'}
              </span>
            </div>
            <div className="mt-1 h-1.5 rounded-full bg-slate-100 dark:bg-slate-800">
              <div
                className="h-full rounded-full bg-sky-600 dark:bg-sky-400"
                style={{ width: `${(c.count / max) * 100}%` }}
              />
            </div>
          </li>
        ))}
      </ol>
    </StatCard>
  )
}
