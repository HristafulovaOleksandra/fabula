import { useEffect, useState } from 'react'
import { Link, useSearchParams } from 'react-router-dom'
import { api, ApiError } from '../api/client'
import type { PlacementResult, PlacementTest, Tag } from '../api/types'
import { useAuth } from '../auth/useAuth'
import { Mascot } from '../components/Brand'
import styles from './WelcomePage.module.css'

type Step = 'topics' | 'test' | 'result'

function TopicsStep({ test, onDone }: { test: PlacementTest; onDone: () => void }) {
  const { user, refresh } = useAuth()
  const [topics, setTopics] = useState<Tag[]>([])
  const [chosen, setChosen] = useState<string[]>(user?.favoriteTopics ?? [])
  const [error, setError] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)

  useEffect(() => {
    api<Tag[]>('/tags')
      .then((tags) => setTopics(tags.filter((tag) => tag.kind === 'Topic')))
      .catch(() => setTopics([]))
  }, [])

  const toggle = (name: string) =>
    setChosen((list) => (list.includes(name) ? list.filter((item) => item !== name) : list.length >= test.maxTopics ? list : [...list, name]))

  const save = async () => {
    setBusy(true)
    setError(null)
    try {
      await api('/profile/topics', { method: 'PUT', body: { topics: chosen } })
      await refresh()
      onDone()
    } catch (caught) {
      setError(caught instanceof ApiError ? caught.message : 'Не вдалося зберегти теми.')
    } finally {
      setBusy(false)
    }
  }

  const enough = chosen.length >= test.minTopics

  return (
    <section className={`sticker ${styles.card}`}>
      <div className={styles.head}>
        <Mascot mood="happy" size={110} />
        <div>
          <h1>Про що тобі цікаво читати?</h1>
          <p>
            Обери від {test.minTopics} до {test.maxTopics} тем — з них складатиметься добірка «Для тебе».
          </p>
        </div>
      </div>
      <div className={styles.topics}>
        {topics.map((tag) => (
          <button
            key={tag.id}
            type="button"
            className={`${styles.topic} ${chosen.includes(tag.name) ? styles.topicOn : ''}`}
            aria-pressed={chosen.includes(tag.name)}
            disabled={!chosen.includes(tag.name) && chosen.length >= test.maxTopics}
            onClick={() => toggle(tag.name)}
          >
            {tag.name}
          </button>
        ))}
      </div>
      <p className={styles.counter}>
        Обрано: {chosen.length} з {test.maxTopics}
      </p>
      {error && <p className="error-text">{error}</p>}
      <div className={styles.actions}>
        <button type="button" className="btn" disabled={!enough || busy} onClick={save}>
          {enough ? 'Далі' : `Обери ще ${test.minTopics - chosen.length}`}
        </button>
      </div>
    </section>
  )
}

function TestStep({ test, onDone }: { test: PlacementTest; onDone: (result: PlacementResult) => void }) {
  const { refresh } = useAuth()
  const [index, setIndex] = useState(0)
  const [answers, setAnswers] = useState<Record<number, number>>({})
  const [error, setError] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)

  if (test.questions.length === 0) {
    return (
      <section className={`sticker ${styles.card}`}>
        <div className={styles.head}>
          <Mascot mood="thinking" size={110} />
          <div>
            <h1>Тест ще готується</h1>
            <p>Питання вступного тесту скоро з'являться. Поки що можна читати історії будь-якого рівня.</p>
          </div>
        </div>
        <div className={styles.actions}>
          <Link to="/library" className="btn">
            До бібліотеки
          </Link>
        </div>
      </section>
    )
  }

  const question = test.questions[index]
  const last = index === test.questions.length - 1

  const submit = async (final: Record<number, number>) => {
    setBusy(true)
    setError(null)
    try {
      const result = await api<PlacementResult>('/placement', {
        method: 'POST',
        body: { answers: Object.entries(final).map(([questionId, choice]) => ({ questionId: Number(questionId), choice })) },
      })
      await refresh()
      onDone(result)
    } catch (caught) {
      setError(caught instanceof ApiError ? caught.message : 'Не вдалося надіслати відповіді.')
      setBusy(false)
    }
  }

  const answer = (choice: number) => {
    const next = { ...answers, [question.id]: choice }
    setAnswers(next)
    if (last) submit(next)
    else setIndex(index + 1)
  }

  return (
    <section className={`sticker ${styles.card}`}>
      <div className={styles.progress} aria-label={`Питання ${index + 1} з ${test.questions.length}`}>
        <span style={{ width: `${(index / test.questions.length) * 100}%` }} />
      </div>
      <p className={styles.counter}>
        Питання {index + 1} з {test.questions.length}
      </p>
      {question.instruction && <p className={styles.instruction}>{question.instruction}</p>}
      <h2 className={styles.question}>{question.text}</h2>
      <div className={styles.options}>
        {question.options.map((option) => (
          <button
            key={option.id}
            type="button"
            className={`${styles.option} ${answers[question.id] === option.id ? styles.optionChosen : ''}`}
            disabled={busy}
            onClick={() => answer(option.id)}
          >
            {option.text}
          </button>
        ))}
        <button type="button" className={styles.dontKnow} disabled={busy} onClick={() => answer(-1)}>
          Не знаю
        </button>
      </div>
      {error && <p className="error-text">{error}</p>}
      {index > 0 && !busy && (
        <button type="button" className={styles.back} onClick={() => setIndex(index - 1)}>
          ← Попереднє питання
        </button>
      )}
    </section>
  )
}

function ResultStep({ result }: { result: PlacementResult }) {
  return (
    <section className={`sticker ${styles.card} ${styles.result}`}>
      <Mascot mood="celebrate" size={170} />
      <h1>Твій рівень — {result.level}</h1>
      <p>Добірка «Для тебе» тепер складається з історій цього рівня і твоїх улюблених тем. Тест можна пройти ще раз у профілі.</p>
      <ul className={styles.scores}>
        {result.levels.map((score) => (
          <li key={score.level}>
            <b>{score.level}</b>
            <span>
              {score.correct} з {score.total}
            </span>
          </li>
        ))}
      </ul>
      <div className={styles.actions}>
        <Link to="/library" className="btn">
          До бібліотеки
        </Link>
        <Link to="/" className="btn btn-ghost">
          На головну
        </Link>
      </div>
    </section>
  )
}

export function WelcomePage() {
  const { user } = useAuth()
  const [params] = useSearchParams()
  const requested = params.get('step')
  const [step, setStep] = useState<Step>(requested === 'test' || (requested !== 'topics' && (user?.favoriteTopics.length ?? 0) > 0) ? 'test' : 'topics')
  const [test, setTest] = useState<PlacementTest | null>(null)
  const [result, setResult] = useState<PlacementResult | null>(null)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    api<PlacementTest>('/placement')
      .then(setTest)
      .catch(() => setError('Не вдалося завантажити тест.'))
  }, [])

  if (error) return <div className="page error-text">{error}</div>
  if (!test) return <div className="page">Завантажуємо…</div>

  return (
    <div className={`page ${styles.page}`}>
      {step === 'topics' && <TopicsStep test={test} onDone={() => setStep(requested === 'topics' ? 'result' : 'test')} />}
      {step === 'test' && (
        <TestStep
          test={test}
          onDone={(value) => {
            setResult(value)
            setStep('result')
          }}
        />
      )}
      {step === 'result' && result && <ResultStep result={result} />}
      {step === 'result' && !result && (
        <section className={`sticker ${styles.card} ${styles.result}`}>
          <Mascot mood="celebrate" size={150} />
          <h1>Теми збережено</h1>
          <div className={styles.actions}>
            <Link to="/library" className="btn">
              До бібліотеки
            </Link>
            <Link to="/profile" className="btn btn-ghost">
              До профілю
            </Link>
          </div>
        </section>
      )}
      {step !== 'result' && (
        <p className={styles.later}>
          <Link to="/">Пройти пізніше</Link>
        </p>
      )}
    </div>
  )
}
