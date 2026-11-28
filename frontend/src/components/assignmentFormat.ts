export function formatDue(date: string | null) {
  if (!date) return 'без терміну'
  return `до ${new Date(`${date}T00:00:00`).toLocaleDateString('uk-UA', { day: 'numeric', month: 'long' })}`
}
