import { Navigate, Outlet, useLocation } from 'react-router-dom'
import { useAuth } from '../hooks/useAuth'

export function ProtectedRoute() {
  const { status } = useAuth()
  const location = useLocation()

  if (status === 'booting') {
    return (
      <div className="flex min-h-screen items-center justify-center text-slate-500">
        Loading…
      </div>
    )
  }

  if (status === 'anonymous') {
    return (
      <Navigate to="/login" replace state={{ from: location.pathname + location.search }} />
    )
  }

  return <Outlet />
}
