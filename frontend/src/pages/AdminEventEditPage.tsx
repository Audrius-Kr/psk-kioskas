import { useEffect, useState, type FormEvent } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { api, ConcurrencyConflictError } from '../api/client';
import { useAuth } from '../auth/AuthContext';
import type { EventDto, TicketCategory } from '../auth/types';

const blankEvent = (): EventDto => ({
  id: 0, title: '', venue: '', description: '',
  eventDate: new Date(Date.now() + 7 * 86400_000).toISOString().slice(0, 16),
  pricingStrategy: 0, xmin: 0, categories: []
});

export function AdminEventEditPage() {
  const { id } = useParams();
  const isNew = id === 'new';
  const { user } = useAuth();
  const nav = useNavigate();
  const [ev, setEv] = useState<EventDto | null>(isNew ? blankEvent() : null);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [conflict, setConflict] = useState<EventDto | null>(null);

  useEffect(() => {
    if (isNew) return;
    api<EventDto>(`/api/events/${id}`).then(setEv).catch(() => setError('Failed to load.'));
  }, [id, isNew]);

  if (!user || user.role !== 'Admin') return <p>Admin only.</p>;
  if (!ev) return <p>Loading…</p>;

  function update<K extends keyof EventDto>(k: K, v: EventDto[K]) {
    setEv(e => e && { ...e, [k]: v });
  }
  function updateCat(idx: number, patch: Partial<TicketCategory>) {
    setEv(e => e && { ...e, categories: e.categories.map((c, i) => i === idx ? { ...c, ...patch } : c) });
  }
  function addCat() {
    setEv(e => e && { ...e, categories: [...e.categories, { name: '', basePrice: 0, totalQuantity: 0, soldQuantity: 0 }] });
  }
  function removeCat(idx: number) {
    setEv(e => e && { ...e, categories: e.categories.filter((_, i) => i !== idx) });
  }

  async function save(e: FormEvent) {
    e.preventDefault();
    if (!ev) return;
    setBusy(true); setError(null); setConflict(null);
    try {
      const payload = { ...ev, eventDate: new Date(ev.eventDate).toISOString() };
      const saved = isNew
        ? await api<EventDto>('/api/events', { method: 'POST', body: JSON.stringify(payload), token: user!.token })
        : await api<EventDto>(`/api/events/${ev.id}`, { method: 'PUT', body: JSON.stringify(payload), token: user!.token });
      setEv(saved);
      if (isNew) nav(`/admin/events/${saved.id}`, { replace: true });
    } catch (err) {
      if (err instanceof ConcurrencyConflictError) {
        setConflict((err.body.current as EventDto) ?? null);
      } else {
        setError((err as any).body?.error ?? 'Save failed.');
      }
    } finally { setBusy(false); }
  }

  async function del() {
    if (!ev || isNew) return;
    if (!confirm('Delete this event?')) return;
    await api(`/api/events/${ev.id}`, { method: 'DELETE', token: user!.token });
    nav('/admin');
  }

  return (
    <div className="card">
      <h2>{isNew ? 'Create event' : `Edit: ${ev.title}`}</h2>

      {conflict && (
        <div className="warn">
          <p><strong>Someone else edited this event.</strong> Their version is shown below.</p>
          <p>You can either reload (lose your edits but use the latest), or overwrite (force-save your version).</p>
          <button onClick={() => { setEv(conflict); setConflict(null); }}>Reload latest</button>
          <button onClick={() => { setEv(e => e && { ...e, xmin: conflict.xmin }); setConflict(null); }}>
            Keep my edits & overwrite
          </button>
        </div>
      )}

      <form onSubmit={save}>
        <label>Title<input value={ev.title} onChange={e => update('title', e.target.value)} required /></label>
        <label>Venue<input value={ev.venue} onChange={e => update('venue', e.target.value)} required /></label>
        <label>Description<textarea value={ev.description} onChange={e => update('description', e.target.value)} /></label>
        <label>Date<input type="datetime-local" value={ev.eventDate.slice(0, 16)}
          onChange={e => update('eventDate', e.target.value)} required /></label>
        <label>Pricing strategy
          <select value={ev.pricingStrategy} onChange={e => update('pricingStrategy', +e.target.value as 0 | 1)}>
            <option value={0}>Regular</option>
            <option value={1}>Early bird (20% off &gt;30 days out)</option>
          </select>
        </label>

        <h3>Ticket categories</h3>
        {ev.categories.map((c, idx) => (
          <div key={idx} className="row">
            <input placeholder="Name" value={c.name} onChange={e => updateCat(idx, { name: e.target.value })} required />
            <input placeholder="Price" type="number" step="0.01" value={c.basePrice}
              onChange={e => updateCat(idx, { basePrice: +e.target.value })} required />
            <input placeholder="Qty" type="number" value={c.totalQuantity}
              onChange={e => updateCat(idx, { totalQuantity: +e.target.value })} required />
            <span>sold: {c.soldQuantity}</span>
            <button type="button" onClick={() => removeCat(idx)}>×</button>
          </div>
        ))}
        <button type="button" onClick={addCat}>+ Add category</button>

        <div className="row">
          <button type="submit" disabled={busy}>{busy ? 'Saving…' : 'Save'}</button>
          {!isNew && <button type="button" onClick={del} className="danger">Delete</button>}
        </div>
        {error && <p className="error">{error}</p>}
      </form>
    </div>
  );
}
