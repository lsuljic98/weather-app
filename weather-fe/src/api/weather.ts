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
