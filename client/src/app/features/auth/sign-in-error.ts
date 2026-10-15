import { HttpErrorResponse, HttpStatusCode } from '@angular/common/http';

const TOO_MANY_ATTEMPTS_KEY = 'auth.tooManyAttempts';
const INVALID_CREDENTIALS_KEY = 'auth.invalidCredentials';
const SIGN_IN_FAILED_KEY = 'auth.signInFailed';

export function signInErrorKeyOf(error: unknown): string {
    if (!(error instanceof HttpErrorResponse)) {
        return SIGN_IN_FAILED_KEY;
    }

    switch (error.status) {
        case HttpStatusCode.TooManyRequests:
            return TOO_MANY_ATTEMPTS_KEY;
        case HttpStatusCode.BadRequest:
        case HttpStatusCode.Unauthorized:
            return INVALID_CREDENTIALS_KEY;
        default:
            return SIGN_IN_FAILED_KEY;
    }
}
