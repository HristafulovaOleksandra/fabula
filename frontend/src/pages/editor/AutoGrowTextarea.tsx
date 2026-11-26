import { useLayoutEffect, useRef, type TextareaHTMLAttributes } from 'react'
import styles from './Editor.module.css'

export function AutoGrowTextarea({ className, value, ...props }: TextareaHTMLAttributes<HTMLTextAreaElement> & { value: string }) {
  const area = useRef<HTMLTextAreaElement>(null)

  useLayoutEffect(() => {
    const element = area.current
    if (!element) return
    element.style.height = 'auto'
    element.style.height = `${element.scrollHeight + 4}px`
  }, [value])

  return <textarea ref={area} value={value} className={`${styles.autoGrow} ${className ?? ''}`} {...props} />
}
