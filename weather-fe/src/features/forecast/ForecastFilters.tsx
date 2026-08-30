import type { ForecastDay } from '../../api/weather'
import {
  METRIC_LABELS,
  METRICS,
  PART_LABELS,
  PARTS_OF_DAY,
  type ForecastFilters as Filters,
} from './filters'
import { weekday } from './format'

interface ForecastFiltersProps {
  days: ForecastDay[]
  filters: Filters
  onChange: (patch: Partial<Filters>) => void
}

export function ForecastFilters({ days, filters, onChange }: ForecastFiltersProps) {
  const first = days[0]?.date
  const last = days[days.length - 1]?.date

  return (
    <div className="flex flex-wrap items-end gap-3">
      <Select
        label="From"
        value={filters.from ?? first ?? ''}
        onChange={(v) => onChange({ from: v === first ? undefined : v, ...(filters.to && v > filters.to ? { to: v } : {}) })}
        options={days.map((d) => [d.date, weekday(d.date)])}
      />
      <Select
        label="To"
        value={filters.to ?? last ?? ''}
        onChange={(v) => onChange({ to: v === last ? undefined : v, ...(filters.from && v < filters.from ? { from: v } : {}) })}
        options={days.map((d) => [d.date, weekday(d.date)])}
      />
      <Select
        label="Part of day"
        value={filters.partOfDay}
        onChange={(v) => onChange({ partOfDay: v as Filters['partOfDay'] })}
        options={PARTS_OF_DAY.map((p) => [p, PART_LABELS[p]])}
      />
      <Select
        label="Metric"
        value={filters.metric}
        onChange={(v) => onChange({ metric: v as Filters['metric'] })}
        options={METRICS.map((m) => [m, METRIC_LABELS[m]])}
      />
      {(filters.from || filters.to || filters.partOfDay !== 'all') && (
        <button
          type="button"
          onClick={() => onChange({ from: undefined, to: undefined, partOfDay: 'all' })}
          className="rounded-md px-3 py-2 text-sm text-sky-600 hover:underline"
        >
          Reset
        </button>
      )}
    </div>
  )
}

interface SelectProps {
  label: string
  value: string
  options: [value: string, label: string][]
  onChange: (value: string) => void
}

function Select({ label, value, options, onChange }: SelectProps) {
  return (
    <label className="text-xs text-slate-500">
      <span className="mb-1 block">{label}</span>
      <select
        value={value}
        onChange={(e) => onChange(e.target.value)}
        className="rounded-md border border-slate-300 bg-white px-2 py-2 text-sm text-slate-900 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-100"
      >
        {options.map(([v, l]) => (
          <option key={v} value={v}>
            {l}
          </option>
        ))}
      </select>
    </label>
  )
}
