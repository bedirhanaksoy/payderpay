import type { SessionUser } from '../types'

const SESSION_KEY = 'payderpay_frontend_session'

interface SessionPayload {
  user: SessionUser
}

export const session = {
  save(user: SessionUser) {
    const payload: SessionPayload = { user }
    localStorage.setItem(SESSION_KEY, JSON.stringify(payload))
  },

  clear() {
    localStorage.removeItem(SESSION_KEY)
  },

  getUser(): SessionUser | null {
    const raw = localStorage.getItem(SESSION_KEY)
    if (!raw) {
      return null
    }

    try {
      const parsed = JSON.parse(raw) as SessionPayload
      return parsed.user ?? null
    } catch {
      return null
    }
  },

  isAuthenticated() {
    return !!this.getUser()
  },
}
