import { Link } from 'react-router-dom'
import { Mascot } from '../components/Brand'

export function NotFoundPage() {
  return (
    <div className="page" style={{ display: 'grid', justifyItems: 'center', textAlign: 'center', gap: 12 }}>
      <Mascot mood="sad" size={180} />
      <h1>Такої сторінки немає</h1>
      <p>Можливо, історію перенесли або посилання застаріло.</p>
      <Link to="/library" className="btn">
        До бібліотеки
      </Link>
    </div>
  )
}
