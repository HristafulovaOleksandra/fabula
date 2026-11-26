import { useRef, useState } from 'react'
import { errorText } from '../../api/studio'
import styles from './Editor.module.css'

interface ImageInputProps {
  value: string | null
  label: string
  onUpload: (file: File) => Promise<string>
  onChange: (path: string | null) => void
  compact?: boolean
}

export function ImageInput({ value, label, onUpload, onChange, compact = false }: ImageInputProps) {
  const input = useRef<HTMLInputElement>(null)
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const pick = async (file: File | undefined) => {
    if (!file) return
    setBusy(true)
    setError(null)
    try {
      onChange(await onUpload(file))
    } catch (caught) {
      setError(errorText(caught))
    } finally {
      setBusy(false)
      if (input.current) input.current.value = ''
    }
  }

  return (
    <div className={`${styles.imageInput} ${compact ? styles.imageCompact : ''}`}>
      {label && <span className={styles.imageLabel}>{label}</span>}
      <button type="button" className={styles.imageBox} onClick={() => input.current?.click()} disabled={busy}>
        {value ? <img src={value} alt="" /> : <span>{busy ? 'Завантаження…' : '+ Додати'}</span>}
      </button>
      {value && (
        <button type="button" className={styles.linkButton} onClick={() => onChange(null)}>
          Прибрати
        </button>
      )}
      <input ref={input} type="file" accept="image/png,image/jpeg,image/webp" hidden onChange={(e) => pick(e.target.files?.[0])} />
      {error && <small className="error-text">{error}</small>}
    </div>
  )
}
