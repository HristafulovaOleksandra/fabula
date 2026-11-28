import { useCallback, useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { api, ApiError } from '../api/client'
import type { AdminUser, ModerationReport, QueueItem, UserRole } from '../api/types'
import { useAuth } from '../auth/useAuth'
import { Avatar } from '../components/Avatar'
import { AGE_LABELS } from '../components/ageRating'
import { Mascot } from '../components/Brand'
import { FlaggedText } from './editor/ModerationSummary'
import { AdminMlStats } from './AdminMlStats'
import styles from './AdminPage.module.css'

type Tab = 'queue' | 'history' | 'users' | 'ml'

const ROLE_LABELS: Record<UserRole, string> = { Student: 'Учень', Author: 'Автор', Admin: 'Адміністратор' }

const errorText = (caught: unknown) => (caught instanceof ApiError ? caught.message : 'Щось пішло не так.')

function StoryDetails({ storyId }: { storyId: string }) {
  const [report, setReport] = useState<ModerationReport | null>(null)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    api<ModerationReport>(`/stories/${storyId}/moderation`, { method: 'POST' })
      .then(setReport)
      .catch((caught) => setError(errorText(caught)))
  }, [storyId])

  if (error) return <p className="error-text">{error}</p>
  if (!report) return <p className={styles.muted}>Модель перевіряє історію…</p>
  const flagged = report.findings.filter((finding) => finding.verdict !== 'Ok')
  return (
    <ul className={styles.findings}>
      {(flagged.length > 0 ? flagged : report.findings).map((finding, index) => (
        <li key={`${finding.location}-${index}`}>
          <b>{finding.location}</b>
          {finding.text && (
            <span>
              <FlaggedText text={finding.text} words={finding.words} />
            </span>
          )}
          {finding.imagePath && <img src={finding.imagePath} alt="" className={styles.findingImage} />}
          {finding.reasons.length > 0 && <small>{finding.reasons.join(', ')}</small>}
        </li>
      ))}
      {report.findings.length === 0 && <li>Зараз модель нічого не знаходить — автор, мабуть, уже виправив історію.</li>}
    </ul>
  )
}

function QueueCard({ item, onDone, readOnly }: { item: QueueItem; onDone: () => void; readOnly: boolean }) {
  const [note, setNote] = useState('')
  const [open, setOpen] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)

  const decide = async (decision: 'approve' | 'reject') => {
    setBusy(true)
    setError(null)
    try {
      await api(`/admin/queue/${item.id}/${decision}`, { method: 'POST', body: { note } })
      onDone()
    } catch (caught) {
      setError(errorText(caught))
      setBusy(false)
    }
  }

  const kind = item.tag ? 'Тег' : item.status === 'Appealed' ? 'Оскарження' : 'Історія на перевірці'
  return (
    <article className={`sticker ${styles.card}`}>
      <div className={styles.cardHead}>
        <span className={`${styles.kind} ${item.status === 'Appealed' ? styles.kindAppeal : ''}`}>{kind}</span>
        <span className={styles.muted}>{new Date(item.createdAt).toLocaleString('uk-UA')}</span>
      </div>
      {item.story && (
        <>
          <h3>
            {item.story.title}{' '}
            <span className={styles.muted}>
              · автор {item.story.authorUsername} · {AGE_LABELS[item.story.ageRating]}
              {item.story.visibility === 'Private' ? ' · приватна' : ''}
            </span>
          </h3>
          <div className={styles.links}>
            <Link to={`/play/${item.story.id}`} target="_blank">
              Переглянути в програвачі
            </Link>
            <button type="button" className={styles.linkButton} onClick={() => setOpen(!open)}>
              {open ? 'Сховати знахідки моделі' : 'Що знайшла модель'}
            </button>
          </div>
          {open && <StoryDetails storyId={item.story.id} />}
        </>
      )}
      {item.tag && (
        <h3>
          «{item.tag.name}» <span className={styles.muted}>· {item.tag.kind === 'Genre' ? 'жанр' : 'тема'} · автор {item.tag.creatorUsername ?? '—'}</span>
        </h3>
      )}
      {item.reason && <p className={styles.reason}>Причина: {item.reason}</p>}
      {item.authorMessage && <p className={styles.appeal}>Пояснення автора: «{item.authorMessage}»</p>}
      {readOnly ? (
        <p className={styles.muted}>
          {item.status === 'Approved' ? 'Схвалено' : 'Відхилено'}
          {item.reviewedBy ? ` · ${item.reviewedBy}` : ' · автоматично'}
          {item.resolutionNote ? ` · «${item.resolutionNote}»` : ''}
        </p>
      ) : (
        <div className={styles.decision}>
          <input maxLength={1000} placeholder="Коментар для автора (необов'язково)" value={note} onChange={(e) => setNote(e.target.value)} />
          <button type="button" className="btn" disabled={busy} onClick={() => decide('approve')}>
            Схвалити
          </button>
          <button type="button" className={`btn ${styles.reject}`} disabled={busy} onClick={() => decide('reject')}>
            Відхилити
          </button>
        </div>
      )}
      {error && <p className="error-text">{error}</p>}
    </article>
  )
}

function Queue({ history }: { history: boolean }) {
  const [items, setItems] = useState<QueueItem[] | null>(null)
  const load = useCallback(() => {
    api<QueueItem[]>(`/admin/queue${history ? '?history=true' : ''}`)
      .then(setItems)
      .catch(() => setItems([]))
  }, [history])

  useEffect(() => {
    load()
  }, [load])

  if (!items) return <p>Завантажуємо…</p>
  if (items.length === 0) {
    return (
      <div className={`sticker ${styles.empty}`}>
        <Mascot mood="happy" size={110} />
        <p>{history ? 'Ще немає ухвалених рішень.' : 'Черга порожня — усе перевірено.'}</p>
      </div>
    )
  }
  return (
    <div className={styles.list}>
      {items.map((item) => (
        <QueueCard key={item.id} item={item} readOnly={history} onDone={load} />
      ))}
    </div>
  )
}

function Users() {
  const { user: me } = useAuth()
  const [query, setQuery] = useState('')
  const [users, setUsers] = useState<AdminUser[] | null>(null)
  const [error, setError] = useState<string | null>(null)

  const load = useCallback((q: string) => {
    api<AdminUser[]>(`/admin/users?q=${encodeURIComponent(q)}`)
      .then(setUsers)
      .catch((caught) => setError(errorText(caught)))
  }, [])

  useEffect(() => {
    const timer = window.setTimeout(() => load(query.trim()), 300)
    return () => window.clearTimeout(timer)
  }, [query, load])

  const change = async (target: AdminUser, path: string, body: unknown, confirmText: string) => {
    if (!window.confirm(confirmText)) return
    setError(null)
    try {
      await api(`/admin/users/${target.id}/${path}`, { method: 'PUT', body })
      load(query.trim())
    } catch (caught) {
      setError(errorText(caught))
    }
  }

  return (
    <div>
      <input className={styles.search} type="search" placeholder="Нік або пошта" value={query} onChange={(e) => setQuery(e.target.value)} />
      {error && <p className="error-text">{error}</p>}
      {!users ? (
        <p>Завантажуємо…</p>
      ) : (
        <ul className={styles.users}>
          {users.map((target) => (
            <li key={target.id} className={target.isBlocked ? styles.blockedUser : undefined}>
              <Avatar avatar={target.avatar} username={target.username} size={40} />
              <div className={styles.userInfo}>
                <Link to={`/authors/${target.username}`}>
                  <b>{target.username}</b>
                </Link>
                <small>
                  {target.email} · історій: {target.storyCount}
                  {target.isTeacher ? ' · викладач' : ''}
                  {target.isBlocked ? ' · заблоковано' : ''}
                </small>
              </div>
              <select
                value={target.role}
                disabled={target.id === me?.id}
                onChange={(e) =>
                  change(target, 'role', { role: e.target.value }, `Змінити роль ${target.username} на «${ROLE_LABELS[e.target.value as UserRole]}»? Користувачеві доведеться увійти знову.`)
                }
                aria-label="Роль"
              >
                {(Object.keys(ROLE_LABELS) as UserRole[]).map((role) => (
                  <option key={role} value={role}>
                    {ROLE_LABELS[role]}
                  </option>
                ))}
              </select>
              {target.id !== me?.id && (
                <button
                  type="button"
                  className={styles.linkButton}
                  onClick={() =>
                    change(
                      target,
                      'blocked',
                      { blocked: !target.isBlocked },
                      target.isBlocked ? `Розблокувати ${target.username}?` : `Заблокувати ${target.username}? Користувач не зможе увійти.`,
                    )
                  }
                >
                  {target.isBlocked ? 'Розблокувати' : 'Заблокувати'}
                </button>
              )}
            </li>
          ))}
        </ul>
      )}
    </div>
  )
}

export function AdminPage() {
  const [tab, setTab] = useState<Tab>('queue')
  return (
    <div className="page">
      <h1>Адмінпанель</h1>
      <div className={styles.tabs} role="tablist">
        {(
          [
            ['queue', 'Черга'],
            ['history', 'Рішення'],
            ['users', 'Користувачі'],
            ['ml', 'Статистика ML'],
          ] as const
        ).map(([value, label]) => (
          <button key={value} type="button" role="tab" aria-selected={tab === value} className={tab === value ? styles.tabOn : ''} onClick={() => setTab(value)}>
            {label}
          </button>
        ))}
      </div>
      {tab === 'queue' && <Queue history={false} />}
      {tab === 'history' && <Queue history />}
      {tab === 'users' && <Users />}
      {tab === 'ml' && <AdminMlStats />}
    </div>
  )
}
