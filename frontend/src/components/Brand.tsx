import catCelebrate from '../assets/brand/cat-celebrate.webp'
import catHappy from '../assets/brand/cat-happy.webp'
import catSad from '../assets/brand/cat-sad.webp'
import catThinking from '../assets/brand/cat-thinking.webp'
import crystal from '../assets/brand/crystal.webp'
import logo from '../assets/brand/logo.webp'
import streakOff from '../assets/brand/streak-off.webp'
import streakOn from '../assets/brand/streak-on.webp'
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

export function Crystals({ amount, size = 18 }: { amount?: number; size?: number }) {
  return (
    <span className={styles.crystals}>
      <img src={crystal} alt={amount === undefined ? 'кристали' : ''} width={size} height={size} className={styles.crystalIcon} />
      {amount !== undefined && <span>{amount}</span>}
      {amount !== undefined && <span className={styles.visuallyHidden}> кристалів</span>}
    </span>
  )
}

export function StreakFlame({ lit, size = 22, days }: { lit: boolean; size?: number; days?: number }) {
  return (
    <span className={styles.flame}>
      <img src={lit ? streakOn : streakOff} alt={days === undefined ? (lit ? 'вогник горить' : 'вогник згас') : ''} height={size} className={styles.flameIcon} />
      {days !== undefined && <span>{days}</span>}
      {days !== undefined && <span className={styles.visuallyHidden}> днів поспіль</span>}
    </span>
  )
}
