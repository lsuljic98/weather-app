import { describe, expect, it } from 'vitest'
import type { ForecastPoint } from '../../api/weather'
import { applyFilters, partOfDayFor } from './filters'

const point = (localTime: string): ForecastPoint => ({
  localTime,
  temperatureC: 20,
  humidity: 50,
  windSpeed: 1,
  precipitationProbability: 0,
  condition: 'Clear',
  description: 'clear sky',
  icon: '01d',
})

const points = [
  point('2026-08-30T03:00:00+02:00'),
  point('2026-08-30T09:00:00+02:00'),
  point('2026-08-30T15:00:00+02:00'),
  point('2026-08-31T21:00:00+02:00'),
  point('2026-09-01T12:00:00+02:00'),
]

describe('applyFilters', () => {
  it('returns everything when unbounded', () => {
    expect(applyFilters(points, { partOfDay: 'all', metric: 'temperature' })).toHaveLength(5)
  })

  it('keeps the date range inclusive on both ends', () => {
    const result = applyFilters(points, {
      from: '2026-08-31',
      to: '2026-09-01',
      partOfDay: 'all',
      metric: 'temperature',
    })
    expect(result.map((p) => p.localTime)).toEqual([
      '2026-08-31T21:00:00+02:00',
      '2026-09-01T12:00:00+02:00',
    ])
  })

  it('filters by part of day using the local hour, not the browser timezone', () => {
    const result = applyFilters(points, { partOfDay: 'morning', metric: 'temperature' })
    expect(result.map((p) => p.localTime)).toEqual(['2026-08-30T09:00:00+02:00'])
  })

  it('combines date range and part of day', () => {
    const result = applyFilters(points, {
      from: '2026-08-30',
      to: '2026-08-30',
      partOfDay: 'night',
      metric: 'temperature',
    })
    expect(result).toHaveLength(1)
  })
})

describe('partOfDayFor', () => {
  it.each([
    [0, 'night'],
    [5, 'night'],
    [6, 'morning'],
    [11, 'morning'],
    [12, 'afternoon'],
    [17, 'afternoon'],
    [18, 'evening'],
    [23, 'evening'],
  ])('hour %i → %s', (hour, expected) => {
    expect(partOfDayFor(hour)).toBe(expected)
  })
})
