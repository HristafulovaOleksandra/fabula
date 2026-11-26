import { useState } from 'react'
import { errorText, studio, type CharacterForm } from '../../api/studio'
import type { CharacterState, StoryCharacter, StoryDetail } from '../../api/types'
import { BUILT_IN_STATES, isBuiltInState } from './characterStates'
import { ImageInput } from './ImageInput'
import styles from './Editor.module.css'

const MAX_STATES = 16

function toForm(character: StoryCharacter): CharacterForm {
  return { name: character.name, states: character.states.map((s) => ({ ...s })) }
}

function customStateProblem(states: CharacterState[]) {
  const custom = states.filter((s) => !isBuiltInState(s.key))
  const names = custom.map((s) => s.key.trim().toLowerCase())
  if (names.some((n) => !n)) return 'Дай назву кожному власному стану.'
  if (new Set(names).size !== names.length) return 'Назви станів не можуть повторюватися.'
  if (names.some((n) => BUILT_IN_STATES.some((b) => b.label.toLowerCase() === n || b.key === n))) return 'Такий стан уже є серед стандартних.'
  return null
}

function CharacterCard({ storyId, character, onChanged }: { storyId: string; character: StoryCharacter; onChanged: () => Promise<void> }) {
  const [initial, setInitial] = useState(() => toForm(character))
  const [form, setForm] = useState(initial)
  const [message, setMessage] = useState<{ ok: boolean; text: string } | null>(null)
  const dirty = JSON.stringify(form) !== JSON.stringify(initial)
  const problem = customStateProblem(form.states)
  const customStates = form.states.map((state, index) => ({ state, index })).filter(({ state }) => !isBuiltInState(state.key))

  const change = (next: CharacterForm) => {
    setForm(next)
    setMessage(null)
  }

  const setBuiltIn = (key: string, path: string | null) => {
    const exists = form.states.some((s) => s.key === key)
    change({
      ...form,
      states: exists
        ? form.states.map((s) => (s.key === key ? { ...s, imagePath: path } : s))
        : [...form.states, { id: null, key, imagePath: path }],
    })
  }

  const setCustom = (index: number, patch: Partial<CharacterState>) =>
    change({ ...form, states: form.states.map((s, i) => (i === index ? { ...s, ...patch } : s)) })

  const save = async () => {
    try {
      const states = [
        ...BUILT_IN_STATES.map((b) => form.states.find((s) => s.key === b.key)).filter((s): s is CharacterState => !!s?.imagePath),
        ...form.states.filter((s) => !isBuiltInState(s.key)).map((s) => ({ ...s, key: s.key.trim() })),
      ]
      const saved = await studio.saveCharacter(storyId, character.id, { name: form.name.trim(), states })
      setInitial(toForm(saved))
      setForm(toForm(saved))
      setMessage({ ok: true, text: 'Збережено' })
      await onChanged()
    } catch (caught) {
      setMessage({ ok: false, text: errorText(caught) })
    }
  }

  const remove = async () => {
    if (!window.confirm(`Видалити персонажа «${character.name}»? Він зникне з усіх сцен.`)) return
    try {
      await studio.deleteCharacter(storyId, character.id)
      await onChanged()
    } catch (caught) {
      setMessage({ ok: false, text: errorText(caught) })
    }
  }

  return (
    <div className={styles.fieldset}>
      <div className={styles.row}>
        <label className="field">
          Ім'я
          <input value={form.name} maxLength={60} onChange={(e) => change({ ...form, name: e.target.value })} />
        </label>
      </div>
      <div className={styles.emotionGrid}>
        {BUILT_IN_STATES.map(({ key, label }) => (
          <ImageInput
            key={key}
            compact
            label={label}
            value={form.states.find((s) => s.key === key)?.imagePath ?? null}
            onUpload={(file) => studio.uploadImage('characters', file)}
            onChange={(path) => setBuiltIn(key, path)}
          />
        ))}
        {customStates.map(({ state, index }) => (
          <div key={state.id ?? `new-${index}`} className={styles.customState}>
            <div className={styles.customStateName}>
              <input
                placeholder="Назва стану"
                maxLength={30}
                value={state.key}
                aria-label="Назва власного стану"
                onChange={(e) => setCustom(index, { key: e.target.value })}
              />
              <button
                type="button"
                className={styles.iconButton}
                aria-label="Видалити стан"
                title="Видалити стан"
                onClick={() => change({ ...form, states: form.states.filter((_, i) => i !== index) })}
              >
                ✕
              </button>
            </div>
            <ImageInput
              compact
              label=""
              value={state.imagePath}
              onUpload={(file) => studio.uploadImage('characters', file)}
              onChange={(path) => setCustom(index, { imagePath: path })}
            />
          </div>
        ))}
        {form.states.length < MAX_STATES && (
          <div className={styles.imageInput}>
            <span className={`${styles.imageLabel} ${styles.labelSpacer}`} aria-hidden="true">
              Свій стан
            </span>
            <button
              type="button"
              className={styles.addStateTile}
              onClick={() => change({ ...form, states: [...form.states, { id: null, key: '', imagePath: null }] })}
            >
              + Свій стан
            </button>
          </div>
        )}
      </div>
      {problem && <small className="error-text">{problem}</small>}
      <div className={styles.actions}>
        <button type="button" className="btn" disabled={!dirty || !form.name.trim() || !!problem} onClick={save}>
          Зберегти
        </button>
        {message && <span className={message.ok ? 'success-text' : 'error-text'}>{message.text}</span>}
        <button type="button" className={styles.dangerButton} onClick={remove}>
          Видалити
        </button>
      </div>
    </div>
  )
}

export function CastPanel({ story, onChanged }: { story: StoryDetail; onChanged: () => Promise<void> }) {
  const [name, setName] = useState('')
  const [error, setError] = useState<string | null>(null)

  const add = async () => {
    setError(null)
    try {
      await studio.createCharacter(story.id, { name: name.trim(), states: [] })
      setName('')
      await onChanged()
    } catch (caught) {
      setError(errorText(caught))
    }
  }

  return (
    <div className={styles.panel}>
      <h2>Персонажі</h2>
      <p className={styles.hint}>
        Персонаж створюється один раз для всієї історії. У сцені ти обираєш, хто в ній є, у якому стані, і перетягуєш його туди, де він має стояти.
      </p>
      {story.characters.map((character) => (
        <CharacterCard key={character.id} storyId={story.id} character={character} onChanged={onChanged} />
      ))}
      <div className={styles.addRow}>
        <input placeholder="Ім'я нового персонажа" value={name} maxLength={60} onChange={(e) => setName(e.target.value)} />
        <button type="button" className="btn" disabled={!name.trim()} onClick={add}>
          + Додати
        </button>
      </div>
      {error && <p className="error-text">{error}</p>}
    </div>
  )
}
