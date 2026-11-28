import { createContext, useContext } from 'react'
import type { User, UserRole } from '../api/types'

export interface RegisterInput {
  email: string
  username: string
  displayName: string
  password: string
  birthDate: string
  role: UserRole
}

export interface AuthState {
  user: User | null
  loading: boolean
  login: (login: string, password: string) => Promise<void>
  register: (input: RegisterInput) => Promise<void>
  logout: () => void
  refresh: () => Promise<void>
}

export const AuthContext = createContext<AuthState | null>(null)

export function useAuth(): AuthState {
  const context = useContext(AuthContext)
  if (!context) throw new Error('useAuth must be used inside AuthProvider')
  return context
}
