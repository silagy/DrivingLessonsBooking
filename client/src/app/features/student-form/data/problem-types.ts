import { HttpErrorResponse } from '@angular/common/http';

const HTTP_CONFLICT = 409;

export const SUBMISSION_WINDOW_CLOSED_PROBLEM = 'problems/submission-window-closed';

export function isWindowClosedProblem(error: unknown): boolean {
    return (
        error instanceof HttpErrorResponse &&
        error.status === HTTP_CONFLICT &&
        error.error?.type === SUBMISSION_WINDOW_CLOSED_PROBLEM
    );
}
