import type { StoryCharacter } from '../../api/types'

export const BUILT_IN_STATES: { key: string; label: string }[] = [
  { key: 'neutral', label: 'Звичайна' },
  { key: 'happy', label: 'Радісна' },
  { key: 'sad', label: 'Сумна' },
  { key: 'surprised', label: 'Здивована' },
]

const BUILT_IN_KEYS = new Set(BUILT_IN_STATES.map((s) => s.key))

export const isBuiltInState = (key: string) => BUILT_IN_KEYS.has(key)

export const stateLabel = (key: string) => BUILT_IN_STATES.find((s) => s.key === key)?.label ?? key

export function stateImage(character: StoryCharacter, key: string) {
  return (
    character.states.find((s) => s.key === key)?.imagePath ?? character.states.find((s) => s.key === 'neutral')?.imagePath ?? null
  )
}

export function availableStates(character: StoryCharacter) {
  const custom = character.states.filter((s) => !isBuiltInState(s.key)).map((s) => s.key)
  return [...BUILT_IN_STATES.map((s) => s.key), ...custom]
}
