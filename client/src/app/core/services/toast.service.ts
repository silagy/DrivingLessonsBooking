import { Injectable, inject } from '@angular/core';
import { HttpErrorResponse, HttpStatusCode } from '@angular/common/http';
import { MessageService } from 'primeng/api';
import { TranslocoService } from '@jsverse/transloco';
import { ProblemDetails } from '../../shared/models/problem-details';

const UNEXPECTED_ERROR_KEY = 'general.unexpectedError';

const GENERIC_ERROR_KEYS: Partial<Record<number, string>> = {
    [HttpStatusCode.NotFound]: 'errors.notFound',
    [HttpStatusCode.Conflict]: 'errors.conflict',
};

export interface ToastDetail {
    key: string;
    params?: Record<string, string>;
}

@Injectable({ providedIn: 'root' })
export class ToastService {
    private readonly messages = inject(MessageService);
    private readonly transloco = inject(TranslocoService);

    success(key: string, detail?: ToastDetail): void {
        const summary = this.transloco.translate(key);

        if (!detail) {
            this.messages.add({ severity: 'success', summary });
            return;
        }

        this.messages.add({
            severity: 'success',
            summary,
            detail: this.transloco.translate(detail.key, detail.params),
        });
    }

    apiError(error: unknown): void {
        this.messages.add({ severity: 'error', summary: this.messageOf(error) });
    }

    messageOf(error: unknown): string {
        if (!(error instanceof HttpErrorResponse)) {
            return this.transloco.translate(UNEXPECTED_ERROR_KEY);
        }

        const problem = error.error as ProblemDetails | null;
        const ruleKey = problem?.code ? `errors.${problem.code}` : undefined;

        if (ruleKey && this.hasTranslation(ruleKey)) {
            return this.transloco.translate(ruleKey, problem?.params);
        }

        return this.transloco.translate(GENERIC_ERROR_KEYS[error.status] ?? UNEXPECTED_ERROR_KEY);
    }

    private hasTranslation(key: string): boolean {
        return this.transloco.translate(key) !== key;
    }
}
