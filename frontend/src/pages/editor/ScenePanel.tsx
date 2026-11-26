import { useEffect, useRef, useState } from 'react'
import { errorText, studio, type SceneForm } from '../../api/studio'
import type { Background, Economy, Scene, StoryDetail } from '../../api/types'
import { AnalyzedLine, LevelBadge } from './AnalyzedLine'
import { prefetchAnalyses } from './lineAnalysis'
import { PriceInput } from './PriceInput'
import { SaveStatusLine } from './SaveStatusLine'
import { useAutosave } from './useAutosave'
import { StageEditor } from './StageEditor'
import styles from './Editor.module.css'

function toSceneForm(scene: Scene): SceneForm {
  return {
    title: scene.title,
    backgroundId: scene.backgroundId,
    characters: scene.characters.map((c) => ({
      position: c.position,
      storyCharacterId: c.storyCharacterId,
      emotion: c.emotion ?? 'neutral',
      x: c.x,
      y: c.y,
      scale: c.scale,
      flip: c.flip,
    })),
    lines: scene.lines.map((l) => ({ id: l.id, speaker: l.speaker || null, text: l.text })),
    choices: scene.choices.map((c) => ({ id: c.id, text: c.text, nextSceneId: c.nextSceneId, priceCrystals: c.priceCrystals })),
  }
}

function move<T>(items: T[], index: number, shift: number): T[] {
  const target = index + shift
  if (target < 0 || target >= items.length) return items
  const next = [...items]
  ;[next[index], next[target]] = [next[target], next[index]]
  return next
}

interface ScenePanelProps {
  story: StoryDetail
  economy: Economy
  scene: Scene
  onSaved: () => Promise<void>
  onDirtyChange: (dirty: boolean) => void
  onDelete: () => void
}

function sceneProblem(form: SceneForm) {
  if (!form.title.trim()) return 'назва сцени порожня'
  if (form.lines.some((l) => !l.text.trim())) return 'є порожня репліка — допиши або видали її'
  if (form.choices.some((c) => !c.text.trim())) return 'є вибір без тексту'
  return null
}

function withSavedIds(value: SceneForm, sent: SceneForm, saved: Scene): SceneForm {
  return {
    ...value,
    lines: value.lines.map((line, i) => (sent.lines[i]?.text === line.text && saved.lines[i] ? { ...line, id: saved.lines[i].id } : line)),
    choices: value.choices.map((choice, i) =>
      sent.choices[i]?.text === choice.text && saved.choices[i] ? { ...choice, id: saved.choices[i].id } : choice,
    ),
  }
}

export function ScenePanel({ story, economy, scene, onSaved, onDirtyChange, onDelete }: ScenePanelProps) {
  const [baseline, setBaseline] = useState(() => toSceneForm(scene))
  const [form, setForm] = useState(baseline)
  const [backgrounds, setBackgrounds] = useState<Background[]>([])
  const [uploadError, setUploadError] = useState<string | null>(null)
  const fileInput = useRef<HTMLInputElement>(null)

  const autosave = useAutosave({
    value: form,
    baseline,
    problem: sceneProblem(form),
    save: async (sent) => {
      const saved = await studio.saveScene(story.id, scene.id, {
        ...sent,
        title: sent.title.trim(),
        lines: sent.lines.map((l) => ({ ...l, text: l.text.trim() })),
        choices: sent.choices.map((c) => ({ ...c, text: c.text.trim() })),
      })
      setBaseline(withSavedIds(sent, sent, saved))
      setForm((current) => withSavedIds(current, sent, saved))
      await onSaved()
    },
  })

  useEffect(() => {
    onDirtyChange(autosave.dirty)
  }, [autosave.dirty, onDirtyChange])

  useEffect(() => {
    studio.backgrounds().then(setBackgrounds).catch(() => setBackgrounds([]))
  }, [])

  useEffect(() => {
    prefetchAnalyses(
      scene.lines.map((l) => ({ text: l.text, narration: !l.speaker })),
      story.level,
    ).catch(() => undefined)
  }, [scene.lines, story.level])

  const update = (next: SceneForm) => setForm(next)

  const castName = (id: string) => story.characters.find((c) => c.id === id)?.name ?? ''

  const speakers = form.characters.map((c) => castName(c.storyCharacterId)).filter(Boolean)

  const uploadBackground = async (file: File | undefined) => {
    if (!file) return
    setUploadError(null)
    try {
      const name = file.name.replace(/\.[^.]+$/, '').slice(0, 100) || 'Фон'
      const background = await studio.uploadBackground(name, file)
      setBackgrounds((list) => [...list, background])
      update({ ...form, backgroundId: background.id })
    } catch (caught) {
      setUploadError(errorText(caught))
    } finally {
      if (fileInput.current) fileInput.current.value = ''
    }
  }

  const otherScenes = story.scenes.filter((s) => s.id !== scene.id)

  return (
    <div className={styles.panel}>
      <div className={styles.sceneHeader}>
        <label className="field">
          Назва сцени
          <input maxLength={120} value={form.title} onChange={(e) => update({ ...form, title: e.target.value })} />
        </label>
        {story.startSceneId === scene.id && <span className={styles.startBadge}>Початок історії</span>}
      </div>

      <fieldset className={styles.fieldset}>
        <legend>Фон</legend>
        <div className={styles.backgroundGrid}>
          <button
            type="button"
            className={`${styles.backgroundTile} ${form.backgroundId === null ? styles.tileOn : ''}`}
            onClick={() => update({ ...form, backgroundId: null })}
          >
            <span className={styles.noBackground}>Без фону</span>
          </button>
          {backgrounds.map((background) => (
            <button
              type="button"
              key={background.id}
              title={background.name}
              className={`${styles.backgroundTile} ${form.backgroundId === background.id ? styles.tileOn : ''}`}
              onClick={() => update({ ...form, backgroundId: background.id })}
            >
              <img src={background.imagePath} alt={background.name} />
            </button>
          ))}
          <button type="button" className={`${styles.backgroundTile} ${styles.uploadTile}`} onClick={() => fileInput.current?.click()}>
            + Свій фон
          </button>
        </div>
        <input ref={fileInput} type="file" accept="image/png,image/jpeg,image/webp" hidden onChange={(e) => uploadBackground(e.target.files?.[0])} />
        {uploadError && <small className="error-text">{uploadError}</small>}
      </fieldset>

      <fieldset className={styles.fieldset}>
        <legend>Персонажі в сцені</legend>
        {story.characters.length === 0 ? (
          <p className={styles.hint}>Спершу додай персонажів у розділі «Персонажі».</p>
        ) : (
          <StageEditor
            cast={story.characters}
            backgroundPath={backgrounds.find((b) => b.id === form.backgroundId)?.imagePath ?? null}
            placements={form.characters}
            onChange={(characters) => update({ ...form, characters })}
          />
        )}
      </fieldset>

      <fieldset className={styles.fieldset}>
        <legend>Репліки ({form.lines.length})</legend>
        {form.lines.map((line, index) => (
          <div key={index} className={styles.lineRow}>
            <div className={styles.lineSide}>
              <select
                value={line.speaker ?? ''}
                onChange={(e) => update({ ...form, lines: form.lines.map((l, i) => (i === index ? { ...l, speaker: e.target.value || null } : l)) })}
              >
                <option value="">Оповідач</option>
                {[...new Set([...speakers, ...(line.speaker ? [line.speaker] : [])])].map((name) => (
                  <option key={name} value={name}>
                    {name}
                  </option>
                ))}
              </select>
              <LevelBadge text={line.text} target={story.level} narration={!line.speaker} />
            </div>
            <AnalyzedLine
              text={line.text}
              target={story.level}
              narration={!line.speaker}
              placeholder={line.speaker ? 'Що каже персонаж англійською' : 'Опис того, що відбувається'}
              targetWords={story.words.map((w) => w.word)}
              allowedWords={story.allowedWords}
              onChange={(text) => update({ ...form, lines: form.lines.map((l, i) => (i === index ? { ...l, text } : l)) })}
              onAllow={(lemma) => {
                studio
                  .allowWord(story.id, lemma)
                  .then(onSaved)
                  .catch(() => undefined)
              }}
            />
            <div className={styles.rowButtons}>
              <button type="button" className={styles.iconButton} aria-label="Вище" onClick={() => update({ ...form, lines: move(form.lines, index, -1) })}>
                ↑
              </button>
              <button type="button" className={styles.iconButton} aria-label="Нижче" onClick={() => update({ ...form, lines: move(form.lines, index, 1) })}>
                ↓
              </button>
              <button
                type="button"
                className={styles.iconButton}
                aria-label="Видалити репліку"
                onClick={() => update({ ...form, lines: form.lines.filter((_, i) => i !== index) })}
              >
                ✕
              </button>
            </div>
          </div>
        ))}
        <div className={styles.actions}>
          <button
            type="button"
            className="btn btn-ghost"
            disabled={speakers.length === 0}
            onClick={() => update({ ...form, lines: [...form.lines, { id: null, speaker: speakers[0] ?? null, text: '' }] })}
          >
            + Репліка
          </button>
          <button type="button" className="btn btn-ghost" onClick={() => update({ ...form, lines: [...form.lines, { id: null, speaker: null, text: '' }] })}>
            + Оповідь
          </button>
        </div>
      </fieldset>

      <fieldset className={styles.fieldset}>
        <legend>Вибори ({form.choices.length})</legend>
        {form.choices.length === 0 && <p className={styles.hint}>Сцена без виборів — це кінець історії.</p>}
        {form.choices.map((choice, index) => (
          <div key={index} className={styles.choiceRow}>
            <input
              placeholder="Текст вибору"
              maxLength={200}
              value={choice.text}
              onChange={(e) => update({ ...form, choices: form.choices.map((c, i) => (i === index ? { ...c, text: e.target.value } : c)) })}
            />
            <select
              value={choice.nextSceneId ?? ''}
              onChange={(e) =>
                update({ ...form, choices: form.choices.map((c, i) => (i === index ? { ...c, nextSceneId: e.target.value || null } : c)) })
              }
            >
              <option value="">Кінець історії</option>
              {otherScenes.map((s) => (
                <option key={s.id} value={s.id}>
                  {s.title}
                </option>
              ))}
            </select>
            <PriceInput
              label="Ціна вибору в кристалах"
              value={choice.priceCrystals}
              min={economy.choicePriceMin}
              max={economy.choicePriceMax}
              onChange={(price) => update({ ...form, choices: form.choices.map((c, i) => (i === index ? { ...c, priceCrystals: price } : c)) })}
            />
            <button
              type="button"
              className={styles.iconButton}
              aria-label="Видалити вибір"
              onClick={() => update({ ...form, choices: form.choices.filter((_, i) => i !== index) })}
            >
              ✕
            </button>
          </div>
        ))}
        <div className={styles.actions}>
          <button
            type="button"
            className="btn btn-ghost"
            disabled={form.choices.length >= 10}
            onClick={() => update({ ...form, choices: [...form.choices, { id: null, text: '', nextSceneId: null, priceCrystals: 0 }] })}
          >
            + Вибір
          </button>
        </div>
      </fieldset>

      <div className={styles.actions}>
        <button
          type="button"
          className="btn"

          onClick={autosave.saveNow}
        >
          Зберегти зараз
        </button>
        <SaveStatusLine status={autosave.status} onRetry={autosave.saveNow} />
        <button type="button" className={styles.dangerButton} onClick={onDelete}>
          Видалити сцену
        </button>
      </div>
    </div>
  )
}
