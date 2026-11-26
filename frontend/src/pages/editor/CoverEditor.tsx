import { useRef, useState } from 'react'
import { errorText, studio } from '../../api/studio'
import { coverImageStyle, type CoverFraming } from '../../components/coverFraming'
import { FrameDialog } from '../../components/FrameDialog'
import styles from './Editor.module.css'

const CENTERED = { coverFocusX: 50, coverFocusY: 50, coverZoom: 1 }

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
        <FrameDialog
          title="Редагувати обкладинку"
          imagePath={framing.coverImagePath}
          initial={{ focusX: framing.coverFocusX, focusY: framing.coverFocusY, zoom: framing.coverZoom }}
          onClose={() => setEditing(false)}
          onApply={(next) => {
            onChange({ coverImagePath: framing.coverImagePath, coverFocusX: next.focusX, coverFocusY: next.focusY, coverZoom: next.zoom })
            setEditing(false)
          }}
        />
      )}
    </div>
  )
}
