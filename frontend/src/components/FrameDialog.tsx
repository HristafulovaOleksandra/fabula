import { useEffect, useRef, useState, type PointerEvent } from 'react'
import { CENTERED_FRAMING, framedImageStyle, type Framing } from './coverFraming'
import styles from './FrameDialog.module.css'

const clamp = (value: number, min: number, max: number) => Math.min(max, Math.max(min, value))

interface FrameDialogProps {
  title: string
  imagePath: string
  initial: Framing
  round?: boolean
  aspect?: string
  onApply: (framing: Framing) => void
  onClose: () => void
}

export function FrameDialog({ title, imagePath, initial, round = false, aspect, onApply, onClose }: FrameDialogProps) {
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
    drag.current = { x: event.clientX, y: event.clientY, focusX: draft.focusX, focusY: draft.focusY }
    try {
      event.currentTarget.setPointerCapture(event.pointerId)
    } catch {
      return
    }
  }

  const move = (event: PointerEvent<HTMLDivElement>) => {
    if (!drag.current) return
    const box = event.currentTarget.getBoundingClientRect()
    const speed = 100 / draft.zoom
    const origin = drag.current
    setDraft((current) => ({
      ...current,
      focusX: Math.round(clamp(origin.focusX - ((event.clientX - origin.x) / box.width) * speed, 0, 100)),
      focusY: Math.round(clamp(origin.focusY - ((event.clientY - origin.y) / box.height) * speed, 0, 100)),
    }))
  }

  const stop = () => {
    drag.current = null
  }

  return (
    <div className={styles.backdrop} onPointerDown={(e) => e.target === e.currentTarget && onClose()}>
      <div className={`${styles.dialog} ${round ? styles.roundDialog : ''}`} role="dialog" aria-modal="true" aria-label={title}>
        <h3>{title}</h3>
        <div
          className={`${styles.frame} ${round ? styles.round : ''}`}
          style={aspect ? { aspectRatio: aspect } : undefined}
          onPointerDown={start}
          onPointerMove={move}
          onPointerUp={stop}
          onPointerCancel={stop}
        >
          <img src={imagePath} alt="" draggable={false} style={framedImageStyle(draft)} />
        </div>
        <p className={styles.hint}>Перетягни картинку мишкою або пальцем, щоб обрати, яку частину видно.</p>
        <label className={styles.zoom}>
          <span>Масштаб</span>
          <input type="range" min={1} max={3} step={0.05} value={draft.zoom} onChange={(e) => setDraft({ ...draft, zoom: Number(e.target.value) })} />
        </label>
        <div className={styles.actions}>
          <button type="button" className="btn" onClick={() => onApply(draft)}>
            Готово
          </button>
          <button type="button" className="btn btn-ghost" onClick={onClose}>
            Скасувати
          </button>
          <button type="button" className={styles.reset} onClick={() => setDraft(CENTERED_FRAMING)}>
            Скинути
          </button>
        </div>
      </div>
    </div>
  )
}
