import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { SubmissionsApiService } from './submissions-api.service';

const NATIONAL_ID = '000000018';

describe('SubmissionsApiService', () => {
    beforeEach(() => {
        TestBed.configureTestingModule({
            providers: [provideZonelessChangeDetection(), provideHttpClient(), provideHttpClientTesting()],
        });
    });

    it('posts the national ID in the body and never in the URL', () => {
        //given
        const api = TestBed.inject(SubmissionsApiService);
        const http = TestBed.inject(HttpTestingController);

        //when
        api.identifyStudent('link/token', { nationalId: NATIONAL_ID }).subscribe();

        //then
        const request = http.expectOne('api/submissions/by-link/link%2Ftoken/identify');
        expect(request.request.method).toBe('POST');
        expect(request.request.body).toEqual({ nationalId: NATIONAL_ID });
        expect(request.request.urlWithParams).not.toContain(NATIONAL_ID);
        http.verify();
    });
});
