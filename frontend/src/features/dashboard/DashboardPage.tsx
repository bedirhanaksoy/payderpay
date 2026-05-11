import { useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { customersApi } from '../../shared/api/customers'
import { getCurrentUserOrThrow } from '../../shared/auth/use-current-user'
import { formatCurrency } from '../../shared/format/amount'
import { formatDateOnly } from '../../shared/format/dateTime'
import { subscriptionTypeLabel } from '../../shared/format/enums'

function currentUtcPeriod() {
  const now = new Date()
  return {
    year: now.getUTCFullYear(),
    month: now.getUTCMonth() + 1,
  }
}

export default function DashboardPage() {
  const user = getCurrentUserOrThrow()
  const defaultPeriod = currentUtcPeriod()
  const [year, setYear] = useState(defaultPeriod.year)
  const [month, setMonth] = useState(defaultPeriod.month)

  const accountQuery = useQuery({
    queryKey: ['main-account', user.customerId],
    queryFn: () => customersApi.getMainAccount(user.customerId),
  })

  const dashboardQuery = useQuery({
    queryKey: ['dashboard', user.customerId, year, month],
    queryFn: () => customersApi.getDashboard(user.customerId, year, month),
  })

  const loading = accountQuery.isLoading || dashboardQuery.isLoading

  if (loading) {
    return <div className="empty-state"><span className="spin">◌</span></div>
  }

  const account = accountQuery.data
  const dashboard = dashboardQuery.data

  if (!account || !dashboard) {
    return (
      <div className="empty-state">
        <div className="empty-state-icon">⚠</div>
        <div className="empty-state-text">Dashboard data could not be loaded.</div>
      </div>
    )
  }

  return (
    <>
      <div className="page-header">
        <div className="page-header-left">
          <h1 className="page-title">Dashboard</h1>
          <p className="page-subtitle">Customer-level subscription payment overview</p>
        </div>
      </div>

      <div className="card" style={{ marginBottom: '1rem' }}>
        <div className="card-header">
          <div className="card-title">Main Account</div>
        </div>
        <div className="card-body">
          <div className="summary-kv-grid">
            <div>
              <div className="section-title">IBAN</div>
              <div className="summary-kv-value">{account.iban}</div>
            </div>
            <div>
              <div className="section-title">Current Balance</div>
              <div className="summary-kv-value amount">{formatCurrency(account.balance)}</div>
            </div>
          </div>
        </div>
      </div>

      <div className="filters-bar">
        <input
          className="field-input"
          type="number"
          min={2000}
          max={3000}
          value={year}
          onChange={event => setYear(Number(event.target.value))}
          placeholder="Year"
        />
        <input
          className="field-input"
          type="number"
          min={1}
          max={12}
          value={month}
          onChange={event => setMonth(Number(event.target.value))}
          placeholder="Month"
        />
      </div>

      <div className="stats-grid">
        <div className="stat-card">
          <div className="stat-label">Active Subscriptions</div>
          <div className="stat-value">{dashboard.activeSubscriptionCount}</div>
        </div>
        <div className="stat-card">
          <div className="stat-label">Unpaid in Period</div>
          <div className="stat-value">{dashboard.unpaidThisMonthCount}</div>
        </div>
        <div className="stat-card">
          <div className="stat-label">Successful Total</div>
          <div className="stat-value">{formatCurrency(dashboard.successfulPaymentsThisMonthTotal)}</div>
        </div>
      </div>

      <div className="card">
        <div className="card-header">
          <div className="card-title">Unpaid Subscriptions ({year}/{String(month).padStart(2, '0')})</div>
        </div>
        <div className="card-body">
          {dashboard.unpaidSubscriptions.length === 0 ? (
            <div className="empty-state">
              <div className="empty-state-icon">✓</div>
              <div className="empty-state-text">No unpaid active subscription for this period.</div>
            </div>
          ) : (
            <div className="table-wrap">
              <table className="table">
                <thead>
                  <tr>
                    <th>Type</th>
                    <th>Provider</th>
                    <th>Subscriber No</th>
                    <th>Due Date</th>
                  </tr>
                </thead>
                <tbody>
                  {dashboard.unpaidSubscriptions.map(item => (
                    <tr key={item.subscriptionId}>
                      <td>{subscriptionTypeLabel(item.subscriptionType)}</td>
                      <td>{item.providerName}</td>
                      <td>{item.subscriberNumber}</td>
                      <td>{formatDateOnly(item.dueDate)}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </div>
      </div>
    </>
  )
}
