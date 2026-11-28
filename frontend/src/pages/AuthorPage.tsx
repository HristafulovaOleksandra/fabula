import { useEffect, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { api, ApiError } from '../api/client'
import type { AuthorProfile, CountItem } from '../api/types'
import { useAuth } from '../auth/useAuth'
import { Avatar } from '../components/Avatar'
import { Mascot } from '../components/Brand'
import { RatingBadge } from '../components/StarRating'
import { StoryCard, StoryGrid } from '../components/StoryCard'
import styles from './AuthorPage.module.css'

function readersLabel(count: number) {
  const lastTwo = count % 100
  const last = count % 10
  if (lastTwo >= 11 && lastTwo <= 14) return 'читачів'
  if (last === 1) return 'читач'
  if (last >= 2 && last <= 4) return 'читачі'
  return 'читачів'
}

function Distribution({ title, items }: { title: string; items: CountItem[] }) {
  if (items.length === 0) return null
  const max = Math.max(...items.map((item) => item.count))
  return (
    <div className={styles.distribution}>
      <h3>{title}</h3>
      <ul>
        {items.map((item) => (
          <li key={item.name}>
            <span className={styles.distName}>{item.name}</span>
            <span className={styles.distTrack}>
              <span className={styles.distFill} style={{ width: `${(item.count / max) * 100}%` }} />
            </span>
            <b>{item.count}</b>
          </li>
        ))}
      </ul>
    </div>
  )
}

export function AuthorPage() {
  const { username } = useParams()
  const { user } = useAuth()
  const [profile, setProfile] = useState<AuthorProfile | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)

  useEffect(() => {
    api<AuthorProfile>(`/authors/${encodeURIComponent(username ?? '')}`)
      .then(setProfile)
      .catch((caught) => setError(caught instanceof ApiError && caught.status === 404 ? 'Такого автора не знайдено.' : 'Не вдалося завантажити сторінку автора.'))
  }, [username])

  if (error) {
    return (
      <div className="page" style={{ textAlign: 'center' }}>
        <Mascot mood="sad" size={150} />
        <p className="error-text">{error}</p>
        <Link to="/library">До бібліотеки</Link>
      </div>
    )
  }

  if (!profile) return <div className="page">Завантажуємо…</div>

  const { author, stats } = profile
  const isMe = user?.id === author.id

  const toggleFollow = async () => {
    setBusy(true)
    try {
      await api(`/authors/${encodeURIComponent(author.username)}/follow`, { method: profile.isFollowedByMe ? 'DELETE' : 'POST' })
      setProfile({
        ...profile,
        isFollowedByMe: !profile.isFollowedByMe,
        followerCount: profile.followerCount + (profile.isFollowedByMe ? -1 : 1),
      })
    } finally {
      setBusy(false)
    }
  }

  return (
    <div className="page">
      <section className={`sticker ${styles.header}`}>
        <Avatar avatar={author.avatar} username={author.username} size={110} />
        <div className={styles.identity}>
          <h1>{author.username}</h1>
          {author.displayName && <p className={styles.name}>{author.displayName}</p>}
          <div className={styles.badges}>
            <span className={styles.badge}>Автор</span>
            {author.isTeacher && <span className={`${styles.badge} ${styles.teacher}`}>Викладач</span>}
          </div>
        </div>
        <div className={styles.follow}>
          <span>
            <b>{profile.followerCount}</b> підписників
          </span>
          {user && !isMe && (
            <button type="button" className={profile.isFollowedByMe ? 'btn btn-ghost' : 'btn'} disabled={busy} onClick={toggleFollow}>
              {profile.isFollowedByMe ? 'Відписатися' : 'Підписатися'}
            </button>
          )}
          {!user && <Link to="/login">Увійди, щоб підписатися</Link>}
        </div>
      </section>

      <section className={styles.stats}>
        <div className="sticker">
          <b>{stats.storyCount}</b>
          <span>опублікованих історій</span>
        </div>
        <div className="sticker">
          <b>{stats.readerCount}</b>
          <span>{readersLabel(stats.readerCount)} пройшли історії</span>
        </div>
        <div className="sticker">
          <b>{stats.rating === null ? '—' : <RatingBadge rating={stats.rating} count={stats.ratingCount} />}</b>
          <span>{stats.rating === null ? 'ще немає оцінок' : 'середня оцінка'}</span>
        </div>
      </section>

      {(stats.levels.length > 0 || stats.genres.length > 0) && (
        <section className={`sticker ${styles.distributions}`}>
          <Distribution title="Рівні" items={stats.levels} />
          <Distribution title="Жанри" items={stats.genres} />
        </section>
      )}

      <h2>Історії автора</h2>
      {profile.stories.length === 0 ? (
        <p>Автор ще не опублікував жодної історії.</p>
      ) : (
        <StoryGrid>
          {profile.stories.map((story) => (
            <StoryCard key={story.id} story={story} to={`/stories/${story.id}`} footer={`${story.sceneCount} сцен`} />
          ))}
        </StoryGrid>
      )}
    </div>
  )
}
