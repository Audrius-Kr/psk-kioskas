import { createContext, useContext, useEffect, useState, type ReactNode } from 'react';
import { api } from '../api/client';

export type AuthUser = { token: string; userId: number; email: string; role: 'Customer' | 'Admin' };

type AuthContextValue = {
  user: AuthUser | null;
  loading: boolean;
  login: (email: string, password: string) => Promise<void>;
  register: (email: string, password: string) => Promise<void>;
  logout: () => void;
};

const Ctx = createContext<AuthContextValue | undefined>(undefined);

const STORAGE_KEY = 'kps_auth';

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<AuthUser | null>(null);
  const [loading, setLoading] = useState(true);

  // Token-in-localStorage means each browser tab reads the same auth independently
  // and the server stays stateless — satisfies the "multiple tabs, same session" requirement.
  useEffect(() => {
    const raw = localStorage.getItem(STORAGE_KEY);
    if (raw) {
      try { setUser(JSON.parse(raw)); } catch { localStorage.removeItem(STORAGE_KEY); }
    }
    setLoading(false);
  }, []);

  const persist = (u: AuthUser | null) => {
    setUser(u);
    if (u) localStorage.setItem(STORAGE_KEY, JSON.stringify(u));
    else localStorage.removeItem(STORAGE_KEY);
  };

  const login = async (email: string, password: string) => {
    const r = await api<{ token: string; userId: number; email: string; role: AuthUser['role'] }>(
      '/api/auth/login', { method: 'POST', body: JSON.stringify({ email, password }) });
    persist(r);
  };

  const register = async (email: string, password: string) => {
    const r = await api<{ token: string; userId: number; email: string; role: AuthUser['role'] }>(
      '/api/auth/register', { method: 'POST', body: JSON.stringify({ email, password }) });
    persist(r);
  };

  const logout = () => persist(null);

  return <Ctx.Provider value={{ user, loading, login, register, logout }}>{children}</Ctx.Provider>;
}

export function useAuth() {
  const ctx = useContext(Ctx);
  if (!ctx) throw new Error('useAuth must be used inside AuthProvider');
  return ctx;
}
