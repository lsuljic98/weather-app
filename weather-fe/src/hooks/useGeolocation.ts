import { useEffect, useState } from 'react'
import type { Coordinates } from '../api/weather'

export type GeolocationState =
  | { status: 'locating' }
  | { status: 'located'; coords: Coordinates }
  | { status: 'unavailable'; reason: 'denied' | 'unsupported' | 'failed' }

// Asks the browser once for the device position.
export function useGeolocation(): GeolocationState {
  const [state, setState] = useState<GeolocationState>(() =>
    'geolocation' in navigator
      ? { status: 'locating' }
      : { status: 'unavailable', reason: 'unsupported' },
  )

  useEffect(() => {
    if (!('geolocation' in navigator)) return

    let cancelled = false
    navigator.geolocation.getCurrentPosition(
      ({ coords }) => {
        if (cancelled) return
        setState({
          status: 'located',
          coords: { latitude: coords.latitude, longitude: coords.longitude },
        })
      },
      (error) => {
        if (cancelled) return
        setState({
          status: 'unavailable',
          reason: error.code === error.PERMISSION_DENIED ? 'denied' : 'failed',
        })
      },
      { timeout: 10_000, maximumAge: 5 * 60_000 },
    )

    return () => {
      cancelled = true
    }
  }, [])

  return state
}
