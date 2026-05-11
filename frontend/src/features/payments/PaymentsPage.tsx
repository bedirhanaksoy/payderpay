import { useMemo, useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { paymentsApi } from '../../shared/api/payments'
import { getCurrentUserOrThrow } from '../../shared/auth/use-current-user'
import { formatCurrency } from '../../shared/format/amount'
import { formatDateTime } from '../../shared/format/dateTime'
import { paymentStatusLabel } from '../../shared/format/enums'

function currentUtcPeriod() {
  const now = new Date()
  return {
    year: now.getUTCFullYear(),
    month: now.getUTCMonth() + 1,
  }
}

export default function PaymentsPage() {
  const user = getCurrentUserOrThrow()
  const defaultPeriod = currentUtcPeriod()

  const [year, setYear] = useState(defaultPeriod.year)
  const [month, setMonth] = useState<number | 'all'>('all')

  const paymentsQuery = useQuery({
    queryKey: ['payments', user.customerId],
    queryFn: () => paymentsApi.getByCustomer(user.customerId),
  })

  const filtered = useMemo(() => {
    const items = paymentsQuery.data ?? []

    return items.filter(item => {
      if (item.periodYear !== year) {
        return false
      }

      if (month !== 'all' && item.periodMonth !== month) {
        return false
      }

      return true
    })
  }, [month, paymentsQuery.data, year])

  if (paymentsQuery.isLoading) {
    return <div className="empty-state"><span className="spin">◌</span></div>
  }

  return (
    <>
      <div className="page-header">
        <div className="page-header-left">
          <h1 className="page-title">Payments</h1>
          <p className="page-subtitle">Customer-wide payment history</p>
        </div>
      </div>

      <div className="filters-bar">
        <input
          className="field-input"
          type="number"
          min="2000"
          max="3000"
          value={year}
          onChange={event => setYear(Number(event.target.value))}
        />

        <select className="field-select" value={month} onChange={event => {
          const value = event.target.value
          setMonth(value === 'all' ? 'all' : Number(value))
        }}>
          <option value="all">All months</option>
          {Array.from({ length: 12 }, (_, i) => i + 1).map(item => (
            <option key={item} value={item}>{item}</option>
          ))}
        </select>
      </div>

      {filtered.length === 0 ? (
        <div className="empty-state">
          <div className="empty-state-icon">◌</div>
          <div className="empty-state-text">No payment record for selected period.</div>
        </div>
      ) : (
        <div className="table-wrap">
          <table className="table">
            <thead>
              <tr>
                <th>Subscription Id</th>
                <th>Period</th>
                <th>Amount</th>
                <th>Payment Date</th>
                <th>Status</th>
              </tr>
            </thead>
            <tbody>
              {filtered.map(item => (
                <tr key={item.id}>
                  <td className="muted" style={{ fontFamily: 'var(--font-mono)', fontSize: 12 }}>{item.subscriptionId}</td>
                  <td>{item.periodYear}/{String(item.periodMonth).padStart(2, '0')}</td>
                  <td className="amount">{formatCurrency(item.amount)}</td>
                  <td>{formatDateTime(item.paymentDateUtc)}</td>
                  <td>
                    <span className={`badge ${item.status === 1 ? 'badge-success' : 'badge-error'}`}>
                      {paymentStatusLabel(item.status)}
                    </span>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </>
  )
}
