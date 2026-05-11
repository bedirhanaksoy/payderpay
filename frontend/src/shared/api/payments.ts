import { apiClient } from './client'
import type { PaymentHistoryItemResponse } from '../types'

export const paymentsApi = {
  getByCustomer: async (customerId: string) => {
    const response = await apiClient.get<PaymentHistoryItemResponse[]>(`/api/payments?customerId=${customerId}`)
    return response.data
  },
}
