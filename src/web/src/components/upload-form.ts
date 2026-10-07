import { uploadMedia } from '../api/media';

export function renderUploadForm(container: HTMLElement): void {
  container.innerHTML = `
    <form id="upload-form">
      <label>File <input id="upload-file" name="file" type="file" required /></label>
      <button id="upload-submit" type="submit">Upload</button>
      <progress id="upload-progress" max="100" value="0" hidden></progress>
      <p id="upload-status" class="status" role="status"></p>
    </form>
  `;

  const form = container.querySelector<HTMLFormElement>('#upload-form')!;
  const input = container.querySelector<HTMLInputElement>('#upload-file')!;
  const submit = container.querySelector<HTMLButtonElement>('#upload-submit')!;
  const progress = container.querySelector<HTMLProgressElement>('#upload-progress')!;
  const status = container.querySelector<HTMLElement>('#upload-status')!;

  function show(message: string, isError: boolean): void {
    status.textContent = message;
    status.classList.toggle('error', isError);
  }

  form.addEventListener('submit', async (event) => {
    event.preventDefault();

    const file = input.files?.[0];
    if (!file) {
      return;
    }

    submit.disabled = true;
    progress.value = 0;
    progress.hidden = false;
    show('Uploading... 0%', false);

    try {
      const media = await uploadMedia(file, (fraction) => {
        const percent = Math.round(fraction * 100);
        progress.value = percent;
        show(`Uploading... ${percent}%`, false);
      });
      show(`Uploaded ${media.fileName} (${formatSize(media.sizeBytes)}) - ${media.status}`, false);
      form.reset();
    } catch (error) {
      show(error instanceof Error ? error.message : 'Upload failed.', true);
    } finally {
      submit.disabled = false;
      progress.hidden = true;
    }
  });
}

function formatSize(bytes: number): string {
  if (bytes < 1024) {
    return `${bytes} B`;
  }

  const units = ['KB', 'MB', 'GB', 'TB'];
  let value = bytes / 1024;
  let unit = 0;
  while (value >= 1024 && unit < units.length - 1) {
    value /= 1024;
    unit++;
  }

  return `${value.toFixed(1)} ${units[unit]}`;
}
