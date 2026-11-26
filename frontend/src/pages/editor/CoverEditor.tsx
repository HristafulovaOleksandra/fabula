import { useEffect, useRef, useState, type PointerEvent } from 'react'
import { errorText, studio } from '../../api/studio'
import { coverImageStyle, type CoverFraming } from '../../components/coverFraming'
import styles from './Editor.module.css'

const clamp = (value: number, min: number, max: number) => Math.min(max, Math.max(min, value))
const CENTERED = { coverFocusX: 50, coverFocusY: 50, coverZoom: 1 }

function CoverDialog({ initial, onApply, onClose }: { initial: CoverFraming; onApply: (framing: CoverFraming) => void; onClose: () => void }) {
  const [draft, setDraft] = useState(initial)
  const drag = useRef<{ x: number; y: number; focusX: number; focusY: number } | null>(null)

  useEffect(() => {
    const closeOnEscape = (event: KeyboardEvent) => {
      if (event.key === 'Escape') onClose()
    }
    window.addEventListener('keydown', closeOnEscape)
    return () => window.removeEventListener('keydown', closeOnEscape)
  }, [onClose])

  const start = (event: PointerEvent<HTMLDivElement>) => {
    drag.current = { x: event.clientX, y: event.clientY, focusX: draft.coverFocusX, focusY: draft.coverFocusY }
    try {
      event.currentTarget.setPointerCapture(event.pointerId)
    } catch {
      return
    }
  }

  const move = (event: PointerEvent<HTMLDivElement>) => {
    if (!drag.current) return
    const box = event.currentTarget.getBoundingClientRect()
    const speed = 100 / draft.coverZoom
    const origin = drag.current
    setDraft((current) => ({
      ...current,
      coverFocusX: Math.round(clamp(origin.focusX - ((event.clientX - origin.x) / box.width) * speed, 0, 100)),
      coverFocusY: Math.round(clamp(origin.focusY - ((event.clientY - origin.y) / box.height) * speed, 0, 100)),
    }))
  }

  const stop = () => {
    drag.current = null
  }

  return (
    <div className={styles.dialogBackdrop} onPointerDown={(e) => e.target === e.currentTarget && onClose()}>
      <div className={styles.dialog} role="dialog" aria-modal="true" aria-labelledby="cover-dialog-title">
        <h3 id="cover-dialog-title">Редагувати обкладинку</h3>
        <div className={styles.dialogFrame} onPointerDown={start} onPointerMove={move} onPointerUp={stop} onPointerCancel={stop}>
          <img src={draft.coverImagePath ?? ''} alt="" draggable={false} style={coverImageStyle(draft)} />
        </div>
        <p className={styles.hint}>Перетягни картинку мишкою або пальцем, щоб обрати, яку частину видно.</p>
        <label className={styles.zoom}>
          <span>Масштаб</span>
          <input
            type="range"
            min={1}
            max={3}
            step={0.05}
            value={draft.coverZoom}
            onChange={(e) => setDraft({ ...draft, coverZoom: Number(e.target.value) })}
          />
        </label>
        <div className={styles.actions}>
          <button type="button" className="btn" onClick={() => onApply(draft)}>
            Готово
          </button>
          <button type="button" className="btn btn-ghost" onClick={onClose}>
            Скасувати
          </button>
          <button type="button" className={styles.dangerButton} onClick={() => setDraft({ ...draft, ...CENTERED })}>
            Скинути
          </button>
        </div>
      </div>
    </div>
  )
}

interface CoverEditorProps {
  framing: CoverFraming
  onChange: (framing: CoverFraming) => void
}

export function CoverEditor({ framing, onChange }: CoverEditorProps) {
  const input = useRef<HTMLInputElement>(null)
  const [busy, setBusy] = useState(false)
  const [editing, setEditing] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const pick = async (file: File | undefined) => {
    if (!file) return
    setBusy(true)
    setError(null)
    try {
      const path = await studio.uploadImage('covers', file)
      onChange({ coverImagePath: path, ...CENTERED })
      setEditing(true)
    } catch (caught) {
      setError(errorText(caught))
    } finally {
      setBusy(false)
      if (input.current) input.current.value = ''
    }
  }

  return (
    <div className={styles.imageInput}>
      <span className={styles.imageLabel}>Обкладинка</span>
      {framing.coverImagePath ? (
        <>
          <button type="button" className={styles.coverFrame} onClick={() => setEditing(true)} title="Редагувати обкладинку">
            <img src={framing.coverImagePath} alt="" draggable={false} style={coverImageStyle(framing)} />
          </button>
          <button type="button" className={`btn btn-ghost ${styles.coverEditButton}`} onClick={() => setEditing(true)}>
            Редагувати обкладинку
          </button>
          <div className={styles.coverButtons}>
            <button type="button" className={styles.linkButton} onClick={() => input.current?.click()} disabled={busy}>
              {busy ? 'Завантаження…' : 'Замінити'}
            </button>
            <button type="button" className={styles.linkButton} onClick={() => onChange({ coverImagePath: null, ...CENTERED })}>
              Прибрати
            </button>
          </div>
        </>
      ) : (
        <button type="button" className={styles.coverFrame} onClick={() => input.current?.click()} disabled={busy}>
          <span>{busy ? 'Завантаження…' : '+ Додати'}</span>
        </button>
      )}
      <input ref={input} type="file" accept="image/png,image/jpeg,image/webp" hidden onChange={(e) => pick(e.target.files?.[0])} />
      {error && <small className="error-text">{error}</small>}
      {editing && framing.coverImagePath && (
        <CoverDialog
          initial={framing}
          onClose={() => setEditing(false)}
          onApply={(next) => {
            onChange(next)
            setEditing(false)
          }}
        />
      )}
    </div>
  )
}
