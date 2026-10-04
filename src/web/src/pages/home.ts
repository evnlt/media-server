import { logout, type CurrentUser } from '../api/auth';

export function renderHome(root: HTMLElement, user: CurrentUser, onLogout: () => void): void {
  root.innerHTML = `
    <section class="box">
      <h1>media-server</h1>
      <p>Logged in as <strong id="username"></strong>.</p>
      <button id="logout" type="button">Log out</button>
    </section>
  `;
  root.querySelector('#username')!.textContent = user.username;

  const button = root.querySelector<HTMLButtonElement>('#logout')!;
  button.addEventListener('click', async () => {
    button.disabled = true;
    try {
      await logout();
    } finally {
      onLogout();
    }
  });
}
