import { api } from './client'
import type { SearchRecord } from './searches'

export interface TopCity {
  city: string
  country: string
  count: number
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
