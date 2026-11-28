import { useEffect, useState } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { api, ApiError } from '../api/client'
import type { StoryAccess, StoryDetail } from '../api/types'
import { notifyWalletChanged } from '../api/wallet'
import { useAuth } from '../auth/useAuth'
import { formatDue } from '../components/assignmentFormat'
import { AssignDialog } from '../components/Assignments'
import { Crystals, Mascot } from '../components/Brand'
import { RateStory, RatingBadge } from '../components/StarRating'
import { AGE_LABELS } from '../components/ageRating'
import { StoryCover } from '../components/StoryCover'
import styles from './StoryIntroPage.module.css'

export function StoryIntroPage() {
  const { storyId } = useParams()
  const { user } = useAuth()
  const navigate = useNavigate()
  const [story, setStory] = useState<StoryDetail | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [access, setAccess] = useState<StoryAccess | null>(null)
  const [confirming, setConfirming] = useState(false)
  const [buyError, setBuyError] = useState<string | null>(null)
  const [assigning, setAssigning] = useState(false)

  useEffect(() => {
    api<StoryDetail>(`/stories/${storyId}`)
      .then(setStory)
      .catch((caught) => setError(caught instanceof ApiError && caught.status === 404 ? 'Історію не знайдено.' : 'Не вдалося завантажити історію.'))
  }, [storyId])

  useEffect(() => {
    if (!user) return
    api<StoryAccess>(`/play/${storyId}/access`)
      .then(setAccess)
      .catch(() => setAccess(null))
  }, [storyId, user])

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

  const purchase = async () => {
    setBuyError(null)
    try {
      setAccess(await api<StoryAccess>(`/play/${story.id}/purchase`, { method: 'POST' }))
      setConfirming(false)
      notifyWalletChanged()
    } catch (caught) {
      setBuyError(caught instanceof ApiError ? caught.message : 'Не вдалося купити історію.')
    }
  }

  const locked = user !== null && access !== null && !access.owned

  const action = !user ? (
    <button className="btn" onClick={start}>
      Увійти й почати
    </button>
  ) : locked && access ? (
    <div className={styles.buy}>
      {!confirming ? (
        <button className="btn" onClick={() => setConfirming(true)}>
          Купити за <Crystals amount={access.price} />
        </button>
      ) : (
        <>
          <p>
            Списати {access.price} кристалів? Після покупки історія лишається твоєю назавжди. У тебе {access.balance}.
          </p>
          <div className={styles.buyActions}>
            <button className="btn" onClick={purchase} disabled={access.balance < access.price}>
              Так, купити
            </button>
            <button className="btn btn-ghost" onClick={() => setConfirming(false)}>
              Скасувати
            </button>
          </div>
        </>
      )}
      {access.balance < access.price && (
        <p className={styles.small}>
          Не вистачає {access.price - access.balance} кристалів. Проходь безкоштовні історії й виконуй щоденну ціль, щоб заробити.
        </p>
      )}
      {buyError && <p className="error-text">{buyError}</p>}
    </div>
  ) : (
    <button className="btn" onClick={start}>
      Почати історію
    </button>
  )

  return (
    <div className={`page ${styles.layout}`}>
      <section className={`sticker ${styles.info}`}>
        <StoryCover story={story} />
        <div className={styles.meta}>
          <span className="level-badge">{story.level}</span>
          {story.ageRating !== 'All' && <span className="age-badge">{AGE_LABELS[story.ageRating]}</span>}
          {story.tags.map((tag) => (
            <span key={tag.id} className={styles.tag}>
              {tag.name}
            </span>
          ))}
        </div>
        <h1>{story.title}</h1>
        <p className={styles.byline}>
          {story.authorUsername && (
            <span>
              Автор: <Link to={`/authors/${story.authorUsername}`}>{story.authorUsername}</Link>
            </span>
          )}
          <RatingBadge rating={story.rating} count={story.ratingCount} />
        </p>
        {story.description && <p>{story.description}</p>}
        <p className={styles.small}>
          {story.scenes.length} сцен
          {story.priceCrystals > 0 && (
            <>
              {' · '}
              {access?.owned && !access.free ? 'куплено' : <Crystals amount={story.priceCrystals} />}
            </>
          )}
          {access && access.reward > 0 && <> · за перше проходження +{access.reward} кристалів</>}
          {access?.completed && <> · уже пройдено</>}
        </p>
        {access?.assignment && (
          <div className={styles.homework}>
            <b>{access.assignment.done ? 'Домашнє завдання виконано' : 'Домашнє завдання'}</b>
            <span>
              від {access.assignment.teacher} · {formatDue(access.assignment.dueDate)}
            </span>
            {access.assignment.note && <span className={styles.homeworkNote}>«{access.assignment.note}»</span>}
          </div>
        )}
        {action}
        {user && user.role !== 'Student' && story.status === 'Published' && (
          <button type="button" className={`btn btn-ghost ${styles.assign}`} onClick={() => setAssigning(true)}>
            Призначити учневі
          </button>
        )}
        {assigning && <AssignDialog story={story} onClose={() => setAssigning(false)} />}
        {access?.canRate && (
          <RateStory
            storyId={story.id}
            initial={access.myRating}
            onRated={(result) => setStory({ ...story, rating: result.rating, ratingCount: result.ratingCount })}
          />
        )}
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
