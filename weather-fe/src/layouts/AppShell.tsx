import { NavLink, Outlet } from 'react-router-dom'
import { CurrentWeatherWidget } from '../features/current-widget/CurrentWeatherWidget'
import { Button } from '../components/Button'
import { useAuth } from '../hooks/useAuth'

const tabs = [
  { to: '/', label: 'Search' },
  { to: '/history', label: 'History' },
  { to: '/statistics', label: 'Statistics' },
]

export function AppShell() {
  const { user, logout } = useAuth()

  return (
    <div className="mx-auto max-w-5xl p-6">
      <header className="flex flex-wrap items-center justify-between gap-4">
        <div className="flex items-center gap-8">
          <h1 className="text-xl font-semibold">Weather</h1>
          <nav className="flex gap-1" aria-label="Main">
            {tabs.map(({ to, label }) => (
              <NavLink
                key={to}
                to={to}
                end
                className={({ isActive }) =>
                  `rounded-md px-3 py-1.5 text-sm font-medium transition ${
                    isActive
                      ? 'bg-sky-600 text-white'
                      : 'text-slate-600 hover:bg-slate-200 dark:text-slate-300 dark:hover:bg-slate-800'
                  }`
                }
              >
                {label}
              </NavLink>
            ))}
          </nav>
        </div>
        <div className="flex items-center gap-3 text-sm">
          <span className="text-slate-500">{user?.email}</span>
          <Button onClick={() => void logout()}>Sign out</Button>
        </div>
      </header>

      <div className="mt-8 grid gap-6 md:grid-cols-[20rem_1fr]">
        <aside>
          <CurrentWeatherWidget />
        </aside>
        <main className="min-w-0">
          <Outlet />
        </main>
      </div>
    </div>
  )
}
