import { zodResolver } from '@hookform/resolvers/zod'
import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { Link, Navigate } from 'react-router-dom'
import { z } from 'zod'
import { ApiError } from '../../api/client'
import { useAuth } from '../../hooks/useAuth'
import { useReturnTo } from '../../hooks/useReturnTo'
import { ErrorAlert } from '../../components/ErrorAlert'
import { Field } from '../../components/Field'
import { Button } from '../../components/Button'
import { AuthCard } from './AuthCard'

// Mirrors LoginRequest on the server.
const schema = z.object({
  email: z.email('Enter a valid email address'),
  password: z.string().min(1, 'Password is required'),
})

type FormValues = z.infer<typeof schema>

export function LoginPage() {
  const { status, login } = useAuth()
  const returnTo = useReturnTo()
  const [serverError, setServerError] = useState<string | null>(null)

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<FormValues>({ resolver: zodResolver(schema) })

  if (status === 'authenticated') return <Navigate to={returnTo} replace />

  const onSubmit = handleSubmit(async (values) => {
    setServerError(null)
    try {
      await login(values)
    } catch (err) {
      setServerError(
        err instanceof ApiError && err.status === 401
          ? 'Unknown email or wrong password.'
          : 'Could not sign in. Please try again.',
      )
    }
  })

  return (
    <AuthCard
      title="Sign in"
      footer={
        <>
          No account?{' '}
          <Link to="/register" className="font-medium text-sky-600 hover:underline">
            Register
          </Link>
        </>
      }
    >
      <form onSubmit={onSubmit} noValidate className="mt-6 space-y-4">
        <Field
          label="Email"
          type="email"
          autoComplete="email"
          autoFocus
          error={errors.email?.message}
          {...register('email')}
        />
        <Field
          label="Password"
          type="password"
          autoComplete="current-password"
          error={errors.password?.message}
          {...register('password')}
        />
        <ErrorAlert message={serverError} />
        <Button type="submit" variant="primary" disabled={isSubmitting} className="w-full">
          {isSubmitting ? 'Signing in…' : 'Sign in'}
        </Button>
      </form>
    </AuthCard>
  )
}
