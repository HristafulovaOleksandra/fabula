import { useEffect, useState } from 'react'
import { studio } from '../../api/studio'
import type { CefrLevel, LineAnalysis } from '../../api/types'

const DEBOUNCE_MS = 600
const cache = new Map<string, LineAnalysis>()
const inflight = new Map<string, Promise<LineAnalysis>>()

const keyOf = (text: string, level: CefrLevel, narration: boolean) => `${level}|${narration ? 'n' : 'd'}|${text.trim()}`

export const LEVEL_ORDER: CefrLevel[] = ['A1', 'A2', 'B1', 'B2', 'C']

export const isAboveLevel = (level: CefrLevel | null, target: CefrLevel) =>
  level !== null && LEVEL_ORDER.indexOf(level) > LEVEL_ORDER.indexOf(target)

export interface AnalysisRequest {
  text: string
  narration: boolean
}

export async function prefetchAnalyses(items: AnalysisRequest[], level: CefrLevel) {
  const missing = items
    .map((item) => ({ text: item.text.trim(), narration: item.narration }))
    .filter((item, index, all) => item.text && !cache.has(keyOf(item.text, level, item.narration)) && all.findIndex((o) => o.text === item.text && o.narration === item.narration) === index)
  if (missing.length === 0) return
  const request = studio.analyze(
    missing.map((m) => m.text),
    level,
    missing.map((m) => m.narration),
  )
  missing.forEach((item, index) =>
    inflight.set(
      keyOf(item.text, level, item.narration),
      request.then((results) => results[index]),
    ),
  )
  try {
    const results = await request
    missing.forEach((item, index) => cache.set(keyOf(item.text, level, item.narration), results[index]))
  } finally {
    missing.forEach((item) => inflight.delete(keyOf(item.text, level, item.narration)))
  }
}

function analyzeOne(text: string, level: CefrLevel, narration: boolean) {
  const key = keyOf(text, level, narration)
  const running = inflight.get(key)
  if (running) return running
  const request = studio
    .analyze([text.trim()], level, [narration])
    .then(([analysis]) => {
      cache.set(key, analysis)
      return analysis
    })
    .finally(() => inflight.delete(key))
  inflight.set(key, request)
  return request
}

export type AnalysisState = { status: 'empty' } | { status: 'pending' } | { status: 'error' } | { status: 'ready'; analysis: LineAnalysis }

export function useLineAnalysis(text: string, level: CefrLevel, narration: boolean): AnalysisState {
  const key = keyOf(text, level, narration)
  const [failed, setFailed] = useState<string | null>(null)
  const [, setVersion] = useState(0)

  useEffect(() => {
    if (!text.trim() || cache.has(key)) return
    let active = true
    const delay = inflight.has(key) ? 0 : DEBOUNCE_MS
    const timer = window.setTimeout(() => {
      analyzeOne(text, level, narration)
        .then(() => active && setVersion((v) => v + 1))
        .catch(() => active && setFailed(key))
    }, delay)
    return () => {
      active = false
      window.clearTimeout(timer)
    }
  }, [key, text, level, narration])

  if (!text.trim()) return { status: 'empty' }
  const cached = cache.get(key)
  if (cached) return { status: 'ready', analysis: cached }
  return failed === key ? { status: 'error' } : { status: 'pending' }
}
