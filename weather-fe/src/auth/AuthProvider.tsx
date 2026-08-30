import { useQueryClient } from '@tanstack/react-query'
import { useCallback, useEffect, useMemo, useState, type ReactNode } from 'react'
import { authApi, type Credentials, type TokenResponse, type User } from '../api/auth'
import { refreshSession, setAccessToken, setSessionListener } from '../api/client'
import { AuthContext, type AuthStatus } from './AuthContext'

// Access token stays in memory; the refresh cookie restores the session on reload.
export function AuthProvider({ children }: { children: ReactNode }) {
  const queryClient = useQueryClient()
  const [status, setStatus] = useState<AuthStatus>('booting')
  const [user, setUser] = useState<User | null>(null)

  const applySession = useCallback((session: TokenResponse) => {
    setAccessToken(session.accessToken)
    setUser(session.user)
    setStatus('authenticated')
  }, [])

  const clearSession = useCallback(() => {
    setAccessToken(null)
    setUser(null)
    setStatus('anonymous')
    queryClient.clear()
  }, [queryClient])

  // Boot and silent refreshes both report back through this listener.
  useEffect(() => {
    setSessionListener((session) => (session ? applySession(session) : clearSession()))
    void refreshSession()
    return () => setSessionListener(null)
  }, [applySession, clearSession])

  const value = useMemo(
    () => ({
      status,
      user,
      login: async (credentials: Credentials) => applySession(await authApi.login(credentials)),
      register: async (credentials: Credentials) =>
        applySession(await authApi.register(credentials)),
      logout: async () => {
        try {
          await authApi.logout()
        } finally {
          clearSession()
        }
      },
    }),
    [status, user, applySession, clearSession],
  )

  return <AuthContext value={value}>{children}</AuthContext>
}
