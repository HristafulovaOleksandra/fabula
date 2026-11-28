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
  birthDate: string | null
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
  rating: number | null
  ratingCount: number
  ageRating: AgeRating
  visibility: StoryVisibility
}

export type AgeRating = 'All' | 'Teen' | 'Mature'

export type StoryVisibility = 'Public' | 'Private'

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
  backgroundFocusX: number
  backgroundFocusY: number
  backgroundZoom: number
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
  authorUsername: string
  rating: number | null
  ratingCount: number
  ageRating: AgeRating
  visibility: StoryVisibility
  moderationNote: string
}

export interface PlaySession {
  sessionId: string
  storyId: string
  currentSceneId: string | null
  free: boolean
  purchasedChoiceIds: string[]
}

export interface ChoiceResult {
  nextSceneId: string | null
  finished: boolean
  spent: number
  balance: number
}

export type TransactionReason =
  | 'StoryCompleted'
  | 'DailyGoal'
  | 'StreakBonus'
  | 'PaidChoice'
  | 'PaidStory'
  | 'StreakFreeze'
  | 'WelcomeBonus'
  | 'AdminAdjustment'

export interface Reward {
  reason: TransactionReason
  amount: number
}

export interface StreakInfo {
  current: number
  longest: number
  playedToday: boolean
  rescueDays: number
  rescuePrice: number
}

export type StreakDayState = 'Done' | 'Rescued' | 'Missed' | 'Today' | 'Future'

export interface StreakDay {
  date: string
  state: StreakDayState
  isToday: boolean
}

export interface Wallet {
  balance: number
  streak: StreakInfo
  dailyGoalReward: number
  week: StreakDay[]
}

export interface RecentStory {
  story: StorySummary
  lastPlayedAt: string
  completed: boolean
}

export interface WalletTransaction {
  id: string
  amount: number
  reason: TransactionReason
  storyTitle: string | null
  createdAt: string
}

export interface StoryAccess {
  price: number
  owned: boolean
  free: boolean
  completed: boolean
  reward: number
  balance: number
  canRate: boolean
  myRating: number | null
  assignment: AssignmentInfo | null
}

export interface PlaySummary {
  scenesVisited: number
  choicesMade: number
  wordsSaved: StoryWord[]
  crystalsEarned: number
  minutes: number
  completed: boolean
  free: boolean
  rewards: Reward[]
  balance: number
  streak: StreakInfo
  canRate: boolean
  myRating: number | null
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
  moderation: TextModeration | null
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

export interface StudioStory {
  story: StorySummary
  totalLines: number
  uncheckedLines: number
  hardLines: number
}

export interface StudioOverview {
  recentStories: StudioStory[]
  storyCount: number
  publishedCount: number
  draftCount: number
  studentCount: number
  pendingInvitationCount: number
  followerCount: number
}

export interface ForYouStory {
  story: StorySummary
  reasons: string[]
}

export interface ForYou {
  level: CefrLevel | null
  levelFromTest: boolean
  stories: ForYouStory[]
}

export interface CountItem {
  name: string
  count: number
}

export interface AuthorStats {
  storyCount: number
  readerCount: number
  rating: number | null
  ratingCount: number
  levels: CountItem[]
  genres: CountItem[]
}

export interface AuthorProfile {
  author: PublicUser
  followerCount: number
  isFollowedByMe: boolean
  stories: StorySummary[]
  stats: AuthorStats
}

export interface StoryRatingResult {
  stars: number
  rating: number | null
  ratingCount: number
}

export interface PlacementQuestion {
  id: number
  instruction: string
  text: string
  options: { id: number; text: string }[]
}

export interface PlacementTest {
  questions: PlacementQuestion[]
  minTopics: number
  maxTopics: number
}

export interface PlacementLevelScore {
  level: CefrLevel
  correct: number
  total: number
}

export interface PlacementResult {
  level: CefrLevel
  levels: PlacementLevelScore[]
  user: User
}

export type AssignmentState = 'Open' | 'Overdue' | 'Done'

export interface Assignment {
  id: string
  teacher: PublicUser
  student: PublicUser
  story: StorySummary
  dueDate: string | null
  note: string
  createdAt: string
  completedAt: string | null
  state: AssignmentState
}

export interface AssignmentInfo {
  teacher: string
  dueDate: string | null
  note: string
  done: boolean
}

export interface UserWord {
  id: string
  word: string
  translation: string
  storyTitle: string | null
  timesTapped: number
  attempts: number
  correct: number
  correctInRow: number
  learned: boolean
  markedKnown: boolean
  firstSeenAt: string
  lastSeenAt: string
}

export type QuizDirection = 'EnglishToUkrainian' | 'UkrainianToEnglish'

export interface QuizQuestion {
  wordId: string
  direction: QuizDirection
  prompt: string
  options: string[]
}

export interface QuizAnswer {
  correct: boolean
  correctAnswer: string
  correctInRow: number
  learned: boolean
}

export type ModerationKind = 'mild' | 'strong' | 'sexual' | 'hate'

export interface FlaggedWord {
  text: string
  start: number
  end: number
  kind: ModerationKind
}

export interface TextModeration {
  age: '6+' | '12+' | '16+'
  verdict: 'ok' | 'review' | 'blocked'
  reasons: string[]
  words: FlaggedWord[]
}

export type ModerationVerdict = 'Ok' | 'Review' | 'Blocked'

export interface ModerationFinding {
  kind: 'text' | 'image'
  location: string
  sceneId: string | null
  text: string | null
  imagePath: string | null
  age: AgeRating
  verdict: ModerationVerdict
  reasons: string[]
  words: FlaggedWord[]
}

export interface ModerationReport {
  available: boolean
  age: AgeRating
  verdict: ModerationVerdict
  findings: ModerationFinding[]
}

export interface QueueStory {
  id: string
  title: string
  authorUsername: string
  status: StoryStatus
  ageRating: AgeRating
  visibility: StoryVisibility
}

export interface QueueTag {
  id: number
  name: string
  kind: Tag['kind']
  status: Tag['status']
  creatorUsername: string | null
}

export interface QueueItem {
  id: string
  targetType: 'Story' | 'Line' | 'Image' | 'Tag'
  status: 'Pending' | 'Approved' | 'Rejected' | 'Appealed'
  reason: string
  authorMessage: string
  resolutionNote: string
  createdAt: string
  reviewedAt: string | null
  reviewedBy: string | null
  story: QueueStory | null
  tag: QueueTag | null
}

export interface AdminUser {
  id: string
  username: string
  email: string
  role: UserRole
  isBlocked: boolean
  isTeacher: boolean
  storyCount: number
  createdAt: string
  avatar: AvatarImage
}

export interface NamedCount {
  name: string
  count: number
}

export interface StoryMlRow {
  id: string
  title: string
  authorUsername: string
  level: CefrLevel
  status: StoryStatus
  ageRating: AgeRating
  totalLines: number
  ratedLines: number
  shortLines: number
  aboveLevel: number
}

export interface MlStats {
  modelName: string | null
  totalLines: number
  ratedLines: number
  shortLines: number
  uncheckedLines: number
  levels: NamedCount[]
  checkedPerDay: NamedCount[]
  storyAges: NamedCount[]
  storyStatuses: NamedCount[]
  imageVerdicts: NamedCount[]
  adminApproved: number
  adminRejected: number
  autoResolved: number
  openItems: number
  stories: StoryMlRow[]
}

export interface LineMl {
  id: string
  sceneTitle: string
  sceneOrder: number
  order: number
  speaker: string
  text: string
  level: CefrLevel | null
  score: number | null
  checkedAt: string | null
}

export interface StoryMlDetail {
  story: StoryMlRow
  lines: LineMl[]
}
