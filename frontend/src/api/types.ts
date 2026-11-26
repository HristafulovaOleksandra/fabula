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
  avatar: AvatarImage
}

export interface AuthResponse {
  token: string
  user: User
}

export interface Tag {
  id: number
  name: string
  kind: 'Genre' | 'Topic'
  status: 'Pending' | 'Approved' | 'Rejected' | 'Appealed'
}

export interface StorySummary {
  id: string
  title: string
  description: string
  coverImagePath: string | null
  coverFocusX: number
  coverFocusY: number
  coverZoom: number
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

export type Emotion = 'neutral' | 'happy' | 'sad' | 'surprised'

export interface Character {
  position: CharacterPosition
  storyCharacterId: string
  name: string | null
  imagePath: string | null
  emotion: string | null
  x: number
  y: number
  scale: number
  flip: boolean
}

export interface CharacterState {
  id: string | null
  key: string
  imagePath: string | null
}

export interface StoryCharacter {
  id: string
  name: string
  states: CharacterState[]
}

export interface Background {
  id: string
  name: string
  imagePath: string
  isLibrary: boolean
}

export interface Line {
  id: string | null
  speaker: string | null
  text: string
  level: CefrLevel | null
  levelScore: number | null
  levelChecked: boolean
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
  coverFocusX: number
  coverFocusY: number
  coverZoom: number
  level: CefrLevel
  priceCrystals: number
  status: StoryStatus
  startSceneId: string | null
  tags: Tag[]
  words: StoryWord[]
  allowedWords: string[]
  characters: StoryCharacter[]
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

export interface Economy {
  choicePriceMin: number
  choicePriceMax: number
  storyPriceMin: number
  storyPriceMax: number
}

export interface AnalyzedWord {
  text: string
  start: number
  end: number
  lemma: string
  level: CefrLevel | null
  aboveTarget: boolean
  rare: boolean
  simpler: string[]
}

export interface AnalyzedSentence {
  start: number
  end: number
  level: CefrLevel
  score: number
}

export interface LineAnalysis {
  level: CefrLevel | null
  score: number | null
  confidence: number | null
  scale: 'dialogue' | 'narration'
  tooShort: boolean
  sentences: AnalyzedSentence[]
  words: AnalyzedWord[]
}

export interface PublicUser {
  id: string
  username: string
  displayName: string | null
  isTeacher: boolean
  avatar: AvatarImage | null
}

export interface AvatarImage {
  path: string | null
  focusX: number
  focusY: number
  zoom: number
}

export interface TeachingLink {
  id: string
  teacher: PublicUser
  student: PublicUser
  status: 'Pending' | 'Accepted'
  createdAt: string
  acceptedAt: string | null
}
