import { useCallback, useEffect, useMemo, useRef, useState, type KeyboardEvent, type MouseEvent } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { api, ApiError } from '../api/client'
import type { Character, PlaySession, PlaySummary, Scene, StoryDetail, StoryWord } from '../api/types'
import { Crystals, Mascot } from '../components/Brand'
import { COVER_COLORS } from '../components/coverColors'
import { placementStyle } from '../components/stageLayout'
import styles from './PlayerPage.module.css'

type Phase = 'loading' | 'lines' | 'choices' | 'summary' | 'error'

interface Segment {
  text: string
  word?: StoryWord
}

function escapeRegExp(value: string) {
  return value.replace(/[.*+?^${}()|[\]\\]/g, '\\$&')
}

function splitByWords(text: string, words: StoryWord[]): Segment[] {
  if (words.length === 0) return [{ text }]
  const sorted = [...words].sort((a, b) => b.word.length - a.word.length)
  const pattern = new RegExp(`\\b(${sorted.map((w) => escapeRegExp(w.word)).join('|')})(s|es|ed|d|ing)?\\b`, 'gi')
  const segments: Segment[] = []
  let last = 0
  for (const match of text.matchAll(pattern)) {
    const index = match.index ?? 0
    if (index > last) segments.push({ text: text.slice(last, index) })
    const word = sorted.find((w) => w.word.toLowerCase() === match[1].toLowerCase())
    segments.push({ text: match[0], word })
    last = index + match[0].length
  }
  if (last < text.length) segments.push({ text: text.slice(last) })
  return segments
}

function CharacterFigure({ character, active }: { character: Character; active: boolean }) {
  return (
    <figure
      className={`${styles.character} ${character.imagePath ? '' : styles.noImage} ${active ? styles.speaking : styles.listening}`}
      style={placementStyle(character, true)}
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
  const [popup, setPopup] = useState<{ word: StoryWord; saved: boolean } | null>(null)
  const [busy, setBusy] = useState(false)
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
    try {
      const result = await api<{ nextSceneId: string | null; finished: boolean }>(`/play/sessions/${session.sessionId}/choice`, {
        method: 'POST',
        body: { choiceId },
      })
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
    setPopup({ word, saved: false })
    try {
      await api('/words', { method: 'POST', body: { word: word.word, translation: word.translation, storyId } })
      setPopup({ word, saved: true })
    } catch {
      setPopup({ word, saved: false })
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

  const background = {
    backgroundColor: COVER_COLORS[story.level],
    backgroundImage: scene.backgroundImagePath ? `url(${scene.backgroundImagePath})` : undefined,
  }
  const isNarration = !line?.speaker
  const speakerIsCharacter = scene.characters.some((c) => c.name === line?.speaker)

  return (
    <div className={styles.player} style={background}>
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
          {scene.choices.map((choice) => (
            <button key={choice.id} className={styles.choice} disabled={busy} onClick={() => choose(choice.id)}>
              <span>{choice.text}</span>
              {choice.priceCrystals > 0 && <span className={styles.gem}><Crystals amount={choice.priceCrystals} /></span>}
            </button>
          ))}
        </div>
      )}

      {popup && (
        <div className={styles.popupBackdrop} onClick={() => setPopup(null)}>
          <div className={`sticker ${styles.popup}`} role="dialog" aria-label="Переклад слова" onClick={(event) => event.stopPropagation()}>
            <b>{popup.word.word}</b>
            <span className={styles.translation}>{popup.word.translation}</span>
            <small>{popup.saved ? 'Додано у твій словник' : 'Зберігаємо…'}</small>
            <button className="btn btn-ghost" onClick={() => setPopup(null)}>
              Зрозуміло
            </button>
          </div>
        </div>
      )}
    </div>
  )
}
