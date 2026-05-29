import { useEffect, useState } from 'react';
import { api } from '../api/client';
import { useAuth } from '../auth/AuthContext';
import type { OrderResponse } from '../auth/types';

const statusLabel = { 0: 'Pending', 1: 'Paid', 2: 'Failed' } as const;

export function MyOrdersPage() {
  const { user } = useAuth();
  const [orders, setOrders] = useState<OrderResponse[] | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!user) return;
    api<OrderResponse[]>('/api/orders/mine', { token: user.token })
      .then(setOrders).catch(() => setError('Could not load orders.'));
  }, [user]);

  if (!user) return <p>Please sign in.</p>;
  if (error) return <p className="error">{error}</p>;
  if (orders === null) return <p>Loading…</p>;
  if (orders.length === 0) return <p>No orders yet.</p>;

  return (
    <div>
      <h2>My orders</h2>
      {orders.map(o => (
        <div key={o.id} className="card">
          <h3>Order #{o.id} — {statusLabel[o.status]}</h3>
          <p>Placed: {new Date(o.createdAt).toLocaleString()}</p>
          <ul>
            {o.items.map((i, idx) => (
              <li key={idx}>
                {i.quantity} × {i.ticketCategoryName} — {i.eventTitle} @ {i.unitPrice.toFixed(2)} €
              </li>
            ))}
          </ul>
          <p><strong>Total: {o.totalAmount.toFixed(2)} €</strong></p>
        </div>
      ))}
    </div>
  );
}
