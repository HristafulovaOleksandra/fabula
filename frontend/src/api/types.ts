export type UserRole = 'Student' | 'Author' | 'Admin'
export type CefrLevel = 'A1' | 'A2' | 'B1' | 'B2' | 'C'
export type NameVisibility = 'Private' | 'Teachers' | 'Public'
export type StoryStatus = 'Draft' | 'PendingModeration' | 'Published' | 'Blocked'
export type CharacterPosition = 'Left' | 'Right'

export interface User {
  id: string
  email: string
  username: string
  displayName: string
  nameVisibility: NameVisibility
  role: UserRole
  isTeacher: boolean
  level: CefrLevel | null
  favoriteTopics: string[]
  crystalBalance: number
}

export interface AuthResponse {
  token: string
  user: User
}

export interface Tag {
  id: number
  name: string
  kind: 'Genre' | 'Topic'
}

export interface StorySummary {
  id: string
  title: string
  description: string
  coverImagePath: string | null
  level: CefrLevel
  priceCrystals: number
  status: StoryStatus
  authorUsername: string
  authorDisplayName: string | null
  tags: Tag[]
  sceneCount: number
  updatedAt: string
}

export interface StoryWord {
  word: string
  translation: string
}

export interface Character {
  position: CharacterPosition
  name: string
  imagePath: string | null
  emotion: string | null
}

export interface Line {
  id: string | null
  speaker: string | null
  text: string
  level: CefrLevel | null
  levelScore: number | null
}

export interface Choice {
  id: string | null
  text: string
  nextSceneId: string | null
  priceCrystals: number
}

export interface Scene {
  id: string
  title: string
  order: number
  backgroundId: string | null
  backgroundImagePath: string | null
  characters: Character[]
  lines: Line[]
  choices: Choice[]
}

export interface StoryDetail {
  id: string
  authorId: string
  title: string
  description: string
  coverImagePath: string | null
  level: CefrLevel
  priceCrystals: number
  status: StoryStatus
  startSceneId: string | null
  tags: Tag[]
  words: StoryWord[]
  scenes: Scene[]
}

export interface PlaySession {
  sessionId: string
  storyId: string
  currentSceneId: string | null
}

export interface PlaySummary {
  scenesVisited: number
  choicesMade: number
  wordsSaved: StoryWord[]
  crystalsEarned: number
  minutes: number
}
