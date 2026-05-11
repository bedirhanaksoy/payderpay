import { apiClient } from './client'
import type { CreateCustomerRequest, CustomerResponse, DashboardSummaryResponse, MainAccountResponse } from '../types'

export const customersApi = {
  create: async (payload: CreateCustomerRequest) => {
    const response = await apiClient.post<CustomerResponse>('/api/customers', payload)
    return response.data
  },

  getById: async (id: string) => {
    const response = await apiClient.get<CustomerResponse>(`/api/customers/${id}`)
    return response.data
  },

  getAll: async () => {
    const response = await apiClient.get<CustomerResponse[]>('/api/customers')
    return response.data
  },

  delete: async (id: string) => {
    await apiClient.delete(`/api/customers/${id}`)
  },

  getMainAccount: async (id: string) => {
    const response = await apiClient.get<MainAccountResponse>(`/api/customers/${id}/main-account`)
    return response.data
  },

  getDashboard: async (id: string, year: number, month: number) => {
    const response = await apiClient.get<DashboardSummaryResponse>(
      `/api/customers/${id}/dashboard?year=${year}&month=${month}`,
    )
    return response.data
  },
}
