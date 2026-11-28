import { useEffect, useState, type FormEvent } from 'react'
import { Link } from 'react-router-dom'
import { api, ApiError } from '../api/client'
import { profileApi } from '../api/profile'
import type { Assignment, AssignmentState, PublicUser, StorySummary } from '../api/types'
import { formatDue } from './assignmentFormat'
import styles from './Assignments.module.css'

const STATE_LABELS: Record<AssignmentState, string> = {
  Open: 'Очікує',
  Overdue: 'Прострочено',
  Done: 'Виконано',
}

export function StateBadge({ state }: { state: AssignmentState }) {
  return <span className={`${styles.state} ${styles[state.toLowerCase()]}`}>{STATE_LABELS[state]}</span>
}

function todayIso() {
  const now = new Date()
  return new Date(now.getTime() - now.getTimezoneOffset() * 60000).toISOString().slice(0, 10)
}

interface AssignDialogProps {
  story?: Pick<StorySummary, 'id' | 'title'>
  student?: PublicUser
  onClose: () => void
  onAssigned?: (assignment: Assignment) => void
}

export function AssignDialog({ story, student, onClose, onAssigned }: AssignDialogProps) {
  const [students, setStudents] = useState<PublicUser[]>([])
  const [stories, setStories] = useState<StorySummary[]>([])
  const [studentId, setStudentId] = useState(student?.id ?? '')
  const [storyId, setStoryId] = useState(story?.id ?? '')
  const [dueDate, setDueDate] = useState('')
  const [note, setNote] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [done, setDone] = useState(false)
  const [busy, setBusy] = useState(false)

  useEffect(() => {
    if (!student) {
      profileApi
        .students()
        .then((links) => setStudents(links.filter((link) => link.status === 'Accepted').map((link) => link.student)))
        .catch(() => setStudents([]))
    }
    if (!story) {
      api<StorySummary[]>('/stories')
        .then(setStories)
        .catch(() => setStories([]))
    }
  }, [student, story])

  useEffect(() => {
    const closeOnEscape = (event: KeyboardEvent) => event.key === 'Escape' && onClose()
    window.addEventListener('keydown', closeOnEscape)
    return () => window.removeEventListener('keydown', closeOnEscape)
  }, [onClose])

  const submit = async (event: FormEvent) => {
    event.preventDefault()
    setBusy(true)
    setError(null)
    try {
      const assignment = await profileApi.assign(studentId, storyId, dueDate || null, note)
      setDone(true)
      onAssigned?.(assignment)
    } catch (caught) {
      setError(caught instanceof ApiError ? caught.message : 'Не вдалося призначити історію.')
    } finally {
      setBusy(false)
    }
  }

  return (
    <div className={styles.backdrop} onPointerDown={(e) => e.target === e.currentTarget && onClose()}>
      <form className={`sticker ${styles.dialog}`} role="dialog" aria-modal="true" aria-label="Призначити історію" onSubmit={submit}>
        <h3>Домашнє завдання</h3>
        {done ? (
          <>
            <p>Завдання призначено. Учень побачить його на головній і в профілі.</p>
            <div className={styles.actions}>
              <button type="button" className="btn" onClick={onClose}>
                Готово
              </button>
            </div>
          </>
        ) : (
          <>
            {story ? (
              <p className={styles.fixed}>
                Історія: <b>{story.title}</b>
              </p>
            ) : (
              <label className="field">
                Історія
                <select required value={storyId} onChange={(e) => setStoryId(e.target.value)}>
                  <option value="">Обери історію…</option>
                  {stories.map((item) => (
                    <option key={item.id} value={item.id}>
                      {item.level} · {item.title} · {item.authorUsername}
                    </option>
                  ))}
                </select>
              </label>
            )}
            {student ? (
              <p className={styles.fixed}>
                Учень: <b>{student.username}</b>
              </p>
            ) : students.length === 0 ? (
              <p className={styles.hint}>
                У тебе ще немає учнів, які прийняли запрошення. Запроси їх у <Link to="/profile">профілі</Link>.
              </p>
            ) : (
              <label className="field">
                Учень
                <select required value={studentId} onChange={(e) => setStudentId(e.target.value)}>
                  <option value="">Обери учня…</option>
                  {students.map((item) => (
                    <option key={item.id} value={item.id}>
                      {item.displayName ? `${item.username} (${item.displayName})` : item.username}
                    </option>
                  ))}
                </select>
              </label>
            )}
            <label className="field">
              Термін (необов'язково)
              <input type="date" min={todayIso()} value={dueDate} onChange={(e) => setDueDate(e.target.value)} />
            </label>
            <label className="field">
              Коментар для учня (необов'язково)
              <textarea maxLength={300} rows={3} placeholder="Наприклад: зверни увагу на нові слова" value={note} onChange={(e) => setNote(e.target.value)} />
            </label>
            {error && <p className="error-text">{error}</p>}
            <div className={styles.actions}>
              <button className="btn" disabled={busy || !studentId || !storyId}>
                Призначити
              </button>
              <button type="button" className="btn btn-ghost" onClick={onClose}>
                Скасувати
              </button>
            </div>
          </>
        )}
      </form>
    </div>
  )
}

interface AssignmentListProps {
  assignments: Assignment[]
  viewer: 'teacher' | 'student'
  onRemove?: (id: string) => void
}

export function AssignmentList({ assignments, viewer, onRemove }: AssignmentListProps) {
  return (
    <ul className={styles.list}>
      {assignments.map((item) => (
        <li key={item.id} className={styles.item}>
          <StateBadge state={item.state} />
          <div className={styles.body}>
            <Link to={`/stories/${item.story.id}`} className={styles.title}>
              {item.story.title}
            </Link>
            <span className={styles.meta}>
              <span className="level-badge">{item.story.level}</span>
              {viewer === 'teacher' ? `учень ${item.student.username}` : `від ${item.teacher.username}`} · {formatDue(item.dueDate)}
            </span>
            {item.note && <span className={styles.note}>«{item.note}»</span>}
          </div>
          {viewer === 'teacher' && onRemove && (
            <button type="button" className={styles.remove} onClick={() => onRemove(item.id)}>
              {item.state === 'Done' ? 'Прибрати' : 'Скасувати'}
            </button>
          )}
        </li>
      ))}
    </ul>
  )
}
