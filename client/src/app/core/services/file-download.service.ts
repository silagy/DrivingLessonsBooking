import { Injectable } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class FileDownloadService {
    download(blob: Blob, fileName: string): void {
        const url = URL.createObjectURL(blob);
        const anchor = document.createElement('a');

        anchor.href = url;
        anchor.download = fileName;
        anchor.click();

        URL.revokeObjectURL(url);
    }
}
