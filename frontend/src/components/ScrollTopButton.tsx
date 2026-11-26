import { useEffect, useState } from 'react'
import arrowUp from '../assets/brand/arrow-up.webp'
import styles from './ScrollTopButton.module.css'

const SHOW_AFTER_PX = 400

export function ScrollTopButton() {
  const [visible, setVisible] = useState(false)

  useEffect(() => {
    const update = () => setVisible(window.scrollY > SHOW_AFTER_PX)
    update()
    window.addEventListener('scroll', update, { passive: true })
    return () => window.removeEventListener('scroll', update)
  }, [])

  if (!visible) return null
  return (
    <button
      type="button"
      className={styles.button}
      aria-label="Вгору сторінки"
      title="Вгору сторінки"
      onClick={() => window.scrollTo({ top: 0, behavior: 'smooth' })}
    >
      <img src={arrowUp} alt="" />
    </button>
  )
}
