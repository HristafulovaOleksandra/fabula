import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { DEFAULT_STORY_TITLE, errorText, studio } from '../api/studio'
import type { StorySummary } from '../api/types'
import { Mascot } from '../components/Brand'
import { StoryCard, StoryGrid } from '../components/StoryCard'
import styles from './StudioPage.module.css'

const STATUS_LABELS: Record<StorySummary['status'], string> = {
  Draft: 'Чернетка',
  PendingModeration: 'На перевірці',
  Published: 'Опубліковано',
  Blocked: 'Заблоковано',
}

export function StudioPage() {
  const navigate = useNavigate()
  const [stories, setStories] = useState<StorySummary[] | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)

  useEffect(() => {
    studio.myStories().then(setStories).catch((caught) => setError(errorText(caught)))
  }, [])

  const create = async () => {
    setBusy(true)
    try {
      const story = await studio.createStory(DEFAULT_STORY_TITLE)
      navigate(`/studio/stories/${story.id}`)
    } catch (caught) {
      setError(errorText(caught))
      setBusy(false)
    }
  }

  return (
    <div className="page">
      <div className={styles.head}>
        <h1>Мої історії</h1>
        <button className="btn" onClick={create} disabled={busy}>
          + Нова історія
        </button>
      </div>
      {error && <p className="error-text">{error}</p>}
      {stories?.length === 0 && (
        <div className={styles.empty}>
          <Mascot mood="thinking" size={150} />
          <p>У тебе ще немає історій. Створи першу — почни з назви, рівня й кількох сцен.</p>
        </div>
      )}
      <StoryGrid>
        {stories?.map((story) => (
          <StoryCard
            key={story.id}
            story={story}
            to={`/studio/stories/${story.id}`}
            badge={<span className={`${styles.status} ${styles[story.status]}`}>{STATUS_LABELS[story.status]}</span>}
            footer={`оновлено ${new Date(story.updatedAt).toLocaleDateString('uk-UA')}`}
          />
        ))}
      </StoryGrid>
    </div>
  )
}
