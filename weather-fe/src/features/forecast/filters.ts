import type { ForecastPoint } from '../../api/weather'

export const PARTS_OF_DAY = ['all', 'night', 'morning', 'afternoon', 'evening'] as const
export type PartOfDay = (typeof PARTS_OF_DAY)[number]

export const METRICS = ['temperature', 'humidity', 'wind', 'precipitation'] as const
export type Metric = (typeof METRICS)[number]

export interface ForecastFilters {
  /** Inclusive local dates, "YYYY-MM-DD". Undefined means unbounded. */
  from?: string
  to?: string
  partOfDay: PartOfDay
  metric: Metric
}

export const METRIC_LABELS: Record<Metric, string> = {
  temperature: 'Temperature',
  humidity: 'Humidity',
  wind: 'Wind',
  precipitation: 'Rain chance',
}

export const PART_LABELS: Record<PartOfDay, string> = {
  all: 'Whole day',
  night: 'Night (00–05)',
  morning: 'Morning (06–11)',
  afternoon: 'Afternoon (12–17)',
  evening: 'Evening (18–23)',
}

// The API's localTime already carries the city's offset, so the date and hour are
// read straight off the string instead of going through the browser's timezone.
export const localDate = (iso: string) => iso.slice(0, 10)
export const localHour = (iso: string) => Number(iso.slice(11, 13))

export function partOfDayFor(hour: number): Exclude<PartOfDay, 'all'> {
  if (hour < 6) return 'night'
  if (hour < 12) return 'morning'
  if (hour < 18) return 'afternoon'
  return 'evening'
}

// The one place grid and chart both get their rows from.
export function applyFilters(points: ForecastPoint[], filters: ForecastFilters): ForecastPoint[] {
  return points.filter((p) => {
    const date = localDate(p.localTime)
    if (filters.from && date < filters.from) return false
    if (filters.to && date > filters.to) return false
    if (filters.partOfDay !== 'all' && partOfDayFor(localHour(p.localTime)) !== filters.partOfDay)
      return false
    return true
  })
}

export function metricValue(p: ForecastPoint, metric: Metric): number {
  switch (metric) {
    case 'temperature':
      return p.temperatureC
    case 'humidity':
      return p.humidity
    case 'wind':
      return p.windSpeed
    case 'precipitation':
      return Math.round(p.precipitationProbability * 100)
  }
}

export function formatMetric(value: number, metric: Metric): string {
  switch (metric) {
    case 'temperature':
      return `${Math.round(value)}°`
    case 'humidity':
    case 'precipitation':
      return `${Math.round(value)}%`
    case 'wind':
      return `${value.toFixed(1)} m/s`
  }
}
