import { useMemo, useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { subscriptionsApi } from '../../shared/api/subscriptions'
import { getCurrentUserOrThrow } from '../../shared/auth/use-current-user'
import type { CreateSubscriptionRequest, SubscriptionResponse, SubscriptionStatus, SubscriptionType, UpdateSubscriptionRequest } from '../../shared/types'
import { parseProblemDetails } from '../../shared/errors/problem-details'
import { Field, Input, Select } from '../../components/Field'
import { ConfirmModal, Modal } from '../../components/Modal'
import Pagination from '../../components/Pagination'
import SpinnerLabel from '../../components/SpinnerLabel'
import { subscriptionStatusLabel, subscriptionTypeLabel } from '../../shared/format/enums'
import { formatDateOnly } from '../../shared/format/dateTime'

const subscriptionTypes: SubscriptionType[] = [1, 2, 3, 4, 5, 6]
const subscriptionStatuses: SubscriptionStatus[] = [1, 2]

const schema = z.object({
  subscriptionType: z.number().int().min(1).max(6),
  providerName: z.string().min(1).max(200),
  subscriberNumber: z.string().min(1).max(100),
  status: z.number().int().min(1).max(2).optional(),
})

type FormValues = z.infer<typeof schema>

interface FormProps {
  defaultValues?: Partial<FormValues>
  isEdit?: boolean
  isPending: boolean
  error: unknown
  onSubmit: (values: FormValues) => void
}

function SubscriptionForm({ defaultValues, isEdit, isPending, error, onSubmit }: FormProps) {
  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues,
  })

  const apiError = error ? parseProblemDetails(error, 'subscription') : null

  return (
    <form className="form" onSubmit={handleSubmit(onSubmit)}>
      {apiError && <div className="alert alert-error">{apiError.userMessage}</div>}

      <Field label="Subscription type" error={errors.subscriptionType?.message}>
        <Select error={!!errors.subscriptionType} {...register('subscriptionType', { valueAsNumber: true })}>
          {subscriptionTypes.map(type => (
            <option key={type} value={type}>{subscriptionTypeLabel(type)}</option>
          ))}
        </Select>
      </Field>

      <Field label="Provider name" error={errors.providerName?.message}>
        <Input placeholder="Provider Co." error={!!errors.providerName} {...register('providerName')} />
      </Field>

      <Field label="Subscriber number" error={errors.subscriberNumber?.message}>
        <Input placeholder="SUB-0001" error={!!errors.subscriberNumber} {...register('subscriberNumber')} />
      </Field>

      {isEdit && (
        <Field label="Status" error={errors.status?.message}>
          <Select error={!!errors.status} {...register('status', { valueAsNumber: true })}>
            {subscriptionStatuses.map(status => (
              <option key={status} value={status}>{subscriptionStatusLabel(status)}</option>
            ))}
          </Select>
        </Field>
      )}

      <div className="form-actions">
        <button className="btn btn-primary" type="submit" disabled={isPending}>
          <SpinnerLabel loading={isPending}>{isEdit ? 'Save changes' : 'Create subscription'}</SpinnerLabel>
        </button>
      </div>
    </form>
  )
}

export default function SubscriptionsPage() {
  const queryClient = useQueryClient()
  const user = getCurrentUserOrThrow()

  const [showCreate, setShowCreate] = useState(false)
  const [editing, setEditing] = useState<SubscriptionResponse | null>(null)
  const [deleting, setDeleting] = useState<SubscriptionResponse | null>(null)
  const [page, setPage] = useState(1)
  const pageSize = 20

  const subscriptionsQuery = useQuery({
    queryKey: ['subscriptions', user.customerId, page, pageSize],
    queryFn: () => subscriptionsApi.listByCustomer(user.customerId, page, pageSize),
    placeholderData: previous => previous,
  })

  const createMutation = useMutation({
    mutationFn: (values: CreateSubscriptionRequest) => subscriptionsApi.create(values),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['subscriptions', user.customerId] })
      setShowCreate(false)
    },
  })

  const updateMutation = useMutation({
    mutationFn: ({ id, values }: { id: string; values: UpdateSubscriptionRequest }) => subscriptionsApi.update(id, values),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['subscriptions', user.customerId] })
      setEditing(null)
    },
  })

  const deleteMutation = useMutation({
    mutationFn: (id: string) => subscriptionsApi.delete(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['subscriptions', user.customerId] })
      setDeleting(null)
    },
  })

  const items = subscriptionsQuery.data?.items ?? []

  const sorted = useMemo(
    () => [...items].sort((a, b) => Number(a.status) - Number(b.status)),
    [items],
  )

  if (subscriptionsQuery.isLoading) {
    return <div className="empty-state"><span className="spin">◌</span></div>
  }

  return (
    <>
      <div className="page-header">
        <div className="page-header-left">
          <h1 className="page-title">Subscriptions</h1>
          <p className="page-subtitle">Manage customer subscription records</p>
        </div>
        <button className="btn btn-primary" onClick={() => setShowCreate(true)}>
          <span className="btn-plus" aria-hidden>+</span>
          New subscription
        </button>
      </div>

      {sorted.length === 0 ? (
        <div className="empty-state">
          <div className="empty-state-icon">◈</div>
          <div className="empty-state-text">No subscriptions yet. Create one to continue.</div>
        </div>
      ) : (
        <>
        <div className="table-wrap">
          <table className="table">
            <thead>
              <tr>
                <th>Type</th>
                <th>Provider</th>
                <th>Subscriber No</th>
                <th>Due Date</th>
                <th>Status</th>
                <th className="right">Actions</th>
              </tr>
            </thead>
            <tbody>
              {sorted.map(item => (
                <tr key={item.id}>
                  <td>{subscriptionTypeLabel(item.subscriptionType)}</td>
                  <td>{item.providerName}</td>
                  <td>{item.subscriberNumber}</td>
                  <td>{item.currentDueDate ? formatDateOnly(item.currentDueDate) : '-'}</td>
                  <td>
                    <span className={`badge ${item.status === 1 ? 'badge-success' : 'badge-neutral'}`}>
                      {subscriptionStatusLabel(item.status)}
                    </span>
                  </td>
                  <td className="right">
                    <div style={{ display: 'inline-flex', gap: '0.4rem' }}>
                      <button className="btn btn-secondary btn-sm" onClick={() => setEditing(item)}>Edit</button>
                      <button className="btn btn-danger btn-sm" onClick={() => setDeleting(item)}>Delete</button>
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
        <Pagination
          page={subscriptionsQuery.data?.page ?? page}
          totalPages={subscriptionsQuery.data?.totalPages ?? 0}
          totalCount={subscriptionsQuery.data?.totalCount}
          isFetching={subscriptionsQuery.isFetching}
          onPageChange={setPage}
        />
        </>
      )}

      {showCreate && (
        <Modal title="Create subscription" onClose={() => { setShowCreate(false); createMutation.reset() }}>
          <SubscriptionForm
            defaultValues={{ subscriptionType: 1 }}
            isPending={createMutation.isPending}
            error={createMutation.error}
            onSubmit={values => {
              createMutation.mutate({
                customerId: user.customerId,
                subscriptionType: values.subscriptionType as SubscriptionType,
                providerName: values.providerName,
                subscriberNumber: values.subscriberNumber,
              })
            }}
          />
        </Modal>
      )}

      {editing && (
        <Modal title="Update subscription" onClose={() => { setEditing(null); updateMutation.reset() }}>
          <SubscriptionForm
            isEdit
            defaultValues={{
              subscriptionType: editing.subscriptionType,
              providerName: editing.providerName,
              subscriberNumber: editing.subscriberNumber,
              status: editing.status,
            }}
            isPending={updateMutation.isPending}
            error={updateMutation.error}
            onSubmit={values => {
              updateMutation.mutate({
                id: editing.id,
                values: {
                  subscriptionType: values.subscriptionType as SubscriptionType,
                  providerName: values.providerName,
                  subscriberNumber: values.subscriberNumber,
                  status: (values.status ?? 1) as SubscriptionStatus,
                },
              })
            }}
          />
        </Modal>
      )}

      {deleting && (
        <ConfirmModal
          title="Delete subscription"
          message={`Delete ${deleting.providerName} / ${deleting.subscriberNumber}?`}
          confirmLabel="Delete"
          danger
          loading={deleteMutation.isPending}
          onConfirm={() => deleteMutation.mutate(deleting.id)}
          onClose={() => { setDeleting(null); deleteMutation.reset() }}
        />
      )}
    </>
  )
}
