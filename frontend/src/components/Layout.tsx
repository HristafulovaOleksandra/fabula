import { Link, NavLink, Outlet, useNavigate } from 'react-router-dom'
import { useAuth } from '../auth/useAuth'
import { Avatar } from './Avatar'
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
      <main>
        <Outlet />
        <ScrollTopButton />
      </main>
    </>
  )
}
