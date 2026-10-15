import { HttpErrorResponse, HttpStatusCode } from '@angular/common/http';
import { signInErrorKeyOf } from './sign-in-error';

describe('signInErrorKeyOf', () => {
    it('asks the admin to wait after too many attempts', () => {
        //given
        const error = new HttpErrorResponse({ status: HttpStatusCode.TooManyRequests });

        //when
        const key = signInErrorKeyOf(error);

        //then
        expect(key).toBe('auth.tooManyAttempts');
    });

    it('reports wrong credentials when the sign-in is refused', () => {
        //given
        const error = new HttpErrorResponse({ status: HttpStatusCode.Unauthorized });

        //when
        const key = signInErrorKeyOf(error);

        //then
        expect(key).toBe('auth.invalidCredentials');
    });
});

describe('signInErrorKeyOf when the sign-in did not reach a decision', () => {
    it('reports wrong credentials when the request is rejected as invalid', () => {
        //given
        const error = new HttpErrorResponse({ status: HttpStatusCode.BadRequest });

        //when
        const key = signInErrorKeyOf(error);

        //then
        expect(key).toBe('auth.invalidCredentials');
    });

    it('asks the admin to retry when the server fails', () => {
        //given
        const error = new HttpErrorResponse({ status: HttpStatusCode.InternalServerError });

        //when
        const key = signInErrorKeyOf(error);

        //then
        expect(key).toBe('auth.signInFailed');
    });

    it('asks the admin to retry when the network is down', () => {
        //given
        const error = new HttpErrorResponse({ status: 0 });

        //when
        const key = signInErrorKeyOf(error);

        //then
        expect(key).toBe('auth.signInFailed');
    });

    it('asks the admin to retry on an unexpected error', () => {
        //given
        const error = new Error('boom');

        //when
        const key = signInErrorKeyOf(error);

        //then
        expect(key).toBe('auth.signInFailed');
    });
});
