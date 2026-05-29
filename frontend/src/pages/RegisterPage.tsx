import { useState, type FormEvent } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { useAuth } from '../auth/AuthContext';

export function RegisterPage() {
  const { register } = useAuth();
  const nav = useNavigate();
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function submit(e: FormEvent) {
    e.preventDefault();
    setBusy(true); setError(null);
    try {
      await register(email, password);
      nav('/');
    } catch (err: any) {
      setError(err.body?.error ?? 'Registration failed');
    } finally { setBusy(false); }
  }

  return (
    <div className="card narrow">
      <h2>Create account</h2>
      <form onSubmit={submit}>
        <label>Email<input value={email} onChange={e => setEmail(e.target.value)} type="email" required /></label>
        <label>Password (min 6)<input value={password} onChange={e => setPassword(e.target.value)} type="password" required minLength={6} /></label>
        <button disabled={busy} type="submit">{busy ? 'Creating…' : 'Create account'}</button>
        {error && <p className="error">{error}</p>}
      </form>
      <p>Have an account? <Link to="/login">Sign in</Link></p>
    </div>
  );
}
