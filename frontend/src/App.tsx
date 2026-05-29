import { Link, Route, Routes } from 'react-router-dom';
import { useAuth } from './auth/AuthContext';
import { LoginPage } from './pages/LoginPage';
import { RegisterPage } from './pages/RegisterPage';
import { EventsListPage } from './pages/EventsListPage';
import { EventDetailPage } from './pages/EventDetailPage';
import { MyOrdersPage } from './pages/MyOrdersPage';
import { AdminEventsPage } from './pages/AdminEventsPage';
import { AdminEventEditPage } from './pages/AdminEventEditPage';

function NavBar() {
  const { user, logout } = useAuth();
  return (
    <nav className="navbar">
      <Link to="/" className="brand">TicketPlatform</Link>
      <div className="links">
        <Link to="/">Events</Link>
        {user && <Link to="/orders">My orders</Link>}
        {user?.role === 'Admin' && <Link to="/admin">Admin</Link>}
        {user
          ? <><span className="muted">{user.email}</span><button onClick={logout}>Logout</button></>
          : <><Link to="/login">Sign in</Link><Link to="/register">Register</Link></>}
      </div>
    </nav>
  );
}

export default function App() {
  const { loading } = useAuth();
  if (loading) return <p>Loading…</p>;
  return (
    <>
      <NavBar />
      <main className="container">
        <Routes>
          <Route path="/" element={<EventsListPage />} />
          <Route path="/events/:id" element={<EventDetailPage />} />
          <Route path="/login" element={<LoginPage />} />
          <Route path="/register" element={<RegisterPage />} />
          <Route path="/orders" element={<MyOrdersPage />} />
          <Route path="/admin" element={<AdminEventsPage />} />
          <Route path="/admin/events/:id" element={<AdminEventEditPage />} />
        </Routes>
      </main>
    </>
  );
}
