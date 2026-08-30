import { api } from './client'

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

export interface PagedResult<T> {
  items: T[]
  page: number
  pageSize: number
  totalCount: number
  totalPages: number
  hasNext: boolean
  hasPrevious: boolean
}

export const searchesApi = {
  history: (page: number, pageSize: number) =>
    api<PagedResult<SearchRecord>>(`/searches?page=${page}&pageSize=${pageSize}`),
}
