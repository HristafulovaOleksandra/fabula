import { Link } from 'react-router-dom'
import { useAuth } from '../auth/useAuth'
import { Mascot } from '../components/Brand'
import styles from './HomePage.module.css'

export function HomePage() {
  const { user } = useAuth()

  return (
    <div className="page">
      <section className={styles.hero}>
        <div>
          <h1>Історії, які ти розумієш</h1>
          <p className={styles.lead}>
            Короткі візуальні новели англійською на твоєму рівні. Ти не відповідаєш на питання після тексту — ти обираєш, що
            зробить герой. Що краще розумієш, то цікавіший поворот.
          </p>
          <div className={styles.actions}>
            <Link to="/library" className="btn">
              Почати історію
            </Link>
            {!user && (
              <Link to="/register" className="btn btn-ghost">
                Створити акаунт
              </Link>
            )}
          </div>
        </div>
        <Mascot mood="happy" size={240} className={styles.cat} />
      </section>

      <section className={styles.features}>
        <article className="sticker">
          <h3>Рівень під тебе</h3>
          <p>Кожну репліку перевіряє модель рівня CEFR, тож позначка «A2» на історії справді означає A2.</p>
        </article>
        <article className="sticker">
          <h3>Нові слова в сюжеті</h3>
          <p>Торкнись підсвіченого слова — побачиш переклад, а слово потрапить у твій словник.</p>
        </article>
        <article className="sticker">
          <h3>Для викладачів</h3>
          <p>Створюй історії для своїх учнів і призначай їх як домашнє завдання.</p>
        </article>
      </section>
    </div>
  )
}
