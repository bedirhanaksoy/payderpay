import { useState } from 'react'
import { useMutation, useQuery } from '@tanstack/react-query'
import { useNavigate } from 'react-router-dom'
import { customersApi } from '../../shared/api/customers'
import { getCurrentUserOrThrow } from '../../shared/auth/use-current-user'
import { session } from '../../shared/auth/session'
import { formatCurrency } from '../../shared/format/amount'
import { formatDateTime } from '../../shared/format/dateTime'
import { ConfirmModal } from '../../components/Modal'
import { parseProblemDetails } from '../../shared/errors/problem-details'

export default function ProfilePage() {
  const navigate = useNavigate()
  const user = getCurrentUserOrThrow()

  const [showDeleteConfirm, setShowDeleteConfirm] = useState(false)

  const customerQuery = useQuery({
    queryKey: ['customer', user.customerId],
    queryFn: () => customersApi.getById(user.customerId),
  })

  const mainAccountQuery = useQuery({
    queryKey: ['main-account', user.customerId],
    queryFn: () => customersApi.getMainAccount(user.customerId),
  })

  const deleteMutation = useMutation({
    mutationFn: () => customersApi.delete(user.customerId),
    onSuccess: () => {
      session.clear()
      navigate('/auth/register', { replace: true })
    },
  })

  if (customerQuery.isLoading || mainAccountQuery.isLoading) {
    return <div className="empty-state"><span className="spin">◌</span></div>
  }

  const customer = customerQuery.data
  const mainAccount = mainAccountQuery.data

  if (!customer || !mainAccount) {
    return (
      <div className="empty-state">
        <div className="empty-state-icon">⚠</div>
        <div className="empty-state-text">Profile information could not be loaded.</div>
      </div>
    )
  }

  const parsedDeleteError = deleteMutation.error ? parseProblemDetails(deleteMutation.error) : null

  return (
    <>
      <div className="page-header">
        <div className="page-header-left">
          <h1 className="page-title">Profile</h1>
          <p className="page-subtitle">Customer profile and main account details</p>
        </div>
      </div>

      <div className="card" style={{ marginBottom: '1rem' }}>
        <div className="card-header">
          <div className="card-title">Customer</div>
        </div>
        <div className="card-body">
          <div className="summary-kv-grid">
            <div>
              <div className="section-title">Full Name</div>
              <div className="summary-kv-value">{customer.fullName}</div>
            </div>
            <div>
              <div className="section-title">Email</div>
              <div className="summary-kv-value">{customer.email}</div>
            </div>
            <div>
              <div className="section-title">Phone</div>
              <div className="summary-kv-value">{customer.phoneNumber}</div>
            </div>
            <div>
              <div className="section-title">Created</div>
              <div className="summary-kv-value">{formatDateTime(customer.createdAtUtc)}</div>
            </div>
          </div>
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
              <div className="summary-kv-value">{mainAccount.iban}</div>
            </div>
            <div>
              <div className="section-title">Balance</div>
              <div className="summary-kv-value amount">{formatCurrency(mainAccount.balance)}</div>
            </div>
            <div>
              <div className="section-title">Updated</div>
              <div className="summary-kv-value">{formatDateTime(mainAccount.updatedAtUtc)}</div>
            </div>
          </div>
        </div>
      </div>

      {parsedDeleteError && <div className="alert alert-error" style={{ marginBottom: '1rem' }}>{parsedDeleteError.detail ?? parsedDeleteError.title}</div>}

      <button className="btn btn-danger" onClick={() => setShowDeleteConfirm(true)}>
        Close Account (Soft Delete Customer)
      </button>

      {showDeleteConfirm && (
        <ConfirmModal
          title="Close account"
          message="Your customer record will be soft-deleted. Continue?"
          confirmLabel="Close account"
          danger
          loading={deleteMutation.isPending}
          onConfirm={() => deleteMutation.mutate()}
          onClose={() => {
            setShowDeleteConfirm(false)
            deleteMutation.reset()
          }}
        />
      )}
    </>
  )
}
