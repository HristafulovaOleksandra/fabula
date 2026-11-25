import { useCallback, useEffect, useMemo, useState, type ReactNode } from 'react'
import { api, tokenStore } from '../api/client'
import type { AuthResponse, User } from '../api/types'
import { AuthContext, type AuthState } from './useAuth'

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<User | null>(null)
  const [loading, setLoading] = useState(() => tokenStore.get() !== null)

  const refresh = useCallback(async () => {
    if (!tokenStore.get()) return
    try {
      setUser(await api<User>('/auth/me'))
    } catch {
      tokenStore.clear()
      setUser(null)
    }
  }, [])

  useEffect(() => {
    if (!tokenStore.get()) return
    api<User>('/auth/me')
      .then(setUser)
      .catch(() => tokenStore.clear())
      .finally(() => setLoading(false))
  }, [])

  const value = useMemo<AuthState>(() => {
    const accept = (response: AuthResponse) => {
      tokenStore.set(response.token)
      setUser(response.user)
    }
    return {
      user,
      loading,
      login: async (email, password) => accept(await api<AuthResponse>('/auth/login', { method: 'POST', body: { email, password } })),
      register: async (input) => accept(await api<AuthResponse>('/auth/register', { method: 'POST', body: input })),
      logout: () => {
        tokenStore.clear()
        setUser(null)
      },
      refresh,
    }
  }, [user, loading, refresh])

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}
