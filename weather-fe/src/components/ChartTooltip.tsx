import type { ReactNode } from 'react'

export function ChartTooltip({ title, children }: { title: ReactNode; children: ReactNode }) {
  return (
    <div className="rounded-md border border-slate-200 bg-white px-3 py-2 text-xs shadow-md dark:border-slate-700 dark:bg-slate-900">
      <p className="font-medium">{title}</p>
      {children}
    </div>
  )
}
