import { apiClient } from './client'
import type {
  CreateSubscriptionRequest,
  DebtQueryRequest,
  DebtQueryResponse,
  PaymentHistoryItemResponse,
  PaymentResponse,
  SubscriptionResponse,
  UnpaidSubscriptionResponse,
  UpdateSubscriptionRequest,
} from '../types'

export const subscriptionsApi = {
  listByCustomer: async (customerId: string) => {
    const response = await apiClient.get<SubscriptionResponse[]>(`/api/subscriptions?customerId=${customerId}`)
    return response.data
  },

  listActive: async () => {
    const response = await apiClient.get<SubscriptionResponse[]>('/api/subscriptions/active')
    return response.data
  },

  create: async (payload: CreateSubscriptionRequest) => {
    const response = await apiClient.post<SubscriptionResponse>('/api/subscriptions', payload)
    return response.data
  },

  update: async (id: string, payload: UpdateSubscriptionRequest) => {
    const response = await apiClient.put<SubscriptionResponse>(`/api/subscriptions/${id}`, payload)
    return response.data
  },

  delete: async (id: string) => {
    await apiClient.delete(`/api/subscriptions/${id}`)
  },

  getUnpaid: async (customerId: string, year: number, month: number) => {
    const response = await apiClient.get<UnpaidSubscriptionResponse[]>(
      `/api/subscriptions/unpaid?customerId=${customerId}&year=${year}&month=${month}`,
    )
    return response.data
  },

  queryDebt: async (subscriptionId: string, payload: DebtQueryRequest = {}) => {
    const response = await apiClient.post<DebtQueryResponse>(
      `/api/subscriptions/${subscriptionId}/debt-queries`,
      payload,
    )
    return response.data
  },

  getDebtHistory: async (subscriptionId: string) => {
    const response = await apiClient.get<DebtQueryResponse>(
      `/api/subscriptions/${subscriptionId}/debt-queries`,
    )
    return response.data
  },

  createPayment: async (subscriptionId: string, debtId: string) => {
    const response = await apiClient.post<PaymentResponse>(`/api/subscriptions/${subscriptionId}/payments`, {
      debtId,
    })
    return response.data
  },

  getPaymentHistory: async (subscriptionId: string) => {
    const response = await apiClient.get<PaymentHistoryItemResponse[]>(
      `/api/subscriptions/${subscriptionId}/payments`,
    )
    return response.data
  },
}
