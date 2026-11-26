import type { StreakDayState, Wallet } from '../api/types'
import { daysLabel } from '../api/wallet'
import { Crystals, StreakFlame } from './Brand'
import styles from './StreakWeek.module.css'

const WEEKDAYS = ['Пн', 'Вт', 'Ср', 'Чт', 'Пт', 'Сб', 'Нд']

const DAY_TITLES: Record<StreakDayState, string> = {
  Done: 'Ціль виконано',
  Rescued: 'Пропущений день, серію відновлено',
  Missed: 'Пропущено',
  Today: 'Сьогодні',
  Future: 'Ще попереду',
}

export function StreakWeek({ wallet }: { wallet: Wallet }) {
  const { streak } = wallet
  return (
    <div className={styles.streakCard}>
      <div className={styles.main}>
        <StreakFlame lit={streak.playedToday} size={72} />
        <div>
          <b className={styles.days}>{daysLabel(streak.current)}</b>
          <span className={styles.caption}>щоденна серія</span>
        </div>
      </div>
      <ol className={styles.week}>
        {wallet.week.map((day, index) => (
          <li
            key={day.date}
            className={`${styles.day} ${styles[day.state.toLowerCase()]} ${day.isToday ? styles.today : ''}`}
            title={DAY_TITLES[day.state]}
          >
            <StreakFlame lit={day.state === 'Done' || day.state === 'Rescued'} size={30} />
            <span>{WEEKDAYS[index]}</span>
          </li>
        ))}
      </ol>
      <p className={styles.goal}>
        {streak.playedToday ? (
          'Сьогоднішню ціль виконано — вогник горить.'
        ) : (
          <>
            Пройди одну історію сьогодні, щоб вогник загорівся{' '}
            <span className={styles.reward}>
              +<Crystals amount={wallet.dailyGoalReward} />
            </span>
          </>
        )}
      </p>
      {streak.longest > 0 && <p className={styles.longest}>Найдовша серія: {daysLabel(streak.longest)}</p>}
    </div>
  )
}
