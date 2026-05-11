import axios, { type AxiosRequestConfig } from 'axios'
import type { AuthResponse } from '../types'
import { session } from '../auth/session'

const baseUrl = (import.meta.env.VITE_API_BASE_URL ?? '').trim()

export const apiClient = axios.create({
  baseURL: baseUrl,
  headers: {
    'Content-Type': 'application/json',
  },
})

/* ── Request interceptor: attach bearer token ──────────────── */
apiClient.interceptors.request.use(config => {
  const accessToken = session.getAccessToken()
  if (accessToken) {
    config.headers.Authorization = `Bearer ${accessToken}`
  }
  return config
})

/* ── Response interceptor: 401 → refresh-and-retry with queue ── */
const AUTH_BYPASS_PATHS = [
  '/auth/login',
  '/auth/register',
  '/auth/refresh',
  '/auth/logout',
]

function shouldBypass401(url?: string) {
  if (!url) return false
  return AUTH_BYPASS_PATHS.some(path => url.includes(path))
}

let isRefreshing = false
let pendingQueue: Array<{
  resolve: (token: string) => void
  reject: (error: unknown) => void
}> = []

function drainQueue(token: string | null, error: unknown) {
  pendingQueue.forEach(p => (token ? p.resolve(token) : p.reject(error)))
  pendingQueue = []
}

apiClient.interceptors.response.use(
  response => response,
  async (error) => {
    const original: AxiosRequestConfig & { _retried?: boolean } = error.config ?? {}

    if (shouldBypass401(original.url)) {
      return Promise.reject(error)
    }

    if (error.response?.status !== 401 || original._retried) {
      return Promise.reject(error)
    }

    const refreshToken = session.getRefreshToken()
    if (!refreshToken) {
      session.clear()
      window.location.href = '/auth/login'
      return Promise.reject(error)
    }

    if (isRefreshing) {
      return new Promise((resolve, reject) => {
        pendingQueue.push({
          resolve: (token) => {
            if (original.headers) {
              original.headers.Authorization = `Bearer ${token}`
            }
            resolve(apiClient(original))
          },
          reject,
        })
      })
    }

    isRefreshing = true
    original._retried = true

    try {
      const { data } = await axios.post<AuthResponse>(
        `${baseUrl}/api/auth/refresh`,
        { refreshToken },
        { headers: { 'Content-Type': 'application/json' } },
      )
      session.updateTokens(data)
      drainQueue(data.accessToken, null)
      if (original.headers) {
        original.headers.Authorization = `Bearer ${data.accessToken}`
      }
      return apiClient(original)
    } catch (refreshError) {
      drainQueue(null, refreshError)
      session.clear()
      window.location.href = '/auth/login'
      return Promise.reject(refreshError)
    } finally {
      isRefreshing = false
    }
  },
)
