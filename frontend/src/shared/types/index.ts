export type SubscriptionType = 1 | 2 | 3 | 4 | 5 | 6
export type SubscriptionStatus = 1 | 2
export type PaymentStatus = 1 | 2

export interface CustomerResponse {
  id: string
  fullName: string
  email: string
  phoneNumber: string
  isActive: boolean
  createdAtUtc: string
  updatedAtUtc: string
}

export interface CreateCustomerRequest {
  fullName: string
  email: string
  phoneNumber: string
  initialMainAccountBalance: number
}

export interface MainAccountResponse {
  customerId: string
  iban: string
  balance: number
  createdAtUtc: string
  updatedAtUtc: string
}

export interface SubscriptionResponse {
  id: string
  customerId: string
  subscriptionType: SubscriptionType
  providerName: string
  subscriberNumber: string
  status: SubscriptionStatus
  dueDayOfMonth: number
  createdAtUtc: string
  updatedAtUtc: string
}

export interface CreateSubscriptionRequest {
  customerId: string
  subscriptionType: SubscriptionType
  providerName: string
  subscriberNumber: string
  dueDayOfMonth: number
}

export interface UpdateSubscriptionRequest {
  subscriptionType: SubscriptionType
  providerName: string
  subscriberNumber: string
  status: SubscriptionStatus
  dueDayOfMonth: number
}

export interface DebtQueryRequest {
  periodYear?: number
  periodMonth?: number
}

export interface DebtQueryResponse {
  subscriptionId: string
  amount: number
  dueDate: string
  periodYear: number
  periodMonth: number
  queriedAtUtc: string
  providerRef?: string
}

export interface DebtQueryHistoryItemResponse {
  id: string
  subscriptionId: string
  amount: number
  dueDate: string
  periodYear: number
  periodMonth: number
  queriedAtUtc: string
  providerRef?: string
}

export interface CreatePaymentRequest {
  debtQueryResultId: string
}

export interface PaymentResponse {
  id: string
  subscriptionId: string
  amount: number
  paymentDateUtc: string
  periodYear: number
  periodMonth: number
  status: PaymentStatus
  externalTransactionId?: string
  failureReason?: string
}

export interface PaymentHistoryItemResponse {
  id: string
  subscriptionId: string
  amount: number
  paymentDateUtc: string
  periodYear: number
  periodMonth: number
  status: PaymentStatus
}

export interface UnpaidSubscriptionResponse {
  subscriptionId: string
  customerId: string
  subscriptionType: SubscriptionType
  providerName: string
  subscriberNumber: string
  periodYear: number
  periodMonth: number
  dueDate: string
}

export interface DashboardSummaryResponse {
  activeSubscriptionCount: number
  unpaidThisMonthCount: number
  successfulPaymentsThisMonthTotal: number
  unpaidSubscriptions: UnpaidSubscriptionResponse[]
}

export interface ProblemDetails {
  type?: string
  title?: string
  status?: number
  detail?: string
  traceId?: string
  errors?: Record<string, string[]>
  message?: string
}

export interface SessionUser {
  customerId: string
  fullName: string
  email: string
  phoneNumber: string
}
