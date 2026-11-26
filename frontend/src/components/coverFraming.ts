import type { CSSProperties } from 'react'
import type { StorySummary } from '../api/types'

export type CoverFraming = Pick<StorySummary, 'coverImagePath' | 'coverFocusX' | 'coverFocusY' | 'coverZoom'>

export function coverImageStyle(framing: CoverFraming): CSSProperties {
  const position = `${framing.coverFocusX}% ${framing.coverFocusY}%`
  return { objectPosition: position, transformOrigin: position, transform: `scale(${framing.coverZoom})` }
}
