import type { CSSProperties } from 'react'

export const CHARACTER_BASE_HEIGHT = 75
export const CHARACTER_MAX_WIDTH_VW = 55
export const STAGE_MAX_Y = 60
export const DEFAULT_X = { Left: 25, Right: 75 } as const

export function placementStyle(placement: { x: number; y: number; scale: number }, fitToScreenWidth = false): CSSProperties {
  const height = `${CHARACTER_BASE_HEIGHT * placement.scale}%`
  return {
    left: `${placement.x}%`,
    bottom: `${placement.y}%`,
    height: fitToScreenWidth ? `min(${height}, ${CHARACTER_MAX_WIDTH_VW * placement.scale}vw)` : height,
  }
}
