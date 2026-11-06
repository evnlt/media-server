import { listMedia, type MediaFile } from '../api/media';
import { formatDuration, formatSize } from '../format';

const pollIntervalMs = 10_000;

export interface LibraryList {
  refresh: () => Promise<void>;
}

// Polls only while something is still in progress, and skips ticks while the tab is hidden.
export function renderLibraryList(container: HTMLElement): LibraryList {
  container.innerHTML = `
    <p id="library-status" class="status" role="status">Loading...</p>
    <ul id="library-items" class="library"></ul>
  `;

  const status = container.querySelector<HTMLElement>('#library-status')!;
  const items = container.querySelector<HTMLUListElement>('#library-items')!;
  let timer: number | undefined;

  async function refresh(): Promise<void> {
    window.clearTimeout(timer);
    timer = undefined;

    let mediaFiles: MediaFile[];
    try {
      mediaFiles = await listMedia();
    } catch (error) {
      status.textContent = error instanceof Error ? error.message : 'Could not load the library.';
      status.classList.add('error');
      schedule();
      return;
    }

    status.classList.remove('error');
    status.textContent = mediaFiles.length === 0 ? 'No videos yet. Upload one above.' : '';
    items.replaceChildren(...mediaFiles.map(renderItem));

    if (mediaFiles.some(isInProgress)) {
      schedule();
    }
  }

  function schedule(): void {
    window.clearTimeout(timer);
    timer = window.setTimeout(() => {
      if (!container.isConnected) {
        return;
      }

      if (document.hidden) {
        schedule();
        return;
      }

      void refresh();
    }, pollIntervalMs);
  }

  void refresh();

  return { refresh };
}

function isInProgress(mediaFile: MediaFile): boolean {
  return mediaFile.status === 'Uploading' || mediaFile.status === 'Queued' || mediaFile.status === 'Transcoding';
}

function renderItem(mediaFile: MediaFile): HTMLLIElement {
  const item = document.createElement('li');
  item.className = `library-item status-${mediaFile.status.toLowerCase()}`;

  const name = document.createElement('span');
  name.className = 'library-name';
  name.textContent = mediaFile.fileName;

  const details = document.createElement('span');
  details.className = 'library-details';
  details.textContent = describe(mediaFile);

  item.append(name, details);
  return item;
}

function describe(mediaFile: MediaFile): string {
  const size = formatSize(mediaFile.sizeBytes);
  switch (mediaFile.status) {
    case 'Ready':
      return mediaFile.durationSeconds === null ? size : `${formatDuration(mediaFile.durationSeconds)} - ${size}`;
    case 'Transcoding':
      return mediaFile.progressPercent === null ? 'converting' : `converting ${mediaFile.progressPercent}%`;
    case 'Queued':
      return 'waiting to convert';
    case 'Uploading':
      return 'uploading';
    case 'Missing':
      return 'file missing from storage';
    case 'Failed':
      return mediaFile.failureReason ? `failed: ${mediaFile.failureReason}` : 'failed';
  }
}
