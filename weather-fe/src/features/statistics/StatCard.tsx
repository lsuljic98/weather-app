import type { ReactNode } from 'react'

interface StatCardProps {
  title: string
  isPending: boolean
  isError: boolean
  isEmpty: boolean
  emptyMessage: string
  children: ReactNode
}

// Shared frame: title, then loading / error / empty / content.
export function StatCard({ title, isPending, isError, isEmpty, emptyMessage, children }: StatCardProps) {
  return (
    <section className="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm dark:border-slate-800 dark:bg-slate-900">
      <h2 className="text-sm font-semibold text-slate-500">{title}</h2>
      <div className="mt-3">
        {isPending ? (
          <div className="animate-pulse space-y-2">
            <div className="h-4 w-3/4 rounded bg-slate-200 dark:bg-slate-800" />
            <div className="h-4 w-1/2 rounded bg-slate-200 dark:bg-slate-800" />
            <div className="h-4 w-2/3 rounded bg-slate-200 dark:bg-slate-800" />
          </div>
        ) : isError ? (
          <p className="text-sm text-red-600 dark:text-red-400">Couldn't load this card.</p>
        ) : isEmpty ? (
          <p className="text-sm text-slate-500">{emptyMessage}</p>
        ) : (
          children
        )}
      </div>
    </section>
  )
}
