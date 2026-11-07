import { listMedia, type MediaFile } from '../api/media';
import { formatDuration, formatSize } from '../format';

const pollIntervalMs = 10_000;

export interface LibraryList {
  refresh: () => Promise<void>;
  setSelected: (id: string | null) => void;
}

// Polls only while something is still in progress, and skips ticks while the tab is hidden.
// Only Ready items are selectable; what selecting does is up to the caller.
export function renderLibraryList(container: HTMLElement, onSelect: (mediaFile: MediaFile) => void): LibraryList {
  container.innerHTML = `
    <p id="library-status" class="status" role="status">Loading...</p>
    <ul id="library-items" class="library"></ul>
  `;

  const status = container.querySelector<HTMLElement>('#library-status')!;
  const items = container.querySelector<HTMLUListElement>('#library-items')!;
  let timer: number | undefined;
  let mediaFiles: MediaFile[] = [];
  let selectedId: string | null = null;

  async function refresh(): Promise<void> {
    window.clearTimeout(timer);
    timer = undefined;

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
    render();

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

  function render(): void {
    items.replaceChildren(...mediaFiles.map((mediaFile) => renderItem(mediaFile, mediaFile.id === selectedId, onSelect)));
  }

  function setSelected(id: string | null): void {
    selectedId = id;
    render();
  }

  void refresh();

  return { refresh, setSelected };
}

function isInProgress(mediaFile: MediaFile): boolean {
  return mediaFile.status === 'Uploading' || mediaFile.status === 'Queued' || mediaFile.status === 'Transcoding';
}

function renderItem(mediaFile: MediaFile, selected: boolean, onSelect: (mediaFile: MediaFile) => void): HTMLLIElement {
  const item = document.createElement('li');
  item.className = `library-item status-${mediaFile.status.toLowerCase()}`;
  if (selected) {
    item.classList.add('selected');
  }

  const name = document.createElement(mediaFile.status === 'Ready' ? 'button' : 'span');
  name.className = 'library-name';
  name.textContent = mediaFile.fileName;
  if (name instanceof HTMLButtonElement) {
    name.type = 'button';
    name.addEventListener('click', () => onSelect(mediaFile));
  }

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
