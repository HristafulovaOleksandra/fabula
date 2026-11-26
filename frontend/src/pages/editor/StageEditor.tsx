import { useRef, useState, type PointerEvent } from 'react'
import type { ScenePlacement } from '../../api/studio'
import type { CharacterPosition, StoryCharacter } from '../../api/types'
import { DEFAULT_X, placementStyle, STAGE_MAX_Y } from '../../components/stageLayout'
import { availableStates, stateImage, stateLabel } from './characterStates'
import styles from './Editor.module.css'

const SLOTS: { position: CharacterPosition; label: string }[] = [
  { position: 'Left', label: 'Персонаж 1' },
  { position: 'Right', label: 'Персонаж 2' },
]

const clamp = (value: number, min: number, max: number) => Math.min(max, Math.max(min, value))

interface StageEditorProps {
  cast: StoryCharacter[]
  backgroundPath: string | null
  placements: ScenePlacement[]
  onChange: (placements: ScenePlacement[]) => void
}

export function StageEditor({ cast, backgroundPath, placements, onChange }: StageEditorProps) {
  const stage = useRef<HTMLDivElement>(null)
  const drag = useRef<{ position: CharacterPosition; x: number; y: number; startX: number; startY: number } | null>(null)
  const [selected, setSelected] = useState<CharacterPosition | null>(null)

  const patch = (position: CharacterPosition, changes: Partial<ScenePlacement>) =>
    onChange(placements.map((p) => (p.position === position ? { ...p, ...changes } : p)))

  const choose = (position: CharacterPosition, storyCharacterId: string) => {
    const others = placements.filter((p) => p.position !== position)
    if (!storyCharacterId) {
      onChange(others)
      return
    }
    const current = placements.find((p) => p.position === position)
    onChange([
      ...others,
      current
        ? { ...current, storyCharacterId, emotion: 'neutral' }
        : { position, storyCharacterId, emotion: 'neutral', x: DEFAULT_X[position], y: 0, scale: 1, flip: false },
    ])
  }

  const start = (event: PointerEvent<HTMLElement>, placement: ScenePlacement) => {
    event.preventDefault()
    setSelected(placement.position)
    drag.current = { position: placement.position, x: placement.x, y: placement.y, startX: event.clientX, startY: event.clientY }
    try {
      event.currentTarget.setPointerCapture(event.pointerId)
    } catch {
      return
    }
  }

  const move = (event: PointerEvent<HTMLElement>) => {
    const current = drag.current
    const box = stage.current?.getBoundingClientRect()
    if (!current || !box) return
    patch(current.position, {
      x: Math.round(clamp(current.x + ((event.clientX - current.startX) / box.width) * 100, 0, 100)),
      y: Math.round(clamp(current.y - ((event.clientY - current.startY) / box.height) * 100, 0, STAGE_MAX_Y)),
    })
  }

  const stop = () => {
    drag.current = null
  }

  return (
    <div className={styles.stageEditor}>
      <div
        ref={stage}
        className={styles.stagePreview}
        style={backgroundPath ? { backgroundImage: `url(${backgroundPath})` } : undefined}
        onPointerDown={(e) => e.target === e.currentTarget && setSelected(null)}
      >
        {placements.map((placement) => {
          const character = cast.find((c) => c.id === placement.storyCharacterId)
          if (!character) return null
          const image = stateImage(character, placement.emotion)
          return (
            <figure
              key={placement.position}
              className={`${styles.stageFigure} ${selected === placement.position ? styles.stageFigureSelected : ''}`}
              style={placementStyle(placement)}
              onPointerDown={(e) => start(e, placement)}
              onPointerMove={move}
              onPointerUp={stop}
              onPointerCancel={stop}
              title="Перетягни, щоб змістити персонажа"
            >
              {image ? (
                <img src={image} alt="" draggable={false} style={placement.flip ? { transform: 'scaleX(-1)' } : undefined} />
              ) : (
                <span className={styles.stagePlaceholder} />
              )}
              <figcaption>{character.name}</figcaption>
            </figure>
          )
        })}
      </div>
      <p className={styles.hint}>Перетягни персонажа мишкою або пальцем. Так само він стоятиме в програвачі над вікном діалогу.</p>

      <div className={styles.slots}>
        {SLOTS.map(({ position, label }) => {
          const current = placements.find((p) => p.position === position)
          const character = cast.find((c) => c.id === current?.storyCharacterId)
          const taken = placements.find((p) => p.position !== position)?.storyCharacterId
          return (
            <div
              key={position}
              className={`${styles.slot} ${selected === position ? styles.slotSelected : ''}`}
              onFocus={() => current && setSelected(position)}
            >
              <span className={styles.imageLabel}>{label}</span>
              <select value={current?.storyCharacterId ?? ''} onChange={(e) => choose(position, e.target.value)}>
                <option value="">Нікого</option>
                {cast
                  .filter((c) => c.id !== taken)
                  .map((c) => (
                    <option key={c.id} value={c.id}>
                      {c.name}
                    </option>
                  ))}
              </select>
              {current && character && (
                <>
                  <select aria-label="Стан персонажа" value={current.emotion} onChange={(e) => patch(position, { emotion: e.target.value })}>
                    {availableStates(character).map((key) => (
                      <option key={key} value={key}>
                        {stateLabel(key)}
                      </option>
                    ))}
                  </select>
                  <label className={styles.zoom}>
                    <span>Розмір</span>
                    <input
                      type="range"
                      min={0.4}
                      max={2}
                      step={0.05}
                      value={current.scale}
                      onChange={(e) => patch(position, { scale: Number(e.target.value) })}
                    />
                  </label>
                  <div className={styles.slotButtons}>
                    <button type="button" className={styles.linkButton} onClick={() => patch(position, { flip: !current.flip })}>
                      Віддзеркалити
                    </button>
                    <button
                      type="button"
                      className={styles.linkButton}
                      onClick={() => patch(position, { x: DEFAULT_X[position], y: 0, scale: 1 })}
                    >
                      Скинути положення
                    </button>
                  </div>
                </>
              )}
            </div>
          )
        })}
      </div>
    </div>
  )
}
