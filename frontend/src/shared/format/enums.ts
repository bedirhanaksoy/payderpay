import type { PaymentStatus, SubscriptionStatus, SubscriptionType } from '../types'

export function subscriptionTypeLabel(value: SubscriptionType) {
  switch (value) {
    case 1:
      return 'Electricity'
    case 2:
      return 'Water'
    case 3:
      return 'Internet'
    case 4:
      return 'GSM'
    case 5:
      return 'Natural Gas'
    default:
      return 'Other'
  }
}

export function subscriptionStatusLabel(value: SubscriptionStatus) {
  return value === 1 ? 'Active' : 'Passive'
}

export function paymentStatusLabel(value: PaymentStatus) {
  return value === 1 ? 'Successful' : 'Failed'
}
