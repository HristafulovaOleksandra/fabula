import { Crystals } from '../../components/Brand'
import styles from './Editor.module.css'

interface PriceInputProps {
  value: number
  min: number
  max: number
  label: string
  onChange: (price: number) => void
  autoFocus?: boolean
}

export function PriceInput({ value, min, max, label, onChange, autoFocus = false }: PriceInputProps) {
  const typed = (text: string) => Math.min(max, Number(text.replace(/\D/g, '').slice(0, String(max).length)) || 0)

  return (
    <label className={styles.choicePrice} title={`${label}: Free або від ${min} до ${max}`}>
      <Crystals size={18} />
      <input
        inputMode="numeric"
        placeholder="Free"
        aria-label={label}
        autoFocus={autoFocus}
        value={value === 0 ? '' : String(value)}
        onChange={(e) => onChange(typed(e.target.value))}
        onBlur={() => {
          if (value > 0 && value < min) onChange(min)
        }}
      />
    </label>
  )
}
