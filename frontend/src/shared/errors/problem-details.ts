import type { ProblemDetails } from '../types'

export type ErrorContext =
  | 'generic'
  | 'auth_login'
  | 'auth_register'
  | 'debt_query'
  | 'payment'
  | 'subscription'
  | 'profile'

export interface ParsedError {
  title: string
  detail?: string
  fieldErrors: Record<string, string[]>
  status: number
  userMessage: string
}

export function parseProblemDetails(error: unknown, context: ErrorContext = 'generic'): ParsedError {
  if (!error || typeof error !== 'object') {
    const fallback = 'An unexpected error occurred. Please try again.'
    return { title: fallback, fieldErrors: {}, status: 500, userMessage: fallback }
  }

  const err = error as {
    response?: { data?: ProblemDetails; status?: number }
    message?: string
  }

  const data = err.response?.data
  const status = err.response?.status ?? 500

  if (!data || typeof data !== 'object') {
    const fallback = mapMessageByStatus(status, err.message, '', {}, context)
    return {
      title: 'Request failed.',
      detail: err.message,
      fieldErrors: {},
      status,
      userMessage: fallback,
    }
  }

  const title = data.title ?? data.message ?? 'Request failed.'
  const detail = data.detail ?? data.message
  const fieldErrors = data.errors ?? {}
  const userMessage = mapMessageByStatus(status, title, detail, fieldErrors, context)

  return {
    title,
    detail,
    fieldErrors,
    status,
    userMessage,
  }
}

export function errorMessage(error: unknown, context: ErrorContext = 'generic') {
  const parsed = parseProblemDetails(error, context)
  return parsed.userMessage
}

function mapMessageByStatus(
  status: number,
  title: string | undefined,
  detail: string | undefined,
  fieldErrors: Record<string, string[]>,
  context: ErrorContext,
): string {
  const normalizedTitle = (title ?? '').toLowerCase()
  const normalizedDetail = (detail ?? '').toLowerCase()
  const firstFieldError = Object.values(fieldErrors).flat()[0]

  if (status === 400) {
    if (firstFieldError) {
      return firstFieldError
    }

    if (context === 'debt_query') {
      return 'Please check year and month values.'
    }

    return 'Please check the entered information.'
  }

  if (status === 401) {
    if (context === 'auth_login') {
      return 'Invalid email or password.'
    }

    return 'Your session has expired. Please sign in again.'
  }

  if (status === 403) {
    return 'You do not have permission for this action.'
  }

  if (status === 404) {
    if (context === 'debt_query'
      || normalizedDetail.includes('fatura bulunamad')
      || normalizedDetail.includes('borç kaydı bulunamad')
      || normalizedDetail.includes('debt')) {
      return 'No unpaid debt found for selected subscription.'
    }

    return 'Requested record was not found.'
  }

  if (status === 409) {
    if (normalizedDetail.includes('insufficient balance')) {
      return 'Insufficient balance.'
    }

    if (normalizedDetail.includes('debt changed')
      || normalizedDetail.includes('debt amount changed')
      || normalizedDetail.includes('refresh and try again')) {
      return 'Debt changed. Please refresh and try again.'
    }

    if (normalizedDetail.includes('already exists for this debt')
      || normalizedDetail.includes('already exists for this subscription and period')
      || normalizedDetail.includes('already paid')
      || normalizedDetail.includes('already exists')) {
      return 'This debt has already been paid.'
    }

    return 'This action conflicts with current data.'
  }

  if (status === 502) {
    if (context === 'debt_query' || normalizedDetail.includes('debt provider')) {
      return 'Debt service is temporarily unavailable.'
    }

    if (context === 'payment' || normalizedDetail.includes('payment gateway')) {
      return 'Payment service is temporarily unavailable.'
    }

    return 'External service is temporarily unavailable.'
  }

  if (status >= 500) {
    return 'Server error. Please try again.'
  }

  if (normalizedTitle.includes('network') || normalizedDetail.includes('network')) {
    return 'Cannot reach the server. Please try again.'
  }

  return detail || title || 'Request failed.'
}
