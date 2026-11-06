import { uploadMedia } from '../api/media';
import { formatSize } from '../format';

export function renderUploadForm(container: HTMLElement, onUploaded: () => void): void {
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
      const uploaded = `Uploaded ${media.fileName} (${formatSize(media.sizeBytes)})`;
      if (media.status === 'Queued') {
        show(`${uploaded}. Converting in the background.`, false);
      } else {
        show(`${uploaded} - ${media.status}`, false);
      }

      form.reset();
      onUploaded();
    } catch (error) {
      show(error instanceof Error ? error.message : 'Upload failed.', true);
    } finally {
      submit.disabled = false;
      progress.hidden = true;
    }
  });
}
