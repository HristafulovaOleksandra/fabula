import { useCallback, useEffect, useRef, useState } from 'react'
import { errorText } from '../../api/studio'

const AUTOSAVE_DELAY_MS = 900

export type SaveStatus =
  | { kind: 'saved' }
  | { kind: 'waiting' }
  | { kind: 'saving' }
  | { kind: 'invalid'; reason: string }
  | { kind: 'error'; reason: string }

interface AutosaveOptions<T> {
  value: T
  baseline: T
  problem: string | null
  save: (value: T) => Promise<void>
}

export function useAutosave<T>({ value, baseline, problem, save }: AutosaveOptions<T>) {
  const serialized = JSON.stringify(value)
  const dirty = serialized !== JSON.stringify(baseline)
  const [saving, setSaving] = useState(false)
  const [failure, setFailure] = useState<{ value: string; reason: string } | null>(null)
  const saveRef = useRef(save)
  const latest = useRef(value)
  const running = useRef(false)

  useEffect(() => {
    saveRef.current = save
    latest.current = value
  })

  const run = useCallback(async () => {
    if (running.current) return
    running.current = true
    setSaving(true)
    const sent = latest.current
    try {
      await saveRef.current(sent)
      setFailure(null)
    } catch (caught) {
      setFailure({ value: JSON.stringify(sent), reason: errorText(caught) })
    } finally {
      running.current = false
      setSaving(false)
    }
  }, [])

  useEffect(() => {
    if (!dirty || problem || saving || failure?.value === serialized) return
    const timer = window.setTimeout(run, AUTOSAVE_DELAY_MS)
    return () => window.clearTimeout(timer)
  }, [serialized, dirty, problem, saving, failure, run])

  let status: SaveStatus = { kind: 'saved' }
  if (saving) status = { kind: 'saving' }
  else if (dirty && problem) status = { kind: 'invalid', reason: problem }
  else if (dirty && failure?.value === serialized) status = { kind: 'error', reason: failure.reason }
  else if (dirty) status = { kind: 'waiting' }

  const saveNow = useCallback(() => {
    if (!problem) run()
  }, [problem, run])

  return { status, dirty, saveNow }
}
