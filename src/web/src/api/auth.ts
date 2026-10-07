export interface CurrentUser {
  username: string;
}

export type LoginResult = 'ok' | 'invalid' | 'rate-limited' | 'error';

export async function getCurrentUser(): Promise<CurrentUser | null> {
  const response = await fetch('/api/auth/me');
  if (response.status === 401) {
    return null;
  }

  if (!response.ok) {
    throw new Error(`Unexpected status ${response.status}`);
  }

  return response.json();
}

export async function login(username: string, password: string): Promise<LoginResult> {
  const response = await fetch('/api/auth/login', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ username, password }),
  });

  if (response.ok) {
    return 'ok';
  }

  if (response.status === 401 || response.status === 400) {
    return 'invalid';
  }

  if (response.status === 429) {
    return 'rate-limited';
  }

  return 'error';
}

export async function logout(): Promise<void> {
  await fetch('/api/auth/logout', { method: 'POST' });
}
