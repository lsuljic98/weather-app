import type { ReactNode } from 'react'

interface AuthCardProps {
  title: string
  children: ReactNode
  footer: ReactNode
}

export function AuthCard({ title, children, footer }: AuthCardProps) {
  return (
    <main className="flex min-h-screen items-center justify-center p-4">
      <div className="w-full max-w-sm rounded-xl border border-slate-200 bg-white p-8 shadow-sm dark:border-slate-800 dark:bg-slate-900">
        <h1 className="text-2xl font-semibold">{title}</h1>
        <p className="mt-1 text-sm text-slate-500">Weather app</p>
        {children}
        <p className="mt-6 text-center text-sm text-slate-500">{footer}</p>
      </div>
    </main>
  )
}
