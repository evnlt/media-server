import { streamUrl, type MediaFile } from '../api/media';

export interface VideoPlayer {
  play: (mediaFile: MediaFile) => void;
  close: () => void;
}

export function renderVideoPlayer(container: HTMLElement, onClose: () => void): VideoPlayer {
  container.innerHTML = `
    <div class="player-header">
      <span id="player-title" class="player-title"></span>
      <button id="player-close" type="button" aria-label="Close player">x</button>
    </div>
    <video id="player-video" controls preload="metadata"></video>
    <p id="player-status" class="error" role="status"></p>
  `;
  container.classList.add('player');
  container.hidden = true;

  const title = container.querySelector<HTMLElement>('#player-title')!;
  const video = container.querySelector<HTMLVideoElement>('#player-video')!;
  const status = container.querySelector<HTMLElement>('#player-status')!;

  video.addEventListener('error', () => {
    if (video.getAttribute('src')) {
      status.textContent = 'Could not play this video.';
    }
  });

  container.querySelector('#player-close')!.addEventListener('click', () => {
    close();
    onClose();
  });

  function play(mediaFile: MediaFile): void {
    title.textContent = mediaFile.fileName;
    status.textContent = '';
    video.src = streamUrl(mediaFile.id);
    container.hidden = false;
    // Autoplay can be refused by the browser; the controls are still there to start it.
    video.play().catch(() => undefined);
  }

  // Removing src and calling load() stops the browser from downloading the rest of the stream.
  function close(): void {
    video.pause();
    video.removeAttribute('src');
    video.load();
    container.hidden = true;
  }

  return { play, close };
}
