import { logout, type CurrentUser } from '../api/auth';
import { renderLibraryList } from '../components/library-list';
import { renderUploadForm } from '../components/upload-form';
import { renderVideoPlayer } from '../components/video-player';

export function renderHome(root: HTMLElement, user: CurrentUser, onLogout: () => void): void {
  root.innerHTML = `
    <section class="box">
      <h1>media-server</h1>
      <p>Logged in as <strong id="username"></strong>.</p>
      <div id="player"></div>
      <h2>Upload</h2>
      <div id="upload"></div>
      <h2>Library</h2>
      <div id="library"></div>
      <button id="logout" type="button">Log out</button>
    </section>
  `;
  root.querySelector('#username')!.textContent = user.username;
  const player = renderVideoPlayer(root.querySelector<HTMLElement>('#player')!, () => library.setSelected(null));
  const library = renderLibraryList(root.querySelector<HTMLElement>('#library')!, (mediaFile) => {
    library.setSelected(mediaFile.id);
    player.play(mediaFile);
  });
  renderUploadForm(root.querySelector<HTMLElement>('#upload')!, () => void library.refresh());

  const button = root.querySelector<HTMLButtonElement>('#logout')!;
  button.addEventListener('click', async () => {
    button.disabled = true;
    try {
      player.close();
      await logout();
    } finally {
      onLogout();
    }
  });
}
