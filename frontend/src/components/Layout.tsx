import { Link, NavLink, Outlet, useNavigate } from 'react-router-dom'
import { useAuth } from '../auth/useAuth'
import { Logo } from './Brand'
import { ScrollTopButton } from './ScrollTopButton'
import styles from './Layout.module.css'

export function Layout() {
  const { user, logout } = useAuth()
  const navigate = useNavigate()

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
              {user.role !== 'Student' && <NavLink to="/studio">Мої історії</NavLink>}
              <span className={styles.user}>
                @{user.username}
                {user.isTeacher && <span className={styles.teacher}>Teacher</span>}
              </span>
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
      <main>
        <Outlet />
        <ScrollTopButton />
      </main>
    </>
  )
}
