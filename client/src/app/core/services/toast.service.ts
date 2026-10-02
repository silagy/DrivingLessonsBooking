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
