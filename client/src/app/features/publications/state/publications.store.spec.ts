import { HttpErrorResponse } from '@angular/common/http';
import { ApplicationRef, provideZonelessChangeDetection, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { NEVER, Observable, of, throwError } from 'rxjs';
import { AuthService } from '../../../core/auth.service';
import { LanguageService } from '../../../core/language.service';
import { ClipboardService } from '../../../core/services/clipboard.service';
import { FileDownloadService } from '../../../core/services/file-download.service';
import { ToastService } from '../../../core/services/toast.service';
import { PublicationState } from '../../../shared/models/publication-state.enum';
import { GetPublicationResponse } from '../data/get-publication.response';
import { ItemForFindTeachersResponse } from '../data/item-for-find-teachers.response';
import { PublicationsApiService } from '../data/publications-api.service';
import { TeacherOptionsApiService } from '../data/teacher-options-api.service';
import { TeacherOption } from '../domain/teacher-option.model';
import { PublicationsStore } from './publications.store';

const HTTP_NOT_FOUND = 404;
const HTTP_INTERNAL_SERVER_ERROR = 500;

const TEACHERS: ItemForFindTeachersResponse[] = [
    { id: 'teacher-levi', name: 'Teacher Levi' },
    { id: 'teacher-cohen', name: 'Teacher Cohen' },
    { id: 'teacher-mizrahi', name: 'Teacher Mizrahi' },
];

const OPEN_PUBLICATION: GetPublicationResponse = {
    id: 'publication-1',
    weekStart: '2026-10-04',
    weekNumber: 41,
    state: PublicationState.open,
    linkToken: 'link-token',
    windowStartUtc: '2026-10-01T16:00:00Z',
    windowEndUtc: '2026-10-09T11:00:00Z',
};

interface SignedInAs {
    isAdministrator: boolean;
    isTeacher: boolean;
    teacherId: string | null;
}

const ADMINISTRATOR: SignedInAs = { isAdministrator: true, isTeacher: false, teacherId: null };
const TEACHER: SignedInAs = { isAdministrator: false, isTeacher: true, teacherId: 'teacher-yael' };
const LINKED_ADMINISTRATOR: SignedInAs = { isAdministrator: true, isTeacher: false, teacherId: 'teacher-levi' };

const isAdministrator = signal(true);
const isTeacher = signal(false);
const teacherId = signal<string | null>(null);

function signIn(user: SignedInAs): void {
    isAdministrator.set(user.isAdministrator);
    isTeacher.set(user.isTeacher);
    teacherId.set(user.teacherId);
}

function createStore(
    teachers: Observable<ItemForFindTeachersResponse[]>,
    publication: Observable<GetPublicationResponse>,
    user: SignedInAs = ADMINISTRATOR,
): PublicationsStore {
    signIn(user);

    TestBed.configureTestingModule({
        providers: [
            provideZonelessChangeDetection(),
            { provide: LanguageService, useValue: { lang: signal('en'), locale: signal('en-IL') } },
            {
                provide: AuthService,
                useValue: { isAdministrator, isTeacher, teacherId },
            },
            { provide: TeacherOptionsApiService, useValue: { findTeachers: vi.fn(() => teachers) } },
            {
                provide: PublicationsApiService,
                useValue: {
                    getByWeek: () => publication,
                    getDashboard: () => NEVER,
                    findHistory: () => of([]),
                    downloadExcel: vi.fn(() => of(new Blob())),
                },
            },
            { provide: ToastService, useValue: { success: () => undefined, apiError: () => undefined } },
            { provide: ClipboardService, useValue: { copy: async () => true } },
            { provide: FileDownloadService, useValue: { download: vi.fn() } },
        ],
    });

    return TestBed.inject(PublicationsStore);
}

async function loadedStore(user: SignedInAs = ADMINISTRATOR): Promise<PublicationsStore> {
    const store = createStore(
        of(TEACHERS),
        throwError(() => new HttpErrorResponse({ status: HTTP_NOT_FOUND })),
        user,
    );
    await TestBed.inject(ApplicationRef).whenStable();

    return store;
}

describe('PublicationsStore', () => {
    it('selects the linked Teacher when an Administrator signs out and a Teacher signs in', async () => {
        //given
        const store = await loadedStore();
        expect(store.selectedTeacherId()).toBe('teacher-cohen');

        //when
        signIn(TEACHER);
        await TestBed.inject(ApplicationRef).whenStable();

        //then
        expect(store.selectedTeacherId()).toBe('teacher-yael');
        expect(store.canChooseTeacher()).toBe(false);
    });

    it('selects the first teacher by name once the teachers load', async () => {
        //given
        const store = await loadedStore();

        //expected
        expect(store.selectedTeacherId()).toBe('teacher-cohen');
    });

    it('keeps an explicitly chosen teacher', async () => {
        //given
        const store = await loadedStore();

        //when
        store.selectTeacher('teacher-levi');

        //then
        expect(store.selectedTeacherId()).toBe('teacher-levi');
    });

    it('stays loading until the teachers arrive, so an open week shows no placeholder counts', async () => {
        //given
        const store = createStore(NEVER, of(OPEN_PUBLICATION));

        //when
        await vi.waitFor(() => expect(store.publication()).toBeDefined());

        //then
        expect(store.isLoading()).toBe(true);
    });

    it('names a history download after the row week, not the week selected on the dashboard', async () => {
        //given
        const store = await loadedStore();
        const fileDownload = TestBed.inject(FileDownloadService);
        store.selectWeek('2026-10-04');

        //when
        await store.downloadExcel('publication-week-38', 'teacher-levi', '2026-09-13');

        //then
        expect(fileDownload.download).toHaveBeenCalledWith(expect.any(Blob), 'week-2026-09-13.xlsx');
    });

    it('names a dashboard download after the selected week', async () => {
        //given
        const store = await loadedStore();
        const fileDownload = TestBed.inject(FileDownloadService);
        store.selectWeek('2026-10-04');

        //when
        await store.downloadExcel('publication-1');

        //then
        expect(fileDownload.download).toHaveBeenCalledWith(expect.any(Blob), 'week-2026-10-04.xlsx');
    });

    it('reports the load error instead of throwing when the week fails to load', async () => {
        //given
        const store = createStore(
            of(TEACHERS),
            throwError(() => new HttpErrorResponse({ status: HTTP_INTERNAL_SERVER_ERROR })),
        );

        //when
        await TestBed.inject(ApplicationRef).whenStable();

        //then
        expect(store.publication()).toBeUndefined();
        expect(store.state()).toBeUndefined();
        expect(store.weekNumber()).toBeUndefined();
        expect(store.dashboard()).toBeUndefined();
        expect(store.loadError()).toBe('publications.loadFailed');
    });

    it('offers the Teacher picker and the lifecycle controls to an Administrator', async () => {
        //given
        const store = await loadedStore();

        //expected
        expect(store.canChooseTeacher()).toBe(true);
        expect(store.canManageLifecycle()).toBe(true);
    });

    it('never asks for the teacher list for a Teacher', async () => {
        //given
        const store = createStore(of(TEACHERS), of(OPEN_PUBLICATION), TEACHER);

        //when
        await vi.waitFor(() => expect(store.publication()).toBeDefined());

        //then
        expect(TestBed.inject(TeacherOptionsApiService).findTeachers).not.toHaveBeenCalled();
        expect(store.teachers()).toEqual([]);
        expect(store.canChooseTeacher()).toBe(false);
        expect(store.canManageLifecycle()).toBe(false);
    });

    it('selects the linked Teacher for a Teacher and ignores picking another', async () => {
        //given
        const store = createStore(of(TEACHERS), of(OPEN_PUBLICATION), TEACHER);
        await vi.waitFor(() => expect(store.publication()).toBeDefined());

        //when
        store.selectTeacher('teacher-levi');

        //then
        expect(store.selectedTeacherId()).toBe('teacher-yael');
    });

    it('downloads the linked Teacher\'s Excel workbook for a Teacher', async () => {
        //given
        const store = createStore(of(TEACHERS), of(OPEN_PUBLICATION), TEACHER);
        await vi.waitFor(() => expect(store.publication()).toBeDefined());

        //when
        await store.downloadExcel();

        //then
        expect(TestBed.inject(PublicationsApiService).downloadExcel).toHaveBeenCalledWith('publication-1', 'teacher-yael');
    });

    it('selects a linked Administrator\'s own Teacher once the teachers load', async () => {
        //given
        const store = await loadedStore(LINKED_ADMINISTRATOR);

        //expected
        expect(store.selectedTeacherId()).toBe('teacher-levi');
        expect(store.canChooseTeacher()).toBe(true);
    });

    it('keeps the query-param Teacher over the linked default', async () => {
        //given
        const store = createStore(
            of(TEACHERS),
            throwError(() => new HttpErrorResponse({ status: HTTP_NOT_FOUND })),
            LINKED_ADMINISTRATOR,
        );
        expect(store.teachers()).toEqual([]);

        //when
        store.selectTeacher('teacher-mizrahi');
        await TestBed.inject(ApplicationRef).whenStable();

        //then
        expect(store.teachers().length).toBe(3);
        expect(store.selectedTeacherId()).toBe('teacher-mizrahi');
    });

    it('keeps the query-param Teacher over the linked default when it arrives after the teachers', async () => {
        //given
        const store = await loadedStore(LINKED_ADMINISTRATOR);

        //when
        store.selectTeacher('teacher-mizrahi');
        await TestBed.inject(ApplicationRef).whenStable();

        //then
        expect(store.selectedTeacherId()).toBe('teacher-mizrahi');
    });

    it('marks the signed-in User\'s own Teacher as me', async () => {
        //given
        const expected: TeacherOption[] = [
            { id: 'teacher-cohen', name: 'Teacher Cohen', isMe: false },
            { id: 'teacher-levi', name: 'Teacher Levi', isMe: true },
            { id: 'teacher-mizrahi', name: 'Teacher Mizrahi', isMe: false },
        ];

        //when
        const store = await loadedStore(LINKED_ADMINISTRATOR);

        //then
        expect(store.teachers()).toEqual(expected);
    });
});
