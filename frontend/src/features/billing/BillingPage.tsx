import { useEffect, useMemo, useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useSearchParams } from 'react-router-dom'
import { subscriptionsApi } from '../../shared/api/subscriptions'
import { customersApi } from '../../shared/api/customers'
import { getCurrentUserOrThrow } from '../../shared/auth/use-current-user'
import { Field, Select } from '../../components/Field'
import { Modal } from '../../components/Modal'
import Pagination from '../../components/Pagination'
import SpinnerLabel from '../../components/SpinnerLabel'
import { formatCurrency } from '../../shared/format/amount'
import { formatDateOnly, formatDateTime } from '../../shared/format/dateTime'
import { paymentStatusLabel, subscriptionTypeLabel } from '../../shared/format/enums'
import { parseProblemDetails } from '../../shared/errors/problem-details'
import type { DebtItemResponse, DebtQueryResponse } from '../../shared/types'

export default function BillingPage() {
  const queryClient = useQueryClient()
  const user = getCurrentUserOrThrow()
  const [searchParams, setSearchParams] = useSearchParams()

  const subscriptionsQuery = useQuery({
    queryKey: ['subscriptions', user.customerId, 'all'],
    queryFn: () => subscriptionsApi.listAllByCustomer(user.customerId),
  })

  const activeSubscriptions = useMemo(
    () => (subscriptionsQuery.data ?? []).filter(x => x.status === 1),
    [subscriptionsQuery.data],
  )

  const [subscriptionId, setSubscriptionId] = useState(searchParams.get('subscriptionId') ?? '')
  const [historySubscriptionId, setHistorySubscriptionId] = useState(searchParams.get('subscriptionId') ?? '')

  useEffect(() => {
    if (!subscriptionId && activeSubscriptions.length > 0) {
      const initialSubscriptionId = activeSubscriptions[0].id
      // Initializing default selection once subscriptions data arrives from the server —
      // legitimate "sync state with async data" effect.
      // eslint-disable-next-line react-hooks/set-state-in-effect
      setSubscriptionId(initialSubscriptionId)
      setHistorySubscriptionId(initialSubscriptionId)
      setSearchParams({ subscriptionId: initialSubscriptionId })
    }
  }, [activeSubscriptions, searchParams, setSearchParams, subscriptionId])

  const debtHistoryQuery = useQuery({
    queryKey: ['debt-history', historySubscriptionId],
    queryFn: () => subscriptionsApi.getDebtHistory(historySubscriptionId),
    enabled: false,
  })

  const [paymentHistoryPage, setPaymentHistoryPage] = useState(1)
  const paymentHistoryPageSize = 20

  useEffect(() => {
    // Reset pagination to first page when the user switches the active subscription —
    // legitimate "reset local state on dependency change" effect.
    // eslint-disable-next-line react-hooks/set-state-in-effect
    setPaymentHistoryPage(1)
  }, [historySubscriptionId])

  const paymentHistoryQuery = useQuery({
    queryKey: ['subscription-payments', historySubscriptionId, paymentHistoryPage, paymentHistoryPageSize],
    queryFn: () => subscriptionsApi.getPaymentHistory(historySubscriptionId, paymentHistoryPage, paymentHistoryPageSize),
    enabled: !!historySubscriptionId,
    placeholderData: previous => previous,
  })

  const accountQuery = useQuery({
    queryKey: ['main-account', user.customerId],
    queryFn: () => customersApi.getMainAccount(user.customerId),
  })

  const [queryError, setQueryError] = useState<string | null>(null)
  const [querySuccess, setQuerySuccess] = useState<string | null>(null)
  const [paymentError, setPaymentError] = useState<string | null>(null)
  const [paymentSuccess, setPaymentSuccess] = useState<string | null>(null)
  const [confirmDebt, setConfirmDebt] = useState<DebtItemResponse | null>(null)

  const queryDebtMutation = useMutation({
    mutationFn: () => {
      if (!subscriptionId) {
        throw new Error('Please select an active subscription.')
      }

      return subscriptionsApi.queryDebt(subscriptionId)
    },
    onSuccess: response => {
      setQueryError(null)
      setQuerySuccess(response.debts.length === 0 ? 'No unpaid debt found for selected subscription.' : 'Debt list updated.')
      queryClient.setQueryData(['debt-history', subscriptionId], response)
      setHistorySubscriptionId(subscriptionId)
    },
    onError: error => {
      setQuerySuccess(null)
      const parsed = parseProblemDetails(error, 'debt_query')
      setQueryError(parsed.userMessage)
    },
  })

  const payMutation = useMutation({
    retry: 0,
    mutationFn: (debtId: string) => {
      if (!subscriptionId) {
        throw new Error('Please select a subscription.')
      }
      return subscriptionsApi.createPayment(subscriptionId, debtId)
    },
    onSuccess: response => {
      const paidDebtId = confirmDebt?.debtId
      setConfirmDebt(null)
      setPaymentError(null)
      setPaymentSuccess(
        response.status === 1
          ? 'Payment completed successfully.'
          : 'Payment failed. Please try again.',
      )
      if (subscriptionId && paidDebtId) {
        queryClient.setQueryData(['debt-history', subscriptionId], (previous: DebtQueryResponse | undefined) => {
          if (!previous) {
            return previous
          }

          return {
            ...previous,
            debts: previous.debts.filter(x => x.debtId !== paidDebtId),
          }
        })
      }
      queryClient.invalidateQueries({ queryKey: ['subscription-payments', subscriptionId] })
      queryClient.invalidateQueries({ queryKey: ['main-account', user.customerId] })
      queryClient.invalidateQueries({ queryKey: ['dashboard'] })
      queryClient.invalidateQueries({ queryKey: ['dashboard-unpaid-debts', user.customerId] })
    },
    onError: error => {
      setConfirmDebt(null)
      const parsed = parseProblemDetails(error, 'payment')
      setPaymentSuccess(null)
      setPaymentError(parsed.userMessage)

      if (parsed.status === 409 && subscriptionId) {
        queryClient.invalidateQueries({ queryKey: ['debt-history', subscriptionId] })
      }
    },
  })

  if (subscriptionsQuery.isLoading) {
    return <div className="empty-state"><span className="spin">◌</span></div>
  }

  const selectedSubscription = activeSubscriptions.find(x => x.id === subscriptionId)
  const debts = debtHistoryQuery.data?.debts ?? []
  const payments = paymentHistoryQuery.data?.items ?? []

  function handleConfirmPayment() {
    if (!confirmDebt || payMutation.isPending) {
      return
    }

    payMutation.mutate(confirmDebt.debtId)
  }

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
                  setQuerySuccess(null)
                  setQueryError(null)
                  setPaymentError(null)
                  setPaymentSuccess(null)
                  setConfirmDebt(null)
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
              <button
                className="btn btn-primary"
                onClick={() => queryDebtMutation.mutate()}
                disabled={queryDebtMutation.isPending}
              >
                <SpinnerLabel loading={queryDebtMutation.isPending}>Query debt</SpinnerLabel>
              </button>
            </div>

            {queryError && <div className="alert alert-error">{queryError}</div>}
            {querySuccess && <div className="alert alert-success">{querySuccess}</div>}
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
              <div className="empty-state-text">No unpaid debt for selected subscription.</div>
            </div>
          ) : (
            <div className="table-wrap">
              <table className="table">
                <thead>
                  <tr>
                    <th>Debt Id</th>
                    <th>Period</th>
                    <th>Amount</th>
                    <th>Due Date</th>
                    <th>Queried At</th>
                    <th>Provider</th>
                    <th>Provider Ref</th>
                    <th className="right">Action</th>
                  </tr>
                </thead>
                <tbody>
                  {debts.map(debt => (
                    <tr key={debt.debtId}>
                      <td>{debt.debtId.slice(0, 8)}...</td>
                      <td>{debt.periodYear}/{String(debt.periodMonth).padStart(2, '0')}</td>
                      <td className="amount">{formatCurrency(debt.amount)}</td>
                      <td>{formatDateOnly(debt.dueDate)}</td>
                      <td>{formatDateTime(debt.queriedAtUtc)}</td>
                      <td>{debt.providerName}</td>
                      <td>{debt.providerRef ?? '-'}</td>
                      <td className="right">
                        <button
                          className="btn btn-accent btn-sm"
                          onClick={() => setConfirmDebt(debt)}
                          disabled={payMutation.isPending}
                        >
                          Pay
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
          {paymentHistoryQuery.data && (
            <Pagination
              page={paymentHistoryQuery.data.page}
              totalPages={paymentHistoryQuery.data.totalPages}
              totalCount={paymentHistoryQuery.data.totalCount}
              isFetching={paymentHistoryQuery.isFetching}
              onPageChange={setPaymentHistoryPage}
            />
          )}
        </div>
      </div>

      {confirmDebt && (
        <Modal
          title="Confirm Payment"
          onClose={() => {
            if (!payMutation.isPending) {
              setConfirmDebt(null)
            }
          }}
          footer={(
            <>
              <button
                type="button"
                className="btn btn-secondary"
                onClick={() => setConfirmDebt(null)}
                disabled={payMutation.isPending}
              >
                Cancel
              </button>
              <button
                type="button"
                className="btn btn-primary"
                onClick={handleConfirmPayment}
                disabled={payMutation.isPending}
                style={{ minWidth: '160px' }}
              >
                {payMutation.isPending ? 'Processing...' : 'Confirm payment'}
              </button>
            </>
          )}
        >
          <div className="summary-kv-grid">
            <div>
              <div className="section-title">Amount</div>
              <div className="summary-kv-value amount">{formatCurrency(confirmDebt.amount)}</div>
            </div>
            <div>
              <div className="section-title">Due Date</div>
              <div className="summary-kv-value">{formatDateOnly(confirmDebt.dueDate)}</div>
            </div>
            <div>
              <div className="section-title">Provider</div>
              <div className="summary-kv-value">{confirmDebt.providerName}</div>
            </div>
            <div>
              <div className="section-title">Subscriber No</div>
              <div className="summary-kv-value">{confirmDebt.subscriberNumber}</div>
            </div>
          </div>
        </Modal>
      )}
    </>
  )
}
