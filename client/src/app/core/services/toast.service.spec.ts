import { HttpErrorResponse } from '@angular/common/http';
import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { TranslocoTestingModule } from '@jsverse/transloco';
import { MessageService } from 'primeng/api';
import { ToastService } from './toast.service';

const HTTP_NOT_FOUND = 404;
const HTTP_CONFLICT = 409;
const HTTP_SERVER_ERROR = 500;

const EN = {
    errors: {
        carNameMustNotBeEmpty: 'Enter the car\'s name.',
        conflict: 'This change conflicts with the latest data. Refresh the page and try again.',
        notFound: 'This item no longer exists. Refresh the page.',
        rosterFileMustContainRequiredColumns: 'The file is missing required columns: {{columns}}.',
    },
    general: {
        unexpectedError: 'Something went wrong. Please try again.',
    },
};

function setUp() {
    const add = vi.fn();
    TestBed.configureTestingModule({
        imports: [
            TranslocoTestingModule.forRoot({
                langs: { en: EN },
                translocoConfig: { availableLangs: ['en'], defaultLang: 'en' },
                preloadLangs: true,
            }),
        ],
        providers: [provideZonelessChangeDetection(), { provide: MessageService, useValue: { add } }],
    });

    return { toast: TestBed.inject(ToastService), add };
}

function problem(status: number, body: object): HttpErrorResponse {
    return new HttpErrorResponse({ status, error: body });
}

function shownSummary(add: ReturnType<typeof vi.fn>): string {
    return add.mock.calls[0][0].summary;
}

describe('ToastService', () => {
    describe('apiError', () => {
        it('names the broken rule in the active language', () => {
            //given
            const { toast, add } = setUp();
            const error = problem(HTTP_CONFLICT, {
                status: HTTP_CONFLICT,
                title: 'Conflict',
                detail: 'Car name must not be empty.',
                code: 'carNameMustNotBeEmpty',
            });

            //when
            toast.apiError(error);

            //then
            expect(shownSummary(add)).toBe('Enter the car\'s name.');
        });

        it('fills the rule\'s values into its message', () => {
            //given
            const { toast, add } = setUp();
            const error = problem(HTTP_CONFLICT, {
                status: HTTP_CONFLICT,
                title: 'Conflict',
                detail: 'Roster file must contain required columns: Phone.',
                code: 'rosterFileMustContainRequiredColumns',
                params: { columns: 'Phone, Teacher' },
            });

            //when
            toast.apiError(error);

            //then
            expect(shownSummary(add)).toBe('The file is missing required columns: Phone, Teacher.');
        });

        it('falls back to the generic message for a rule it has no key for', () => {
            //given
            const { toast, add } = setUp();
            const error = problem(HTTP_CONFLICT, {
                status: HTTP_CONFLICT,
                title: 'Conflict',
                detail: 'Publication 3f2a9c1e must be draft.',
                code: 'publicationMustBeDraft',
            });

            //when
            toast.apiError(error);

            //then
            expect(shownSummary(add)).toBe('This change conflicts with the latest data. Refresh the page and try again.');
        });

        it('says the item is gone when a not-found problem has no code', () => {
            //given
            const { toast, add } = setUp();
            const error = problem(HTTP_NOT_FOUND, {
                status: HTTP_NOT_FOUND,
                title: 'Not Found',
                detail: 'Teacher 3f2a9c1e was not found.',
            });

            //when
            toast.apiError(error);

            //then
            expect(shownSummary(add)).toBe('This item no longer exists. Refresh the page.');
        });

        it('never shows the server detail of an unexpected failure', () => {
            //given
            const { toast, add } = setUp();
            const error = problem(HTTP_SERVER_ERROR, { status: HTTP_SERVER_ERROR, detail: 'Npgsql: connection reset' });

            //when
            toast.apiError(error);

            //then
            expect(shownSummary(add)).toBe('Something went wrong. Please try again.');
        });

        it('shows the unexpected error for anything that is not an HTTP failure', () => {
            //given
            const { toast, add } = setUp();

            //when
            toast.apiError(undefined);

            //then
            expect(shownSummary(add)).toBe('Something went wrong. Please try again.');
        });
    });
});
