import { useEffect, useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { api } from '../api/client';
import { useAuth } from '../auth/AuthContext';
import type { EventListItem } from '../auth/types';

export function AdminEventsPage() {
  const { user } = useAuth();
  const nav = useNavigate();
  const [events, setEvents] = useState<EventListItem[] | null>(null);

  useEffect(() => {
    if (!user || user.role !== 'Admin') return;
    api<EventListItem[]>('/api/events').then(setEvents);
  }, [user]);

  if (!user || user.role !== 'Admin') return <p>Admin only.</p>;

  return (
    <div>
      <h2>Admin · Events</h2>
      <button onClick={() => nav('/admin/events/new')}>+ New event</button>
      {events === null ? <p>Loading…</p> : (
        <ul className="event-list">
          {events.map(e => (
            <li key={e.id} className="card">
              <Link to={`/admin/events/${e.id}`}>{e.title}</Link>
              <p>{new Date(e.eventDate).toLocaleString()} — {e.venue}</p>
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}
