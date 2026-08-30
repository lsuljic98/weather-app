import type { ButtonHTMLAttributes } from 'react'

interface ButtonProps extends ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: 'primary' | 'secondary'
}

const variants = {
  primary: 'bg-sky-600 px-4 py-2 font-medium text-white hover:bg-sky-700',
  secondary:
    'border border-slate-300 px-3 py-1 hover:bg-slate-100 dark:border-slate-700 dark:hover:bg-slate-800',
}

export function Button({ variant = 'secondary', className = '', type = 'button', ...props }: ButtonProps) {
  return (
    <button
      type={type}
      className={`rounded-md text-sm transition disabled:opacity-50 disabled:hover:bg-transparent ${variants[variant]} ${className}`}
      {...props}
    />
  )
}
