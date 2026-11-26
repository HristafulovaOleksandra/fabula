import { useEffect, useRef, useState, type FormEvent } from 'react'
import { DEFAULT_STORY_TITLE, errorText, studio, type StoryForm } from '../../api/studio'
import type { CefrLevel, Economy, StoryDetail, Tag } from '../../api/types'
import { Crystals } from '../../components/Brand'
import { AutoGrowTextarea } from './AutoGrowTextarea'
import { CoverEditor } from './CoverEditor'
import { PriceInput } from './PriceInput'
import { SaveStatusLine } from './SaveStatusLine'
import { useAutosave } from './useAutosave'
import styles from './Editor.module.css'

const LEVELS: CefrLevel[] = ['A1', 'A2', 'B1', 'B2', 'C']
const STORY_PRICE_STEP = 25
const KIND_LABELS: Record<Tag['kind'], { title: string; add: string }> = {
  Genre: { title: 'Жанр', add: '+ Свій жанр' },
  Topic: { title: 'Тема', add: '+ Своя тема' },
}

function toStoryForm(story: StoryDetail): StoryForm {
  return {
    title: story.title === DEFAULT_STORY_TITLE ? '' : story.title,
    description: story.description,
    coverImagePath: story.coverImagePath,
    coverFocusX: story.coverFocusX,
    coverFocusY: story.coverFocusY,
    coverZoom: story.coverZoom,
    level: story.level,
    priceCrystals: story.priceCrystals,
    tagIds: story.tags.map((t) => t.id),
    words: story.words,
    startSceneId: story.startSceneId,
  }
}

function TagAdder({ kind, onCreate }: { kind: Tag['kind']; onCreate: (name: string, kind: Tag['kind']) => Promise<void> }) {
  const [open, setOpen] = useState(false)
  const [name, setName] = useState('')
  const [error, setError] = useState<string | null>(null)

  const submit = async () => {
    if (name.trim().length < 2) return
    try {
      await onCreate(name, kind)
      setName('')
      setOpen(false)
      setError(null)
    } catch (caught) {
      setError(errorText(caught))
    }
  }

  if (!open) {
    return (
      <button type="button" className={`${styles.chip} ${styles.chipAdd}`} onClick={() => setOpen(true)}>
        {KIND_LABELS[kind].add}
      </button>
    )
  }

  return (
    <span className={styles.tagAdder}>
      <input
        autoFocus
        maxLength={30}
        placeholder="Назва"
        value={name}
        onChange={(e) => setName(e.target.value)}
        onKeyDown={(e) => {
          if (e.key === 'Enter') {
            e.preventDefault()
            submit()
          }
          if (e.key === 'Escape') setOpen(false)
        }}
      />
      <button type="button" className={styles.iconButton} aria-label="Додати тег" onClick={submit}>
        ✓
      </button>
      <button type="button" className={styles.iconButton} aria-label="Скасувати" onClick={() => setOpen(false)}>
        ✕
      </button>
      {error && <small className="error-text">{error}</small>}
    </span>
  )
}

function storyPrices(economy: Economy) {
  const steps = Math.floor((economy.storyPriceMax - economy.storyPriceMin) / STORY_PRICE_STEP)
  return [0, ...Array.from({ length: steps + 1 }, (_, i) => economy.storyPriceMin + i * STORY_PRICE_STEP)]
}

interface StoryPanelProps {
  story: StoryDetail
  economy: Economy
  tags: Tag[]
  onTagCreated: (tag: Tag) => void
  onSaved: () => Promise<void>
  onDirtyChange: (dirty: boolean) => void
  onDelete: () => void
}

export function StoryPanel({ story, economy, tags, onTagCreated, onSaved, onDirtyChange, onDelete }: StoryPanelProps) {
  const [baseline, setBaseline] = useState(() => toStoryForm(story))
  const [form, setForm] = useState(baseline)
  const [suggestions, setSuggestions] = useState<Record<string, string[]>>({})
  const latest = useRef(form)
  const autoTranslations = useRef(new Map<number, string>())
  const prices = storyPrices(economy)
  const [customPrice, setCustomPrice] = useState(() => !storyPrices(economy).includes(baseline.priceCrystals))

  const autosave = useAutosave({
    value: form,
    baseline,
    problem: null,
    save: async (sent) => {
      await studio.saveStory(story.id, {
        ...sent,
        title: sent.title.trim() || DEFAULT_STORY_TITLE,
        words: sent.words.filter((w) => w.word.trim() && w.translation.trim()),
      })
      setBaseline(sent)
      await onSaved()
    },
  })

  useEffect(() => {
    onDirtyChange(autosave.dirty)
  }, [autosave.dirty, onDirtyChange])

  const update = (next: StoryForm) => {
    latest.current = next
    setForm(next)
  }

  const save = (event: FormEvent) => {
    event.preventDefault()
    autosave.saveNow()
  }

  const toggleTag = (id: number) =>
    update({ ...form, tagIds: form.tagIds.includes(id) ? form.tagIds.filter((t) => t !== id) : [...form.tagIds, id] })

  const createTag = async (name: string, kind: Tag['kind']) => {
    const tag = await studio.createTag(name, kind)
    onTagCreated(tag)
    const current = latest.current
    if (!current.tagIds.includes(tag.id)) update({ ...current, tagIds: [...current.tagIds, tag.id] })
  }

  const setWord = (index: number, patch: Partial<StoryForm['words'][number]>) => {
    const current = latest.current
    update({ ...current, words: current.words.map((w, i) => (i === index ? { ...w, ...patch } : w)) })
  }

  const translate = async (index: number) => {
    const entry = latest.current.words[index]
    const word = entry?.word.trim()
    if (!word) return
    const canReplace = !entry.translation.trim() || entry.translation === autoTranslations.current.get(index)
    try {
      const translations = await studio.translate(word)
      setSuggestions((all) => ({ ...all, [word.toLowerCase()]: translations }))
      const fresh = latest.current.words[index]
      if (canReplace && translations.length > 0 && fresh?.word.trim() === word) {
        autoTranslations.current.set(index, translations[0])
        setWord(index, { translation: translations[0] })
      }
    } catch {
      setSuggestions((all) => ({ ...all, [word.toLowerCase()]: [] }))
    }
  }

  const removeWord = (index: number) => {
    const shifted = new Map<number, string>()
    autoTranslations.current.forEach((value, key) => {
      if (key < index) shifted.set(key, value)
      if (key > index) shifted.set(key - 1, value)
    })
    autoTranslations.current = shifted
    update({ ...form, words: form.words.filter((_, i) => i !== index) })
  }

  const pendingSelected = tags.filter((t) => t.status === 'Pending' && form.tagIds.includes(t.id))

  return (
    <form className={styles.panel} onSubmit={save}>
      <h2>Про історію</h2>
      <div className={styles.twoColumns}>
        <div className={styles.stack}>
          <label className="field">
            Назва
            <input maxLength={200} placeholder={DEFAULT_STORY_TITLE} value={form.title} onChange={(e) => update({ ...form, title: e.target.value })} />
          </label>
          <label className="field">
            Опис
            <AutoGrowTextarea
              rows={3}
              maxLength={2000}
              placeholder="Про що ця історія — коротко, щоб учневі захотілося її пройти"
              value={form.description}
              onChange={(e) => update({ ...form, description: e.target.value })}
            />
          </label>
          <div className={styles.row}>
            <label className="field">
              Рівень
              <select value={form.level} onChange={(e) => update({ ...form, level: e.target.value as CefrLevel })}>
                {LEVELS.map((level) => (
                  <option key={level}>{level}</option>
                ))}
              </select>
            </label>
            <label className="field">
              <span className={styles.labelWithIcon}>
                Ціна <Crystals size={16} />
              </span>
              {customPrice ? (
                <span className={styles.customPrice}>
                  <PriceInput
                    autoFocus
                    label="Своя ціна історії в кристалах"
                    value={form.priceCrystals}
                    min={economy.storyPriceMin}
                    max={economy.storyPriceMax}
                    onChange={(price) => update({ ...latest.current, priceCrystals: price })}
                  />
                  <button
                    type="button"
                    className={styles.iconButton}
                    aria-label="Обрати ціну зі списку"
                    title="Обрати ціну зі списку"
                    onClick={() => {
                      setCustomPrice(false)
                      if (!prices.includes(form.priceCrystals)) update({ ...form, priceCrystals: 0 })
                    }}
                  >
                    ✕
                  </button>
                </span>
              ) : (
                <select
                  value={form.priceCrystals}
                  onChange={(e) => {
                    if (e.target.value === 'custom') {
                      setCustomPrice(true)
                      return
                    }
                    update({ ...form, priceCrystals: Number(e.target.value) })
                  }}
                >
                  {prices.map((price) => (
                    <option key={price} value={price}>
                      {price === 0 ? 'Free' : price}
                    </option>
                  ))}
                  <option value="custom">Своя ціна…</option>
                </select>
              )}
            </label>
            <label className="field">
              Початкова сцена
              <select value={form.startSceneId ?? ''} onChange={(e) => update({ ...form, startSceneId: e.target.value || null })}>
                {!form.startSceneId && <option value="">—</option>}
                {story.scenes.map((scene) => (
                  <option key={scene.id} value={scene.id}>
                    {scene.title}
                  </option>
                ))}
              </select>
            </label>
          </div>
        </div>
        <CoverEditor
          framing={form}
          onChange={(framing) => update({ ...latest.current, ...framing })}
        />
      </div>

      <fieldset className={styles.fieldset}>
        <legend>Теги</legend>
        {(['Genre', 'Topic'] as const).map((kind) => (
          <div key={kind} className={styles.chips}>
            <span className={styles.chipsLabel}>{KIND_LABELS[kind].title}</span>
            {tags
              .filter((tag) => tag.kind === kind)
              .map((tag) => (
                <button
                  type="button"
                  key={tag.id}
                  title={tag.status === 'Pending' ? 'Власний тег, чекає на перевірку модератором' : undefined}
                  className={`${styles.chip} ${form.tagIds.includes(tag.id) ? styles.chipOn : ''} ${tag.status === 'Pending' ? styles.chipPending : ''}`}
                  onClick={() => toggleTag(tag.id)}
                >
                  {tag.name}
                </button>
              ))}
            <TagAdder kind={kind} onCreate={createTag} />
          </div>
        ))}
        {pendingSelected.length > 0 && (
          <small className={styles.hint}>
            Пунктиром позначено власні теги: історія вже може їх мати, але в бібліотеці вони з'являться після перевірки модератором.
          </small>
        )}
      </fieldset>

      <fieldset className={styles.fieldset}>
        <legend>Цільові слова ({form.words.length})</legend>
        {form.words.map((word, index) => {
          const options = suggestions[word.word.trim().toLowerCase()] ?? []
          return (
            <div key={index} className={styles.wordRow}>
              <input
                placeholder="слово"
                value={word.word}
                maxLength={64}
                onChange={(e) => setWord(index, { word: e.target.value })}
                onBlur={() => translate(index)}
              />
              <input
                placeholder="переклад"
                list={`translations-${index}`}
                value={word.translation}
                maxLength={128}
                onChange={(e) => {
                  autoTranslations.current.delete(index)
                  setWord(index, { translation: e.target.value })
                }}
              />
              <datalist id={`translations-${index}`}>
                {options.map((option) => (
                  <option key={option} value={option} />
                ))}
              </datalist>
              <button type="button" className={styles.iconButton} aria-label="Видалити слово" onClick={() => removeWord(index)}>
                ✕
              </button>
            </div>
          )
        })}
        <button type="button" className="btn btn-ghost" onClick={() => update({ ...form, words: [...form.words, { word: '', translation: '' }] })}>
          + Слово
        </button>
      </fieldset>

      {story.allowedWords.length > 0 && (
        <fieldset className={styles.fieldset}>
          <legend>Дозволені слова ({story.allowedWords.length})</legend>
          <small className={styles.hint}>Ці слова в репліках цієї історії не підсвічуються як складні.</small>
          <div className={styles.chips}>
            {story.allowedWords.map((word) => (
              <span key={word} className={styles.allowedChip}>
                {word}
                <button
                  type="button"
                  aria-label={`Знову підсвічувати «${word}»`}
                  title="Знову підсвічувати це слово"
                  onClick={() => {
                    studio
                      .disallowWord(story.id, word)
                      .then(onSaved)
                      .catch(() => undefined)
                  }}
                >
                  ✕
                </button>
              </span>
            ))}
          </div>
        </fieldset>
      )}

      <div className={styles.actions}>
        <button className="btn">
          Зберегти зараз
        </button>
        <SaveStatusLine status={autosave.status} onRetry={autosave.saveNow} />
        <button type="button" className={styles.dangerButton} onClick={onDelete}>
          Видалити історію
        </button>
      </div>
    </form>
  )
}
