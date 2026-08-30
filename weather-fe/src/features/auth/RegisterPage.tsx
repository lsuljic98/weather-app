import { zodResolver } from '@hookform/resolvers/zod'
import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { Link, Navigate } from 'react-router-dom'
import { z } from 'zod'
import { ApiError } from '../../api/client'
import { useAuth } from '../../hooks/useAuth'
import { ErrorAlert } from '../../components/ErrorAlert'
import { Field } from '../../components/Field'
import { SubmitButton } from '../../components/SubmitButton'
import { AuthCard } from './AuthCard'

// Mirrors RegisterRequest on the server: email ≤ 320, password 8–128.
const schema = z.object({
  email: z.email('Enter a valid email address').max(320),
  password: z.string().min(8, 'At least 8 characters').max(128),
})

type FormValues = z.infer<typeof schema>

export function RegisterPage() {
  const { status, register: registerUser } = useAuth()
  const [serverError, setServerError] = useState<string | null>(null)

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<FormValues>({ resolver: zodResolver(schema) })

  if (status === 'authenticated') return <Navigate to="/" replace />

  const onSubmit = handleSubmit(async (values) => {
    setServerError(null)
    try {
      await registerUser(values)
    } catch (err) {
      setServerError(
        err instanceof ApiError && err.status === 409
          ? 'That email is already registered.'
          : 'Could not create the account. Please try again.',
      )
    }
  })

  return (
    <AuthCard
      title="Create account"
      footer={
        <>
          Already have an account?{' '}
          <Link to="/login" className="font-medium text-sky-600 hover:underline">
            Sign in
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
          autoComplete="new-password"
          error={errors.password?.message}
          {...register('password')}
        />
        <ErrorAlert message={serverError} />
        <SubmitButton pending={isSubmitting} pendingLabel="Creating…">
          Create account
        </SubmitButton>
      </form>
    </AuthCard>
  )
}
