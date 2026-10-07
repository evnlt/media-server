import { logout, type CurrentUser } from '../api/auth';
import { renderUploadForm } from '../components/upload-form';

export function renderHome(root: HTMLElement, user: CurrentUser, onLogout: () => void): void {
  root.innerHTML = `
    <section class="box">
      <h1>media-server</h1>
      <p>Logged in as <strong id="username"></strong>.</p>
      <h2>Upload</h2>
      <div id="upload"></div>
      <button id="logout" type="button">Log out</button>
    </section>
  `;
  root.querySelector('#username')!.textContent = user.username;
  renderUploadForm(root.querySelector<HTMLElement>('#upload')!);

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
