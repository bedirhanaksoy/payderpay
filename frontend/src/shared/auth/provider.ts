import { apiClient } from '../api/client'
import { session } from './session'
import type {
  AuthResponse,
  CreateCustomerRequest,
  CustomerResponse,
  SessionUser,
} from '../types'

export interface LoginRequest {
  email: string
  password: string
}

export interface RegisterRequest {
  fullName: string
  email: string
  phoneNumber: string
  password: string
  initialMainAccountBalance: number
}

export interface AuthProvider {
  login(request: LoginRequest): Promise<SessionUser>
  register(request: RegisterRequest): Promise<SessionUser>
  logout(): Promise<void>
}

function customerToSessionUser(customer: CustomerResponse): SessionUser {
  return {
    customerId: customer.id,
    fullName: customer.fullName,
    email: customer.email,
    phoneNumber: customer.phoneNumber,
  }
}

/**
 * Stub provider — keeps the legacy "find-customer-by-email" UX flow alive
 * for demos that don't have the JWT backend running yet.
 */
class StubAuthProvider implements AuthProvider {
  async login(request: LoginRequest): Promise<SessionUser> {
    const response = await apiClient.get<CustomerResponse[]>('/api/customers')
    const customer = response.data.find(
      x => x.email.toLowerCase() === request.email.trim().toLowerCase(),
    )

    if (!customer) {
      throw new Error('User not found. Please register first.')
    }

    return customerToSessionUser(customer)
  }

  async register(request: RegisterRequest): Promise<SessionUser> {
    const payload: CreateCustomerRequest = {
      fullName: request.fullName,
      email: request.email,
      phoneNumber: request.phoneNumber,
      initialMainAccountBalance: request.initialMainAccountBalance,
    }

    const response = await apiClient.post<CustomerResponse>('/api/customers', payload)
    return customerToSessionUser(response.data)
  }

  async logout(): Promise<void> {
    return
  }
}

/**
 * Backend provider — talks to the real JWT-backed /api/auth endpoints,
 * persists access + refresh tokens via the session module so the axios
 * interceptors can auto-attach and auto-refresh them.
 */
class BackendAuthProvider implements AuthProvider {
  async login(request: LoginRequest): Promise<SessionUser> {
    const response = await apiClient.post<AuthResponse>('/api/auth/login', request)
    session.saveAuth(response.data)
    return customerToSessionUser(response.data.customer)
  }

  async register(request: RegisterRequest): Promise<SessionUser> {
    const response = await apiClient.post<AuthResponse>('/api/auth/register', request)
    session.saveAuth(response.data)
    return customerToSessionUser(response.data.customer)
  }

  async logout(): Promise<void> {
    const refreshToken = session.getRefreshToken()
    try {
      await apiClient.post('/api/auth/logout', { refreshToken })
    } finally {
      session.clear()
    }
  }
}

const providerName = (import.meta.env.VITE_AUTH_PROVIDER ?? 'backend').toLowerCase()

export const authProvider: AuthProvider =
  providerName === 'stub' ? new StubAuthProvider() : new BackendAuthProvider()
