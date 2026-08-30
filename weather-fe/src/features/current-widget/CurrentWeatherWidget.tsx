import type { ReactNode } from 'react'
import type { Coordinates, CurrentWeather } from '../../api/weather'
import { Button } from '../../components/Button'
import { WeatherIcon } from '../../components/WeatherIcon'
import { useCurrentWeather } from '../../hooks/useCurrentWeather'
import { useGeolocation } from '../../hooks/useGeolocation'
import { formatTime } from '../../lib/format'

// Where the widget points when the browser can't tell us where the user is.
const FALLBACK: Coordinates = { latitude: 45.815, longitude: 15.9819 } // Zagreb

export function CurrentWeatherWidget() {
  const location = useGeolocation()
  const coords =
    location.status === 'located'
      ? location.coords
      : location.status === 'unavailable'
        ? FALLBACK
        : null

  const { data, isPending, isError, refetch } = useCurrentWeather(coords)

  if (location.status === 'locating' || (coords && isPending)) {
    return <Skeleton label={location.status === 'locating' ? 'Finding your location…' : 'Loading weather…'} />
  }

  if (isError || !data) {
    return (
      <Card className="bg-slate-100 dark:bg-slate-800">
        <p className="text-sm text-slate-600 dark:text-slate-300">
          Couldn't load the current weather.
        </p>
        <Button onClick={() => void refetch()} className="mt-3">
          Try again
        </Button>
      </Card>
    )
  }

  return (
    <Card className={`text-white ${backgroundFor(data)}`}>
      <div className="flex items-start justify-between gap-4">
        <div>
          <p className="text-sm font-medium opacity-90">
            {data.city}
            {data.country && `, ${data.country}`}
          </p>
          <p className="mt-1 text-5xl font-semibold tracking-tight">
            {Math.round(data.temperatureC)}°
          </p>
          <p className="mt-1 text-sm capitalize opacity-90">{data.description}</p>
        </div>
        {data.icon && <WeatherIcon code={data.icon} size={88} label={data.condition} className="-mt-1 drop-shadow" />}
      </div>

      <dl className="mt-4 grid grid-cols-3 gap-2 text-sm">
        <Stat label="Feels like" value={`${Math.round(data.feelsLikeC)}°`} />
        <Stat label="Humidity" value={`${data.humidity}%`} />
        <Stat label="Wind" value={`${data.windSpeed.toFixed(1)} m/s`} />
      </dl>

      <p className="mt-4 text-xs opacity-75">
        {location.status === 'unavailable'
          ? `${unavailableNote(location.reason)} Showing ${data.city}.`
          : `Updated ${formatTime(data.observedAt)}`}
      </p>
    </Card>
  )
}

function Card({ className, children }: { className?: string; children: ReactNode }) {
  return <section className={`rounded-2xl p-6 shadow-md ${className ?? ''}`}>{children}</section>
}

function Stat({ label, value }: { label: string; value: string }) {
  return (
    <div className="rounded-lg bg-white/15 px-3 py-2">
      <dt className="text-xs opacity-80">{label}</dt>
      <dd className="font-medium">{value}</dd>
    </div>
  )
}

function Skeleton({ label }: { label: string }) {
  return (
    <Card className="animate-pulse bg-slate-200 dark:bg-slate-800">
      <div className="h-4 w-32 rounded bg-slate-300 dark:bg-slate-700" />
      <div className="mt-3 h-12 w-24 rounded bg-slate-300 dark:bg-slate-700" />
      <div className="mt-3 h-4 w-40 rounded bg-slate-300 dark:bg-slate-700" />
      <p className="mt-4 text-xs text-slate-500">{label}</p>
    </Card>
  )
}

function unavailableNote(reason: 'denied' | 'unsupported' | 'failed') {
  switch (reason) {
    case 'denied':
      return 'Location access was denied.'
    case 'unsupported':
      return "This browser can't share your location."
    default:
      return "Couldn't determine your location."
  }
}

// OpenWeather icon codes end in "d" (day) or "n" (night).
function backgroundFor({ condition, icon }: CurrentWeather) {
  const night = icon.endsWith('n')
  switch (condition) {
    case 'Clear':
      return night
        ? 'bg-gradient-to-br from-indigo-900 to-slate-900'
        : 'bg-gradient-to-br from-sky-400 to-blue-600'
    case 'Clouds':
      return night
        ? 'bg-gradient-to-br from-slate-700 to-slate-900'
        : 'bg-gradient-to-br from-slate-400 to-slate-600'
    case 'Rain':
    case 'Drizzle':
      return 'bg-gradient-to-br from-slate-500 to-blue-800'
    case 'Thunderstorm':
      return 'bg-gradient-to-br from-slate-700 to-indigo-950'
    case 'Snow':
      return 'bg-gradient-to-br from-sky-200 to-slate-400'
    default: // Mist, Fog, Haze, …
      return 'bg-gradient-to-br from-slate-400 to-slate-500'
  }
}
