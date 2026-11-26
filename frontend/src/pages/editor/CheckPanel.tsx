import type { StoryDetail } from '../../api/types'
import { aboveTarget, aboveTitle, useCalibration } from './certainty'
import { LEVEL_ORDER } from './lineAnalysis'
import { storyIssues } from './storyIssues'
import styles from './Editor.module.css'

function LevelSummary({ story, onOpenScene }: { story: StoryDetail; onOpenScene: (sceneId: string) => void }) {
  const calibration = useCalibration()
  const lines = story.scenes.flatMap((scene) => scene.lines.map((line) => ({ scene, line })))
  const checked = lines.filter(({ line }) => line.levelChecked)
  const rated = checked
    .filter(({ line }) => line.level !== null)
    .map((item) => ({ ...item, above: aboveTarget(calibration, item.line.level, item.line.levelScore, !item.line.speaker, story.level) }))
  const short = checked.length - rated.length
  const pending = lines.length - checked.length
  const above = rated
    .filter((item) => item.above !== null)
    .sort((a, b) => (a.above?.kind === b.above?.kind ? 0 : a.above?.kind === 'sure' ? -1 : 1))
  const sure = above.filter((item) => item.above?.kind === 'sure').length
  const maybe = above.length - sure
  const share = checked.length === 0 ? 0 : Math.round((above.length / checked.length) * 100)
  const counts = LEVEL_ORDER.map((level) => {
    const atLevel = rated.filter(({ line }) => line.level === level)
    return {
      level,
      count: atLevel.length,
      sure: atLevel.filter((item) => item.above?.kind === 'sure').length,
      maybe: atLevel.filter((item) => item.above?.kind === 'maybe').length,
    }
  })
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
            історії ({share}%)
            {above.length > 0 && (
              <>
                {' '}
                — <span className={styles.sureText}>точно: {sure}</span>, <span className={styles.maybeText}>можливо: {maybe}</span>
              </>
            )}
            .
          </p>
          <div className={styles.levelBars}>
            {counts.map(({ level, count, sure: sureCount, maybe: maybeCount }) => {
              const plain = count - sureCount - maybeCount
              return (
                <div key={level} className={styles.levelBar}>
                  <span className={styles.levelBarTrack}>
                    <span className={styles.levelBarStack} style={{ height: `${(count / max) * 100}%` }}>
                      {sureCount > 0 && <span className={styles.levelBarAbove} style={{ flexGrow: sureCount }} />}
                      {maybeCount > 0 && <span className={styles.levelBarMaybe} style={{ flexGrow: maybeCount }} />}
                      {plain > 0 && <span className={styles.levelBarFill} style={{ flexGrow: plain }} />}
                    </span>
                  </span>
                  <b>{count}</b>
                  <span>{level}</span>
                </div>
              )
            })}
          </div>
          {above.length > 0 && (
            <p className={styles.legend}>
              <span className={styles.legendSure} /> модель права в 80% і більше таких випадків
              <span className={styles.legendMaybe} /> менше 80% — варто перечитати самому
            </p>
          )}
          {short > 0 && (
            <p className={styles.hint}>
              Коротких реплік (1–2 слова): {short} — модель їх не оцінює, у статистиці вони рахуються як нескладні.
            </p>
          )}
          {pending > 0 && <p className={styles.hint}>Ще оцінюються: {pending}. Онови сторінку за хвилину.</p>}
          {above.length > 0 && (
            <ul className={styles.aboveList}>
              {above.map(({ scene, line, above: mark }) => (
                <li key={line.id ?? `${scene.id}-${line.text}`}>
                  <span
                    className={`${styles.levelBadge} ${mark?.kind === 'maybe' ? styles.levelMaybe : styles.levelAbove}`}
                    title={mark ? `Рівень за моделлю: ${line.level} – ${aboveTitle(mark, story.level)}` : undefined}
                  >
                    {line.level}
                  </span>
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
