import { provideZonelessChangeDetection, signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { TranslocoTestingModule } from '@jsverse/transloco';
import { DynamicDialogConfig, DynamicDialogRef } from 'primeng/dynamicdialog';
import { CarOption } from '../../../domain/car-option.model';
import { StudentRefusal, StudentRefusalKind } from '../../../domain/student-refusal';
import { Student } from '../../../domain/student.model';
import { TeacherOption } from '../../../domain/teacher-option.model';
import { Transmission } from '../../../domain/transmission.enum';
import { ChangeTeacherDialogData } from './change-teacher-dialog-data';
import { ChangeTeacherDialog } from './change-teacher.dialog';

const TEACHERS: TeacherOption[] = [
    { id: 'teacher-michal', name: 'Michal Ben-David' },
    { id: 'teacher-oren', name: 'Oren Levi' },
    { id: 'teacher-ronit', name: 'Ronit Avraham' },
    { id: 'teacher-yael', name: 'Yael Carmi' },
];

const CARS: CarOption[] = [
    { id: 'car-corolla', name: 'Corolla White', transmission: Transmission.automatic, teacherIds: ['teacher-ronit'] },
    { id: 'car-i20', name: 'i20 Silver', transmission: Transmission.manual, teacherIds: ['teacher-ronit', 'teacher-yael'] },
    { id: 'car-picanto', name: 'Picanto Red', transmission: Transmission.automatic, teacherIds: ['teacher-yael'] },
    { id: 'car-mazda', name: 'Mazda 3 Grey', transmission: Transmission.manual, teacherIds: ['teacher-oren'] },
    { id: 'car-kia', name: 'Kia Rio Blue', transmission: Transmission.automatic, teacherIds: ['teacher-oren'] },
];

const OMER: Student = {
    id: 'student-omer',
    nationalId: '312456783',
    name: 'Omer Shalev',
    phone: '052-7654321',
    teacherId: 'teacher-ronit',
    teacherName: 'Ronit Avraham',
    carId: 'car-i20',
    carName: 'i20 Silver',
    carTransmission: Transmission.manual,
    isCarOfTeacher: true,
    isActive: true,
};

const SAME_TEACHER: StudentRefusal = {
    kind: StudentRefusalKind.sameTeacher,
    message: "This is already this Student's Teacher. Refresh the screen.",
    existingStudentName: null,
};

interface Setup {
    fixture: ComponentFixture<ChangeTeacherDialog>;
    refusal: ReturnType<typeof signal<StudentRefusal | null>>;
    confirm: ReturnType<typeof vi.fn<(teacherId: string, carId: string) => Promise<boolean>>>;
    refresh: ReturnType<typeof vi.fn>;
    clearRefusal: ReturnType<typeof vi.fn>;
    close: ReturnType<typeof vi.fn>;
}

async function setUp(confirmResult = true): Promise<Setup> {
    const refusal = signal<StudentRefusal | null>(null);
    const confirm = vi.fn<(teacherId: string, carId: string) => Promise<boolean>>(() => Promise.resolve(confirmResult));
    const refresh = vi.fn();
    const clearRefusal = vi.fn(() => refusal.set(null));
    const close = vi.fn();
    const data: ChangeTeacherDialogData = {
        student: OMER,
        teachers: signal(TEACHERS),
        cars: signal(CARS),
        refusal,
        isSaving: signal(false),
        confirm,
        refresh,
        clearRefusal,
    };

    TestBed.configureTestingModule({
        imports: [
            ChangeTeacherDialog,
            TranslocoTestingModule.forRoot({
                langs: { en: {} },
                translocoConfig: { availableLangs: ['en'], defaultLang: 'en' },
            }),
        ],
        providers: [
            provideZonelessChangeDetection(),
            provideRouter([]),
            { provide: DynamicDialogConfig, useValue: { data } },
            { provide: DynamicDialogRef, useValue: { close } },
        ],
    });

    const fixture = TestBed.createComponent(ChangeTeacherDialog);
    await fixture.whenStable();

    return { fixture, refusal, confirm, refresh, clearRefusal, close };
}

interface TeacherChoiceView {
    id: string;
    isCurrent: boolean;
    hasCars: boolean;
}

interface DialogInternals {
    carId: () => string | null;
    teacherChoices: () => TeacherChoiceView[];
    canSave: () => boolean;
    selectTeacher: (teacherId: string) => void;
    submit: () => Promise<void>;
}

function internals(fixture: ComponentFixture<ChangeTeacherDialog>): DialogInternals {
    return fixture.componentInstance as unknown as DialogInternals;
}

function host(fixture: ComponentFixture<ChangeTeacherDialog>): HTMLElement {
    return fixture.nativeElement as HTMLElement;
}

async function chooseTeacher(fixture: ComponentFixture<ChangeTeacherDialog>, teacherId: string): Promise<void> {
    internals(fixture).selectTeacher(teacherId);
    await fixture.whenStable();
}

describe('ChangeTeacherDialog', () => {
    it('shows who is changing and keeps Save disabled until a Teacher and Car are chosen', async () => {
        //given
        const { fixture } = await setUp();

        //then
        expect(host(fixture).querySelector('.who-card__name')?.textContent).toContain('Omer Shalev');
        expect(internals(fixture).carId()).toBeNull();
        expect(internals(fixture).canSave()).toBe(false);
        expect(host(fixture).querySelector('.change-teacher__note')?.textContent).toContain('students.changeTeacher.note');
    });

    it('offers every Teacher, the current one disabled and those without Cars noted', async () => {
        //given
        const { fixture } = await setUp();

        //when
        const choices = internals(fixture).teacherChoices();

        //then
        expect(choices.find((choice) => choice.id === 'teacher-ronit')?.isCurrent).toBe(true);
        expect(choices.filter((choice) => choice.isCurrent).length).toBe(1);
        expect(choices.find((choice) => choice.id === 'teacher-michal')?.hasCars).toBe(false);
    });

    it('keeps the current Car when the new Teacher also teaches on it', async () => {
        //given
        const { fixture } = await setUp();

        //when
        await chooseTeacher(fixture, 'teacher-yael');

        //then
        expect(internals(fixture).carId()).toBe('car-i20');
        expect(internals(fixture).canSave()).toBe(true);
        expect(host(fixture).querySelector('.change-teacher__car-hint')?.textContent).toContain('students.changeTeacher.carKept');
    });

    it('clears the Car when the Teacher changes to one without the current Car', async () => {
        //given
        const { fixture } = await setUp();
        await chooseTeacher(fixture, 'teacher-yael');

        //when
        await chooseTeacher(fixture, 'teacher-oren');

        //then
        expect(internals(fixture).carId()).toBeNull();
        expect(internals(fixture).canSave()).toBe(false);
        expect(host(fixture).querySelector('.change-teacher__car-hint')?.textContent).toContain('students.add.carScoped');
    });

    it('keeps Save disabled when the new Teacher has no Cars', async () => {
        //given
        const { fixture } = await setUp();

        //when
        await chooseTeacher(fixture, 'teacher-michal');

        //then
        expect(host(fixture).querySelector('.change-teacher__no-cars')).not.toBeNull();
        expect(internals(fixture).canSave()).toBe(false);
    });

    it('changes the Teacher and Car and closes', async () => {
        //given
        const { fixture, confirm, close } = await setUp();
        await chooseTeacher(fixture, 'teacher-yael');

        //when
        await internals(fixture).submit();

        //then
        expect(confirm).toHaveBeenCalledWith('teacher-yael', 'car-i20');
        expect(close).toHaveBeenCalled();
    });

    it('shows a stale refusal with Refresh, and Refresh closes the dialog and reloads', async () => {
        //given
        const { fixture, refusal, refresh, close } = await setUp(false);
        await chooseTeacher(fixture, 'teacher-yael');
        refusal.set(SAME_TEACHER);
        await fixture.whenStable();

        //when
        (host(fixture).querySelector('.change-teacher__refresh') as HTMLButtonElement).click();

        //then
        expect(host(fixture).querySelector('app-dialog-refusal')?.textContent).toContain('students.changeTeacher.sameTeacher');
        expect(internals(fixture).canSave()).toBe(false);
        expect(refresh).toHaveBeenCalled();
        expect(close).toHaveBeenCalled();
    });

    it('forgets the refusal when another Teacher is chosen', async () => {
        //given
        const { fixture, refusal, clearRefusal } = await setUp(false);
        refusal.set(SAME_TEACHER);
        await fixture.whenStable();

        //when
        await chooseTeacher(fixture, 'teacher-oren');

        //then
        expect(clearRefusal).toHaveBeenCalled();
        expect(host(fixture).querySelector('app-dialog-refusal')).toBeNull();
    });
});
