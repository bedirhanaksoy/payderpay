import { useEffect, useMemo, useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useSearchParams } from 'react-router-dom'
import { subscriptionsApi } from '../../shared/api/subscriptions'
import { customersApi } from '../../shared/api/customers'
import { getCurrentUserOrThrow } from '../../shared/auth/use-current-user'
import { Field, Input, Select } from '../../components/Field'
import { formatCurrency } from '../../shared/format/amount'
import { formatDateOnly, formatDateTime } from '../../shared/format/dateTime'
import { paymentStatusLabel, subscriptionTypeLabel } from '../../shared/format/enums'
import { parseProblemDetails } from '../../shared/errors/problem-details'

function currentUtcPeriod() {
  const now = new Date()
  return {
    year: now.getUTCFullYear(),
    month: now.getUTCMonth() + 1,
  }
}

export default function BillingPage() {
  const queryClient = useQueryClient()
  const user = getCurrentUserOrThrow()
  const [searchParams, setSearchParams] = useSearchParams()

  const subscriptionsQuery = useQuery({
    queryKey: ['subscriptions', user.customerId],
    queryFn: () => subscriptionsApi.listByCustomer(user.customerId),
  })

  const activeSubscriptions = useMemo(
    () => (subscriptionsQuery.data ?? []).filter(x => x.status === 1),
    [subscriptionsQuery.data],
  )

  const [subscriptionId, setSubscriptionId] = useState(searchParams.get('subscriptionId') ?? '')

  useEffect(() => {
    if (!subscriptionId && activeSubscriptions.length > 0) {
      setSubscriptionId(activeSubscriptions[0].id)
      setSearchParams({ subscriptionId: activeSubscriptions[0].id })
    }
  }, [activeSubscriptions, searchParams, setSearchParams, subscriptionId])

  const [year, setYear] = useState<number | ''>(currentUtcPeriod().year)
  const [month, setMonth] = useState<number | ''>(currentUtcPeriod().month)

  const debtHistoryQuery = useQuery({
    queryKey: ['debt-history', subscriptionId],
    queryFn: () => subscriptionsApi.getDebtHistory(subscriptionId),
    enabled: !!subscriptionId,
  })

  const paymentHistoryQuery = useQuery({
    queryKey: ['subscription-payments', subscriptionId],
    queryFn: () => subscriptionsApi.getPaymentHistory(subscriptionId),
    enabled: !!subscriptionId,
  })

  const accountQuery = useQuery({
    queryKey: ['main-account', user.customerId],
    queryFn: () => customersApi.getMainAccount(user.customerId),
  })

  const [queryError, setQueryError] = useState<string | null>(null)
  const [paymentError, setPaymentError] = useState<string | null>(null)
  const [paymentSuccess, setPaymentSuccess] = useState<string | null>(null)

  const queryDebtMutation = useMutation({
    mutationFn: () => {
      if (!subscriptionId) {
        throw new Error('Please select an active subscription.')
      }

      const payload: { periodYear?: number; periodMonth?: number } = {}

      if (year !== '' && month !== '') {
        payload.periodYear = Number(year)
        payload.periodMonth = Number(month)
      }

      if ((year === '' && month !== '') || (year !== '' && month === '')) {
        throw new Error('Year and month must be provided together, or both left empty.')
      }

      return subscriptionsApi.queryDebt(subscriptionId, payload)
    },
    onSuccess: () => {
      setQueryError(null)
      queryClient.invalidateQueries({ queryKey: ['debt-history', subscriptionId] })
    },
    onError: error => {
      const parsed = parseProblemDetails(error)
      setQueryError(parsed.detail ?? parsed.title)
    },
  })

  const payMutation = useMutation({
    mutationFn: (debtQueryResultId: string) => {
      if (!subscriptionId) {
        throw new Error('Please select a subscription.')
      }
      return subscriptionsApi.createPayment(subscriptionId, debtQueryResultId)
    },
    onSuccess: response => {
      setPaymentError(null)
      setPaymentSuccess(
        response.status === 1
          ? 'Payment successful. Main account balance was updated.'
          : `Payment failed: ${response.failureReason ?? 'Unknown gateway error.'}`,
      )
      queryClient.invalidateQueries({ queryKey: ['subscription-payments', subscriptionId] })
      queryClient.invalidateQueries({ queryKey: ['main-account', user.customerId] })
      queryClient.invalidateQueries({ queryKey: ['dashboard'] })
    },
    onError: error => {
      const parsed = parseProblemDetails(error)
      setPaymentSuccess(null)
      setPaymentError(parsed.detail ?? parsed.title)
    },
  })

  if (subscriptionsQuery.isLoading) {
    return <div className="empty-state"><span className="spin">◌</span></div>
  }

  const selectedSubscription = activeSubscriptions.find(x => x.id === subscriptionId)
  const debts = debtHistoryQuery.data ?? []
  const payments = paymentHistoryQuery.data ?? []

  return (
    <>
      <div className="page-header">
        <div className="page-header-left">
          <h1 className="page-title">Debt & Payments</h1>
          <p className="page-subtitle">Query debt and pay from your main account</p>
        </div>
      </div>

      <div className="card" style={{ marginBottom: '1rem' }}>
        <div className="card-body">
          <div className="summary-kv-grid">
            <div>
              <div className="section-title">Main Account Balance</div>
              <div className="summary-kv-value amount">{formatCurrency(accountQuery.data?.balance ?? 0)}</div>
            </div>
            <div>
              <div className="section-title">Selected Subscription</div>
              <div className="summary-kv-value">
                {selectedSubscription
                  ? `${selectedSubscription.providerName} (${subscriptionTypeLabel(selectedSubscription.subscriptionType)})`
                  : '-'}
              </div>
            </div>
          </div>
        </div>
      </div>

      <div className="card" style={{ marginBottom: '1rem' }}>
        <div className="card-header">
          <div className="card-title">Debt Query</div>
        </div>
        <div className="card-body">
          <div className="form" style={{ gap: '0.8rem' }}>
            <Field label="Subscription">
              <Select
                value={subscriptionId}
                onChange={event => {
                  const nextValue = event.target.value
                  setSubscriptionId(nextValue)
                  if (nextValue) {
                    setSearchParams({ subscriptionId: nextValue })
                  }
                }}
              >
                <option value="">Select subscription</option>
                {activeSubscriptions.map(item => (
                  <option key={item.id} value={item.id}>
                    {item.providerName} - {item.subscriberNumber}
                  </option>
                ))}
              </Select>
            </Field>

            <div className="filters-bar" style={{ marginBottom: 0 }}>
              <Input
                type="number"
                min="2000"
                max="3000"
                placeholder="Year (optional)"
                value={year}
                onChange={event => setYear(event.target.value === '' ? '' : Number(event.target.value))}
              />
              <Input
                type="number"
                min="1"
                max="12"
                placeholder="Month (optional)"
                value={month}
                onChange={event => setMonth(event.target.value === '' ? '' : Number(event.target.value))}
              />
              <button className="btn btn-primary" onClick={() => queryDebtMutation.mutate()} disabled={queryDebtMutation.isPending}>
                {queryDebtMutation.isPending ? <span className="spin">◌</span> : 'Query debt'}
              </button>
            </div>

            <div className="field-hint">Leave year/month empty to use current UTC month in backend.</div>
            {queryError && <div className="alert alert-error">{queryError}</div>}
            {paymentError && <div className="alert alert-error">{paymentError}</div>}
            {paymentSuccess && <div className="alert alert-success">{paymentSuccess}</div>}
          </div>
        </div>
      </div>

      <div className="card" style={{ marginBottom: '1rem' }}>
        <div className="card-header">
          <div className="card-title">Debt History</div>
        </div>
        <div className="card-body">
          {debtHistoryQuery.isLoading ? (
            <div className="empty-state"><span className="spin">◌</span></div>
          ) : debts.length === 0 ? (
            <div className="empty-state">
              <div className="empty-state-icon">⌁</div>
              <div className="empty-state-text">No debt query history for selected subscription.</div>
            </div>
          ) : (
            <div className="table-wrap">
              <table className="table">
                <thead>
                  <tr>
                    <th>Period</th>
                    <th>Amount</th>
                    <th>Due Date</th>
                    <th>Queried At</th>
                    <th>Provider Ref</th>
                    <th className="right">Action</th>
                  </tr>
                </thead>
                <tbody>
                  {debts.map(debt => (
                    <tr key={debt.id}>
                      <td>{debt.periodYear}/{String(debt.periodMonth).padStart(2, '0')}</td>
                      <td className="amount">{formatCurrency(debt.amount)}</td>
                      <td>{formatDateOnly(debt.dueDate)}</td>
                      <td>{formatDateTime(debt.queriedAtUtc)}</td>
                      <td>{debt.providerRef ?? '-'}</td>
                      <td className="right">
                        <button
                          className="btn btn-accent btn-sm"
                          onClick={() => payMutation.mutate(debt.id)}
                          disabled={payMutation.isPending}
                        >
                          {payMutation.isPending ? <span className="spin">◌</span> : 'Pay'}
                        </button>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </div>
      </div>

      <div className="card">
        <div className="card-header">
          <div className="card-title">Subscription Payment History</div>
        </div>
        <div className="card-body">
          {paymentHistoryQuery.isLoading ? (
            <div className="empty-state"><span className="spin">◌</span></div>
          ) : payments.length === 0 ? (
            <div className="empty-state">
              <div className="empty-state-icon">◌</div>
              <div className="empty-state-text">No payment record for selected subscription.</div>
            </div>
          ) : (
            <div className="table-wrap">
              <table className="table">
                <thead>
                  <tr>
                    <th>Period</th>
                    <th>Amount</th>
                    <th>Payment Date</th>
                    <th>Status</th>
                  </tr>
                </thead>
                <tbody>
                  {payments.map(item => (
                    <tr key={item.id}>
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
        </div>
      </div>
    </>
  )
}
