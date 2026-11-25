import type { StorySummary } from '../api/types'
import { COVER_COLORS } from './coverColors'
import styles from './StoryCover.module.css'

export function StoryCover({ story }: { story: Pick<StorySummary, 'coverImagePath' | 'level' | 'title'> }) {
  return story.coverImagePath ? (
    <img className={styles.cover} src={story.coverImagePath} alt="" />
  ) : (
    <div className={styles.cover} style={{ background: COVER_COLORS[story.level] }} aria-hidden="true" />
  )
}
