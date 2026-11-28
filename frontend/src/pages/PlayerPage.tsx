import { useCallback, useEffect, useMemo, useRef, useState, type KeyboardEvent, type MouseEvent } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { api, ApiError } from '../api/client'
import type { Character, ChoiceResult, PlaySession, PlaySummary, Scene, StoryDetail, StoryWord, Wallet } from '../api/types'
import { notifyWalletChanged, REASON_LABELS } from '../api/wallet'
import { Crystals, Mascot, StreakFlame } from '../components/Brand'
import { COVER_COLORS } from '../components/coverColors'
import { framedImageStyle, sceneBackgroundFraming } from '../components/coverFraming'
import { placementStyle } from '../components/stageLayout'
import { RateStory } from '../components/StarRating'
import { findTargetWords } from '../components/targetWords'
import styles from './PlayerPage.module.css'

type Phase = 'loading' | 'lines' | 'choices' | 'summary' | 'error'

interface Segment {
  text: string
  word?: StoryWord
  plain?: boolean
}

interface WordPopup {
  word: string
  translations: string[]
  target: boolean
  status: 'loading' | 'ready' | 'saving' | 'saved' | 'error'
}

const ENGLISH_WORD = /[A-Za-z]+(?:['’][A-Za-z]+)*/g

function splitPlain(text: string): Segment[] {
  const segments: Segment[] = []
  let last = 0
  for (const match of text.matchAll(ENGLISH_WORD)) {
    const start = match.index ?? 0
    if (start > last) segments.push({ text: text.slice(last, start) })
    segments.push({ text: match[0], plain: true })
    last = start + match[0].length
  }
  if (last < text.length) segments.push({ text: text.slice(last) })
  return segments
}

function splitByWords(text: string, words: StoryWord[]): Segment[] {
  const segments: Segment[] = []
  let last = 0
  for (const match of findTargetWords(text, words.map((w) => w.word))) {
    if (match.start > last) segments.push(...splitPlain(text.slice(last, match.start)))
    segments.push({ text: text.slice(match.start, match.end), word: words.find((w) => w.word.trim() === match.word) })
    last = match.end
  }
  if (last < text.length) segments.push(...splitPlain(text.slice(last)))
  return segments
}

function CharacterFigure({ character, active }: { character: Character; active: boolean }) {
  return (
    <figure
      className={`${styles.character} ${character.imagePath ? '' : styles.noImage} ${active ? styles.speaking : styles.listening}`}
      style={placementStyle(character)}
    >
      {character.imagePath && (
        <img src={character.imagePath} alt={character.name ?? ''} style={character.flip ? { transform: 'scaleX(-1)' } : undefined} />
      )}
      <figcaption>{character.name}</figcaption>
    </figure>
  )
}

export function PlayerPage() {
  const { storyId } = useParams()
  const navigate = useNavigate()
  const [story, setStory] = useState<StoryDetail | null>(null)
  const [session, setSession] = useState<PlaySession | null>(null)
  const [sceneId, setSceneId] = useState<string | null>(null)
  const [lineIndex, setLineIndex] = useState(0)
  const [phase, setPhase] = useState<Phase>('loading')
  const [summary, setSummary] = useState<PlaySummary | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [popup, setPopup] = useState<WordPopup | null>(null)
  const [busy, setBusy] = useState(false)
  const [bought, setBought] = useState<Set<string>>(new Set())
  const [pending, setPending] = useState<string | null>(null)
  const [choiceError, setChoiceError] = useState<string | null>(null)
  const [balance, setBalance] = useState<number | null>(null)
  const started = useRef(false)

  useEffect(() => {
    if (started.current) return
    started.current = true
    Promise.all([api<StoryDetail>(`/stories/${storyId}`), api<PlaySession>(`/play/${storyId}/start`, { method: 'POST' })])
      .then(([loadedStory, loadedSession]) => {
        loadedStory.scenes.forEach((s) => {
          if (s.backgroundImagePath) new Image().src = s.backgroundImagePath
        })
        setStory(loadedStory)
        setSession(loadedSession)
        setBought(new Set(loadedSession.purchasedChoiceIds))
        api<Wallet>('/wallet')
          .then((wallet) => setBalance(wallet.balance))
          .catch(() => setBalance(null))
        setSceneId(loadedSession.currentSceneId)
        const first = loadedStory.scenes.find((s) => s.id === loadedSession.currentSceneId)
        setPhase(first && first.lines.length === 0 && first.choices.length > 0 ? 'choices' : 'lines')
      })
      .catch((caught) => {
        setError(caught instanceof ApiError ? caught.message : 'Не вдалося почати історію.')
        setPhase('error')
      })
  }, [storyId])

  const scene: Scene | undefined = useMemo(() => story?.scenes.find((s) => s.id === sceneId), [story, sceneId])
  const line = scene?.lines[lineIndex]

  const finish = useCallback(async () => {
    if (!session) return
    setBusy(true)
    try {
      setSummary(await api<PlaySummary>(`/play/sessions/${session.sessionId}/finish`, { method: 'POST' }))
      setPhase('summary')
      notifyWalletChanged()
    } catch (caught) {
      setError(caught instanceof ApiError ? caught.message : 'Не вдалося завершити історію.')
      setPhase('error')
    } finally {
      setBusy(false)
    }
  }, [session])

  const advance = useCallback(() => {
    if (!scene || phase !== 'lines' || popup || busy) return
    if (lineIndex < scene.lines.length - 1) {
      setLineIndex(lineIndex + 1)
    } else if (scene.choices.length > 0) {
      setPhase('choices')
    } else {
      finish()
    }
  }, [scene, phase, popup, busy, lineIndex, finish])

  const choose = async (choiceId: string | null) => {
    if (!session || !choiceId || busy) return
    setBusy(true)
    setChoiceError(null)
    try {
      let result: ChoiceResult
      try {
        result = await api<ChoiceResult>(`/play/sessions/${session.sessionId}/choice`, { method: 'POST', body: { choiceId } })
      } catch (caught) {
        if (caught instanceof ApiError && caught.status === 409) {
          setChoiceError(caught.message)
          setPending(null)
          return
        }
        throw caught
      }
      setPending(null)
      if (result.spent > 0) {
        setBought((set) => new Set(set).add(choiceId))
        notifyWalletChanged()
      }
      setBalance(result.balance)
      if (result.finished || !result.nextSceneId) {
        await finish()
      } else {
        const next = story?.scenes.find((s) => s.id === result.nextSceneId)
        setSceneId(result.nextSceneId)
        setLineIndex(0)
        if (next && next.lines.length === 0 && next.choices.length === 0) await finish()
        else setPhase(next && next.lines.length === 0 ? 'choices' : 'lines')
      }
    } catch (caught) {
      setError(caught instanceof ApiError ? caught.message : 'Не вдалося зробити вибір.')
      setPhase('error')
    } finally {
      setBusy(false)
    }
  }

  const openWord = async (event: MouseEvent | KeyboardEvent, word: StoryWord) => {
    event.stopPropagation()
    const base = { word: word.word, translations: [word.translation], target: true }
    setPopup({ ...base, status: 'saving' })
    try {
      await api('/words', { method: 'POST', body: { word: word.word, translation: word.translation, storyId } })
      setPopup({ ...base, status: 'saved' })
    } catch {
      setPopup({ ...base, status: 'error' })
    }
  }

  const openPlainWord = async (event: MouseEvent, text: string) => {
    event.stopPropagation()
    setPopup({ word: text, translations: [], target: false, status: 'loading' })
    try {
      const found = await api<{ word: string; translations: string[] }>(`/dictionary?word=${encodeURIComponent(text)}`)
      setPopup({ word: found.word, translations: found.translations.slice(0, 3), target: false, status: 'ready' })
    } catch {
      setPopup({ word: text, translations: [], target: false, status: 'error' })
    }
  }

  const addToDictionary = async () => {
    if (!popup || popup.translations.length === 0) return
    setPopup({ ...popup, status: 'saving' })
    try {
      await api('/words', { method: 'POST', body: { word: popup.word, translation: popup.translations[0], storyId } })
      setPopup({ ...popup, status: 'saved' })
    } catch {
      setPopup({ ...popup, status: 'error' })
    }
  }

  const onKey = (event: KeyboardEvent) => {
    if (event.key === 'Escape' && popup) setPopup(null)
    else if ((event.key === ' ' || event.key === 'Enter' || event.key === 'ArrowRight') && event.target === event.currentTarget) {
      event.preventDefault()
      advance()
    }
  }

  if (phase === 'error') {
    return (
      <div className={styles.center}>
        <Mascot mood="sad" size={160} />
        <p className="error-text">{error}</p>
        <Link to={`/stories/${storyId}`} className="btn btn-ghost">
          Назад до історії
        </Link>
      </div>
    )
  }

  if (phase === 'summary' && summary && story) {
    return (
      <div className={styles.center}>
        <Mascot mood="celebrate" size={190} />
        <h1>Історію пройдено!</h1>
        <p className={styles.storyName}>{story.title}</p>
        <div className={styles.stats}>
          <div className="sticker">
            <b>{summary.scenesVisited}</b>
            <span>сцен</span>
          </div>
          <div className="sticker">
            <b>{summary.choicesMade}</b>
            <span>виборів</span>
          </div>
          <div className="sticker">
            <b>{summary.wordsSaved.length}</b>
            <span>слів у словнику</span>
          </div>
          <div className="sticker">
            <b>{Math.max(1, Math.round(summary.minutes))}</b>
            <span>хв</span>
          </div>
        </div>
        <div className={`sticker ${styles.rewards}`}>
          {summary.free ? (
            <p>Це перегляд автора — кристали й серія не змінюються.</p>
          ) : summary.rewards.length > 0 ? (
            <ul>
              {summary.rewards.map((reward, index) => (
                <li key={`${reward.reason}-${index}`}>
                  <span>{REASON_LABELS[reward.reason]}</span>
                  <b>
                    +<Crystals amount={reward.amount} />
                  </b>
                </li>
              ))}
            </ul>
          ) : (
            <p>
              {summary.completed
                ? 'Кристали за цю історію вже отримано, а щоденну ціль сьогодні виконано.'
                : 'Історію не дочитано до кінцівки, тому нагороди немає.'}
            </p>
          )}
          {!summary.free && (
            <p className={styles.rewardsFooter}>
              Серія: <StreakFlame lit={summary.streak.playedToday} days={summary.streak.current} /> · На рахунку: <Crystals amount={summary.balance} />
            </p>
          )}
        </div>
        {summary.canRate && <RateStory storyId={story.id} initial={summary.myRating} />}
        {summary.wordsSaved.length > 0 && (
          <ul className={styles.savedWords}>
            {summary.wordsSaved.map((word) => (
              <li key={word.word}>
                <b>{word.word}</b> — {word.translation}
              </li>
            ))}
          </ul>
        )}
        <div className={styles.summaryActions}>
          <button className="btn" onClick={() => navigate('/library')}>
            До бібліотеки
          </button>
          <button className="btn btn-ghost" onClick={() => window.location.reload()}>
            Пройти ще раз
          </button>
        </div>
      </div>
    )
  }

  if (!story || !scene) {
    return <div className={styles.center}>Відкриваємо історію…</div>
  }

  const isNarration = !line?.speaker
  const speakerIsCharacter = scene.characters.some((c) => c.name === line?.speaker)

  return (
    <div className={styles.player} style={{ backgroundColor: COVER_COLORS[story.level] }}>
      {scene.backgroundImagePath && (
        <img
          key={scene.id}
          className={styles.backdrop}
          src={scene.backgroundImagePath}
          alt=""
          draggable={false}
          style={framedImageStyle(sceneBackgroundFraming(scene))}
        />
      )}
      <header className={styles.top}>
        <button className={styles.close} aria-label="Вийти з історії" onClick={() => navigate(`/stories/${story.id}`)}>
          ✕
        </button>
        <span className={styles.sceneTitle}>{scene.title}</span>
        <span className="level-badge">{story.level}</span>
      </header>

      <div className={styles.stage} role="button" tabIndex={0} aria-label="Далі" onClick={advance} onKeyDown={onKey}>
        <div className={styles.cast}>
          {scene.characters.map((character) => (
            <CharacterFigure key={character.position} character={character} active={!speakerIsCharacter || character.name === line?.speaker} />
          ))}
        </div>

        {phase === 'lines' && line && (
          <div className={`${styles.dialog} ${isNarration ? styles.narration : ''}`} aria-live="polite">
            {!isNarration && <span className={styles.speaker}>{line.speaker}</span>}
            <p>
              {splitByWords(line.text, story.words).map((segment, index) =>
                segment.word ? (
                  <button key={index} className={styles.word} onClick={(event) => openWord(event, segment.word!)}>
                    {segment.text}
                  </button>
                ) : segment.plain ? (
                  <span key={index} className={styles.plainWord} onClick={(event) => openPlainWord(event, segment.text)}>
                    {segment.text}
                  </span>
                ) : (
                  <span key={index}>{segment.text}</span>
                ),
              )}
            </p>
            <span className={styles.next} aria-hidden="true">
              ▶
            </span>
          </div>
        )}
      </div>

      {phase === 'choices' && (
        <div className={styles.choices}>
          <p className={styles.choicesTitle}>Що далі?</p>
          {scene.choices.map((choice) => {
            const owned = choice.id !== null && bought.has(choice.id)
            const paid = choice.priceCrystals > 0 && !session?.free && !owned
            return pending === choice.id ? (
              <div key={choice.id} className={styles.confirmChoice}>
                <span>
                  Списати {choice.priceCrystals} кристалів за цей вибір?{balance !== null && ` У тебе ${balance}.`}
                </span>
                <button className="btn" disabled={busy} onClick={() => choose(choice.id)}>
                  Так
                </button>
                <button className="btn btn-ghost" disabled={busy} onClick={() => setPending(null)}>
                  Ні
                </button>
              </div>
            ) : (
              <button
                key={choice.id}
                className={styles.choice}
                disabled={busy}
                onClick={() => (paid ? (setChoiceError(null), setPending(choice.id)) : choose(choice.id))}
              >
                <span>{choice.text}</span>
                {choice.priceCrystals > 0 && (
                  <span className={styles.gem}>
                    {owned ? 'куплено' : <Crystals amount={choice.priceCrystals} />}
                  </span>
                )}
              </button>
            )
          })}
          {choiceError && <p className={styles.choiceError}>{choiceError}</p>}
        </div>
      )}

      {popup && (
        <div className={styles.popupBackdrop} onClick={() => setPopup(null)}>
          <div className={`sticker ${styles.popup}`} role="dialog" aria-label="Переклад слова" onClick={(event) => event.stopPropagation()}>
            <b>{popup.word}</b>
            {popup.status === 'loading' ? (
              <span className={styles.translation}>Шукаємо переклад…</span>
            ) : popup.translations.length > 0 ? (
              <span className={styles.translation}>{popup.translations.join(', ')}</span>
            ) : (
              <span className={styles.translation}>Цього слова немає в офлайн-словнику</span>
            )}
            {popup.status === 'saved' && <small>Додано у твій словник</small>}
            {popup.status === 'saving' && <small>Зберігаємо…</small>}
            {popup.status === 'error' && <small>Не вдалося зберегти, спробуй ще раз</small>}
            <div className={styles.popupActions}>
              {!popup.target && popup.translations.length > 0 && popup.status !== 'saved' && (
                <button className="btn" disabled={popup.status === 'saving'} onClick={addToDictionary}>
                  Додати в словник
                </button>
              )}
              <button className="btn btn-ghost" onClick={() => setPopup(null)}>
                Зрозуміло
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  )
}
