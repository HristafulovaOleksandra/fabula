import type { SaveStatus } from './useAutosave'
import styles from './Editor.module.css'

export function SaveStatusLine({ status, onRetry }: { status: SaveStatus; onRetry: () => void }) {
  switch (status.kind) {
    case 'saved':
      return <span className={`${styles.saveStatus} ${styles.saveOk}`}>Усі зміни збережено</span>
    case 'waiting':
      return <span className={styles.saveStatus}>Є зміни, зберігаємо…</span>
    case 'saving':
      return <span className={styles.saveStatus}>Зберігаємо…</span>
    case 'invalid':
      return <span className={`${styles.saveStatus} ${styles.saveWarn}`}>Не збережено: {status.reason}</span>
    case 'error':
      return (
        <span className={`${styles.saveStatus} ${styles.saveWarn}`}>
          Не вдалося зберегти: {status.reason}{' '}
          <button type="button" className={styles.linkButton} onClick={onRetry}>
            Спробувати ще
          </button>
        </span>
      )
  }
}
