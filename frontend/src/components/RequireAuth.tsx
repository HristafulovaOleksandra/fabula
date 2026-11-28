import type { ReactNode } from 'react'
import { Navigate, useLocation } from 'react-router-dom'
import { useAuth } from '../auth/useAuth'

export function RequireAuth({ children, authorOnly = false, adminOnly = false }: { children: ReactNode; authorOnly?: boolean; adminOnly?: boolean }) {
  const { user, loading } = useAuth()
  const location = useLocation()
  if (loading) return null
  if (!user) return <Navigate to="/login" replace state={{ from: location.pathname }} />
  if (authorOnly && user.role === 'Student') return <Navigate to="/" replace />
  if (adminOnly && user.role !== 'Admin') return <Navigate to="/" replace />
  return children
}
