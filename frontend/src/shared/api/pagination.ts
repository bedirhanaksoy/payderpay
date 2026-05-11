import type { AxiosResponse } from 'axios'
import type { PaginatedResult } from '../types'

function readHeaderNumber(headers: Record<string, unknown>, key: string, fallback: number): number {
  const raw = headers[key]
  if (typeof raw === 'string') {
    const parsed = Number(raw)
    return Number.isFinite(parsed) ? parsed : fallback
  }

  return fallback
}

export function toPaginatedResult<T>(
  response: AxiosResponse<T[]>,
  fallbackPage: number,
  fallbackPageSize: number,
): PaginatedResult<T> {
  const page = readHeaderNumber(response.headers as Record<string, unknown>, 'x-page', fallbackPage)
  const pageSize = readHeaderNumber(response.headers as Record<string, unknown>, 'x-page-size', fallbackPageSize)
  const totalCount = readHeaderNumber(response.headers as Record<string, unknown>, 'x-total-count', response.data.length)
  const totalPages = readHeaderNumber(
    response.headers as Record<string, unknown>,
    'x-total-pages',
    totalCount === 0 ? 0 : Math.ceil(totalCount / pageSize),
  )

  return {
    items: response.data,
    page,
    pageSize,
    totalCount,
    totalPages,
  }
}
