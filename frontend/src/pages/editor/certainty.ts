import { useEffect, useState } from 'react'
import { api } from '../../api/client'
import type { CefrLevel } from '../../api/types'
import { LEVEL_ORDER } from './lineAnalysis'

type Scale = 'dialogue' | 'narration'

interface CertaintyBin {
  maxMargin: number | null
  certainty: number
}

export interface Calibration {
  redFrom: number
  scales: Record<Scale, { edges: number[]; bins: CertaintyBin[] }>
}

export type AboveKind = 'sure' | 'maybe'

export interface AboveTarget {
  kind: AboveKind
  certainty: number | null
}

let pending: Promise<Calibration | null> | null = null

function loadCalibration() {
  pending ??= api<Calibration>('/ml/calibration').catch(() => {
    pending = null
    return null
  })
  return pending
}

export function useCalibration() {
  const [calibration, setCalibration] = useState<Calibration | null>(null)
  useEffect(() => {
    let active = true
    loadCalibration().then((value) => active && setCalibration(value))
    return () => {
      active = false
    }
  }, [])
  return calibration
}

export function aboveTarget(
  calibration: Calibration | null,
  level: CefrLevel | null,
  score: number | null,
  narration: boolean,
  target: CefrLevel,
): AboveTarget | null {
  if (!level || LEVEL_ORDER.indexOf(level) <= LEVEL_ORDER.indexOf(target)) return null
  if (!calibration || score === null) return { kind: 'sure', certainty: null }
  const scale = calibration.scales[narration ? 'narration' : 'dialogue']
  const edge = scale.edges[LEVEL_ORDER.indexOf(target)]
  const margin = Math.max(0, score - edge)
  const bin = scale.bins.find((b) => b.maxMargin === null || margin <= b.maxMargin) ?? scale.bins[scale.bins.length - 1]
  return { kind: bin.certainty >= calibration.redFrom ? 'sure' : 'maybe', certainty: bin.certainty }
}

export function aboveTitle(above: AboveTarget, target: CefrLevel) {
  const percent = above.certainty === null ? '' : `, впевненість моделі ${Math.round(above.certainty * 100)}%`
  return `${above.kind === 'sure' ? 'складніше' : 'можливо, складніше'} за рівень історії ${target}${percent}`
}
