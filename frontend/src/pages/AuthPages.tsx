import { useState, type FormEvent, type ReactNode } from 'react'
import { Link, useLocation, useNavigate } from 'react-router-dom'
import { api, ApiError } from '../api/client'
import type { UserRole } from '../api/types'
import { useAuth } from '../auth/useAuth'
import { Mascot, type MascotMood } from '../components/Brand'
import styles from './AuthPages.module.css'

function AuthCard({ title, mood, children }: { title: string; mood: MascotMood; children: ReactNode }) {
  return (
    <div className={`page ${styles.wrap}`}>
      <section className={`sticker ${styles.card}`}>
        <Mascot mood={mood} size={110} className={styles.cat} />
        <h1>{title}</h1>
        {children}
      </section>
    </div>
  )
}

function errorText(error: unknown) {
  return error instanceof ApiError ? error.message : 'Щось пішло не так. Спробуй ще раз.'
}

function useRedirectTarget() {
  const location = useLocation()
  return (location.state as { from?: string } | null)?.from ?? '/library'
}

export function LoginPage() {
  const { login } = useAuth()
  const navigate = useNavigate()
  const target = useRedirectTarget()
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)

  const submit = async (event: FormEvent) => {
    event.preventDefault()
    setBusy(true)
    setError(null)
    try {
      await login(email, password)
      navigate(target, { replace: true })
    } catch (caught) {
      setError(errorText(caught))
    } finally {
      setBusy(false)
    }
  }

  return (
    <AuthCard title="Вхід" mood="happy">
      <form className={styles.form} onSubmit={submit}>
        <label className="field">
          Пошта або нік
          <input autoComplete="username" autoCapitalize="none" spellCheck={false} required value={email} onChange={(e) => setEmail(e.target.value)} />
        </label>
        <label className="field">
          Пароль
          <input type="password" autoComplete="current-password" required value={password} onChange={(e) => setPassword(e.target.value)} />
        </label>
        {error && <p className="error-text" role="alert">{error}</p>}
        <button className="btn" disabled={busy}>
          {busy ? 'Входимо…' : 'Увійти'}
        </button>
      </form>
      <p className={styles.links}>
        <Link to="/forgot-password">Забули пароль?</Link>
        <Link to="/register">Створити акаунт</Link>
      </p>
    </AuthCard>
  )
}

export function RegisterPage() {
  const { register } = useAuth()
  const navigate = useNavigate()
  const [form, setForm] = useState({ email: '', username: '', displayName: '', password: '', role: 'Student' as UserRole })
  const [error, setError] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)
  const update = (key: keyof typeof form) => (event: { target: { value: string } }) => setForm({ ...form, [key]: event.target.value })

  const submit = async (event: FormEvent) => {
    event.preventDefault()
    setBusy(true)
    setError(null)
    try {
      await register(form)
      navigate(form.role === 'Student' ? '/welcome' : '/', { replace: true })
    } catch (caught) {
      setError(errorText(caught))
    } finally {
      setBusy(false)
    }
  }

  return (
    <AuthCard title="Реєстрація" mood="happy">
      <form className={styles.form} onSubmit={submit}>
        <fieldset className={styles.roles}>
          <legend>Я хочу</legend>
          <label className={form.role === 'Student' ? styles.roleActive : ''}>
            <input type="radio" name="role" value="Student" checked={form.role === 'Student'} onChange={update('role')} />
            Читати історії
          </label>
          <label className={form.role === 'Author' ? styles.roleActive : ''}>
            <input type="radio" name="role" value="Author" checked={form.role === 'Author'} onChange={update('role')} />
            Створювати історії
          </label>
        </fieldset>
        <label className="field">
          Пошта
          <input type="email" autoComplete="email" required value={form.email} onChange={update('email')} />
        </label>
        <label className="field">
          Нік
          <input required pattern="[a-zA-Z0-9_.]{3,30}" autoComplete="username" value={form.username} onChange={update('username')} />
          <small>3–30 латинських літер, цифр, «_» або «.». Його бачать інші.</small>
        </label>
        <label className="field">
          Ім'я
          <input required minLength={2} maxLength={100} autoComplete="name" value={form.displayName} onChange={update('displayName')} />
          <small>За замовчуванням приховане. Відкрити його можна в профілі.</small>
        </label>
        <label className="field">
          Пароль
          <input type="password" required minLength={8} autoComplete="new-password" value={form.password} onChange={update('password')} />
          <small>Щонайменше 8 символів.</small>
        </label>
        {error && <p className="error-text" role="alert">{error}</p>}
        <button className="btn" disabled={busy}>
          {busy ? 'Створюємо…' : 'Створити акаунт'}
        </button>
      </form>
      <p className={styles.links}>
        <Link to="/login">Уже є акаунт? Увійти</Link>
      </p>
    </AuthCard>
  )
}

export function ForgotPasswordPage() {
  const navigate = useNavigate()
  const [step, setStep] = useState<'email' | 'code' | 'done'>('email')
  const [email, setEmail] = useState('')
  const [code, setCode] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)

  const run = async (action: () => Promise<void>) => {
    setBusy(true)
    setError(null)
    try {
      await action()
    } catch (caught) {
      setError(errorText(caught))
    } finally {
      setBusy(false)
    }
  }

  if (step === 'done') {
    return (
      <AuthCard title="Пароль змінено" mood="celebrate">
        <p className="success-text">Тепер можна увійти з новим паролем.</p>
        <button className="btn" onClick={() => navigate('/login')}>
          До входу
        </button>
      </AuthCard>
    )
  }

  return (
    <AuthCard title="Відновлення пароля" mood="thinking">
      {step === 'email' ? (
        <form
          className={styles.form}
          onSubmit={(event) => {
            event.preventDefault()
            run(async () => {
              await api('/auth/password-reset/request', { method: 'POST', body: { email } })
              setStep('code')
            })
          }}
        >
          <p>Введи пошту акаунта — ми надішлемо на неї код з 6 цифр.</p>
          <label className="field">
            Пошта
            <input type="email" autoComplete="email" required value={email} onChange={(e) => setEmail(e.target.value)} />
          </label>
          {error && <p className="error-text" role="alert">{error}</p>}
          <button className="btn" disabled={busy}>
            Надіслати код
          </button>
        </form>
      ) : (
        <form
          className={styles.form}
          onSubmit={(event) => {
            event.preventDefault()
            run(async () => {
              await api('/auth/password-reset/confirm', { method: 'POST', body: { email, code, newPassword: password } })
              setStep('done')
            })
          }}
        >
          <p>Якщо акаунт з поштою {email} існує, код уже в скриньці. Він діє 10 хвилин.</p>
          <label className="field">
            Код з листа
            <input
              inputMode="numeric"
              autoComplete="one-time-code"
              pattern="[0-9]{6}"
              maxLength={6}
              required
              value={code}
              onChange={(e) => setCode(e.target.value.replace(/\D/g, ''))}
              className={styles.code}
            />
          </label>
          <label className="field">
            Новий пароль
            <input type="password" autoComplete="new-password" minLength={8} required value={password} onChange={(e) => setPassword(e.target.value)} />
          </label>
          {error && <p className="error-text" role="alert">{error}</p>}
          <button className="btn" disabled={busy}>
            Змінити пароль
          </button>
          <button type="button" className={styles.textButton} onClick={() => setStep('email')}>
            Надіслати код ще раз
          </button>
        </form>
      )}
    </AuthCard>
  )
}
