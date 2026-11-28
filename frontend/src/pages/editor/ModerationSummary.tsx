import { useEffect, useState } from 'react'
import { api, ApiError } from '../../api/client'
import type { AgeRating, FlaggedWord, ModerationReport, StoryDetail } from '../../api/types'
import { AGE_LABELS } from '../../components/ageRating'
import styles from './Editor.module.css'

const NEXT_LOWER: Record<AgeRating, string> = {
  All: '',
  Teen: '6+',
  Mature: '12+',
}

const KIND_TITLES: Record<FlaggedWord['kind'], string> = {
  mild: 'Груба лексика — піднімає вік до 12+',
  strong: 'Сильна лайка — піднімає вік до 16+',
  sexual: 'Сексуальний зміст — неприпустимо на платформі',
  hate: 'Ненависть до групи людей — неприпустимо на платформі',
}

export function FlaggedText({ text, words }: { text: string; words: FlaggedWord[] }) {
  const parts: { text: string; word?: FlaggedWord }[] = []
  let cursor = 0
  for (const word of [...words].sort((a, b) => a.start - b.start)) {
    if (word.start < cursor) continue
    parts.push({ text: text.slice(cursor, word.start) }, { text: text.slice(word.start, word.end), word })
    cursor = word.end
  }
  parts.push({ text: text.slice(cursor) })
  return (
    <>
      {parts.map((part, index) =>
        part.word ? (
          <mark key={index} className={styles[`flag_${part.word.kind}`]} title={KIND_TITLES[part.word.kind]}>
            {part.text}
          </mark>
        ) : (
          <span key={index}>{part.text}</span>
        ),
      )}
    </>
  )
}

export function ModerationSummary({ story, onOpenScene }: { story: StoryDetail; onOpenScene: (sceneId: string) => void }) {
  const [report, setReport] = useState<ModerationReport | null>(null)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    let alive = true
    api<ModerationReport>(`/stories/${story.id}/moderation`, { method: 'POST' })
      .then((value) => alive && setReport(value))
      .catch((caught) => alive && setError(caught instanceof ApiError ? caught.message : 'Не вдалося перевірити вміст.'))
    return () => {
      alive = false
    }
  }, [story])

  return (
    <section className={styles.fieldset}>
      <h3>Вік і вміст</h3>
      {error && <p className="error-text">{error}</p>}
      {!error && !report && <p className={styles.hint}>Модель перевіряє текст і картинки історії…</p>}
      {report && (
        <>
          <p className={styles.ageVerdict}>
            Історія підходить для <span className={styles.ageBig}>{AGE_LABELS[report.age]}</span>
          </p>
          {report.verdict === 'Blocked' && (
            <p className={`${styles.verdictBox} ${styles.verdictBlocked}`}>
              Є неприпустимий зміст. Під час публікації історію буде заблоковано — виправ позначене нижче.
            </p>
          )}
          {report.verdict === 'Review' && (
            <p className={`${styles.verdictBox} ${styles.verdictReview}`}>
              Є сумнівний зміст. Під час публікації історія піде на перевірку модератору й до його рішення не з'явиться в бібліотеці.
            </p>
          )}
          {report.verdict === 'Ok' && report.age !== 'All' && (
            <p className={styles.hint}>Щоб історія підійшла для {NEXT_LOWER[report.age]}, заміни позначені слова.</p>
          )}
          {report.findings.length > 0 && (
            <ul className={styles.findingList}>
              {report.findings.map((finding, index) => (
                <li key={`${finding.location}-${index}`}>
                  <span className={`${styles.findingAge} ${finding.verdict !== 'Ok' ? styles.findingBad : ''}`}>
                    {finding.verdict === 'Blocked' ? 'Заборонено' : finding.verdict === 'Review' ? 'Сумнівно' : AGE_LABELS[finding.age]}
                  </span>
                  <div className={styles.findingBody}>
                    {finding.sceneId ? (
                      <button type="button" className={styles.linkButton} onClick={() => onOpenScene(finding.sceneId!)}>
                        {finding.location}
                      </button>
                    ) : (
                      <b>{finding.location}</b>
                    )}
                    {finding.text && (
                      <span className={styles.findingText}>
                        <FlaggedText text={finding.text} words={finding.words} />
                      </span>
                    )}
                    {finding.imagePath && <img className={styles.findingImage} src={finding.imagePath} alt="" />}
                    {finding.reasons.length > 0 && <small>{finding.reasons.join(', ')}</small>}
                  </div>
                </li>
              ))}
            </ul>
          )}
        </>
      )}
    </section>
  )
}
