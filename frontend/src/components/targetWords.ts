export interface TargetMatch {
  start: number
  end: number
  word: string
}

const ENDINGS = '(?:s|es|ed|d|ing)?'

function escapeRegExp(value: string) {
  return value.replace(/[.*+?^${}()|[\]\\]/g, '\\$&')
}

function wordPattern(word: string) {
  return word.trim().split(/\s+/).map(escapeRegExp).join('\\s+')
}

export function findTargetWords(text: string, words: string[]): TargetMatch[] {
  const sorted = words
    .map((word) => word.trim())
    .filter(Boolean)
    .sort((a, b) => b.length - a.length)
  if (sorted.length === 0) return []
  const pattern = new RegExp(`\\b(${sorted.map((w) => `(${wordPattern(w)})`).join('|')})${ENDINGS}\\b`, 'gi')
  const matches: TargetMatch[] = []
  for (const match of text.matchAll(pattern)) {
    const groupIndex = match.slice(2).findIndex((group) => group !== undefined)
    const start = match.index ?? 0
    matches.push({ start, end: start + match[0].length, word: sorted[groupIndex] })
  }
  return matches
}
