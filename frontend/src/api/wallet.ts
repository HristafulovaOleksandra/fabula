import { useCallback, useEffect, useState } from 'react'
import { api } from './client'
import type { TransactionReason, Wallet } from './types'

const WALLET_CHANGED = 'fabula:wallet-changed'

export function notifyWalletChanged() {
  window.dispatchEvent(new Event(WALLET_CHANGED))
}

export function useWallet(enabled: boolean) {
  const [wallet, setWallet] = useState<Wallet | null>(null)

  const reload = useCallback(() => {
    if (!enabled) return
    api<Wallet>('/wallet')
      .then(setWallet)
      .catch(() => setWallet(null))
  }, [enabled])

  useEffect(() => {
    reload()
    window.addEventListener(WALLET_CHANGED, reload)
    return () => window.removeEventListener(WALLET_CHANGED, reload)
  }, [reload])

  return enabled ? wallet : null
}

export const REASON_LABELS: Record<TransactionReason, string> = {
  StoryCompleted: 'Пройдено історію',
  DailyGoal: 'Щоденна ціль',
  StreakBonus: 'Бонус за серію',
  PaidChoice: 'Платний вибір',
  PaidStory: 'Купівля історії',
  StreakFreeze: 'Відновлення серії',
  WelcomeBonus: 'Вітальний бонус',
  AdminAdjustment: 'Зміна адміністратором',
}

export function daysLabel(days: number) {
  const lastTwo = days % 100
  const last = days % 10
  if (lastTwo >= 11 && lastTwo <= 14) return `${days} днів`
  if (last === 1) return `${days} день`
  if (last >= 2 && last <= 4) return `${days} дні`
  return `${days} днів`
}
