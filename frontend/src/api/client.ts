const TOKEN_KEY = 'fabula.token'

export class ApiError extends Error {
  readonly status: number

  constructor(status: number, message: string) {
    super(message)
    this.status = status
  }
}

export const tokenStore = {
  get: () => localStorage.getItem(TOKEN_KEY),
  set: (token: string) => localStorage.setItem(TOKEN_KEY, token),
  clear: () => localStorage.removeItem(TOKEN_KEY),
}

interface ProblemDetails {
  title?: string
  detail?: string
  errors?: Record<string, string[]>
}

function messageFrom(status: number, problem: ProblemDetails | null): string {
  if (problem?.errors) {
    const first = Object.values(problem.errors).flat()[0]
    if (first) return first
  }
  if (problem?.detail) return problem.detail
  if (problem?.title && !problem.title.startsWith('One or more')) return problem.title
  if (status === 401) return 'Потрібно увійти в акаунт.'
  if (status === 403) return 'Недостатньо прав для цієї дії.'
  if (status === 404) return 'Не знайдено.'
  if (status === 429) return 'Забагато спроб. Зачекай хвилину і спробуй знову.'
  return 'Щось пішло не так. Спробуй ще раз.'
}

export async function api<T>(path: string, options: { method?: string; body?: unknown } = {}): Promise<T> {
  const headers: Record<string, string> = {}
  const token = tokenStore.get()
  if (token) headers.Authorization = `Bearer ${token}`
  if (options.body !== undefined) headers['Content-Type'] = 'application/json'

  let response: Response
  try {
    response = await fetch(`/api${path}`, {
      method: options.method ?? 'GET',
      headers,
      body: options.body === undefined ? undefined : JSON.stringify(options.body),
    })
  } catch {
    throw new ApiError(0, 'Сервер недоступний. Перевір, що бекенд запущено.')
  }

  const text = await response.text()
  const data = text ? JSON.parse(text) : null
  if (!response.ok) throw new ApiError(response.status, messageFrom(response.status, data))
  return data as T
}
