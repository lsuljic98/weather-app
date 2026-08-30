import type { TokenResponse } from './auth'


// Thin fetch wrapper. Injects the in-memory bearer token.
export class ApiError extends Error {
  readonly status: number
  readonly problem?: ProblemDetails

  constructor(status: number, message: string, problem?: ProblemDetails) {
    super(message)
    this.name = 'ApiError'
    this.status = status
    this.problem = problem
  }
}

export interface ProblemDetails {
  type?: string
  title?: string
  status?: number
  detail?: string
  errors?: Record<string, string[]>
}

type SessionListener = (session: TokenResponse | null) => void

let accessToken: string | null = null
let refreshInFlight: Promise<TokenResponse | null> | null = null
let onSessionRefreshed: SessionListener | null = null

export function setAccessToken(token: string | null) {
  accessToken = token
}

export function setSessionListener(listener: SessionListener | null) {
  onSessionRefreshed = listener
}

interface RequestOptions extends Omit<RequestInit, 'body'> {
  body?: unknown
  skipAuthRetry?: boolean
}

export async function api<T>(
  path: string,
  { body, skipAuthRetry = false, ...init }: RequestOptions = {},
): Promise<T> {
  const response = await send(path, body, init)

  if (response.status === 401 && !skipAuthRetry && (await refreshSession())) {
    return parse<T>(await send(path, body, init))
  }

  return parse<T>(response)
}

// Shared across concurrent callers; resolves to null if the session can't be restored.
export function refreshSession(): Promise<TokenResponse | null> {
  refreshInFlight ??= api<TokenResponse>('/auth/refresh', { method: 'POST', skipAuthRetry: true })
    .then(
      (session) => {
        accessToken = session.accessToken
        return session
      },
      () => {
        accessToken = null
        return null
      },
    )
    .then((session) => {
      onSessionRefreshed?.(session)
      return session
    })
    .finally(() => {
      refreshInFlight = null
    })

  return refreshInFlight
}

function send(path: string, body: unknown, init: Omit<RequestInit, 'body'>) {
  const headers = new Headers(init.headers)
  if (body !== undefined) headers.set('Content-Type', 'application/json')
  if (accessToken) headers.set('Authorization', `Bearer ${accessToken}`)

  return fetch(`/api${path}`, {
    ...init,
    headers,
    credentials: 'same-origin',
    body: body === undefined ? undefined : JSON.stringify(body),
  })
}

async function parse<T>(response: Response): Promise<T> {
  if (response.ok) {
    if (response.status === 204) return undefined as T
    return (await response.json()) as T
  }

  let problem: ProblemDetails | undefined
  try {
    problem = (await response.json()) as ProblemDetails
  } catch {
    // non-JSON error body
  }

  const message =
    problem?.detail ?? problem?.title ?? `${response.status} ${response.statusText}`.trim()
  throw new ApiError(response.status, message, problem)
}
