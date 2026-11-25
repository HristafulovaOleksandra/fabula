import { useEffect, useState } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { api, ApiError } from '../api/client'
import type { StoryDetail } from '../api/types'
import { useAuth } from '../auth/useAuth'
import { Mascot } from '../components/Brand'
import { StoryCover } from '../components/StoryCover'
import styles from './StoryIntroPage.module.css'

export function StoryIntroPage() {
  const { storyId } = useParams()
  const { user } = useAuth()
  const navigate = useNavigate()
  const [story, setStory] = useState<StoryDetail | null>(null)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    api<StoryDetail>(`/stories/${storyId}`)
      .then(setStory)
      .catch((caught) => setError(caught instanceof ApiError && caught.status === 404 ? 'Історію не знайдено.' : 'Не вдалося завантажити історію.'))
  }, [storyId])

  if (error) {
    return (
      <div className="page" style={{ textAlign: 'center' }}>
        <Mascot mood="sad" size={150} className={styles.centered} />
        <p className="error-text">{error}</p>
        <Link to="/library">До бібліотеки</Link>
      </div>
    )
  }

  if (!story) return <div className="page">Завантажуємо історію…</div>

  const start = () => (user ? navigate(`/play/${story.id}`) : navigate('/login', { state: { from: `/play/${story.id}` } }))

  return (
    <div className={`page ${styles.layout}`}>
      <section className={`sticker ${styles.info}`}>
        <StoryCover story={story} />
        <div className={styles.meta}>
          <span className="level-badge">{story.level}</span>
          {story.tags.map((tag) => (
            <span key={tag.id} className={styles.tag}>
              {tag.name}
            </span>
          ))}
        </div>
        <h1>{story.title}</h1>
        {story.description && <p>{story.description}</p>}
        <p className={styles.small}>{story.scenes.length} сцен</p>
        <button className="btn" onClick={start}>
          {user ? 'Почати історію' : 'Увійти й почати'}
        </button>
      </section>

      <section className={`sticker ${styles.words}`}>
        <div className={styles.wordsHead}>
          <Mascot mood="thinking" size={84} />
          <div>
            <h2>Слова цієї історії</h2>
            <p className={styles.small}>Під час читання вони будуть підсвічені — торкнись, щоб побачити переклад і додати в словник.</p>
          </div>
        </div>
        {story.words.length === 0 ? (
          <p>Автор не додав цільових слів.</p>
        ) : (
          <ul className={styles.wordList}>
            {story.words.map((word) => (
              <li key={word.word}>
                <span className={styles.word}>{word.word}</span>
                <span>{word.translation}</span>
              </li>
            ))}
          </ul>
        )}
      </section>
    </div>
  )
}
