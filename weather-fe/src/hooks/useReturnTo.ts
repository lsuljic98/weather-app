import { useLocation } from 'react-router-dom'

// Path to return to after login (set by ProtectedRoute); same-origin paths only.
export function useReturnTo(fallback = '/') {
  const { state } = useLocation()
  const from = (state as { from?: unknown } | null)?.from
  return typeof from === 'string' && from.startsWith('/') && !from.startsWith('//')
    ? from
    : fallback
}
