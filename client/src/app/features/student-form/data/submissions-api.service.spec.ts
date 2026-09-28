import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { SessionType } from '../domain/session-type.enum';
import { CreateSubmissionRequest } from './create-submission.request';
import { SubmissionsApiService } from './submissions-api.service';

const NATIONAL_ID = '000000018';

const SUBMISSION: CreateSubmissionRequest = {
    nationalId: NATIONAL_ID,
    targetCount: 2,
    slotRequests: [
        { slotId: 'slot-1', sessionType: SessionType.double, constraint: 'only after 16:00' },
        { slotId: 'slot-2', sessionType: SessionType.single, constraint: null },
    ],
};

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

    it.each([
        { method: 'POST', send: (api: SubmissionsApiService) => api.createSubmission('link/token', SUBMISSION) },
        { method: 'PUT', send: (api: SubmissionsApiService) => api.reviseSubmission('link/token', SUBMISSION) },
    ])('sends a submission with $method, the national ID in the body only', ({ method, send }) => {
        //given
        const api = TestBed.inject(SubmissionsApiService);
        const http = TestBed.inject(HttpTestingController);

        //when
        send(api).subscribe();

        //then
        const request = http.expectOne('api/submissions/by-link/link%2Ftoken');
        expect(request.request.method).toBe(method);
        expect(request.request.body).toEqual(SUBMISSION);
        expect(request.request.urlWithParams).not.toContain(NATIONAL_ID);
        request.flush(null, { status: 204, statusText: 'No Content' });
        http.verify();
    });
});
