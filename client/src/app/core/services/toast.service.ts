import { Injectable, inject } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { MessageService } from 'primeng/api';
import { TranslocoService } from '@jsverse/transloco';
import { ProblemDetails } from '../../shared/models/problem-details';

@Injectable({ providedIn: 'root' })
export class ToastService {
    private readonly messages = inject(MessageService);
    private readonly transloco = inject(TranslocoService);

    success(key: string): void {
        this.messages.add({ severity: 'success', summary: this.transloco.translate(key) });
    }

    apiError(error: unknown): void {
        this.messages.add({ severity: 'error', summary: this.resolveMessage(error) });
    }

    private resolveMessage(error: unknown): string {
        if (error instanceof HttpErrorResponse) {
            const problem = error.error as ProblemDetails | null;

            if (problem?.detail) {
                return problem.detail;
            }
        }

        return this.transloco.translate('general.unexpectedError');
    }
}
