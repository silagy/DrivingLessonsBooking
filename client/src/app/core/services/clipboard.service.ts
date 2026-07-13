import { Injectable } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class ClipboardService {
    async copy(text: string): Promise<boolean> {
        try {
            await navigator.clipboard.writeText(text);

            return true;
        } catch {
            return false;
        }
    }
}
