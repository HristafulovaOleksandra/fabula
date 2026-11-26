import type { AvatarImage } from '../api/types'
import { framedImageStyle } from './coverFraming'
import styles from './Avatar.module.css'

export function Avatar({ avatar, username, size = 40 }: { avatar: AvatarImage | null; username: string; size?: number }) {
  return (
    <span className={styles.avatar} style={{ width: size, height: size, fontSize: size * 0.42 }} aria-hidden="true">
      {avatar?.path ? <img src={avatar.path} alt="" style={framedImageStyle(avatar)} /> : username.slice(0, 1).toUpperCase()}
    </span>
  )
}
