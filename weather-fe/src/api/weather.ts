import { api } from './client'

export interface CurrentWeather {
  city: string
  country: string
  latitude: number
  longitude: number
  temperatureC: number
  feelsLikeC: number
  humidity: number
  windSpeed: number
  condition: string
  description: string
  icon: string
  observedAt: string
}

export interface Coordinates {
  latitude: number
  longitude: number
}

export const weatherApi = {
  currentAt: ({ latitude, longitude }: Coordinates) =>
    api<CurrentWeather>(`/weather/current/coordinates?lat=${latitude}&lon=${longitude}`),
}

export interface City {
  name: string
  state: string | null
  country: string
  latitude: number
  longitude: number
}

export interface ForecastPoint {
  /** ISO string carrying the city's UTC offset, e.g. "2026-08-30T15:00:00+02:00". */
  localTime: string
  temperatureC: number
  humidity: number
  windSpeed: number
  precipitationProbability: number
  condition: string
  description: string
  icon: string
}

export interface ForecastDay {
  date: string
  minTemperatureC: number
  maxTemperatureC: number
  humidity: number
  windSpeed: number
  precipitationProbability: number
  condition: string
  description: string
  icon: string
  readingCount: number
}

export interface Forecast {
  city: string
  country: string
  latitude: number
  longitude: number
  days: ForecastDay[]
  points: ForecastPoint[]
}

export interface CityRef {
  city: string
  countryCode?: string
}

export const citiesApi = {
  search: (q: string) => api<City[]>(`/weather/cities?q=${encodeURIComponent(q)}`),
}

export const forecastApi = {
  get: ({ city, countryCode }: CityRef) => {
    const params = new URLSearchParams({ city })
    if (countryCode) params.set('countryCode', countryCode)
    return api<Forecast>(`/weather/forecast?${params}`)
  },
}
