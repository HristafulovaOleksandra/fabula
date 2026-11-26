import type { StoryDetail } from '../../api/types'

export interface StoryIssues {
  problems: string[]
  notes: string[]
  unreachable: Set<string>
  endings: string[]
}

export function reachableScenes(story: StoryDetail): Set<string> {
  const reached = new Set<string>()
  const queue = story.startSceneId ? [story.startSceneId] : []
  while (queue.length > 0) {
    const id = queue.shift()!
    if (reached.has(id)) continue
    reached.add(id)
    const scene = story.scenes.find((s) => s.id === id)
    scene?.choices.forEach((choice) => {
      if (choice.nextSceneId && !reached.has(choice.nextSceneId)) queue.push(choice.nextSceneId)
    })
  }
  return reached
}

export function storyIssues(story: StoryDetail): StoryIssues {
  const problems: string[] = []
  const notes: string[] = []
  const reached = reachableScenes(story)
  const unreachable = new Set(story.scenes.filter((s) => !reached.has(s.id)).map((s) => s.id))
  const endings = story.scenes.filter((s) => s.choices.length === 0 && reached.has(s.id)).map((s) => s.title)

  if (story.scenes.length === 0) problems.push('В історії ще немає сцен.')
  if (story.scenes.length > 0 && !story.startSceneId) problems.push('Не обрано початкову сцену.')
  story.scenes
    .filter((s) => unreachable.has(s.id))
    .forEach((s) => problems.push(`Сцена «${s.title}» недосяжна: жоден вибір до неї не веде.`))
  story.scenes
    .filter((s) => s.lines.length === 0 && s.choices.length === 0)
    .forEach((s) => problems.push(`Сцена «${s.title}» порожня: немає ні реплік, ні виборів.`))
  if (reached.size > 0 && endings.length === 0) problems.push('Історія не має кінця: з кожної сцени є вибір далі.')

  story.scenes.forEach((s) =>
    s.choices
      .filter((c) => !c.nextSceneId)
      .forEach((c) => notes.push(`Вибір «${c.text}» у сцені «${s.title}» завершує історію.`)),
  )
  if (story.words.length === 0) notes.push('Не додано цільових слів — у програвачі нічого не підсвічуватиметься.')
  const allText = story.scenes.flatMap((s) => s.lines.map((l) => l.text.toLowerCase())).join(' ')
  story.words
    .filter((w) => !allText.includes(w.word.toLowerCase()))
    .forEach((w) => notes.push(`Слово «${w.word}» не трапляється в жодній репліці.`))
  if (endings.length > 0) notes.push(`Кінцівки: ${endings.map((e) => `«${e}»`).join(', ')}.`)

  return { problems, notes, unreachable, endings }
}
