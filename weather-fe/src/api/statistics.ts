import { api } from './client'

export interface TopCity {
  city: string
  country: string
  count: number
}

export interface SearchRecord {
  id: string
  city: string
  country: string
  latitude: number
  longitude: number
  searchedAt: string
  temperatureC: number
  humidity: number
  windSpeed: number
  condition: string
  description: string
  icon: string
}

export interface ConditionCount {
  condition: string
  count: number
}

export const statisticsApi = {
  topCities: (take = 3) => api<TopCity[]>(`/statistics/top-cities?take=${take}`),
  recent: (take = 3) => api<SearchRecord[]>(`/statistics/recent?take=${take}`),
  conditions: () => api<ConditionCount[]>('/statistics/conditions'),
}
