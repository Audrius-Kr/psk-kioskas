// JWT-only API client. Token comes from AuthContext (localStorage).
// No use-case state is stored on the server session — only the token is sent per request.
// This means multiple tabs of the same account work concurrently without server-side coupling.

export type ApiError = { status: number; body: unknown };

export class ConcurrencyConflictError extends Error {
  body: { error: string; message: string; current?: unknown };
  constructor(body: ConcurrencyConflictError['body']) {
    super(body.message);
    this.body = body;
  }
}

type Opts = RequestInit & { token?: string | null };

export async function api<T = unknown>(path: string, opts: Opts = {}): Promise<T> {
  const headers = new Headers(opts.headers);
  if (opts.token) headers.set('Authorization', `Bearer ${opts.token}`);
  if (opts.body && !headers.has('Content-Type')) headers.set('Content-Type', 'application/json');

  const res = await fetch(path, { ...opts, headers });

  if (res.status === 204) return undefined as T;

  const text = await res.text();
  const data = text ? JSON.parse(text) : undefined;

  if (!res.ok) {
    if (res.status === 409 && data?.error === 'concurrency_conflict') {
      throw new ConcurrencyConflictError(data);
    }
    const err: ApiError = { status: res.status, body: data };
    throw err;
  }
  return data as T;
}
