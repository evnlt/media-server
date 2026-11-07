export type MediaStatus = 'Uploading' | 'Queued' | 'Transcoding' | 'Ready' | 'Failed' | 'Missing';

export interface MediaFile {
  id: string;
  fileName: string;
  sizeBytes: number;
  durationSeconds: number | null;
  status: MediaStatus;
  progressPercent: number | null;
  failureReason: string | null;
  createdAt: string;
}

export async function listMedia(): Promise<MediaFile[]> {
  const response = await fetch('/api/media');
  if (!response.ok) {
    throw new Error(response.status === 401 ? 'Session expired. Log in again.' : `Could not load the library (${response.status}).`);
  }

  return response.json();
}

// XMLHttpRequest rather than fetch: fetch can't report upload progress.
export function uploadMedia(file: File, onProgress: (fraction: number) => void): Promise<MediaFile> {
  return new Promise((resolve, reject) => {
    const form = new FormData();
    form.append('file', file);

    const xhr = new XMLHttpRequest();
    xhr.open('POST', '/api/media');

    xhr.upload.addEventListener('progress', (event) => {
      if (event.lengthComputable) {
        onProgress(event.loaded / event.total);
      }
    });

    xhr.addEventListener('load', () => {
      if (xhr.status === 201) {
        resolve(JSON.parse(xhr.responseText) as MediaFile);
        return;
      }

      reject(new Error(describeFailure(xhr)));
    });
    xhr.addEventListener('error', () => reject(new Error('Network error.')));
    xhr.addEventListener('abort', () => reject(new Error('Upload cancelled.')));

    xhr.send(form);
  });
}

function describeFailure(xhr: XMLHttpRequest): string {
  if (xhr.status === 401) {
    return 'Session expired. Log in again.';
  }

  try {
    const problem = JSON.parse(xhr.responseText) as { title?: string; detail?: string };
    return problem.detail ?? problem.title ?? `Upload failed (${xhr.status}).`;
  } catch {
    return `Upload failed (${xhr.status}).`;
  }
}

export function streamUrl(id: string): string {
  return `/api/media/${encodeURIComponent(id)}/stream`;
}
