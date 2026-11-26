import { useEffect, useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { api } from '../api/client'
import { DEFAULT_STORY_TITLE, errorText, studio } from '../api/studio'
import type { RecentStory, StoryStatus, StudioOverview, StudioStory, User } from '../api/types'
import { useWallet } from '../api/wallet'
import { Mascot } from '../components/Brand'
import { StoryCard, StoryGrid } from '../components/StoryCard'
import { StreakWeek } from '../components/StreakWeek'
import styles from './HomePage.module.css'

function lastPlayed(value: string) {
  const startOfDay = (date: Date) => new Date(date.getFullYear(), date.getMonth(), date.getDate()).getTime()
  const days = Math.round((startOfDay(new Date()) - startOfDay(new Date(value))) / 86_400_000)
  if (days <= 0) return 'сьогодні'
  if (days === 1) return 'вчора'
  return new Date(value).toLocaleDateString('uk-UA', { day: 'numeric', month: 'long' })
}

function RecentReading({ hideWhenEmpty = false }: { hideWhenEmpty?: boolean }) {
  const [recent, setRecent] = useState<RecentStory[] | null>(null)

  useEffect(() => {
    api<RecentStory[]>('/play/recent')
      .then(setRecent)
      .catch(() => setRecent([]))
  }, [])

  if (hideWhenEmpty && recent !== null && recent.length === 0) return null

  return (
    <section className={styles.section}>
      <h2>Нещодавно ти читав(-ла)</h2>
      {recent === null ? (
        <p>Завантажуємо…</p>
      ) : recent.length === 0 ? (
        <div className={`sticker ${styles.empty}`}>
          <Mascot mood="thinking" size={110} />
          <p>
            Тут поки немає історій. Обери першу в <Link to="/library">бібліотеці</Link>.
          </p>
        </div>
      ) : (
        <StoryGrid>
          {recent.map((item) => (
            <StoryCard
              key={item.story.id}
              story={item.story}
              to={`/stories/${item.story.id}`}
              badge={item.completed ? <span className={`${styles.status} ${styles.statusDone}`}>Пройдено</span> : undefined}
              footer={`Востаннє ${lastPlayed(item.lastPlayedAt)}`}
            />
          ))}
        </StoryGrid>
      )}
    </section>
  )
}

function StreakBox() {
  const wallet = useWallet(true)
  return <div className={`sticker ${styles.streakBox}`}>{wallet ? <StreakWeek wallet={wallet} /> : <p>Завантажуємо серію…</p>}</div>
}

export function StudentHome({ user }: { user: User }) {
  const wallet = useWallet(true)
  const playedToday = wallet?.streak.playedToday ?? false

  return (
    <div className="page">
      <section className={styles.dashboard}>
        <div className={`sticker ${styles.greeting}`}>
          <Mascot mood={playedToday ? 'celebrate' : 'happy'} size={150} className={styles.greetingCat} />
          <div>
            <h1>Привіт, {user.username}!</h1>
            <p className={styles.lead}>
              {playedToday
                ? 'Сьогодні ти вже читав(-ла). Можеш узяти ще одну історію — просто для задоволення.'
                : 'Одна коротка історія — і вогник сьогоднішнього дня загориться.'}
            </p>
            <Link to="/library" className="btn">
              До бібліотеки
            </Link>
          </div>
        </div>
        <StreakBox />
      </section>
      <RecentReading />
    </div>
  )
}

const STATUS_LABELS: Record<StoryStatus, string> = {
  Draft: 'Чернетка',
  PendingModeration: 'На перевірці',
  Published: 'Опубліковано',
  Blocked: 'Заблоковано',
}

function levelSummary(item: StudioStory) {
  if (item.totalLines === 0) return 'Ще немає реплік'
  if (item.uncheckedLines > 0) return `Модель ще оцінює ${item.uncheckedLines} з ${item.totalLines} реплік`
  if (item.hardLines === 0) return `Усі репліки (${item.totalLines}) відповідають рівню ${item.story.level}`
  return `${item.hardLines} з ${item.totalLines} реплік складніші за ${item.story.level}`
}

export function AuthorHome({ user }: { user: User }) {
  const navigate = useNavigate()
  const [overview, setOverview] = useState<StudioOverview | null>(null)
  const [creating, setCreating] = useState(false)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    api<StudioOverview>('/studio/overview')
      .then(setOverview)
      .catch(() => setError('Не вдалося завантажити твої історії.'))
  }, [])

  const create = async () => {
    setCreating(true)
    try {
      const story = await studio.createStory(DEFAULT_STORY_TITLE)
      navigate(`/studio/stories/${story.id}`)
    } catch (caught) {
      setError(errorText(caught))
      setCreating(false)
    }
  }

  return (
    <div className="page">
      <section className={styles.dashboard}>
        <div className={`sticker ${styles.greeting}`}>
          <Mascot mood="happy" size={150} className={styles.greetingCat} />
          <div>
            <h1>Привіт, {user.username}!</h1>
            <p className={styles.lead}>Тут твої історії, учні й читачі. Модель підкаже, де текст складніший за рівень історії.</p>
            <div className={styles.actions}>
              <button type="button" className="btn" onClick={create} disabled={creating}>
                + Нова історія
              </button>
              <Link to="/studio" className="btn btn-ghost">
                Мої історії
              </Link>
            </div>
          </div>
        </div>
        <StreakBox />
      </section>

      {error && <p className="error-text">{error}</p>}

      {overview && (
        <>
          <section className={styles.section}>
            <h2>Продовжити роботу</h2>
            {overview.recentStories.length === 0 ? (
              <div className={`sticker ${styles.empty}`}>
                <Mascot mood="thinking" size={110} />
                <p>У тебе ще немає історій. Натисни «Нова історія», щоб почати.</p>
              </div>
            ) : (
              <StoryGrid>
                {overview.recentStories.map((item) => (
                  <StoryCard
                    key={item.story.id}
                    story={item.story}
                    to={`/studio/stories/${item.story.id}`}
                    badge={
                      <span className={`${styles.status} ${item.story.status === 'Published' ? styles.statusDone : ''}`}>
                        {STATUS_LABELS[item.story.status]}
                      </span>
                    }
                    footer={
                      <span className={item.hardLines > 0 && item.uncheckedLines === 0 ? styles.warning : undefined}>{levelSummary(item)}</span>
                    }
                  />
                ))}
              </StoryGrid>
            )}
          </section>

          <section className={styles.section}>
            <h2>Учні й читачі</h2>
            <div className={styles.counters}>
              <div className="sticker">
                <b>{overview.storyCount}</b>
                <span>
                  історій: {overview.publishedCount} опубліковано, {overview.draftCount} чернеток
                </span>
              </div>
              <div className="sticker">
                <b>{overview.studentCount}</b>
                <span>учнів</span>
              </div>
              <div className="sticker">
                <b>{overview.pendingInvitationCount}</b>
                <span>запрошень чекають відповіді</span>
              </div>
              <div className="sticker">
                <b>{overview.followerCount}</b>
                <span>підписників</span>
              </div>
            </div>
            <Link to="/profile" className={styles.more}>
              Запросити учнів або переглянути список — у профілі
            </Link>
          </section>
        </>
      )}

      <RecentReading hideWhenEmpty />
    </div>
  )
}
