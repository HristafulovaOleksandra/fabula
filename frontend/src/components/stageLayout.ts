import type { CSSProperties } from 'react'

export const CHARACTER_BASE_HEIGHT = 75
export const STAGE_X = { min: -30, max: 130 } as const
export const STAGE_Y = { min: -60, max: 60 } as const
export const STAGE_SCALE = { min: 0.4, max: 3 } as const
export const DEFAULT_X = { Left: 25, Right: 75 } as const

export function placementStyle(placement: { x: number; y: number; scale: number }): CSSProperties {
  return {
    left: `${placement.x}%`,
    bottom: `${placement.y}%`,
    height: `${CHARACTER_BASE_HEIGHT * placement.scale}%`,
    ['--scale' as string]: placement.scale,
  }
}
