import { api, ApiError, tokenStore } from './client'
import type { Background, CefrLevel, CharacterPosition, CharacterState, Economy, Scene, StoryCharacter, StoryDetail, StorySummary, StoryWord, Tag } from './types'

export interface StoryForm {
  title: string
  description: string
  coverImagePath: string | null
  coverFocusX: number
  coverFocusY: number
  coverZoom: number
  level: CefrLevel
  priceCrystals: number
  tagIds: number[]
  words: StoryWord[]
  startSceneId: string | null
}

export interface CharacterForm {
  name: string
  states: CharacterState[]
}

export interface ScenePlacement {
  position: CharacterPosition
  storyCharacterId: string
  emotion: string
  x: number
  y: number
  scale: number
  flip: boolean
}

export interface SceneForm {
  title: string
  backgroundId: string | null
  characters: ScenePlacement[]
  lines: { id: string | null; speaker: string | null; text: string }[]
  choices: { id: string | null; text: string; nextSceneId: string | null; priceCrystals: number }[]
}

async function upload<T>(path: string, form: FormData): Promise<T> {
  const token = tokenStore.get()
  const response = await fetch(`/api${path}`, {
    method: 'POST',
    headers: token ? { Authorization: `Bearer ${token}` } : {},
    body: form,
  })
  const data = await response.json().catch(() => null)
  if (!response.ok) {
    const first = data?.errors ? Object.values(data.errors as Record<string, string[]>).flat()[0] : null
    throw new ApiError(response.status, first ?? data?.detail ?? 'Не вдалося завантажити файл.')
  }
  return data as T
}

export const DEFAULT_STORY_TITLE = 'Нова історія'

export const studio = {
  myStories: () => api<StorySummary[]>('/stories?mine=true'),
  story: (id: string) => api<StoryDetail>(`/stories/${id}`),
  tags: () => api<Tag[]>('/tags/available'),
  economy: () => api<Economy>('/economy'),
  createTag: (name: string, kind: Tag['kind']) => api<Tag>('/tags', { method: 'POST', body: { name, kind } }),
  translate: (word: string) =>
    api<{ word: string; translations: string[] }>(`/dictionary?word=${encodeURIComponent(word)}`).then((result) => result.translations),
  backgrounds: () => api<Background[]>('/backgrounds'),
  createStory: (title: string) =>
    api<StoryDetail>('/stories', { method: 'POST', body: { title, description: '', level: 'A2', priceCrystals: 0, tagIds: [], words: [] } }),
  saveStory: (id: string, form: StoryForm) => api<StoryDetail>(`/stories/${id}`, { method: 'PUT', body: form }),
  publish: (id: string) => api<StoryDetail>(`/stories/${id}/publish`, { method: 'POST' }),
  deleteStory: (id: string) => api<void>(`/stories/${id}`, { method: 'DELETE' }),
  createCharacter: (storyId: string, form: CharacterForm) =>
    api<StoryCharacter>(`/stories/${storyId}/characters`, { method: 'POST', body: form }),
  saveCharacter: (storyId: string, id: string, form: CharacterForm) =>
    api<StoryCharacter>(`/stories/${storyId}/characters/${id}`, { method: 'PUT', body: form }),
  deleteCharacter: (storyId: string, id: string) => api<void>(`/stories/${storyId}/characters/${id}`, { method: 'DELETE' }),
  createScene: (storyId: string, title: string) => api<Scene>(`/stories/${storyId}/scenes`, { method: 'POST', body: { title } }),
  saveScene: (storyId: string, id: string, form: SceneForm) => api<Scene>(`/stories/${storyId}/scenes/${id}`, { method: 'PUT', body: form }),
  deleteScene: (storyId: string, id: string) => api<void>(`/stories/${storyId}/scenes/${id}`, { method: 'DELETE' }),
  uploadImage: (folder: 'covers' | 'characters', file: File) => {
    const form = new FormData()
    form.append('file', file)
    return upload<{ path: string }>(`/uploads/${folder}`, form).then((result) => result.path)
  },
  uploadBackground: (name: string, file: File) => {
    const form = new FormData()
    form.append('name', name)
    form.append('file', file)
    return upload<Background>('/backgrounds', form)
  },
}

export function errorText(error: unknown) {
  return error instanceof ApiError ? error.message : 'Щось пішло не так. Спробуй ще раз.'
}
