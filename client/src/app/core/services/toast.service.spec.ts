import { HttpErrorResponse } from '@angular/common/http';
import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { TranslocoTestingModule } from '@jsverse/transloco';
import { MessageService } from 'primeng/api';
import { isolateDirection } from '../../shared/text/isolate-direction';
import { ToastService } from './toast.service';

const HTTP_NOT_FOUND = 404;
const HTTP_CONFLICT = 409;
const HTTP_FORBIDDEN = 403;
const HTTP_SERVER_ERROR = 500;

const EN = {
    access: {
        refusedTitle: 'You don\'t have access to that page',
        refusedDetail: 'We\'ve taken you to your Week Schedule.',
    },
    errors: {
        carNameMustNotBeEmpty: 'Enter the car\'s name.',
        conflict: 'This change conflicts with the latest data. Refresh the page and try again.',
        forbidden: 'You don\'t have permission to do that.',
        notFound: 'This item no longer exists. Refresh the page.',
        rosterFileMustContainRequiredColumns: 'The file is missing required columns: {{columns}}.',
        teacherAssignmentMustNotHaveActiveStudents:
            'Active Students of {{teacher}} learn on this Car ({{count}}): {{names}}.',
    },
    general: {
        unexpectedError: 'Something went wrong. Please try again.',
    },
    users: {
        restored: 'User restored',
        restoredDetail: '{{name}} can sign in again.',
    },
};

function setUp(preloadLangs = true) {
    const add = vi.fn();
    TestBed.configureTestingModule({
        imports: [
            TranslocoTestingModule.forRoot({
                langs: { en: EN },
                translocoConfig: { availableLangs: ['en'], defaultLang: 'en' },
                preloadLangs,
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
    describe('info', () => {
        it('shows an info toast with a title and a detail', async () => {
            //given
            const { toast, add } = setUp();

            //when
            await toast.info('access.refusedTitle', { key: 'access.refusedDetail' });

            //then
            expect(add).toHaveBeenCalledWith({
                severity: 'info',
                summary: 'You don\'t have access to that page',
                detail: 'We\'ve taken you to your Week Schedule.',
            });
        });

        it('waits for the translations before showing the info toast', async () => {
            //given
            const { toast, add } = setUp(false);

            //when
            await toast.info('access.refusedTitle', { key: 'access.refusedDetail' });

            //then
            expect(shownSummary(add)).toBe('You don\'t have access to that page');
        });
    });

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

        it('says the User may not do that when the server refuses with 403', () => {
            //given
            const { toast, add } = setUp();

            //when
            toast.apiError(new HttpErrorResponse({ status: HTTP_FORBIDDEN }));

            //then
            expect(shownSummary(add)).toBe('You don\'t have permission to do that.');
        });
    });

    describe('success', () => {
        it('shows the confirmation alone', () => {
            //given
            const { toast, add } = setUp();

            //when
            toast.success('users.restored');

            //then
            expect(add).toHaveBeenCalledWith({ severity: 'success', summary: 'User restored' });
        });

        it('adds a detail line with its values filled in', () => {
            //given
            const { toast, add } = setUp();

            //when
            toast.success('users.restored', { key: 'users.restoredDetail', params: { name: 'Gil Nahum' } });

            //then
            expect(add).toHaveBeenCalledWith({
                severity: 'success',
                summary: 'User restored',
                detail: 'Gil Nahum can sign in again.',
            });
        });
    });

    describe('messageOf', () => {
        it('gives the same translated rule message without showing a toast', () => {
            //given
            const { toast, add } = setUp();
            const error = problem(HTTP_CONFLICT, {
                status: HTTP_CONFLICT,
                title: 'Conflict',
                code: 'carNameMustNotBeEmpty',
            });

            //when
            const message = toast.messageOf(error);

            //then
            expect(message).toBe('Enter the car\'s name.');
            expect(add).not.toHaveBeenCalled();
        });

        it('isolates the direction of only the params it is asked to', () => {
            //given
            const { toast } = setUp();
            const error = problem(HTTP_CONFLICT, {
                status: HTTP_CONFLICT,
                title: 'Conflict',
                code: 'rosterFileMustContainRequiredColumns',
                params: { columns: 'name' },
            });

            //when
            const plain = toast.messageOf(error);
            const isolatedMessage = toast.messageOf(error, ['columns']);

            //then
            expect(plain).toBe('The file is missing required columns: name.');
            expect(isolatedMessage).toBe(`The file is missing required columns: ${isolateDirection('name')}.`);
        });
    });

    describe('apiError with isolated params', () => {
        it('isolates the names in a refusal toast', () => {
            //given
            const { toast, add } = setUp();
            const error = problem(HTTP_CONFLICT, {
                status: HTTP_CONFLICT,
                title: 'Conflict',
                code: 'teacherAssignmentMustNotHaveActiveStudents',
                params: { teacher: 'רונית אברהם', count: '2', names: 'נועה מזרחי, עומר שלו' },
            });

            //when
            toast.apiError(error, ['teacher', 'names']);

            //then
            expect(add.mock.calls[0][0].severity).toBe('error');
            expect(shownSummary(add)).toBe(
                `Active Students of ${isolateDirection('רונית אברהם')} learn on this Car (2): ` +
                    `${isolateDirection('נועה מזרחי, עומר שלו')}.`,
            );
        });

        it('leaves the params as they are when none are named', () => {
            //given
            const { toast, add } = setUp();
            const error = problem(HTTP_CONFLICT, {
                status: HTTP_CONFLICT,
                title: 'Conflict',
                code: 'teacherAssignmentMustNotHaveActiveStudents',
                params: { teacher: 'Ronit', count: '1', names: 'Noa' },
            });

            //when
            toast.apiError(error);

            //then
            expect(shownSummary(add)).toBe('Active Students of Ronit learn on this Car (1): Noa.');
        });
    });
});
