import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { api, ApiError } from '../api/client'
import type { CefrLevel, ForYou, PublicUser, StorySummary, Tag } from '../api/types'
import { useAuth } from '../auth/useAuth'
import { Avatar } from '../components/Avatar'
import { Mascot } from '../components/Brand'
import { StoryCard, StoryGrid } from '../components/StoryCard'
import styles from './LibraryPage.module.css'

const LEVELS: CefrLevel[] = ['A1', 'A2', 'B1', 'B2', 'C']

type Sort = 'new' | 'rating'

interface Filters {
  level: CefrLevel | null
  genreId: number | null
  topicId: number | null
  q: string
  sort: Sort
}

function queryString(filters: Filters) {
  const params = new URLSearchParams()
  if (filters.level) params.set('level', filters.level)
  if (filters.genreId) params.set('genreId', String(filters.genreId))
  if (filters.topicId) params.set('topicId', String(filters.topicId))
  if (filters.q) params.set('q', filters.q)
  if (filters.sort !== 'new') params.set('sort', filters.sort)
  const text = params.toString()
  return text ? `?${text}` : ''
}

function useDebounced<T>(value: T, delay: number) {
  const [debounced, setDebounced] = useState(value)
  useEffect(() => {
    const timer = window.setTimeout(() => setDebounced(value), delay)
    return () => window.clearTimeout(timer)
  }, [value, delay])
  return debounced
}

function ForYouSection() {
  const [forYou, setForYou] = useState<ForYou | null>(null)

  useEffect(() => {
    api<ForYou>('/stories/for-you')
      .then(setForYou)
      .catch(() => setForYou(null))
  }, [])

  if (!forYou || forYou.stories.length === 0) return null

  return (
    <section className={styles.forYou}>
      <h2>Для тебе</h2>
      <p className={styles.hint}>
        {forYou.level
          ? `Історії рівня ${forYou.level}${forYou.levelFromTest ? '' : ' (за прочитаним)'}, яких ти ще не читав(-ла).`
          : 'Історії, яких ти ще не читав(-ла). Пройди кілька — і добірка стане точнішою.'}
      </p>
      <StoryGrid>
        {forYou.stories.map((item) => (
          <StoryCard
            key={item.story.id}
            story={item.story}
            to={`/stories/${item.story.id}`}
            footer={item.reasons.filter((reason) => !reason.startsWith('Твій рівень')).join(' · ') || item.story.authorUsername}
          />
        ))}
      </StoryGrid>
    </section>
  )
}

export function LibraryPage() {
  const { user } = useAuth()
  const [filters, setFilters] = useState<Filters>({ level: null, genreId: null, topicId: null, q: '', sort: 'new' })
  const [search, setSearch] = useState('')
  const q = useDebounced(search.trim(), 350)
  const [tags, setTags] = useState<Tag[]>([])
  const [result, setResult] = useState<{ key: string; stories?: StorySummary[]; error?: string } | null>(null)
  const [authors, setAuthors] = useState<{ q: string; list: PublicUser[] }>({ q: '', list: [] })

  const active = { ...filters, q }
  const key = queryString(active)
  const current = result?.key === key ? result : null
  const stories = current?.stories ?? null
  const error = current?.error ?? null
  const filtered = Boolean(active.level || active.genreId || active.topicId || active.q)

  useEffect(() => {
    api<Tag[]>('/tags')
      .then(setTags)
      .catch(() => setTags([]))
  }, [])

  useEffect(() => {
    let alive = true
    api<StorySummary[]>(`/stories${key}`)
      .then((data) => alive && setResult({ key, stories: data }))
      .catch((caught) => alive && setResult({ key, error: caught instanceof ApiError ? caught.message : 'Не вдалося завантажити історії.' }))
    return () => {
      alive = false
    }
  }, [key])

  useEffect(() => {
    if (q.length < 2) return
    let alive = true
    api<PublicUser[]>(`/authors?q=${encodeURIComponent(q)}`)
      .then((list) => alive && setAuthors({ q, list }))
      .catch(() => alive && setAuthors({ q, list: [] }))
    return () => {
      alive = false
    }
  }, [q])

  const shownAuthors = q.length >= 2 && authors.q === q ? authors.list : []
  const genres = tags.filter((tag) => tag.kind === 'Genre')
  const topics = tags.filter((tag) => tag.kind === 'Topic')
  const update = (changes: Partial<Filters>) => setFilters((value) => ({ ...value, ...changes }))

  return (
    <div className="page">
      <h1>Бібліотека</h1>

      <div className={styles.toolbar}>
        <input
          type="search"
          className={styles.search}
          placeholder="Пошук: назва, опис або нік автора"
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          aria-label="Пошук історій і авторів"
        />
        <select value={filters.genreId ?? ''} onChange={(e) => update({ genreId: e.target.value ? Number(e.target.value) : null })} aria-label="Жанр">
          <option value="">Усі жанри</option>
          {genres.map((tag) => (
            <option key={tag.id} value={tag.id}>
              {tag.name}
            </option>
          ))}
        </select>
        <select value={filters.topicId ?? ''} onChange={(e) => update({ topicId: e.target.value ? Number(e.target.value) : null })} aria-label="Тема">
          <option value="">Усі теми</option>
          {topics.map((tag) => (
            <option key={tag.id} value={tag.id}>
              {tag.name}
            </option>
          ))}
        </select>
        <select value={filters.sort} onChange={(e) => update({ sort: e.target.value as Sort })} aria-label="Сортування">
          <option value="new">Спершу нові</option>
          <option value="rating">Найкращі за оцінкою</option>
        </select>
      </div>

      <div className={styles.filters} role="group" aria-label="Рівень">
        <button className={filters.level === null ? styles.active : ''} onClick={() => update({ level: null })}>
          Усі
        </button>
        {LEVELS.map((item) => (
          <button key={item} className={filters.level === item ? styles.active : ''} onClick={() => update({ level: item })}>
            {item}
          </button>
        ))}
      </div>

      {user && !filtered && <ForYouSection />}

      {shownAuthors.length > 0 && (
        <section className={styles.authors}>
          <h2>Автори</h2>
          <div className={styles.authorList}>
            {shownAuthors.map((author) => (
              <Link key={author.id} to={`/authors/${author.username}`} className={`sticker ${styles.author}`}>
                <Avatar avatar={author.avatar ?? null} username={author.username} size={36} />
                <span>
                  <b>{author.username}</b>
                  {author.displayName && <small>{author.displayName}</small>}
                </span>
              </Link>
            ))}
          </div>
        </section>
      )}

      {user && !filtered && <h2 className={styles.allTitle}>Усі історії</h2>}
      {error && <p className="error-text">{error}</p>}
      {!error && stories === null && <p>Завантажуємо історії…</p>}
      {stories?.length === 0 && (
        <div className={styles.empty}>
          <Mascot mood="sad" size={150} />
          <p>{filtered ? 'За цими умовами історій не знайшлося. Спробуй прибрати якийсь фільтр.' : 'Тут поки немає історій. Зазирни трохи пізніше.'}</p>
        </div>
      )}

      <StoryGrid>
        {stories?.map((story) => (
          <StoryCard key={story.id} story={story} to={`/stories/${story.id}`} footer={story.authorUsername} />
        ))}
      </StoryGrid>
    </div>
  )
}
