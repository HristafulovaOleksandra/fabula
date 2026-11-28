import { useState } from 'react'
import { api, ApiError } from '../api/client'
import type { StoryRatingResult } from '../api/types'
import styles from './StarRating.module.css'

const STARS = [1, 2, 3, 4, 5]

export function RatingBadge({ rating, count }: { rating: number | null; count: number }) {
  if (rating === null || count === 0) return null
  return (
    <span className={styles.badge} title={`Середня оцінка ${rating} з 5, оцінок: ${count}`}>
      <span className={styles.star} aria-hidden="true">
        ★
      </span>
      {rating.toFixed(1)}
      <span className={styles.count}>({count})</span>
    </span>
  )
}

export function RateStory({ storyId, initial, onRated }: { storyId: string; initial: number | null; onRated?: (result: StoryRatingResult) => void }) {
  const [value, setValue] = useState(initial)
  const [hover, setHover] = useState<number | null>(null)
  const [message, setMessage] = useState<string | null>(null)

  const rate = async (stars: number) => {
    setMessage(null)
    try {
      const result = await api<StoryRatingResult>(`/stories/${storyId}/rating`, { method: 'PUT', body: { stars } })
      setValue(result.stars)
      setMessage('Дякуємо за оцінку!')
      onRated?.(result)
    } catch (caught) {
      setMessage(caught instanceof ApiError ? caught.message : 'Не вдалося зберегти оцінку.')
    }
  }

  const shown = hover ?? value ?? 0
  return (
    <div className={styles.rate}>
      <span>{value ? 'Твоя оцінка:' : 'Оціни історію:'}</span>
      <div className={styles.stars} role="radiogroup" aria-label="Оцінка від 1 до 5" onMouseLeave={() => setHover(null)}>
        {STARS.map((stars) => (
          <button
            key={stars}
            type="button"
            role="radio"
            aria-checked={value === stars}
            aria-label={`${stars} з 5`}
            className={`${styles.starButton} ${stars <= shown ? styles.on : ''}`}
            onMouseEnter={() => setHover(stars)}
            onClick={() => rate(stars)}
          >
            ★
          </button>
        ))}
      </div>
      {message && <small>{message}</small>}
    </div>
  )
}
