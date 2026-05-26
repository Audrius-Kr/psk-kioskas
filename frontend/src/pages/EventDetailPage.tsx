import { useEffect, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { api } from '../api/client';
import { useAuth } from '../auth/AuthContext';
import type { EventDto, OrderResponse } from '../auth/types';

export function EventDetailPage() {
  const { id } = useParams();
  const { user } = useAuth();
  const nav = useNavigate();
  const [ev, setEv] = useState<EventDto | null>(null);
  const [cart, setCart] = useState<Record<number, number>>({});
  const [placing, setPlacing] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [last, setLast] = useState<OrderResponse | null>(null);

  useEffect(() => {
    if (!id) return;
    api<EventDto>(`/api/events/${id}`).then(setEv).catch(() => setError('Event not found.'));
  }, [id]);

  function set(catId: number, qty: number) {
    setCart(c => ({ ...c, [catId]: Math.max(0, qty) }));
  }

  async function checkout() {
    if (!user) { nav('/login'); return; }
    const items = Object.entries(cart)
      .filter(([, q]) => q > 0)
      .map(([k, q]) => ({ ticketCategoryId: +k, quantity: q }));
    if (items.length === 0) return;
    setPlacing(true); setError(null);
    try {
      const order = await api<OrderResponse>('/api/orders', {
        method: 'POST',
        body: JSON.stringify({ items }),
        token: user.token
      });
      setLast(order);
      setCart({});
      // Refresh event so updated availability is shown
      const fresh = await api<EventDto>(`/api/events/${id}`);
      setEv(fresh);
    } catch (err: any) {
      setError(err.body?.error ?? err.body?.message ?? 'Order failed.');
    } finally { setPlacing(false); }
  }

  if (error && !ev) return <p className="error">{error}</p>;
  if (!ev) return <p>Loading…</p>;

  const total = Object.entries(cart).reduce((sum, [catId, qty]) => {
    const c = ev.categories.find(x => x.id === +catId);
    return c && qty ? sum + (c.effectivePrice ?? c.basePrice) * qty : sum;
  }, 0);

  return (
    <div className="card">
      <h2>{ev.title}</h2>
      <p>{new Date(ev.eventDate).toLocaleString()} — {ev.venue}</p>
      <p>{ev.description}</p>

      <h3>Tickets</h3>
      <table>
        <thead><tr><th>Category</th><th>Price</th><th>Available</th><th>Quantity</th></tr></thead>
        <tbody>
          {ev.categories.map(c => {
            const available = c.totalQuantity - c.soldQuantity;
            return (
              <tr key={c.id}>
                <td>{c.name}</td>
                <td>
                  {(c.effectivePrice ?? c.basePrice).toFixed(2)} €
                  {c.effectivePrice != null && c.effectivePrice < c.basePrice && (
                    <span className="strike"> {c.basePrice.toFixed(2)} €</span>
                  )}
                </td>
                <td>{available}</td>
                <td>
                  <input
                    type="number" min={0} max={available}
                    value={cart[c.id!] ?? 0}
                    onChange={e => set(c.id!, +e.target.value)}
                    disabled={available === 0} />
                </td>
              </tr>
            );
          })}
        </tbody>
      </table>

      <p><strong>Total: {total.toFixed(2)} €</strong></p>
      <button onClick={checkout} disabled={placing || total === 0}>
        {placing ? 'Processing…' : user ? 'Buy tickets' : 'Sign in to buy'}
      </button>
      {error && <p className="error">{error}</p>}
      {last && (
        <p className="success">
          Order #{last.id} placed — confirmation email is being sent in the background.
        </p>
      )}
    </div>
  );
}
