import catCelebrate from '../assets/brand/cat-celebrate.webp'
import catHappy from '../assets/brand/cat-happy.webp'
import catSad from '../assets/brand/cat-sad.webp'
import catThinking from '../assets/brand/cat-thinking.webp'
import logo from '../assets/brand/logo.webp'
import styles from './Brand.module.css'

export type MascotMood = 'happy' | 'thinking' | 'sad' | 'celebrate'

const mascots: Record<MascotMood, { src: string; alt: string }> = {
  happy: { src: catHappy, alt: 'Кіт Fabula вітається' },
  thinking: { src: catThinking, alt: 'Кіт Fabula задумався' },
  sad: { src: catSad, alt: 'Кіт Fabula засмутився' },
  celebrate: { src: catCelebrate, alt: 'Кіт Fabula святкує' },
}

export function Mascot({ mood = 'happy', size = 180, className }: { mood?: MascotMood; size?: number; className?: string }) {
  const image = mascots[mood]
  return <img src={image.src} alt={image.alt} width={size} className={`${styles.mascot} ${className ?? ''}`} />
}

export function Logo() {
  return <img src={logo} alt="Fabula" className={styles.logo} />
}
