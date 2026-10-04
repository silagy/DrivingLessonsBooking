import { provideHttpClient } from '@angular/common/http';
import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { AuthService } from './auth.service';

const TOKEN_KEY = 'auth_token';
const BASE64URL_PAYLOAD = 'eyJzdWIiOiJ1c2VyLW93bmVyIiwiZW1haWwiOiJvd25lckBzY2hvb2wuZXhhbXBsZSIsIm5hbWUiOiJ-fn4_In0';
const TOKEN = `header.${BASE64URL_PAYLOAD}.signature`;
const FRESH_PAYLOAD = 'eyJzdWIiOiJ1c2VyLXlhZWwiLCJlbWFpbCI6InlhZWxAc2Nob29sLmV4YW1wbGUifQ';
const FRESH_TOKEN = `header.${FRESH_PAYLOAD}.signature`;

function createService(token: string | null): AuthService {
    if (token) {
        localStorage.setItem(TOKEN_KEY, token);
    } else {
        localStorage.removeItem(TOKEN_KEY);
    }

    TestBed.configureTestingModule({
        providers: [provideZonelessChangeDetection(), provideHttpClient(), provideRouter([])],
    });

    return TestBed.inject(AuthService);
}

describe('AuthService', () => {
    afterEach(() => localStorage.removeItem(TOKEN_KEY));

    it('reads the signed-in User id from the token', () => {
        //given
        const auth = createService(TOKEN);

        //expected
        expect(auth.userId()).toBe('user-owner');
    });

    it('reads the email from a base64url token payload', () => {
        //given
        const auth = createService(TOKEN);

        //expected
        expect(auth.email()).toBe('owner@school.example');
    });

    it('has no User id when signed out', () => {
        //given
        const auth = createService(null);

        //expected
        expect(auth.userId()).toBeNull();
    });

    it('has no User id when the token is not a JWT', () => {
        //given
        const auth = createService('not-a-token');

        //expected
        expect(auth.userId()).toBeNull();
    });

    it('reads the token another tab stored', () => {
        //given
        const auth = createService(TOKEN);
        localStorage.setItem(TOKEN_KEY, FRESH_TOKEN);

        //expected
        expect(auth.storedToken()).toBe(FRESH_TOKEN);
        expect(auth.token()).toBe(TOKEN);
    });

    it('has no stored token when signed out', () => {
        //given
        const auth = createService(null);

        //expected
        expect(auth.storedToken()).toBeNull();
    });

    it('uses a fresh token for the next requests and keeps it across reloads', () => {
        //given
        const auth = createService(TOKEN);

        //when
        auth.useToken(FRESH_TOKEN);

        //then
        expect(auth.token()).toBe(FRESH_TOKEN);
        expect(localStorage.getItem(TOKEN_KEY)).toBe(FRESH_TOKEN);
        expect(auth.userId()).toBe('user-yael');
        expect(auth.isAuthenticated()).toBe(true);
    });
});
