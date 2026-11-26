import { useEffect, useRef, useState } from 'react'
import { errorText, studio, type SceneForm } from '../../api/studio'
import type { Background, Economy, Scene, StoryDetail } from '../../api/types'
import { AutoGrowTextarea } from './AutoGrowTextarea'
import { PriceInput } from './PriceInput'
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

export function ScenePanel({ story, economy, scene, onSaved, onDirtyChange, onDelete }: ScenePanelProps) {
  const [initial, setInitial] = useState(() => toSceneForm(scene))
  const [form, setForm] = useState(initial)
  const [backgrounds, setBackgrounds] = useState<Background[]>([])
  const [busy, setBusy] = useState(false)
  const [message, setMessage] = useState<{ ok: boolean; text: string } | null>(null)
  const [uploadError, setUploadError] = useState<string | null>(null)
  const fileInput = useRef<HTMLInputElement>(null)

  useEffect(() => {
    studio.backgrounds().then(setBackgrounds).catch(() => setBackgrounds([]))
  }, [])

  const update = (next: SceneForm) => {
    setForm(next)
    setMessage(null)
    onDirtyChange(JSON.stringify(next) !== JSON.stringify(initial))
  }

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

  const save = async () => {
    setBusy(true)
    try {
      const payload: SceneForm = {
        ...form,
        title: form.title.trim(),
        lines: form.lines.filter((l) => l.text.trim()).map((l) => ({ ...l, text: l.text.trim() })),
        choices: form.choices.filter((c) => c.text.trim()).map((c) => ({ ...c, text: c.text.trim() })),
      }
      const saved = await studio.saveScene(story.id, scene.id, payload)
      const next = toSceneForm(saved)
      setInitial(next)
      setForm(next)
      onDirtyChange(false)
      await onSaved()
      setMessage({ ok: true, text: 'Збережено' })
    } catch (caught) {
      setMessage({ ok: false, text: errorText(caught) })
    } finally {
      setBusy(false)
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
            <AutoGrowTextarea
              rows={2}
              maxLength={500}
              className={line.speaker ? undefined : styles.narration}
              placeholder={line.speaker ? 'Що каже персонаж англійською' : 'Опис того, що відбувається'}
              value={line.text}
              onChange={(e) => update({ ...form, lines: form.lines.map((l, i) => (i === index ? { ...l, text: e.target.value } : l)) })}
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
                  → {s.title}
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
        <button type="button" className="btn" disabled={busy || !form.title.trim()} onClick={save}>
          {busy ? 'Зберігаємо…' : 'Зберегти сцену'}
        </button>
        {message && <span className={message.ok ? 'success-text' : 'error-text'}>{message.text}</span>}
        <button type="button" className={styles.dangerButton} onClick={onDelete}>
          Видалити сцену
        </button>
      </div>
    </div>
  )
}
