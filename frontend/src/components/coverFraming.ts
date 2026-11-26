import type { CSSProperties } from 'react'
import type { StorySummary } from '../api/types'

export type CoverFraming = Pick<StorySummary, 'coverImagePath' | 'coverFocusX' | 'coverFocusY' | 'coverZoom'>

export interface Framing {
  focusX: number
  focusY: number
  zoom: number
}

export const CENTERED_FRAMING: Framing = { focusX: 50, focusY: 50, zoom: 1 }

export function framedImageStyle(framing: Framing): CSSProperties {
  const position = `${framing.focusX}% ${framing.focusY}%`
  return { objectPosition: position, transformOrigin: position, transform: `scale(${framing.zoom})` }
}

export function coverImageStyle(framing: CoverFraming): CSSProperties {
  return framedImageStyle({ focusX: framing.coverFocusX, focusY: framing.coverFocusY, zoom: framing.coverZoom })
}
