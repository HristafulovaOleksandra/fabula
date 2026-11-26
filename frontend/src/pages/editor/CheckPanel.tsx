import type { CefrLevel, StoryDetail } from '../../api/types'
import { isAboveLevel, LEVEL_ORDER } from './lineAnalysis'
import { storyIssues } from './storyIssues'
import styles from './Editor.module.css'

function LevelSummary({ story, onOpenScene }: { story: StoryDetail; onOpenScene: (sceneId: string) => void }) {
  const lines = story.scenes.flatMap((scene) => scene.lines.map((line) => ({ scene, line })))
  const checked = lines.filter(({ line }) => line.levelChecked)
  const rated = checked.filter(({ line }) => line.level !== null)
  const short = checked.length - rated.length
  const pending = lines.length - checked.length
  const above = rated.filter(({ line }) => isAboveLevel(line.level, story.level))
  const share = checked.length === 0 ? 0 : Math.round((above.length / checked.length) * 100)
  const counts = LEVEL_ORDER.map((level) => ({ level, count: rated.filter(({ line }) => line.level === level).length }))
  const max = Math.max(1, ...counts.map((c) => c.count))

  return (
    <section className={styles.fieldset}>
      <h3>Рівень реплік за ML-моделлю</h3>
      {rated.length === 0 ? (
        <p className={styles.hint}>Ще немає оцінених реплік.</p>
      ) : (
        <>
          <p className={styles.levelVerdict}>
            Складніших за <b>{story.level}</b>: <b className={share > 0 ? styles.verdictBad : undefined}>{above.length}</b> з {checked.length} реплік
            історії ({share}%).
          </p>
          <div className={styles.levelBars}>
            {counts.map(({ level, count }) => (
              <div key={level} className={styles.levelBar}>
                <span className={styles.levelBarTrack}>
                  <span
                    className={`${styles.levelBarFill} ${isAboveLevel(level as CefrLevel, story.level) ? styles.levelBarAbove : ''}`}
                    style={{ height: `${(count / max) * 100}%` }}
                  />
                </span>
                <b>{count}</b>
                <span>{level}</span>
              </div>
            ))}
          </div>
          {short > 0 && (
            <p className={styles.hint}>
              Коротких реплік (1–2 слова): {short} — модель їх не оцінює, у статистиці вони рахуються як нескладні.
            </p>
          )}
          {pending > 0 && <p className={styles.hint}>Ще оцінюються: {pending}. Онови сторінку за хвилину.</p>}
          {above.length > 0 && (
            <ul className={styles.aboveList}>
              {above.map(({ scene, line }) => (
                <li key={line.id ?? `${scene.id}-${line.text}`}>
                  <span className={`${styles.levelBadge} ${styles.levelAbove}`}>{line.level}</span>
                  <button type="button" className={styles.linkButton} onClick={() => onOpenScene(scene.id)}>
                    {scene.title}
                  </button>
                  <span className={styles.aboveText}>{line.text}</span>
                </li>
              ))}
            </ul>
          )}
        </>
      )}
    </section>
  )
}

export function CheckPanel({ story, onOpenScene }: { story: StoryDetail; onOpenScene: (sceneId: string) => void }) {
  const { problems, notes } = storyIssues(story)
  const lines = story.scenes.reduce((sum, s) => sum + s.lines.length, 0)
  const choices = story.scenes.reduce((sum, s) => sum + s.choices.length, 0)

  return (
    <div className={styles.panel}>
      <h2>Перевірка історії</h2>
      <div className={styles.stats}>
        <span>Сцен: {story.scenes.length}</span>
        <span>Реплік: {lines}</span>
        <span>Виборів: {choices}</span>
        <span>Цільових слів: {story.words.length}</span>
      </div>
      <LevelSummary story={story} onOpenScene={onOpenScene} />
      <section className={styles.fieldset}>
        <h3>{problems.length === 0 ? 'Проблем не знайдено' : `Проблеми (${problems.length})`}</h3>
        {problems.length > 0 && (
          <ul className={styles.problemList}>
            {problems.map((text) => (
              <li key={text}>{text}</li>
            ))}
          </ul>
        )}
      </section>
      {notes.length > 0 && (
        <section className={styles.fieldset}>
          <h3>Зауваження</h3>
          <ul className={styles.noteList}>
            {notes.map((text) => (
              <li key={text}>{text}</li>
            ))}
          </ul>
        </section>
      )}
    </div>
  )
}
