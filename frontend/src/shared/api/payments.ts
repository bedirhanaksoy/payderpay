import { apiClient } from './client'
import { toPaginatedResult } from './pagination'
import type { PaginatedResult, PaymentHistoryItemResponse } from '../types'

export const paymentsApi = {
  getByCustomer: async (
    customerId: string,
    page = 1,
    pageSize = 20,
  ): Promise<PaginatedResult<PaymentHistoryItemResponse>> => {
    const response = await apiClient.get<PaymentHistoryItemResponse[]>(
      `/api/payments?customerId=${customerId}&page=${page}&pageSize=${pageSize}`,
    )
    return toPaginatedResult(response, page, pageSize)
  },
}
