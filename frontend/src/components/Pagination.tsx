interface PaginationProps {
  page: number
  totalPages: number
  totalCount?: number
  onPageChange: (nextPage: number) => void
  isFetching?: boolean
}

/**
 * Simple prev/next pagination control. Hides itself when there's only a single
 * page (or none) of results — pages with small datasets stay clean.
 */
export default function Pagination({
  page,
  totalPages,
  totalCount,
  onPageChange,
  isFetching,
}: PaginationProps) {
  if (totalPages <= 1) {
    return null
  }

  const canPrev = page > 1 && !isFetching
  const canNext = page < totalPages && !isFetching

  return (
    <div
      style={{
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'space-between',
        gap: 12,
        marginTop: 16,
        flexWrap: 'wrap',
      }}
    >
      <div className="muted" style={{ fontSize: 13 }}>
        Sayfa <strong>{page}</strong> / {totalPages}
        {typeof totalCount === 'number' && (
          <span style={{ marginLeft: 8 }}>· {totalCount} kayıt</span>
        )}
      </div>

      <div style={{ display: 'flex', gap: 8 }}>
        <button
          type="button"
          className="btn btn-secondary btn-sm"
          disabled={!canPrev}
          onClick={() => onPageChange(page - 1)}
        >
          Önceki
        </button>
        <button
          type="button"
          className="btn btn-secondary btn-sm"
          disabled={!canNext}
          onClick={() => onPageChange(page + 1)}
        >
          Sonraki
        </button>
      </div>
    </div>
  )
}
