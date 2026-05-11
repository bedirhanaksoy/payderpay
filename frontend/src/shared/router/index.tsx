import { Navigate, useLocation } from 'react-router-dom'
import { session } from '../auth/session'

interface Props {
  children: React.ReactNode
}

export function ProtectedRoute({ children }: Props) {
  const location = useLocation()

  if (!session.isAuthenticated()) {
    return <Navigate to="/auth/login" state={{ from: location }} replace />
  }

  return <>{children}</>
}

export function PublicOnlyRoute({ children }: Props) {
  if (session.isAuthenticated()) {
    return <Navigate to="/dashboard" replace />
  }

  return <>{children}</>
}
