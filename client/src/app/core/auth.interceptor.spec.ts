import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideZonelessChangeDetection, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { AuthService } from './auth.service';
import { authInterceptor } from './auth.interceptor';

const HTTP_UNAUTHORIZED = 401;
const SENT_TOKEN = 'sent-token';
const NEWER_TOKEN = 'newer-token';
const TEACHERS_URL = '/api/teachers';
const LOGIN_URL = '/api/auth/login';

interface AuthStub {
    token: ReturnType<typeof signal<string | null>>;
    storedToken: ReturnType<typeof vi.fn>;
    useToken: ReturnType<typeof vi.fn>;
    logout: ReturnType<typeof vi.fn>;
}

function setUp(token: string | null, stored: string | null): AuthStub {
    const auth: AuthStub = {
        token: signal(token),
        storedToken: vi.fn(() => stored),
        useToken: vi.fn(),
        logout: vi.fn(),
    };

    TestBed.configureTestingModule({
        providers: [
            provideZonelessChangeDetection(),
            provideHttpClient(withInterceptors([authInterceptor])),
            provideHttpClientTesting(),
            { provide: AuthService, useValue: auth },
        ],
    });

    return auth;
}

function failWithUnauthorized(url: string): void {
    TestBed.inject(HttpClient).get(url).subscribe({ error: () => undefined });
    TestBed.inject(HttpTestingController)
        .expectOne(url)
        .flush(null, { status: HTTP_UNAUTHORIZED, statusText: 'Unauthorized' });
}

describe('authInterceptor', () => {
    it('sends the current token as a Bearer header', () => {
        //given
        setUp(SENT_TOKEN, SENT_TOKEN);

        //when
        TestBed.inject(HttpClient).get(TEACHERS_URL).subscribe();

        //then
        const request = TestBed.inject(HttpTestingController).expectOne(TEACHERS_URL);
        expect(request.request.headers.get('Authorization')).toBe(`Bearer ${SENT_TOKEN}`);
    });

    it('signs the User out on a 401 when no newer token is stored', () => {
        //given
        const auth = setUp(SENT_TOKEN, SENT_TOKEN);

        //when
        failWithUnauthorized(TEACHERS_URL);

        //then
        expect(auth.logout).toHaveBeenCalledOnce();
        expect(auth.useToken).not.toHaveBeenCalled();
    });

    it('adopts a newer stored token instead of signing out on a 401', () => {
        //given
        const auth = setUp(SENT_TOKEN, NEWER_TOKEN);

        //when
        failWithUnauthorized(TEACHERS_URL);

        //then
        expect(auth.useToken).toHaveBeenCalledWith(NEWER_TOKEN);
        expect(auth.logout).not.toHaveBeenCalled();
    });

    it('does not sign out on a 401 from the sign-in request', () => {
        //given
        const auth = setUp(null, null);

        //when
        failWithUnauthorized(LOGIN_URL);

        //then
        expect(auth.logout).not.toHaveBeenCalled();
        expect(auth.useToken).not.toHaveBeenCalled();
    });
});
