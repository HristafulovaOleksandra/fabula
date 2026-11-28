import type { ReactNode } from 'react'
import { Link } from 'react-router-dom'
import type { StorySummary } from '../api/types'
import { Crystals } from './Brand'
import { RatingBadge } from './StarRating'
import { StoryCover } from './StoryCover'
import styles from './StoryCard.module.css'

interface StoryCardProps {
  story: StorySummary
  to: string
  badge?: ReactNode
  footer: ReactNode
}

export function StoryCard({ story, to, badge, footer }: StoryCardProps) {
  return (
    <Link to={to} className={`sticker ${styles.card}`}>
      <StoryCover story={story} />
      <div className={styles.meta}>
        <span className={styles.badges}>
          <span className="level-badge">{story.level}</span>
          {badge}
        </span>
        <span className={styles.price}>
          <RatingBadge rating={story.rating} count={story.ratingCount} />
          {story.priceCrystals > 0 && <Crystals amount={story.priceCrystals} />}
        </span>
      </div>
      <h3>{story.title}</h3>
      <p className={styles.small}>
        {story.tags.map((tag) => tag.name).join(' · ') || 'Без тегів'} · {story.sceneCount} сцен
      </p>
      <p className={styles.small}>{footer}</p>
    </Link>
  )
}

export function StoryGrid({ children }: { children: ReactNode }) {
  return <div className={styles.grid}>{children}</div>
}
