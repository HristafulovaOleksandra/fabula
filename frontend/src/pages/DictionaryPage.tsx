import { useCallback, useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { api, ApiError } from '../api/client'
import type { QuizAnswer, QuizQuestion, UserWord } from '../api/types'
import { Mascot } from '../components/Brand'
import styles from './DictionaryPage.module.css'

const LEARN_STREAK = 3

type Tab = 'learning' | 'learned'

function wordsLabel(count: number) {
  const lastTwo = count % 100
  const last = count % 10
  if (lastTwo >= 11 && lastTwo <= 14) return `${count} слів`
  if (last === 1) return `${count} слово`
  if (last >= 2 && last <= 4) return `${count} слова`
  return `${count} слів`
}

function Progress({ word }: { word: UserWord }) {
  if (word.learned) return <span className={styles.learnedBadge}>{word.markedKnown ? 'Знаю' : 'Вивчено'}</span>
  return (
    <span className={styles.dots} title={`Правильно поспіль: ${word.correctInRow} з ${LEARN_STREAK}`}>
      {Array.from({ length: LEARN_STREAK }, (_, index) => (
        <span key={index} className={index < word.correctInRow ? styles.dotOn : styles.dot} />
      ))}
    </span>
  )
}

function Quiz({ onDone }: { onDone: () => void }) {
  const [questions, setQuestions] = useState<QuizQuestion[] | null>(null)
  const [index, setIndex] = useState(0)
  const [answer, setAnswer] = useState<{ chosen: string; result: QuizAnswer } | null>(null)
  const [score, setScore] = useState({ correct: 0, learned: [] as string[] })
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    api<QuizQuestion[]>('/words/quiz?count=10')
      .then(setQuestions)
      .catch(() => setError('Не вдалося підготувати тест.'))
  }, [])

  if (error) return <p className="error-text">{error}</p>
  if (!questions) return <p>Готуємо тест…</p>

  if (questions.length === 0) {
    return (
      <section className={`sticker ${styles.card} ${styles.center}`}>
        <Mascot mood="celebrate" size={130} />
        <h2>Усі слова вивчено!</h2>
        <p>Додавай нові слова в програвачі — торкнись будь-якого слова в репліці.</p>
        <button type="button" className="btn" onClick={onDone}>
          До словника
        </button>
      </section>
    )
  }

  if (index >= questions.length) {
    return (
      <section className={`sticker ${styles.card} ${styles.center}`}>
        <Mascot mood={score.correct >= questions.length / 2 ? 'celebrate' : 'thinking'} size={140} />
        <h2>
          Правильно: {score.correct} з {questions.length}
        </h2>
        {score.learned.length > 0 && <p>Вивчено нові слова: {score.learned.join(', ')}</p>}
        <button type="button" className="btn" onClick={onDone}>
          До словника
        </button>
      </section>
    )
  }

  const question = questions[index]
  const toUkrainian = question.direction === 'EnglishToUkrainian'

  const choose = async (option: string) => {
    if (answer) return
    try {
      const result = await api<QuizAnswer>(`/words/${question.wordId}/answer`, {
        method: 'POST',
        body: { direction: question.direction, answer: option },
      })
      setAnswer({ chosen: option, result })
      setScore((value) => ({
        correct: value.correct + (result.correct ? 1 : 0),
        learned: result.learned && result.correct && result.correctInRow === LEARN_STREAK ? [...value.learned, toUkrainian ? question.prompt : result.correctAnswer] : value.learned,
      }))
    } catch (caught) {
      setError(caught instanceof ApiError ? caught.message : 'Не вдалося зберегти відповідь.')
    }
  }

  const optionClass = (option: string) => {
    if (!answer) return styles.option
    if (option === answer.result.correctAnswer) return `${styles.option} ${styles.optionRight}`
    if (option === answer.chosen) return `${styles.option} ${styles.optionWrong}`
    return styles.option
  }

  return (
    <section className={`sticker ${styles.card}`}>
      <div className={styles.progress}>
        <span style={{ width: `${(index / questions.length) * 100}%` }} />
      </div>
      <p className={styles.counter}>
        Слово {index + 1} з {questions.length}
      </p>
      <p className={styles.instruction}>{toUkrainian ? 'Обери переклад' : 'Обери англійське слово'}</p>
      <h2 className={styles.prompt}>{question.prompt}</h2>
      <div className={styles.options}>
        {question.options.map((option) => (
          <button key={option} type="button" className={optionClass(option)} disabled={answer !== null} onClick={() => choose(option)}>
            {option}
          </button>
        ))}
      </div>
      {answer && (
        <div className={styles.feedback}>
          <b className={answer.result.correct ? styles.right : styles.wrong}>
            {answer.result.correct
              ? answer.result.learned
                ? 'Правильно! Слово вивчено'
                : `Правильно! ${answer.result.correctInRow} з ${LEARN_STREAK} поспіль`
              : `Ні, правильно: ${answer.result.correctAnswer}`}
          </b>
          <button
            type="button"
            className="btn"
            onClick={() => {
              setAnswer(null)
              setIndex(index + 1)
            }}
          >
            {index + 1 < questions.length ? 'Далі' : 'Підсумок'}
          </button>
        </div>
      )}
    </section>
  )
}

export function DictionaryPage() {
  const [words, setWords] = useState<UserWord[] | null>(null)
  const [tab, setTab] = useState<Tab>('learning')
  const [quiz, setQuiz] = useState(false)

  const load = useCallback(() => {
    api<UserWord[]>('/words')
      .then(setWords)
      .catch(() => setWords([]))
  }, [])

  useEffect(() => {
    load()
  }, [load])

  const update = (word: UserWord) => setWords((list) => (list ?? []).map((item) => (item.id === word.id ? word : item)))

  const setKnown = async (word: UserWord, known: boolean) => {
    update(await api<UserWord>(`/words/${word.id}/known`, { method: 'PUT', body: { known } }))
  }

  const remove = async (word: UserWord) => {
    if (!window.confirm(`Прибрати «${word.word}» зі словника?`)) return
    await api(`/words/${word.id}`, { method: 'DELETE' })
    setWords((list) => (list ?? []).filter((item) => item.id !== word.id))
  }

  if (quiz) {
    return (
      <div className={`page ${styles.page}`}>
        <Quiz
          onDone={() => {
            setQuiz(false)
            load()
          }}
        />
      </div>
    )
  }

  const learning = (words ?? []).filter((word) => !word.learned)
  const learned = (words ?? []).filter((word) => word.learned)
  const shown = tab === 'learning' ? learning : learned
  const testable = learning.filter((word) => word.translation).length

  return (
    <div className={`page ${styles.page}`}>
      <div className={styles.header}>
        <div>
          <h1>Мій словник</h1>
          <p className={styles.lead}>
            Слова з історій. Слово вивчене після {LEARN_STREAK} правильних відповідей поспіль у тесті — або натисни «Знаю».
          </p>
        </div>
        <button type="button" className="btn" disabled={testable === 0} onClick={() => setQuiz(true)}>
          Тест · {wordsLabel(Math.min(10, testable))}
        </button>
      </div>

      <div className={styles.tabs} role="tablist">
        <button type="button" role="tab" aria-selected={tab === 'learning'} className={tab === 'learning' ? styles.tabOn : ''} onClick={() => setTab('learning')}>
          Вчу ({learning.length})
        </button>
        <button type="button" role="tab" aria-selected={tab === 'learned'} className={tab === 'learned' ? styles.tabOn : ''} onClick={() => setTab('learned')}>
          Вивчені ({learned.length})
        </button>
      </div>

      {words === null ? (
        <p>Завантажуємо…</p>
      ) : shown.length === 0 ? (
        <div className={`sticker ${styles.empty}`}>
          <Mascot mood="thinking" size={110} />
          <p>
            {tab === 'learning'
              ? words.length === 0
                ? 'Словник порожній. Під час читання торкнись будь-якого слова і натисни «Додати в словник».'
                : 'Усі слова вивчено!'
              : 'Ще немає вивчених слів. Пройди тест кілька разів.'}{' '}
            {words.length === 0 && <Link to="/library">До бібліотеки</Link>}
          </p>
        </div>
      ) : (
        <ul className={styles.list}>
          {shown.map((word) => (
            <li key={word.id} className={styles.row}>
              <div className={styles.wordCell}>
                <b>{word.word}</b>
                <span>{word.translation || 'без перекладу'}</span>
                {word.storyTitle && <small>з історії «{word.storyTitle}»</small>}
              </div>
              <Progress word={word} />
              <div className={styles.rowActions}>
                {word.learned ? (
                  <button type="button" className={styles.linkButton} onClick={() => setKnown(word, false)}>
                    Повторити
                  </button>
                ) : (
                  <button type="button" className={styles.linkButton} onClick={() => setKnown(word, true)}>
                    Знаю
                  </button>
                )}
                <button type="button" className={styles.remove} aria-label={`Прибрати ${word.word}`} onClick={() => remove(word)}>
                  ✕
                </button>
              </div>
            </li>
          ))}
        </ul>
      )}
    </div>
  )
}
