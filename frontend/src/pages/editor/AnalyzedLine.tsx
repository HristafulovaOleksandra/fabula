import type { ChangeEvent } from 'react'
import type { AnalyzedWord, CefrLevel } from '../../api/types'
import { findTargetWords } from '../../components/targetWords'
import { AutoGrowTextarea } from './AutoGrowTextarea'
import { aboveTarget, aboveTitle, useCalibration } from './certainty'
import { useLineAnalysis } from './lineAnalysis'
import styles from './Editor.module.css'

interface AnalyzedLineProps {
  text: string
  target: CefrLevel
  narration: boolean
  placeholder: string
  targetWords: string[]
  allowedWords: string[]
  onChange: (text: string) => void
  onAllow: (lemma: string) => void
}

export function LevelBadge({ text, target, narration }: { text: string; target: CefrLevel; narration: boolean }) {
  const state = useLineAnalysis(text, target, narration)
  const calibration = useCalibration()
  if (state.status === 'empty') return <span className={`${styles.levelBadge} ${styles.levelMuted}`}>—</span>
  if (state.status === 'pending') return <span className={`${styles.levelBadge} ${styles.levelMuted}`} title="Модель оцінює рівень…">…</span>
  if (state.status === 'error') {
    return (
      <span className={`${styles.levelBadge} ${styles.levelMuted}`} title="ML-сервіс зараз недоступний">
        ?
      </span>
    )
  }
  const { analysis } = state
  if (analysis.tooShort || !analysis.level) {
    return (
      <span className={`${styles.levelBadge} ${styles.levelMuted}`} title="Коротка репліка (1–2 слова) — рівень не оцінюється">
        —
      </span>
    )
  }
  const above = aboveTarget(calibration, analysis.level, analysis.score, narration, target)
  return (
    <span
      className={`${styles.levelBadge} ${above ? (above.kind === 'sure' ? styles.levelAbove : styles.levelMaybe) : styles.levelOk}`}
      title={`Рівень за моделлю: ${analysis.level}${above ? ` – ${aboveTitle(above, target)}` : ''}`}
    >
      {analysis.level}
    </span>
  )
}

export function AnalyzedLine({ text, target, narration, placeholder, targetWords, allowedWords, onChange, onAllow }: AnalyzedLineProps) {
  const state = useLineAnalysis(text, target, narration)
  const calibration = useCalibration()
  const offset = text.length - text.trimStart().length
  const targets = new Set(targetWords.map((w) => w.trim().toLowerCase()))
  const allowed = new Set(allowedWords)
  const words = state.status === 'ready' ? state.analysis.words : []
  const targetMatches = findTargetWords(text, targetWords)
  const insideTarget = (start: number, end: number) => targetMatches.some((m) => start < m.end && end > m.start)
  const hard = words.filter(
    (word: AnalyzedWord) =>
      word.aboveTarget &&
      !allowed.has(word.lemma) &&
      !targets.has(word.lemma) &&
      !insideTarget(word.start + offset, word.end + offset),
  )
  const marked = [
    ...targetMatches.map((m) => ({ start: m.start, end: m.end, kind: 'target' as const })),
    ...hard.map((word) => ({ start: word.start + offset, end: word.end + offset, kind: 'hard' as const })),
  ].sort((a, b) => a.start - b.start)

  const segments: { text: string; kind: 'target' | 'hard' | null }[] = []
  let cursor = 0
  marked.forEach(({ start, end, kind }) => {
    if (start < cursor || end > text.length) return
    segments.push({ text: text.slice(cursor, start), kind: null }, { text: text.slice(start, end), kind })
    cursor = end
  })
  segments.push({ text: text.slice(cursor) + (text.endsWith('\n') ? ' ' : ''), kind: null })

  return (
    <div className={styles.lineEditor}>
      <div className={`${styles.highlightBox} ${narration ? styles.narration : ''}`}>
        <div className={styles.highlightMirror} aria-hidden="true">
          {segments.map((segment, index) =>
            segment.kind ? (
              <mark key={index} className={segment.kind === 'target' ? styles.targetMark : undefined}>
                {segment.text}
              </mark>
            ) : (
              <span key={index}>{segment.text}</span>
            ),
          )}
        </div>
        <AutoGrowTextarea
          rows={2}
          maxLength={500}
          placeholder={placeholder}
          value={text}
          onChange={(e: ChangeEvent<HTMLTextAreaElement>) => onChange(e.target.value)}
        />
      </div>
      {state.status === 'ready' && state.analysis.sentences.length > 1 && (
        <div className={styles.sentenceLevels}>
          <span>Речення:</span>
          {state.analysis.sentences.map((sentence, index) => {
            const above = aboveTarget(calibration, sentence.level, sentence.score, narration, target)
            return (
              <span
                key={`${sentence.start}-${index}`}
                className={`${styles.sentenceLevel} ${above ? (above.kind === 'sure' ? styles.sentenceAbove : styles.sentenceMaybe) : ''}`}
                title={`${text.trim().slice(sentence.start, sentence.end)}${above ? `
${aboveTitle(above, target)}` : ''}`}
              >
                {index + 1}. {sentence.level}
              </span>
            )
          })}
        </div>
      )}
      {hard.length > 0 && (
        <div className={styles.hardWords}>
          {hard.map((word, index) => (
            <span
              key={`${word.start}-${index}`}
              className={styles.hardWord}
              title={word.rare ? 'Рідкісне слово, якого немає в словнику CEFR-J' : `Рівень слова за CEFR-J: ${word.level}`}
            >
              <b>{word.text}</b> {word.rare ? 'рідкісне' : word.level}
              {word.simpler.length > 0 && <> → {word.simpler.join(', ')}</>}
              <button
                type="button"
                className={styles.keepWord}
                title="Залишити: не підсвічувати це слово в цій історії"
                onClick={() => onAllow(word.lemma)}
              >
                Залишити
              </button>
            </span>
          ))}
        </div>
      )}
    </div>
  )
}
