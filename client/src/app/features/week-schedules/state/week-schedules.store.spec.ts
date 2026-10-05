import { HttpErrorResponse } from '@angular/common/http';
import { ApplicationRef, provideZonelessChangeDetection, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { AuthService } from '../../../core/auth.service';
import { LanguageService } from '../../../core/language.service';
import { ToastService } from '../../../core/services/toast.service';
import { SignedInUserStore } from '../../../core/signed-in-user/signed-in-user.store';
import { PublicationState } from '../../../shared/models/publication-state.enum';
import { GetPublicationResponse } from '../data/get-publication.response';
import { GetWeekScheduleResponse } from '../data/get-week-schedule.response';
import { PublicationStatusApiService } from '../data/publication-status-api.service';
import { TeacherOptionsApiService } from '../data/teacher-options-api.service';
import { WeekSchedulesApiService } from '../data/week-schedules-api.service';
import { WeekSchedulesStore } from './week-schedules.store';

const HTTP_NOT_FOUND = 404;
const HTTP_INTERNAL_SERVER_ERROR = 500;

interface SignedInAs {
    isAdministrator: boolean;
    isTeacher: boolean;
    teacherId: string | null;
}

const ADMINISTRATOR: SignedInAs = { isAdministrator: true, isTeacher: false, teacherId: null };
const LINKED_ADMINISTRATOR: SignedInAs = { isAdministrator: true, isTeacher: false, teacherId: 'teacher-cohen' };
const TEACHER: SignedInAs = { isAdministrator: false, isTeacher: true, teacherId: 'teacher-yael' };

const TEACHER_NAMES: Record<string, string> = {
    'teacher-yael': 'Yael Carmi',
    'teacher-cohen': 'Teacher Cohen',
};

const WEEK_SCHEDULE: GetWeekScheduleResponse = {
    id: 'schedule-1',
    teacherId: 'teacher-yael',
    weekStart: '2026-10-11',
    slots: [],
};

const DRAFT_PUBLICATION: GetPublicationResponse = {
    id: 'publication-1',
    weekStart: '2026-10-11',
    state: PublicationState.draft,
    linkToken: 'link-token',
    windowStartUtc: null,
    windowEndUtc: null,
};

function notFound() {
    return throwError(() => new HttpErrorResponse({ status: HTTP_NOT_FOUND }));
}

function serverError() {
    return throwError(() => new HttpErrorResponse({ status: HTTP_INTERNAL_SERVER_ERROR }));
}

function teacherNameOf(user: SignedInAs): string | null {
    return user.teacherId ? TEACHER_NAMES[user.teacherId] : null;
}

function storeSignedInAs(
    user: SignedInAs,
    getByTeacherAndWeek: ReturnType<typeof vi.fn> = vi.fn(() => of(WEEK_SCHEDULE)),
) {
    const findTeachers = vi.fn(() =>
        of([
            { id: 'teacher-levi', name: 'Teacher Levi' },
            { id: 'teacher-cohen', name: 'Teacher Cohen' },
        ]),
    );
    const create = vi.fn(() => of({ id: 'schedule-1' }));
    const isAdministrator = signal(user.isAdministrator);
    const isTeacher = signal(user.isTeacher);
    const teacherId = signal(user.teacherId);
    const teacherName = signal<string | null>(teacherNameOf(user));

    TestBed.configureTestingModule({
        providers: [
            provideZonelessChangeDetection(),
            { provide: LanguageService, useValue: { lang: signal('en'), locale: signal('en-IL') } },
            {
                provide: AuthService,
                useValue: { isAdministrator, isTeacher, teacherId },
            },
            { provide: SignedInUserStore, useValue: { name: signal<string | null>('Signed In User'), teacherName } },
            { provide: TeacherOptionsApiService, useValue: { findTeachers } },
            { provide: WeekSchedulesApiService, useValue: { getByTeacherAndWeek, create } },
            { provide: PublicationStatusApiService, useValue: { getByWeek: () => of(DRAFT_PUBLICATION) } },
            { provide: ToastService, useValue: { success: () => undefined, apiError: () => undefined } },
        ],
    });

    const signIn = (next: SignedInAs) => {
        isAdministrator.set(next.isAdministrator);
        isTeacher.set(next.isTeacher);
        teacherId.set(next.teacherId);
        teacherName.set(teacherNameOf(next));
    };

    return { store: TestBed.inject(WeekSchedulesStore), findTeachers, getByTeacherAndWeek, create, signIn };
}

async function settle(): Promise<void> {
    await TestBed.inject(ApplicationRef).whenStable();
}

describe('WeekSchedulesStore', () => {
    it('resets the selected Teacher when a Teacher signs out and an Administrator signs in', async () => {
        //given
        const { store, signIn } = storeSignedInAs(TEACHER);
        await settle();
        expect(store.selectedTeacherId()).toBe('teacher-yael');

        //when
        signIn(ADMINISTRATOR);
        await settle();

        //then
        expect(store.selectedTeacherId()).toBeNull();
        expect(store.canChooseTeacher()).toBe(true);
    });

    it('reports the load error instead of throwing when the week fails to load', async () => {
        //given
        const { store } = storeSignedInAs(ADMINISTRATOR, vi.fn(serverError));
        store.selectTeacher('teacher-cohen');

        //when
        await settle();

        //then
        expect(store.weekSchedule()).toBeUndefined();
        expect(store.slots()).toEqual([]);
        expect(store.unavailableCount()).toBe(0);
        expect(store.isNotCreatedYet()).toBe(false);
        expect(store.loadError()).toBe('weekSchedules.loadFailed');
    });

    it('lets an Administrator choose a Teacher and publish a draft week', async () => {
        //given
        const { store, findTeachers } = storeSignedInAs(ADMINISTRATOR);

        //when
        store.selectTeacher('teacher-cohen');
        await settle();

        //then
        expect(findTeachers).toHaveBeenCalled();
        expect(store.canChooseTeacher()).toBe(true);
        expect(store.selectedTeacherId()).toBe('teacher-cohen');
        expect(store.canPublish()).toBe(true);
    });

    it('still creates a missing week for an Administrator', async () => {
        //given
        const getByTeacherAndWeek = vi.fn().mockReturnValueOnce(notFound()).mockReturnValue(of(WEEK_SCHEDULE));
        const { store, create } = storeSignedInAs(ADMINISTRATOR, getByTeacherAndWeek);

        //when
        store.selectTeacher('teacher-cohen');
        await settle();

        //then
        expect(create).toHaveBeenCalledWith({ teacherId: 'teacher-cohen', weekStart: store.selectedWeekStart() });
        expect(store.weekSchedule()?.id).toBe('schedule-1');
        expect(store.isNotCreatedYet()).toBe(false);
    });

    it('opens a linked Administrator\'s own Teacher by default', async () => {
        //given
        const { store, getByTeacherAndWeek } = storeSignedInAs(LINKED_ADMINISTRATOR);

        //when
        await settle();

        //then
        expect(store.canChooseTeacher()).toBe(true);
        expect(store.selectedTeacherId()).toBe('teacher-cohen');
        expect(getByTeacherAndWeek).toHaveBeenCalledWith('teacher-cohen', store.selectedWeekStart());
        expect(store.weekSchedule()?.id).toBe('schedule-1');
    });

    it('still lets a linked Administrator choose another Teacher', async () => {
        //given
        const { store, getByTeacherAndWeek } = storeSignedInAs(LINKED_ADMINISTRATOR);
        await settle();

        //when
        store.selectTeacher('teacher-levi');
        await settle();

        //then
        expect(store.selectedTeacherId()).toBe('teacher-levi');
        expect(getByTeacherAndWeek).toHaveBeenLastCalledWith('teacher-levi', store.selectedWeekStart());
    });

    it('marks the signed-in User\'s own Teacher as me', async () => {
        //given
        const { store } = storeSignedInAs(LINKED_ADMINISTRATOR);

        //when
        await settle();

        //then
        expect(store.teachers()).toEqual([
            { id: 'teacher-cohen', name: 'Teacher Cohen', isMe: true },
            { id: 'teacher-levi', name: 'Teacher Levi', isMe: false },
        ]);
    });

    it('never asks for the teacher list for a Teacher', async () => {
        //given
        const { store, findTeachers } = storeSignedInAs(TEACHER);

        //when
        await settle();

        //then
        expect(findTeachers).not.toHaveBeenCalled();
        expect(store.teachers()).toEqual([]);
        expect(store.canChooseTeacher()).toBe(false);
        expect(store.loadError()).toBeUndefined();
    });

    it('opens the linked Teacher\'s week for a Teacher', async () => {
        //given
        const { store, getByTeacherAndWeek } = storeSignedInAs(TEACHER);

        //when
        await settle();

        //then
        expect(store.selectedTeacherId()).toBe('teacher-yael');
        expect(getByTeacherAndWeek).toHaveBeenCalledWith('teacher-yael', store.selectedWeekStart());
        expect(store.weekSchedule()?.id).toBe('schedule-1');
    });

    it('names the Teacher\'s own Teacher for the locked field', async () => {
        //given
        const { store } = storeSignedInAs(TEACHER);

        //when
        await settle();

        //then
        expect(store.canChooseTeacher()).toBe(false);
        expect(store.ownTeacherName()).toBe('Yael Carmi');
    });

    it('ignores a Teacher picking another Teacher', async () => {
        //given
        const { store } = storeSignedInAs(TEACHER);

        //when
        store.selectTeacher('teacher-cohen');
        await settle();

        //then
        expect(store.selectedTeacherId()).toBe('teacher-yael');
    });

    it('shows a missing week as not prepared instead of creating it, for a Teacher', async () => {
        //given
        const { store, create } = storeSignedInAs(TEACHER, vi.fn(notFound));

        //when
        await settle();

        //then
        expect(create).not.toHaveBeenCalled();
        expect(store.isNotCreatedYet()).toBe(true);
        expect(store.weekSchedule()).toBeUndefined();
        expect(store.loadError()).toBeUndefined();
    });

    it('never offers publishing to a Teacher', async () => {
        //given
        const { store } = storeSignedInAs(TEACHER);

        //when
        await settle();

        //then
        expect(store.publicationState()).toBe(PublicationState.draft);
        expect(store.canPublish()).toBe(false);
    });
});
