import { useState, type FormEvent } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { useAuth } from '../auth/AuthContext';

export function LoginPage() {
  const { login } = useAuth();
  const nav = useNavigate();
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function submit(e: FormEvent) {
    e.preventDefault();
    setBusy(true); setError(null);
    try {
      await login(email, password);
      nav('/');
    } catch (err: any) {
      setError(err.body?.error ?? 'Login failed');
    } finally { setBusy(false); }
  }

  return (
    <div className="card narrow">
      <h2>Sign in</h2>
      <form onSubmit={submit}>
        <label>Email<input value={email} onChange={e => setEmail(e.target.value)} type="email" required /></label>
        <label>Password<input value={password} onChange={e => setPassword(e.target.value)} type="password" required /></label>
        <button disabled={busy} type="submit">{busy ? 'Signing in…' : 'Sign in'}</button>
        {error && <p className="error">{error}</p>}
      </form>
      <p>No account? <Link to="/register">Register</Link></p>
    </div>
  );
}
