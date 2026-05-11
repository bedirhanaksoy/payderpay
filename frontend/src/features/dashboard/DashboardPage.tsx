import { useMemo } from 'react'
import { useQuery } from '@tanstack/react-query'
import { customersApi } from '../../shared/api/customers'
import { subscriptionsApi } from '../../shared/api/subscriptions'
import { getCurrentUserOrThrow } from '../../shared/auth/use-current-user'
import { formatCurrency } from '../../shared/format/amount'
import { formatDateOnly } from '../../shared/format/dateTime'
import { subscriptionTypeLabel } from '../../shared/format/enums'
import type { SubscriptionType } from '../../shared/types'

type UnpaidDebtRow = {
  subscriptionId: string
  subscriptionType: SubscriptionType
  providerName: string
  subscriberNumber: string
  debtId: string
  amount: number
  dueDate: string
  periodYear: number
  periodMonth: number
}

export default function DashboardPage() {
  const user = getCurrentUserOrThrow()

  const accountQuery = useQuery({
    queryKey: ['main-account', user.customerId],
    queryFn: () => customersApi.getMainAccount(user.customerId),
  })

  const subscriptionsQuery = useQuery({
    queryKey: ['subscriptions', user.customerId],
    queryFn: () => subscriptionsApi.listByCustomer(user.customerId),
  })

  const activeSubscriptions = useMemo(
    () => (subscriptionsQuery.data ?? []).filter(x => x.status === 1),
    [subscriptionsQuery.data],
  )

  const activeSubscriptionIds = useMemo(
    () => activeSubscriptions.map(x => x.id).sort(),
    [activeSubscriptions],
  )

  const unpaidDebtsQuery = useQuery({
    queryKey: ['dashboard-unpaid-debts', user.customerId, activeSubscriptionIds.join(',')],
    enabled: activeSubscriptions.length > 0,
    queryFn: async () => {
      const debtsBySubscription = await Promise.all(
        activeSubscriptions.map(async subscription => {
          const response = await subscriptionsApi.getDebtHistory(subscription.id)
          return response.debts.map<UnpaidDebtRow>(debt => ({
            subscriptionId: subscription.id,
            subscriptionType: subscription.subscriptionType,
            providerName: debt.providerName || subscription.providerName,
            subscriberNumber: subscription.subscriberNumber,
            debtId: debt.debtId,
            amount: debt.amount,
            dueDate: debt.dueDate,
            periodYear: debt.periodYear,
            periodMonth: debt.periodMonth,
          }))
        }),
      )

      return debtsBySubscription
        .flat()
        .sort((a, b) => {
          if (a.dueDate !== b.dueDate) {
            return a.dueDate.localeCompare(b.dueDate)
          }
          return a.subscriberNumber.localeCompare(b.subscriberNumber)
        })
    },
  })

  const loading = accountQuery.isLoading || subscriptionsQuery.isLoading || unpaidDebtsQuery.isLoading

  if (loading) {
    return <div className="empty-state"><span className="spin">◌</span></div>
  }

  const account = accountQuery.data
  const unpaidDebts = unpaidDebtsQuery.data ?? []

  if (!account) {
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

      <div className="stats-grid">
        <div className="stat-card">
          <div className="stat-label">Active Subscriptions</div>
          <div className="stat-value">{activeSubscriptions.length}</div>
        </div>
        <div className="stat-card">
          <div className="stat-label">Unpaid Debts</div>
          <div className="stat-value">{unpaidDebts.length}</div>
        </div>
      </div>

      <div className="card">
        <div className="card-header">
          <div className="card-title">Unpaid Debts</div>
        </div>
        <div className="card-body">
          {unpaidDebtsQuery.isError ? (
            <div className="empty-state">
              <div className="empty-state-icon">⚠</div>
              <div className="empty-state-text">Could not refresh unpaid debts right now.</div>
            </div>
          ) : unpaidDebts.length === 0 ? (
            <div className="empty-state">
              <div className="empty-state-icon">✓</div>
              <div className="empty-state-text">No unpaid debt for your active subscriptions.</div>
            </div>
          ) : (
            <div className="table-wrap">
              <table className="table">
                <thead>
                  <tr>
                    <th>Type</th>
                    <th>Provider</th>
                    <th>Subscriber No</th>
                    <th>Period</th>
                    <th>Amount</th>
                    <th>Due Date</th>
                  </tr>
                </thead>
                <tbody>
                  {unpaidDebts.map(item => (
                    <tr key={item.debtId}>
                      <td>{subscriptionTypeLabel(item.subscriptionType)}</td>
                      <td>{item.providerName}</td>
                      <td>{item.subscriberNumber}</td>
                      <td>{item.periodYear}/{String(item.periodMonth).padStart(2, '0')}</td>
                      <td className="amount">{formatCurrency(item.amount)}</td>
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
