import type { StorySummary } from '../api/types'
import { COVER_COLORS } from './coverColors'
import { coverImageStyle, type CoverFraming } from './coverFraming'
import styles from './StoryCover.module.css'

export function StoryCover({ story }: { story: CoverFraming & Pick<StorySummary, 'level' | 'title'> }) {
  return story.coverImagePath ? (
    <div className={`${styles.cover} ${styles.framed}`}>
      <img className={styles.image} src={story.coverImagePath} alt="" style={coverImageStyle(story)} draggable={false} />
    </div>
  ) : (
    <div className={styles.cover} style={{ background: COVER_COLORS[story.level] }} aria-hidden="true" />
  )
}
