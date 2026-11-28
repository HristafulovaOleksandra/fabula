import { useState } from 'react'
import { Link, NavLink, Outlet, useNavigate } from 'react-router-dom'
import { api, ApiError } from '../api/client'
import { daysLabel, notifyWalletChanged, useWallet } from '../api/wallet'
import { useAuth } from '../auth/useAuth'
import { Avatar } from './Avatar'
import { Crystals, Logo, StreakFlame } from './Brand'
import { ScrollTopButton } from './ScrollTopButton'
import styles from './Layout.module.css'

export function Layout() {
  const { user, logout } = useAuth()
  const navigate = useNavigate()
  const wallet = useWallet(user !== null)
  const [rescueError, setRescueError] = useState<string | null>(null)
  const [rescueHidden, setRescueHidden] = useState(false)

  const rescue = async () => {
    setRescueError(null)
    try {
      await api('/wallet/streak/rescue', { method: 'POST' })
      notifyWalletChanged()
    } catch (caught) {
      setRescueError(caught instanceof ApiError ? caught.message : 'Не вдалося відновити серію.')
    }
  }

  const streak = wallet?.streak

  return (
    <>
      <header className={styles.header}>
        <Link to="/" className={styles.brand} aria-label="Fabula, на головну">
          <Logo />
        </Link>
        <nav className={styles.nav}>
          <NavLink to="/library">Бібліотека</NavLink>
          {user ? (
            <>
              <NavLink to="/words">Словник</NavLink>
              {user.role !== 'Student' && <NavLink to="/studio">Мої історії</NavLink>}
              {user.role === 'Admin' && <NavLink to="/admin">Адмінка</NavLink>}
              {wallet && streak && (
                <Link to="/profile#wallet" className={styles.wallet} title="Кристали і щоденна серія">
                  <Crystals amount={wallet.balance} size={22} />
                  <span className={styles.walletItem} title={streak.playedToday ? 'Сьогоднішню ціль виконано — вогник горить' : 'Пройди історію сьогодні, щоб вогник загорівся'}>
                    <StreakFlame lit={streak.playedToday} days={streak.current} />
                  </span>
                </Link>
              )}
              <NavLink to="/profile" className={styles.user} title="Мій профіль">
                <Avatar avatar={user.avatar} username={user.username} size={32} />
                {user.username}
              </NavLink>
              <button
                type="button"
                className={styles.linkButton}
                onClick={() => {
                  logout()
                  navigate('/')
                }}
              >
                Вийти
              </button>
            </>
          ) : (
            <>
              <NavLink to="/login">Увійти</NavLink>
              <Link to="/register" className={`btn ${styles.cta}`}>
                Реєстрація
              </Link>
            </>
          )}
        </nav>
      </header>
      {streak && streak.rescueDays > 0 && !rescueHidden && (
        <div className={styles.rescue} role="status">
          <span>
            Учора ти пропустив(-ла) день, і серія з {daysLabel(streak.rescueDays)} обнулилася. Її можна відновити лише сьогодні.
          </span>
          <button type="button" className="btn" onClick={rescue}>
            Відновити за <Crystals amount={streak.rescuePrice} />
          </button>
          <button type="button" className={styles.linkButton} onClick={() => setRescueHidden(true)}>
            Не треба
          </button>
          {rescueError && <span className="error-text">{rescueError}</span>}
        </div>
      )}
      <main>
        <Outlet />
        <ScrollTopButton />
      </main>
    </>
  )
}
