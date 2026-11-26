import { useEffect, useState } from 'react'
import { api, ApiError } from '../api/client'
import type { CefrLevel, StorySummary } from '../api/types'
import { Mascot } from '../components/Brand'
import { StoryCard, StoryGrid } from '../components/StoryCard'
import styles from './LibraryPage.module.css'

const LEVELS: CefrLevel[] = ['A1', 'A2', 'B1', 'B2', 'C']

export function LibraryPage() {
  const [level, setLevel] = useState<CefrLevel | null>(null)
  const [result, setResult] = useState<{ level: CefrLevel | null; stories?: StorySummary[]; error?: string } | null>(null)
  const current = result?.level === level ? result : null
  const stories = current?.stories ?? null
  const error = current?.error ?? null

  useEffect(() => {
    let active = true
    api<StorySummary[]>(`/stories${level ? `?level=${level}` : ''}`)
      .then((data) => {
        if (active) setResult({ level, stories: data })
      })
      .catch((caught) => {
        if (active) setResult({ level, error: caught instanceof ApiError ? caught.message : 'Не вдалося завантажити історії.' })
      })
    return () => {
      active = false
    }
  }, [level])

  return (
    <div className="page">
      <h1>Бібліотека</h1>
      <div className={styles.filters} role="group" aria-label="Рівень">
        <button className={level === null ? styles.active : ''} onClick={() => setLevel(null)}>
          Усі
        </button>
        {LEVELS.map((item) => (
          <button key={item} className={level === item ? styles.active : ''} onClick={() => setLevel(item)}>
            {item}
          </button>
        ))}
      </div>

      {error && <p className="error-text">{error}</p>}
      {!error && stories === null && <p>Завантажуємо історії…</p>}
      {stories?.length === 0 && (
        <div className={styles.empty}>
          <Mascot mood="sad" size={150} />
          <p>Тут поки немає історій{level ? ` рівня ${level}` : ''}. Зазирни трохи пізніше.</p>
        </div>
      )}

      <StoryGrid>
        {stories?.map((story) => (
          <StoryCard key={story.id} story={story} to={`/stories/${story.id}`} footer={`@${story.authorUsername}`} />
        ))}
      </StoryGrid>
    </div>
  )
}
