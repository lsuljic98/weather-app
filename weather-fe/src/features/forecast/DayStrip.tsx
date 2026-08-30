import type { ForecastDay } from '../../api/weather'
import { WeatherIcon } from '../../components/WeatherIcon'
import { weekday } from '../../lib/format'

interface DayStripProps {
  days: ForecastDay[]
  selected?: string
  onSelect: (date: string) => void
}

// Five daily summaries; clicking one narrows the filters to that day.
export function DayStrip({ days, selected, onSelect }: DayStripProps) {
  return (
    <ul className="grid grid-cols-5 gap-2">
      {days.map((d) => {
        const active = d.date === selected
        return (
          <li key={d.date}>
            <button
              type="button"
              onClick={() => onSelect(d.date)}
              aria-pressed={active}
              className={`w-full rounded-xl border px-2 py-3 text-center text-sm transition ${
                active
                  ? 'border-sky-500 bg-sky-50 dark:bg-slate-800'
                  : 'border-slate-200 hover:bg-slate-100 dark:border-slate-800 dark:hover:bg-slate-800'
              }`}
            >
              <p className="text-xs text-slate-500">{weekday(d.date)}</p>
              {d.icon && <WeatherIcon code={d.icon} size={36} label={d.condition} className="mx-auto my-1" />}
              <p className="tabular-nums">
                <span className="font-medium">{Math.round(d.maxTemperatureC)}°</span>
                <span className="ml-1 text-slate-500">{Math.round(d.minTemperatureC)}°</span>
              </p>
            </button>
          </li>
        )
      })}
    </ul>
  )
}
