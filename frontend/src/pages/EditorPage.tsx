import { useCallback, useEffect, useState } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { errorText, studio } from '../api/studio'
import type { Economy, StoryDetail, StoryStatus, Tag } from '../api/types'
import { CastPanel } from './editor/CastPanel'
import { CheckPanel } from './editor/CheckPanel'
import { ScenePanel } from './editor/ScenePanel'
import { StoryPanel } from './editor/StoryPanel'
import { storyIssues } from './editor/storyIssues'
import styles from './editor/Editor.module.css'

type View = { kind: 'story' } | { kind: 'cast' } | { kind: 'check' } | { kind: 'scene'; id: string }

const STATUS_LABELS: Record<StoryStatus, string> = {
  Draft: 'Чернетка',
  PendingModeration: 'На перевірці',
  Published: 'Опубліковано',
  Blocked: 'Заблоковано',
}

const LEAVE_WARNING = 'Є незбережені зміни. Перейти без збереження?'

export function EditorPage() {
  const { storyId = '' } = useParams()
  const navigate = useNavigate()
  const [story, setStory] = useState<StoryDetail | null>(null)
  const [tags, setTags] = useState<Tag[]>([])
  const [economy, setEconomy] = useState<Economy | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [view, setView] = useState<View>({ kind: 'story' })
  const [dirty, setDirty] = useState(false)
  const [publishMessage, setPublishMessage] = useState<{ ok: boolean; text: string } | null>(null)

  const reload = useCallback(async () => {
    try {
      setStory(await studio.story(storyId))
    } catch (caught) {
      setError(errorText(caught))
    }
  }, [storyId])

  useEffect(() => {
    studio
      .story(storyId)
      .then(setStory)
      .catch((caught) => setError(errorText(caught)))
    studio.tags().then(setTags).catch(() => setTags([]))
    studio.economy().then(setEconomy).catch((caught) => setError(errorText(caught)))
  }, [storyId])

  useEffect(() => {
    if (!dirty) return
    const warn = (event: BeforeUnloadEvent) => event.preventDefault()
    window.addEventListener('beforeunload', warn)
    return () => window.removeEventListener('beforeunload', warn)
  }, [dirty])

  const open = (next: View) => {
    if (dirty && !window.confirm(LEAVE_WARNING)) return
    setDirty(false)
    setView(next)
  }

  if (error) return <div className="page"><p className="error-text">{error}</p></div>
  if (!story || !economy) return <div className="page">Завантаження…</div>

  const issues = storyIssues(story)
  const scene = view.kind === 'scene' ? story.scenes.find((s) => s.id === view.id) : undefined

  const addScene = async () => {
    if (dirty && !window.confirm(LEAVE_WARNING)) return
    try {
      const created = await studio.createScene(story.id, `Сцена ${story.scenes.length + 1}`)
      await reload()
      setDirty(false)
      setView({ kind: 'scene', id: created.id })
    } catch (caught) {
      setError(errorText(caught))
    }
  }

  const deleteScene = async (id: string, title: string) => {
    if (!window.confirm(`Видалити сцену «${title}»? Вибори, що вели до неї, стануть кінцем історії.`)) return
    await studio.deleteScene(story.id, id)
    setDirty(false)
    setView({ kind: 'story' })
    await reload()
  }

  const deleteStory = async () => {
    if (!window.confirm(`Видалити історію «${story.title}» назавжди? Це не можна скасувати.`)) return
    await studio.deleteStory(story.id)
    setDirty(false)
    navigate('/studio')
  }

  const publish = async () => {
    if (dirty && !window.confirm('Є незбережені зміни — вони не потраплять у публікацію. Продовжити?')) return
    if (issues.problems.length > 0) {
      setPublishMessage({ ok: false, text: 'Спершу виправ проблеми з розділу «Перевірка».' })
      open({ kind: 'check' })
      return
    }
    try {
      setStory(await studio.publish(story.id))
      setPublishMessage({ ok: true, text: 'Історію опубліковано' })
    } catch (caught) {
      setPublishMessage({ ok: false, text: errorText(caught) })
    }
  }

  const navButton = (target: View, label: string, extra?: string) => {
    const active = target.kind === view.kind && (target.kind !== 'scene' || (view.kind === 'scene' && view.id === target.id))
    return (
      <button type="button" className={`${styles.navItem} ${active ? styles.navActive : ''}`} onClick={() => open(target)}>
        <span>{label}</span>
        {extra && <span className={styles.navExtra}>{extra}</span>}
      </button>
    )
  }

  return (
    <div className={`page ${styles.editor}`}>
      <div className={styles.editorHead}>
        <div>
          <Link to="/studio" className={styles.back}>
            ← Мої історії
          </Link>
          <h1>{story.title}</h1>
          <div className={styles.headMeta}>
            <span className="level-badge">{story.level}</span>
            <span className={styles.status}>{STATUS_LABELS[story.status]}</span>
          </div>
        </div>
        <div className={styles.actions}>
          <a className="btn btn-ghost" href={`/play/${story.id}`} target="_blank" rel="noreferrer">
            Переглянути
          </a>
          {story.status !== 'Published' && (
            <button type="button" className="btn" onClick={publish}>
              Опублікувати
            </button>
          )}
        </div>
      </div>
      {publishMessage && <p className={publishMessage.ok ? 'success-text' : 'error-text'}>{publishMessage.text}</p>}

      <div className={styles.layout}>
        <aside className={styles.sidebar}>
          {navButton({ kind: 'story' }, 'Про історію')}
          {navButton({ kind: 'cast' }, 'Персонажі', String(story.characters.length))}
          {navButton({ kind: 'check' }, 'Перевірка', issues.problems.length > 0 ? `⚠ ${issues.problems.length}` : '✓')}
          <h3 className={styles.sidebarTitle}>Сцени</h3>
          {story.scenes.map((s) => {
            const marks = [
              s.id === story.startSceneId ? 'старт' : '',
              issues.unreachable.has(s.id) ? '⚠' : '',
              s.choices.length === 0 && !issues.unreachable.has(s.id) ? 'кінець' : '',
            ]
              .filter(Boolean)
              .join(' · ')
            return <div key={s.id}>{navButton({ kind: 'scene', id: s.id }, s.title, marks)}</div>
          })}
          <button type="button" className="btn btn-ghost" onClick={addScene}>
            + Сцена
          </button>
        </aside>

        <section className={styles.content}>
          {view.kind === 'story' && (
            <StoryPanel
              key={`story-${story.scenes.length}`}
              story={story}
              economy={economy}
              tags={tags}
              onTagCreated={(tag) => setTags((list) => (list.some((t) => t.id === tag.id) ? list : [...list, tag]))}
              onSaved={reload}
              onDirtyChange={setDirty}
              onDelete={deleteStory}
            />
          )}
          {view.kind === 'cast' && <CastPanel story={story} onChanged={reload} />}
          {view.kind === 'check' && <CheckPanel story={story} />}
          {view.kind === 'scene' && scene && (
            <ScenePanel
              key={scene.id}
              story={story}
              economy={economy}
              scene={scene}
              onSaved={reload}
              onDirtyChange={setDirty}
              onDelete={() => deleteScene(scene.id, scene.title)}
            />
          )}
        </section>
      </div>
    </div>
  )
}
