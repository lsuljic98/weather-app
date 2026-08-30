import type { ReactNode } from 'react'

interface SubmitButtonProps {
  pending: boolean
  pendingLabel: ReactNode
  children: ReactNode
}

export function SubmitButton({ pending, pendingLabel, children }: SubmitButtonProps) {
  return (
    <button
      type="submit"
      disabled={pending}
      className="w-full rounded-md bg-sky-600 px-4 py-2 text-sm font-medium text-white transition hover:bg-sky-700 disabled:opacity-60"
    >
      {pending ? pendingLabel : children}
    </button>
  )
}
