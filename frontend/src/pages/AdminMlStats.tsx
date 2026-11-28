import { Fragment, useEffect, useState } from 'react'
import { api } from '../api/client'
import type { MlStats, NamedCount, StoryMlDetail, StoryStatus } from '../api/types'
import { AGE_LABELS } from '../components/ageRating'
import { aboveTarget, aboveTitle, useCalibration } from './editor/certainty'
import styles from './AdminMlStats.module.css'

const STATUS_LABELS: Record<StoryStatus, string> = {
  Draft: 'Чернетки',
  PendingModeration: 'На перевірці',
  Published: 'Опубліковані',
  Blocked: 'Заблоковані',
}

const VERDICT_LABELS: Record<string, string> = { Ok: 'Без зауважень', Review: 'На перевірку', Blocked: 'Неприпустимі' }

function percent(part: number, whole: number) {
  return whole === 0 ? '—' : `${Math.round((part / whole) * 100)}%`
}

function Bars({ items, label }: { items: NamedCount[]; label?: (name: string) => string }) {
  const max = Math.max(1, ...items.map((item) => item.count))
  return (
    <ul className={styles.bars}>
      {items.map((item) => (
        <li key={item.name}>
          <span className={styles.barName}>{label ? label(item.name) : item.name}</span>
          <span className={styles.barTrack}>
            <span className={styles.barFill} style={{ width: `${(item.count / max) * 100}%` }} />
          </span>
          <b>{item.count}</b>
        </li>
      ))}
    </ul>
  )
}

function StoryLines({ storyId }: { storyId: string }) {
  const calibration = useCalibration()
  const [detail, setDetail] = useState<StoryMlDetail | null>(null)

  useEffect(() => {
    api<StoryMlDetail>(`/admin/ml-stats/stories/${storyId}`)
      .then(setDetail)
      .catch(() => setDetail(null))
  }, [storyId])

  if (!detail) return <p className={styles.muted}>Завантажуємо репліки…</p>
  const target = detail.story.level
  return (
    <table className={styles.lines}>
      <thead>
        <tr>
          <th>Сцена</th>
          <th>Репліка</th>
          <th>Рівень</th>
          <th>Число моделі</th>
        </tr>
      </thead>
      <tbody>
        {detail.lines.map((line) => {
          const above = aboveTarget(calibration, line.level, line.score, !line.speaker, target)
          return (
            <tr key={line.id}>
              <td className={styles.muted}>{line.sceneTitle}</td>
              <td>
                {line.speaker ? <b>{line.speaker}: </b> : <i>Оповідь: </i>}
                {line.text}
              </td>
              <td>
                {line.level ? (
                  <span
                    className={`${styles.level} ${above ? (above.kind === 'sure' ? styles.levelSure : styles.levelMaybe) : ''}`}
                    title={above ? aboveTitle(above, target) : undefined}
                  >
                    {line.level}
                  </span>
                ) : (
                  <span className={styles.muted}>{line.checkedAt ? 'коротка' : 'не оцінено'}</span>
                )}
              </td>
              <td className={styles.muted}>{line.score?.toFixed(2) ?? '—'}</td>
            </tr>
          )
        })}
      </tbody>
    </table>
  )
}

export function AdminMlStats() {
  const [stats, setStats] = useState<MlStats | null>(null)
  const [openStory, setOpenStory] = useState<string | null>(null)

  useEffect(() => {
    api<MlStats>('/admin/ml-stats')
      .then(setStats)
      .catch(() => setStats(null))
  }, [])

  if (!stats) return <p>Завантажуємо статистику…</p>

  const flagged = stats.adminApproved + stats.adminRejected
  const above = stats.stories.reduce((sum, story) => sum + story.aboveLevel, 0)

  return (
    <div className={styles.page}>
      <section className={styles.tiles}>
        <div className="sticker">
          <b className={stats.modelName ? styles.online : styles.offline}>{stats.modelName ?? 'недоступний'}</b>
          <span>ML-сервіс</span>
        </div>
        <div className="sticker">
          <b>
            {stats.ratedLines} / {stats.totalLines}
          </b>
          <span>реплік оцінено (коротких {stats.shortLines}, чекають {stats.uncheckedLines})</span>
        </div>
        <div className="sticker">
          <b>{percent(above, stats.ratedLines)}</b>
          <span>оцінених реплік складніші за рівень своєї історії</span>
        </div>
        <div className="sticker">
          <b>{stats.openItems}</b>
          <span>рішень чекають у черзі</span>
        </div>
      </section>

      <section className={styles.grid}>
        <div className={`sticker ${styles.panel}`}>
          <h3>Рівні реплік за моделлю CEFR</h3>
          <Bars items={stats.levels} />
        </div>
        <div className={`sticker ${styles.panel}`}>
          <h3>Оцінено реплік за 14 днів</h3>
          <Bars items={stats.checkedPerDay} label={(name) => new Date(name).toLocaleDateString('uk-UA', { day: 'numeric', month: 'short' })} />
        </div>
        <div className={`sticker ${styles.panel}`}>
          <h3>Вікові позначки історій</h3>
          <Bars items={stats.storyAges} label={(name) => AGE_LABELS[name as keyof typeof AGE_LABELS]} />
          <h3>Статуси історій</h3>
          <Bars items={stats.storyStatuses} label={(name) => STATUS_LABELS[name as StoryStatus]} />
        </div>
        <div className={`sticker ${styles.panel}`}>
          <h3>Модерація</h3>
          <Bars items={stats.imageVerdicts} label={(name) => `Картинки: ${VERDICT_LABELS[name]}`} />
          <p className={styles.note}>
            Рішення адміна щодо позначеного моделлю: відхилено {stats.adminRejected}, схвалено {stats.adminApproved}
            {flagged > 0 && <> — модель помилилась у {percent(stats.adminApproved, flagged)} випадків</>}. Автор сам виправив: {stats.autoResolved}.
          </p>
        </div>
      </section>

      <section className={`sticker ${styles.panel} ${styles.wide}`}>
        <h3>Історії</h3>
        <table className={styles.stories}>
          <thead>
            <tr>
              <th>Історія</th>
              <th>Рівень</th>
              <th>Вік</th>
              <th>Статус</th>
              <th>Оцінено</th>
              <th>Складніші</th>
            </tr>
          </thead>
          <tbody>
            {stats.stories.map((story) => (
              <Fragment key={story.id}>
                <tr className={styles.storyRow} onClick={() => setOpenStory(openStory === story.id ? null : story.id)}>
                  <td>
                    <b>{story.title}</b> <span className={styles.muted}>· {story.authorUsername}</span>
                  </td>
                  <td>{story.level}</td>
                  <td>{AGE_LABELS[story.ageRating]}</td>
                  <td>{STATUS_LABELS[story.status]}</td>
                  <td>
                    {story.ratedLines}/{story.totalLines}
                  </td>
                  <td>{percent(story.aboveLevel, story.ratedLines)}</td>
                </tr>
                {openStory === story.id && (
                  <tr>
                    <td colSpan={6}>
                      <StoryLines storyId={story.id} />
                    </td>
                  </tr>
                )}
              </Fragment>
            ))}
          </tbody>
        </table>
      </section>
    </div>
  )
}
