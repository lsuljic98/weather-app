import { CurrentWeatherWidget } from '../features/current-widget/CurrentWeatherWidget'
import { useAuth } from '../hooks/useAuth'

export function HomePage() {
  const { user, logout } = useAuth()

  return (
    <main className="mx-auto max-w-3xl p-6">
      <header className="flex items-center justify-between">
        <h1 className="text-xl font-semibold">Weather</h1>
        <div className="flex items-center gap-3 text-sm">
          <span className="text-slate-500">{user?.email}</span>
          <button
            type="button"
            onClick={() => void logout()}
            className="rounded-md border border-slate-300 px-3 py-1 hover:bg-slate-100 dark:border-slate-700 dark:hover:bg-slate-800"
          >
            Sign out
          </button>
        </div>
      </header>

      <div className="mt-8 max-w-sm">
        <CurrentWeatherWidget />
      </div>
    </main>
  )
}
