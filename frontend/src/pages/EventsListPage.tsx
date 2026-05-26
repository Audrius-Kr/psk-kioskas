import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { api } from '../api/client';
import type { EventListItem } from '../auth/types';

export function EventsListPage() {
  const [events, setEvents] = useState<EventListItem[] | null>(null);
  const [q, setQ] = useState('');
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const ctrl = new AbortController();
    const t = setTimeout(() => {
      api<EventListItem[]>(`/api/events${q ? `?q=${encodeURIComponent(q)}` : ''}`, { signal: ctrl.signal })
        .then(setEvents)
        .catch(err => { if (err.name !== 'AbortError') setError('Failed to load events.'); });
    }, 200);
    return () => { clearTimeout(t); ctrl.abort(); };
  }, [q]);

  return (
    <div>
      <h2>Upcoming events</h2>
      <input placeholder="Search title or venue…" value={q} onChange={e => setQ(e.target.value)} />
      {error && <p className="error">{error}</p>}
      {events === null && <p>Loading…</p>}
      {events && events.length === 0 && <p>No events match.</p>}
      <ul className="event-list">
        {events?.map(e => (
          <li key={e.id} className="card">
            <Link to={`/events/${e.id}`}>
              <h3>{e.title}</h3>
            </Link>
            <p>{new Date(e.eventDate).toLocaleString()} — {e.venue}</p>
            <p>
              {e.minPrice != null ? `From ${e.minPrice.toFixed(2)} €` : 'Free / TBA'}
              {' · '}
              {e.availableTickets > 0 ? `${e.availableTickets} tickets left` : 'Sold out'}
            </p>
          </li>
        ))}
      </ul>
    </div>
  );
}
