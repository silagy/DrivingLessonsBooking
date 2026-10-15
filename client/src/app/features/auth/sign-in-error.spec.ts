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
