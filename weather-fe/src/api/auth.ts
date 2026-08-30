import { api } from './client'

export interface User {
  id: string
  email: string
}

export interface TokenResponse {
  user: User
  accessToken: string
  expiresIn: number
}

export interface Credentials {
  email: string
  password: string
}

export const authApi = {
  login: (body: Credentials) =>
    api<TokenResponse>('/auth/login', { method: 'POST', body, skipAuthRetry: true }),

  register: (body: Credentials) =>
    api<TokenResponse>('/auth/register', { method: 'POST', body, skipAuthRetry: true }),

  logout: () => api<void>('/auth/logout', { method: 'POST', skipAuthRetry: true }),
}
