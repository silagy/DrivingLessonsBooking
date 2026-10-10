import { Injectable, inject } from '@angular/core';
import { HttpErrorResponse, HttpStatusCode } from '@angular/common/http';
import { MessageService } from 'primeng/api';
import { TranslocoService } from '@jsverse/transloco';
import { firstValueFrom } from 'rxjs';
import { ProblemDetails } from '../../shared/models/problem-details';
import { isolateDirection } from '../../shared/text/isolate-direction';

const UNEXPECTED_ERROR_KEY = 'general.unexpectedError';

const GENERIC_ERROR_KEYS: Partial<Record<number, string>> = {
    [HttpStatusCode.Forbidden]: 'errors.forbidden',
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

    async info(key: string, detail: ToastDetail): Promise<void> {
        const activeLang = this.transloco.getActiveLang();
        await firstValueFrom(this.transloco.load(activeLang));

        this.messages.add({
            severity: 'info',
            summary: this.transloco.translate(key),
            detail: this.transloco.translate(detail.key, detail.params),
        });
    }

    apiError(error: unknown, isolatedParams: readonly string[] = []): void {
        this.messages.add({ severity: 'error', summary: this.messageOf(error, isolatedParams) });
    }

    messageOf(error: unknown, isolatedParams: readonly string[] = []): string {
        if (!(error instanceof HttpErrorResponse)) {
            return this.transloco.translate(UNEXPECTED_ERROR_KEY);
        }

        const problem = error.error as ProblemDetails | null;
        const ruleKey = problem?.code ? `errors.${problem.code}` : undefined;

        if (ruleKey && this.hasTranslation(ruleKey)) {
            return this.transloco.translate(ruleKey, isolated(problem?.params, isolatedParams));
        }

        return this.transloco.translate(GENERIC_ERROR_KEYS[error.status] ?? UNEXPECTED_ERROR_KEY);
    }

    private hasTranslation(key: string): boolean {
        return this.transloco.translate(key) !== key;
    }
}

function isolated(
    params: Record<string, string> | undefined,
    isolatedParams: readonly string[],
): Record<string, string> | undefined {
    if (!params) {
        return params;
    }

    return Object.fromEntries(
        Object.entries(params).map(([name, value]) => [name, isolatedParams.includes(name) ? isolateDirection(value) : value]),
    );
}
