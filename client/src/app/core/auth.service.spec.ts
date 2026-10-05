import { provideHttpClient } from '@angular/common/http';
import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { Role } from '../shared/models/role.enum';
import { AuthService } from './auth.service';

const TOKEN_KEY = 'auth_token';
const BASE64URL_PAYLOAD = 'eyJzdWIiOiJ1c2VyLW93bmVyIiwiZW1haWwiOiJvd25lckBzY2hvb2wuZXhhbXBsZSIsIm5hbWUiOiJ-fn4_In0';
const TOKEN = `header.${BASE64URL_PAYLOAD}.signature`;
const FRESH_PAYLOAD = 'eyJzdWIiOiJ1c2VyLXlhZWwiLCJlbWFpbCI6InlhZWxAc2Nob29sLmV4YW1wbGUifQ';
const FRESH_TOKEN = `header.${FRESH_PAYLOAD}.signature`;
const ADMINISTRATOR_PAYLOAD =
    'eyJzdWIiOiJ1c2VyLWRhbmkiLCJlbWFpbCI6ImRhbmlAc2Nob29sLmV4YW1wbGUiLCJyb2xlIjoiYWRtaW5pc3RyYXRvciJ9';
const TEACHER_PAYLOAD =
    'eyJzdWIiOiJ1c2VyLXlhZWwiLCJlbWFpbCI6InlhZWxAc2Nob29sLmV4YW1wbGUiLCJyb2xlIjoidGVhY2hlciIsInRlYWNoZXJfaWQiOiJ0ZWFjaGVyLXlhZWwifQ';
const LINKED_ADMINISTRATOR_PAYLOAD =
    'eyJzdWIiOiJ1c2VyLXJvbml0IiwiZW1haWwiOiJyb25pdEBzY2hvb2wuZXhhbXBsZSIsInJvbGUiOiJhZG1pbmlzdHJhdG9yIiwidGVhY2hlcl9pZCI6InRlYWNoZXItcm9uaXQifQ';
const UNKNOWN_ROLE_PAYLOAD = 'eyJzdWIiOiJ1c2VyLXgiLCJlbWFpbCI6InhAc2Nob29sLmV4YW1wbGUiLCJyb2xlIjoic3R1ZGVudCJ9';
const ADMINISTRATOR_TOKEN = `header.${ADMINISTRATOR_PAYLOAD}.signature`;
const TEACHER_TOKEN = `header.${TEACHER_PAYLOAD}.signature`;
const LINKED_ADMINISTRATOR_TOKEN = `header.${LINKED_ADMINISTRATOR_PAYLOAD}.signature`;
const UNKNOWN_ROLE_TOKEN = `header.${UNKNOWN_ROLE_PAYLOAD}.signature`;

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

    it('reads the Administrator Role from the token', () => {
        //given
        const auth = createService(ADMINISTRATOR_TOKEN);

        //expected
        expect(auth.role()).toBe(Role.administrator);
        expect(auth.isAdministrator()).toBe(true);
        expect(auth.isTeacher()).toBe(false);
        expect(auth.teacherId()).toBeNull();
    });

    it('reads the Teacher Role and the linked Teacher from the token', () => {
        //given
        const auth = createService(TEACHER_TOKEN);

        //expected
        expect(auth.role()).toBe(Role.teacher);
        expect(auth.isTeacher()).toBe(true);
        expect(auth.isAdministrator()).toBe(false);
        expect(auth.teacherId()).toBe('teacher-yael');
    });

    it('reads the linked Teacher of an Administrator who also teaches', () => {
        //given
        const auth = createService(LINKED_ADMINISTRATOR_TOKEN);

        //expected
        expect(auth.isAdministrator()).toBe(true);
        expect(auth.teacherId()).toBe('teacher-ronit');
    });

    it('grants no Role for an unknown role claim', () => {
        //given
        const auth = createService(UNKNOWN_ROLE_TOKEN);

        //expected
        expect(auth.role()).toBeNull();
        expect(auth.isAdministrator()).toBe(false);
        expect(auth.isTeacher()).toBe(false);
    });

    it('grants no Role when the token has no role claim', () => {
        //given
        const auth = createService(TOKEN);

        //expected
        expect(auth.role()).toBeNull();
        expect(auth.teacherId()).toBeNull();
    });

    it('grants no Role when signed out', () => {
        //given
        const auth = createService(null);

        //expected
        expect(auth.role()).toBeNull();
        expect(auth.isAdministrator()).toBe(false);
    });

    it('grants no Role when the token is not a JWT', () => {
        //given
        const auth = createService('not-a-token');

        //expected
        expect(auth.role()).toBeNull();
        expect(auth.teacherId()).toBeNull();
    });

    it('takes the Role of a fresh token', () => {
        //given
        const auth = createService(ADMINISTRATOR_TOKEN);

        //when
        auth.useToken(TEACHER_TOKEN);

        //then
        expect(auth.role()).toBe(Role.teacher);
        expect(auth.teacherId()).toBe('teacher-yael');
    });
});
