import { login } from '../api/auth';

const messages = {
  invalid: 'Invalid username or password.',
  'rate-limited': 'Too many attempts. Try again in a minute.',
  error: 'Something went wrong. Try again.',
} as const;

export function renderLogin(root: HTMLElement, onSuccess: () => void): void {
  root.innerHTML = `
    <section class="box">
      <h1>media-server</h1>
      <form id="login-form">
        <label>Username <input name="username" autocomplete="username" required autofocus /></label>
        <label>Password <input name="password" type="password" autocomplete="current-password" required /></label>
        <button type="submit">Log in</button>
        <p id="login-error" class="error" role="alert"></p>
      </form>
    </section>
  `;

  const form = root.querySelector<HTMLFormElement>('#login-form')!;
  const button = form.querySelector<HTMLButtonElement>('button')!;
  const error = form.querySelector<HTMLElement>('#login-error')!;

  form.addEventListener('submit', async (event) => {
    event.preventDefault();
    error.textContent = '';
    button.disabled = true;

    const data = new FormData(form);
    try {
      const result = await login(String(data.get('username')), String(data.get('password')));
      if (result === 'ok') {
        onSuccess();
        return;
      }

      error.textContent = messages[result];
    } catch {
      error.textContent = messages.error;
    } finally {
      button.disabled = false;
    }
  });
}
