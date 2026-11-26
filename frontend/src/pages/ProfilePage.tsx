import { useCallback, useEffect, useRef, useState, type FormEvent } from 'react'
import { useNavigate } from 'react-router-dom'
import { api, ApiError } from '../api/client'
import { profileApi } from '../api/profile'
import type { NameVisibility, PublicUser, TeachingLink, User, WalletTransaction } from '../api/types'
import { REASON_LABELS, useWallet } from '../api/wallet'
import { Crystals } from '../components/Brand'
import { StreakWeek } from '../components/StreakWeek'
import { useAuth } from '../auth/useAuth'
import { Avatar } from '../components/Avatar'
import type { Framing } from '../components/coverFraming'
import { FrameDialog } from '../components/FrameDialog'
import styles from './ProfilePage.module.css'

const ROLE_LABELS: Record<User['role'], string> = { Student: 'Учень', Author: 'Автор', Admin: 'Адміністратор' }

const VISIBILITY: { value: NameVisibility; label: string }[] = [
  { value: 'Private', label: 'Лише я' },
  { value: 'Teachers', label: 'Мої викладачі' },
  { value: 'Public', label: 'Усі' },
]

type Message = { ok: boolean; text: string } | null

const errorText = (error: unknown) => (error instanceof ApiError ? error.message : 'Щось пішло не так. Спробуй ще раз.')

function Status({ message }: { message: Message }) {
  if (!message) return null
  return <p className={message.ok ? 'success-text' : 'error-text'}>{message.text}</p>
}

function personName(person: PublicUser) {
  return person.displayName ? `${person.displayName} (${person.username})` : person.username
}

function AvatarSection({ user, onChanged }: { user: User; onChanged: () => Promise<void> }) {
  const input = useRef<HTMLInputElement>(null)
  const [busy, setBusy] = useState(false)
  const [editing, setEditing] = useState(false)
  const [message, setMessage] = useState<Message>(null)
  const hasPhoto = !!user.avatar.path

  const pick = async (file: File | undefined) => {
    if (!file) return
    setBusy(true)
    setMessage(null)
    try {
      await profileApi.uploadAvatar(file)
      await onChanged()
      setEditing(true)
    } catch (caught) {
      setMessage({ ok: false, text: errorText(caught) })
    } finally {
      setBusy(false)
      if (input.current) input.current.value = ''
    }
  }

  const applyFraming = async (framing: Framing) => {
    try {
      await profileApi.frameAvatar(framing)
      await onChanged()
      setEditing(false)
    } catch (caught) {
      setMessage({ ok: false, text: errorText(caught) })
    }
  }

  return (
    <div className={styles.identity}>
      <button
        type="button"
        className={styles.avatarButton}
        onClick={() => (hasPhoto ? setEditing(true) : input.current?.click())}
        title={hasPhoto ? 'Редагувати фото' : 'Додати фото'}
      >
        <Avatar avatar={user.avatar} username={user.username} size={170} />
      </button>
      <div className={styles.identityText}>
        <h1>{user.username}</h1>
        <div className={styles.badges}>
          <span className={`${styles.badge} ${styles.roleBadge}`}>{ROLE_LABELS[user.role]}</span>
          {user.isTeacher && <span className={`${styles.badge} ${styles.teacherBadge}`}>Викладач</span>}
        </div>
        <div className={styles.inlineActions}>
          {hasPhoto && (
            <button type="button" className="btn btn-ghost" onClick={() => setEditing(true)}>
              Редагувати фото
            </button>
          )}
          <button type="button" className={hasPhoto ? styles.linkButton : 'btn btn-ghost'} disabled={busy} onClick={() => input.current?.click()}>
            {busy ? 'Завантаження…' : hasPhoto ? 'Замінити фото' : 'Додати фото'}
          </button>
          {hasPhoto && (
            <button
              type="button"
              className={styles.linkButton}
              onClick={async () => {
                await profileApi.removeAvatar()
                await onChanged()
              }}
            >
              Прибрати фото
            </button>
          )}
        </div>
        <Status message={message} />
      </div>
      <input ref={input} type="file" accept="image/png,image/jpeg,image/webp" hidden onChange={(e) => pick(e.target.files?.[0])} />
      {editing && user.avatar.path && (
        <FrameDialog
          round
          title="Редагувати фото профілю"
          imagePath={user.avatar.path}
          initial={{ focusX: user.avatar.focusX, focusY: user.avatar.focusY, zoom: user.avatar.zoom }}
          onApply={applyFraming}
          onClose={() => setEditing(false)}
        />
      )}
    </div>
  )
}

function NameSection({ user, onChanged }: { user: User; onChanged: () => Promise<void> }) {
  const [name, setName] = useState(user.displayName)
  const [visibility, setVisibility] = useState(user.nameVisibility)
  const [message, setMessage] = useState<Message>(null)
  const dirty = name.trim() !== user.displayName || visibility !== user.nameVisibility

  const save = async (event: FormEvent) => {
    event.preventDefault()
    try {
      await profileApi.update(name.trim(), visibility)
      await onChanged()
      setMessage({ ok: true, text: 'Збережено' })
    } catch (caught) {
      setMessage({ ok: false, text: errorText(caught) })
    }
  }

  return (
    <form className={`sticker ${styles.card}`} onSubmit={save}>
      <h2>Ім'я</h2>
      <div className={styles.row}>
        <label className="field">
          Ім'я
          <input value={name} minLength={2} maxLength={100} required onChange={(e) => setName(e.target.value)} />
        </label>
        <label className="field">
          Хто бачить ім'я
          <select value={visibility} onChange={(e) => setVisibility(e.target.value as NameVisibility)}>
            {VISIBILITY.map((option) => (
              <option key={option.value} value={option.value}>
                {option.label}
              </option>
            ))}
          </select>
        </label>
      </div>
      <div className={styles.inlineActions}>
        <button className="btn" disabled={!dirty || name.trim().length < 2}>
          Зберегти
        </button>
      </div>
      <Status message={message} />
    </form>
  )
}

function EmailSection({ user, onChanged }: { user: User; onChanged: () => Promise<void> }) {
  const [open, setOpen] = useState(false)
  const [newEmail, setNewEmail] = useState('')
  const [password, setPassword] = useState('')
  const [code, setCode] = useState('')
  const [sent, setSent] = useState(false)
  const [message, setMessage] = useState<Message>(null)

  const request = async (event: FormEvent) => {
    event.preventDefault()
    try {
      await profileApi.requestEmailChange(newEmail, password)
      setSent(true)
      setPassword('')
      setMessage({ ok: true, text: `Ми надіслали код на ${newEmail}. Він діє 10 хвилин.` })
    } catch (caught) {
      setMessage({ ok: false, text: errorText(caught) })
    }
  }

  const confirm = async (event: FormEvent) => {
    event.preventDefault()
    try {
      await profileApi.confirmEmailChange(code)
      await onChanged()
      setOpen(false)
      setSent(false)
      setCode('')
      setNewEmail('')
      setMessage({ ok: true, text: 'Пошту змінено.' })
    } catch (caught) {
      setMessage({ ok: false, text: errorText(caught) })
    }
  }

  return (
    <section className={`sticker ${styles.card}`}>
      <h2>Пошта</h2>
      <p>
        <b>{user.email}</b>
      </p>
      {!open && (
        <div className={styles.inlineActions}>
          <button type="button" className="btn btn-ghost" onClick={() => setOpen(true)}>
            Змінити пошту
          </button>
        </div>
      )}
      {open && !sent && (
        <form className={styles.stack} onSubmit={request}>
          <div className={styles.row}>
            <label className="field">
              Нова пошта
              <input type="email" required value={newEmail} onChange={(e) => setNewEmail(e.target.value)} />
            </label>
            <label className="field">
              Поточний пароль
              <input type="password" required autoComplete="current-password" value={password} onChange={(e) => setPassword(e.target.value)} />
            </label>
          </div>
          <div className={styles.inlineActions}>
            <button className="btn">Надіслати код</button>
            <button type="button" className={styles.linkButton} onClick={() => setOpen(false)}>
              Скасувати
            </button>
          </div>
        </form>
      )}
      {open && sent && (
        <form className={styles.stack} onSubmit={confirm}>
          <label className="field">
            Код з листа
            <input inputMode="numeric" pattern="[0-9]{6}" maxLength={6} required value={code} onChange={(e) => setCode(e.target.value.replace(/\D/g, ''))} />
          </label>
          <div className={styles.inlineActions}>
            <button className="btn">Підтвердити</button>
            <button type="button" className={styles.linkButton} onClick={() => setSent(false)}>
              Надіслати новий код
            </button>
          </div>
        </form>
      )}
      <Status message={message} />
    </section>
  )
}

function PasswordSection() {
  const [current, setCurrent] = useState('')
  const [next, setNext] = useState('')
  const [repeat, setRepeat] = useState('')
  const [message, setMessage] = useState<Message>(null)

  const save = async (event: FormEvent) => {
    event.preventDefault()
    if (next !== repeat) {
      setMessage({ ok: false, text: 'Новий пароль і повтор не збігаються.' })
      return
    }
    try {
      await profileApi.changePassword(current, next)
      setCurrent('')
      setNext('')
      setRepeat('')
      setMessage({ ok: true, text: 'Пароль змінено.' })
    } catch (caught) {
      setMessage({ ok: false, text: errorText(caught) })
    }
  }

  return (
    <form className={`sticker ${styles.card}`} onSubmit={save}>
      <h2>Пароль</h2>
      <div className={styles.row}>
        <label className="field">
          Поточний пароль
          <input type="password" required autoComplete="current-password" value={current} onChange={(e) => setCurrent(e.target.value)} />
        </label>
        <label className="field">
          Новий пароль
          <input type="password" required minLength={8} autoComplete="new-password" value={next} onChange={(e) => setNext(e.target.value)} />
          <small>Щонайменше 8 символів</small>
        </label>
        <label className="field">
          Повтори новий
          <input type="password" required minLength={8} autoComplete="new-password" value={repeat} onChange={(e) => setRepeat(e.target.value)} />
        </label>
      </div>
      <div className={styles.inlineActions}>
        <button className="btn">Змінити пароль</button>
      </div>
      <Status message={message} />
    </form>
  )
}

function PeopleList({ people, empty, action }: { people: { key: string; person: PublicUser; note?: string; actions: React.ReactNode }[]; empty: string; action?: React.ReactNode }) {
  return (
    <>
      {people.length === 0 ? (
        empty && <p className={styles.hint}>{empty}</p>
      ) : (
        <ul className={styles.people}>
          {people.map(({ key, person, note, actions }) => (
            <li key={key}>
              <Avatar avatar={person.avatar} username={person.username} size={40} />
              <span className={styles.personName}>
                <span className={styles.nameLine}>
                  {personName(person)}
                  {person.isTeacher && <span className={`${styles.badge} ${styles.teacherBadge}`}>Викладач</span>}
                </span>
                {note && <small>{note}</small>}
              </span>
              <span className={styles.personActions}>{actions}</span>
            </li>
          ))}
        </ul>
      )}
      {action}
    </>
  )
}

function TeachersSection({ onChanged }: { onChanged: () => Promise<void> }) {
  const [links, setLinks] = useState<TeachingLink[] | null>(null)
  const [message, setMessage] = useState<Message>(null)
  const load = useCallback(() => profileApi.teachers().then(setLinks), [])

  useEffect(() => {
    profileApi.teachers().then(setLinks).catch(() => setLinks([]))
  }, [])

  const act = async (work: Promise<unknown>, text: string) => {
    try {
      await work
      await load()
      await onChanged()
      setMessage({ ok: true, text })
    } catch (caught) {
      setMessage({ ok: false, text: errorText(caught) })
    }
  }

  const pending = (links ?? []).filter((l) => l.status === 'Pending')
  const accepted = (links ?? []).filter((l) => l.status === 'Accepted')

  return (
    <section className={`sticker ${styles.card}`}>
      <h2>Мої викладачі</h2>
      {pending.length > 0 && (
        <>
          <h3>Запрошення</h3>
          <PeopleList
            empty=""
            people={pending.map((link) => ({
              key: link.id,
              person: link.teacher,
              note: 'хоче бути твоїм викладачем',
              actions: (
                <>
                  <button type="button" className="btn" onClick={() => act(profileApi.acceptLink(link.id), 'Запрошення прийнято.')}>
                    Прийняти
                  </button>
                  <button type="button" className={styles.linkButton} onClick={() => act(profileApi.removeLink(link.id), 'Запрошення відхилено.')}>
                    Відхилити
                  </button>
                </>
              ),
            }))}
          />
        </>
      )}
      <PeopleList
        empty={
          pending.length > 0 ? '' : "Поки немає викладачів. Коли викладач запросить тебе за поштою або ніком, запрошення з'явиться тут."
        }
        people={accepted.map((link) => ({
          key: link.id,
          person: link.teacher,
          actions: (
            <button
              type="button"
              className={styles.linkButton}
              onClick={() => window.confirm('Припинити навчання з цим викладачем?') && act(profileApi.removeLink(link.id), 'Зв’язок розірвано.')}
            >
              Вийти
            </button>
          ),
        }))}
      />
      <Status message={message} />
    </section>
  )
}

function StudentsSection({ onChanged }: { onChanged: () => Promise<void> }) {
  const [links, setLinks] = useState<TeachingLink[] | null>(null)
  const [invitee, setInvitee] = useState('')
  const [message, setMessage] = useState<Message>(null)
  const load = useCallback(() => profileApi.students().then(setLinks), [])

  useEffect(() => {
    profileApi.students().then(setLinks).catch(() => setLinks([]))
  }, [])

  const invite = async (event: FormEvent) => {
    event.preventDefault()
    try {
      await profileApi.invite(invitee.trim())
      setInvitee('')
      await load()
      setMessage({ ok: true, text: 'Запрошення надіслано. Учень прийме його у своєму профілі.' })
    } catch (caught) {
      setMessage({ ok: false, text: errorText(caught) })
    }
  }

  const remove = async (id: string) => {
    if (!window.confirm('Прибрати цього учня?')) return
    try {
      await profileApi.removeLink(id)
      await load()
      await onChanged()
    } catch (caught) {
      setMessage({ ok: false, text: errorText(caught) })
    }
  }

  return (
    <section className={`sticker ${styles.card}`}>
      <h2>Мої учні</h2>
      <PeopleList
        empty="Поки немає учнів."
        people={(links ?? []).map((link) => ({
          key: link.id,
          person: link.student,
          note: link.status === 'Pending' ? 'запрошення очікує відповіді' : undefined,
          actions: (
            <button type="button" className={styles.linkButton} onClick={() => remove(link.id)}>
              {link.status === 'Pending' ? 'Скасувати' : 'Прибрати'}
            </button>
          ),
        }))}
      />
      <form className={styles.inviteRow} onSubmit={invite}>
        <input placeholder="Пошта або нік учня" required minLength={3} value={invitee} onChange={(e) => setInvitee(e.target.value)} />
        <button className="btn">Запросити</button>
      </form>
      <Status message={message} />
    </section>
  )
}

function FollowsSection() {
  const [authors, setAuthors] = useState<PublicUser[] | null>(null)

  useEffect(() => {
    profileApi.follows().then(setAuthors).catch(() => setAuthors([]))
  }, [])

  return (
    <section className={`sticker ${styles.card}`}>
      <h2>Підписки</h2>
      <PeopleList
        empty="Ти ще ні на кого не підписаний(-а). Підпишись на автора, щоб бачити його нові історії."
        people={(authors ?? []).map((author) => ({
          key: author.id,
          person: author,
          actions: (
            <button
              type="button"
              className={styles.linkButton}
              onClick={async () => {
                await profileApi.unfollow(author.username)
                setAuthors((list) => (list ?? []).filter((a) => a.id !== author.id))
              }}
            >
              Відписатися
            </button>
          ),
        }))}
      />
    </section>
  )
}

function DeleteSection() {
  const { logout } = useAuth()
  const navigate = useNavigate()
  const [open, setOpen] = useState(false)
  const [password, setPassword] = useState('')
  const [message, setMessage] = useState<Message>(null)

  const remove = async (event: FormEvent) => {
    event.preventDefault()
    if (!window.confirm('Видалити акаунт назавжди? Твої історії, словник і прогрес зникнуть. Це не можна скасувати.')) return
    try {
      await profileApi.deleteAccount(password)
      logout()
      navigate('/')
    } catch (caught) {
      setMessage({ ok: false, text: errorText(caught) })
    }
  }

  return (
    <section className={`sticker ${styles.card} ${styles.danger}`}>
      <h2>Видалення акаунта</h2>
      {!open ? (
        <div className={styles.inlineActions}>
          <button type="button" className={styles.dangerButton} onClick={() => setOpen(true)}>
            Видалити акаунт…
          </button>
        </div>
      ) : (
        <form className={styles.inviteRow} onSubmit={remove}>
          <input type="password" placeholder="Пароль для підтвердження" required value={password} onChange={(e) => setPassword(e.target.value)} />
          <button className={`btn ${styles.deleteButton}`}>Видалити назавжди</button>
        </form>
      )}
      <Status message={message} />
    </section>
  )
}

function WalletSection() {
  const wallet = useWallet(true)
  const [history, setHistory] = useState<WalletTransaction[] | null>(null)

  useEffect(() => {
    api<WalletTransaction[]>('/wallet/transactions').then(setHistory).catch(() => setHistory([]))
  }, [wallet?.balance])

  if (!wallet) return null

  return (
    <section id="wallet" className={`sticker ${styles.card}`}>
      <h2>Кристали і серія</h2>
      <p className={styles.walletBalance}>
        На рахунку: <Crystals amount={wallet.balance} size={22} />
      </p>
      <StreakWeek wallet={wallet} />
      {history && history.length > 0 && (
        <ul className={styles.history}>
          {history.map((item) => (
            <li key={item.id}>
              <span>
                {REASON_LABELS[item.reason]}
                {item.storyTitle && <small> · {item.storyTitle}</small>}
              </span>
              <time dateTime={item.createdAt}>{new Date(item.createdAt).toLocaleDateString('uk-UA')}</time>
              <b className={item.amount > 0 ? styles.plus : styles.minus}>
                {item.amount > 0 ? '+' : '−'}
                {Math.abs(item.amount)}
              </b>
            </li>
          ))}
        </ul>
      )}
    </section>
  )
}

export function ProfilePage() {
  const { user, refresh } = useAuth()
  if (!user) return null

  return (
    <div className={`page ${styles.page}`}>
      <AvatarSection user={user} onChanged={refresh} />
      <WalletSection />
      <NameSection key={`${user.displayName}-${user.nameVisibility}`} user={user} onChanged={refresh} />
      {user.role === 'Student' ? <TeachersSection onChanged={refresh} /> : <StudentsSection onChanged={refresh} />}
      <FollowsSection />
      <EmailSection user={user} onChanged={refresh} />
      <PasswordSection />
      <DeleteSection />
    </div>
  )
}
