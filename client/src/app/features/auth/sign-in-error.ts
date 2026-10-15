import { HttpErrorResponse, HttpStatusCode } from '@angular/common/http';

const TOO_MANY_ATTEMPTS_KEY = 'auth.tooManyAttempts';
const INVALID_CREDENTIALS_KEY = 'auth.invalidCredentials';

export function signInErrorKeyOf(error: unknown): string {
    const isRateLimited = error instanceof HttpErrorResponse && error.status === HttpStatusCode.TooManyRequests;

    return isRateLimited ? TOO_MANY_ATTEMPTS_KEY : INVALID_CREDENTIALS_KEY;
}
