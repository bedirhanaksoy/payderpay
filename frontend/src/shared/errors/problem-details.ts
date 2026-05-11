import type { ProblemDetails } from '../types'

export interface ParsedError {
  title: string
  detail?: string
  fieldErrors: Record<string, string[]>
  status: number
}

export function parseProblemDetails(error: unknown): ParsedError {
  if (!error || typeof error !== 'object') {
    return { title: 'An unexpected error occurred.', fieldErrors: {}, status: 500 }
  }

  const err = error as {
    response?: { data?: ProblemDetails; status?: number }
    message?: string
  }

  const data = err.response?.data
  const status = err.response?.status ?? 500

  if (!data || typeof data !== 'object') {
    return {
      title: 'An unexpected error occurred.',
      detail: err.message,
      fieldErrors: {},
      status,
    }
  }

  return {
    title: data.title ?? data.message ?? 'Request failed.',
    detail: data.detail ?? data.message,
    fieldErrors: data.errors ?? {},
    status,
  }
}

export function errorMessage(error: unknown) {
  const parsed = parseProblemDetails(error)
  return parsed.detail ?? parsed.title
}
