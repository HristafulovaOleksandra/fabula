import type { StoryDetail } from '../../api/types'
import { storyIssues } from './storyIssues'
import styles from './Editor.module.css'

export function CheckPanel({ story }: { story: StoryDetail }) {
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
