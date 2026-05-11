import { apiClient } from '../api/client'
import type { CreateCustomerRequest, CustomerResponse, SessionUser } from '../types'

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

class StubAuthProvider implements AuthProvider {
  async login(request: LoginRequest): Promise<SessionUser> {
    const response = await apiClient.get<CustomerResponse[]>('/api/customers')
    const customer = response.data.find(
      x => x.email.toLowerCase() === request.email.trim().toLowerCase(),
    )

    if (!customer) {
      throw new Error('User not found. Please register first.')
    }

    return {
      customerId: customer.id,
      fullName: customer.fullName,
      email: customer.email,
      phoneNumber: customer.phoneNumber,
    }
  }

  async register(request: RegisterRequest): Promise<SessionUser> {
    const payload: CreateCustomerRequest = {
      fullName: request.fullName,
      email: request.email,
      phoneNumber: request.phoneNumber,
      initialMainAccountBalance: request.initialMainAccountBalance,
    }

    const response = await apiClient.post<CustomerResponse>('/api/customers', payload)

    return {
      customerId: response.data.id,
      fullName: response.data.fullName,
      email: response.data.email,
      phoneNumber: response.data.phoneNumber,
    }
  }

  async logout(): Promise<void> {
    return
  }
}

class BackendAuthProvider implements AuthProvider {
  async login(request: LoginRequest): Promise<SessionUser> {
    const response = await apiClient.post<SessionUser>('/api/auth/login', request)
    return response.data
  }

  async register(request: RegisterRequest): Promise<SessionUser> {
    const response = await apiClient.post<SessionUser>('/api/auth/register', request)
    return response.data
  }

  async logout(): Promise<void> {
    await apiClient.post('/api/auth/logout')
  }
}

const providerName = (import.meta.env.VITE_AUTH_PROVIDER ?? 'stub').toLowerCase()

export const authProvider: AuthProvider =
  providerName === 'backend' ? new BackendAuthProvider() : new StubAuthProvider()
