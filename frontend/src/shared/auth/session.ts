import type { AuthResponse, CustomerResponse, SessionUser } from '../types'

const SESSION_KEY = 'payderpay_frontend_session'

interface SessionPayload {
  user: SessionUser
  accessToken?: string
  refreshToken?: string
  accessTokenExpiresAtUtc?: string
  refreshTokenExpiresAtUtc?: string
}

function readPayload(): SessionPayload | null {
  const raw = localStorage.getItem(SESSION_KEY)
  if (!raw) {
    return null
  }
  try {
    return JSON.parse(raw) as SessionPayload
  } catch {
    return null
  }
}

function writePayload(payload: SessionPayload) {
  localStorage.setItem(SESSION_KEY, JSON.stringify(payload))
}

function customerToSessionUser(customer: CustomerResponse): SessionUser {
  return {
    customerId: customer.id,
    fullName: customer.fullName,
    email: customer.email,
    phoneNumber: customer.phoneNumber,
  }
}

export const session = {
  /** Persist a user object only (preserves any existing tokens). */
  save(user: SessionUser) {
    const current = readPayload()
    writePayload({ ...current, user })
  },

  /** Persist the full auth response (user + access & refresh tokens). */
  saveAuth(authResponse: AuthResponse) {
    writePayload({
      user: customerToSessionUser(authResponse.customer),
      accessToken: authResponse.accessToken,
      refreshToken: authResponse.refreshToken,
      accessTokenExpiresAtUtc: authResponse.accessTokenExpiresAtUtc,
      refreshTokenExpiresAtUtc: authResponse.refreshTokenExpiresAtUtc,
    })
  },

  /** Rotate tokens (and refresh user) after a successful /auth/refresh call. */
  updateTokens(authResponse: AuthResponse) {
    const current = readPayload()
    writePayload({
      user: current?.user ?? customerToSessionUser(authResponse.customer),
      accessToken: authResponse.accessToken,
      refreshToken: authResponse.refreshToken,
      accessTokenExpiresAtUtc: authResponse.accessTokenExpiresAtUtc,
      refreshTokenExpiresAtUtc: authResponse.refreshTokenExpiresAtUtc,
    })
  },

  clear() {
    localStorage.removeItem(SESSION_KEY)
  },

  getUser(): SessionUser | null {
    return readPayload()?.user ?? null
  },

  getAccessToken(): string | null {
    return readPayload()?.accessToken ?? null
  },

  getRefreshToken(): string | null {
    return readPayload()?.refreshToken ?? null
  },

  isAuthenticated() {
    return !!this.getUser()
  },
}
