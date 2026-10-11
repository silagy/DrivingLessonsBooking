import { provideZonelessChangeDetection, signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { TranslocoTestingModule } from '@jsverse/transloco';
import { DynamicDialogConfig, DynamicDialogRef } from 'primeng/dynamicdialog';
import { StudentRefusal, StudentRefusalKind } from '../../../domain/student-refusal';
import { CarOption } from '../../../domain/car-option.model';
import { NewStudent } from '../../../domain/new-student.model';
import { Transmission } from '../../../domain/transmission.enum';
import { AddStudentDialogData } from './add-student-dialog-data';
import { AddStudentDialog } from './add-student.dialog';

const YAEL = 'teacher-yael';
const RONIT = 'teacher-ronit';
const DAFNA = 'teacher-dafna';

const I20 = 'car-i20';
const GOLF = 'car-golf';
const POLO = 'car-polo';
const CORSA = 'car-corsa';

const CARS: CarOption[] = [
    { id: I20, name: 'i20 Silver', transmission: Transmission.manual, teacherIds: [YAEL, RONIT] },
    { id: GOLF, name: 'Golf Blue', transmission: Transmission.automatic, teacherIds: [YAEL, RONIT] },
    { id: POLO, name: 'Polo Red', transmission: Transmission.manual, teacherIds: [DAFNA] },
    { id: CORSA, name: 'Corsa White', transmission: Transmission.manual, teacherIds: [RONIT] },
];

const TEACHERS = [
    { id: YAEL, name: 'Yael Carmi' },
    { id: RONIT, name: 'Ronit Avraham' },
    { id: DAFNA, name: 'Dafna Levi' },
];

const NATIONAL_ID_REFUSAL: StudentRefusal = {
    kind: StudentRefusalKind.nationalIdInUse,
    message: 'A Student with this national ID already exists.',
    existingStudentName: 'Noa Mizrahi',
};
const STALE_CAR_REFUSAL: StudentRefusal = {
    kind: StudentRefusalKind.staleCar,
    message: 'Refresh and choose again.',
    existingStudentName: null,
};

interface Setup {
    fixture: ComponentFixture<AddStudentDialog>;
    refusal: ReturnType<typeof signal<StudentRefusal | null>>;
    isSaving: ReturnType<typeof signal<boolean>>;
    confirm: ReturnType<typeof vi.fn<(newStudent: NewStudent) => Promise<boolean>>>;
    refreshCars: ReturnType<typeof vi.fn>;
    clearRefusal: ReturnType<typeof vi.fn>;
}

async function setUp(confirmResult = true): Promise<Setup> {
    const refusal = signal<StudentRefusal | null>(null);
    const isSaving = signal(false);
    const confirm = vi.fn<(newStudent: NewStudent) => Promise<boolean>>(() => Promise.resolve(confirmResult));
    const refreshCars = vi.fn();
    const clearRefusal = vi.fn(() => refusal.set(null));
    const data: AddStudentDialogData = {
        teachers: signal(TEACHERS),
        cars: signal(CARS),
        refusal,
        isSaving,
        confirm,
        refreshCars,
        clearRefusal,
    };

    TestBed.configureTestingModule({
        imports: [
            AddStudentDialog,
            TranslocoTestingModule.forRoot({
                langs: { en: {} },
                translocoConfig: { availableLangs: ['en'], defaultLang: 'en' },
            }),
        ],
        providers: [
            provideZonelessChangeDetection(),
            provideRouter([]),
            { provide: DynamicDialogConfig, useValue: { data } },
            { provide: DynamicDialogRef, useValue: { close: vi.fn() } },
        ],
    });

    const fixture = TestBed.createComponent(AddStudentDialog);
    await fixture.whenStable();

    return { fixture, refusal, isSaving, confirm, refreshCars, clearRefusal };
}

interface DialogInternals {
    form: AddStudentDialog['form'];
    carId: () => string | null;
    selectCar: (carId: string | null) => void;
    submit: () => Promise<void>;
    onRefreshCars: () => void;
}

function internals(fixture: ComponentFixture<AddStudentDialog>): DialogInternals {
    return fixture.componentInstance as unknown as DialogInternals;
}

async function chooseTeacher(fixture: ComponentFixture<AddStudentDialog>, teacherId: string): Promise<void> {
    internals(fixture).form.controls.teacherId.setValue(teacherId);
    await fixture.whenStable();
}

async function chooseCar(fixture: ComponentFixture<AddStudentDialog>, carId: string): Promise<void> {
    internals(fixture).selectCar(carId);
    await fixture.whenStable();
}

async function fillRequiredFields(fixture: ComponentFixture<AddStudentDialog>): Promise<void> {
    internals(fixture).form.patchValue({ nationalId: '123456782', name: 'Shaked Navon', phone: '0501234567' });
    await fixture.whenStable();
}

function saveButton(fixture: ComponentFixture<AddStudentDialog>): HTMLButtonElement {
    return (fixture.nativeElement as HTMLElement).querySelector('button[type="submit"]') as HTMLButtonElement;
}

function refusalBanner(fixture: ComponentFixture<AddStudentDialog>): HTMLElement | null {
    return (fixture.nativeElement as HTMLElement).querySelector('app-dialog-refusal');
}

function errorLines(fixture: ComponentFixture<AddStudentDialog>): number {
    return (fixture.nativeElement as HTMLElement).querySelectorAll('.field__error').length;
}

async function typeNationalId(fixture: ComponentFixture<AddStudentDialog>, value: string): Promise<void> {
    const input = (fixture.nativeElement as HTMLElement).querySelector('#student-national-id') as HTMLInputElement;
    input.value = value;
    input.dispatchEvent(new Event('input'));
    await fixture.whenStable();
}

describe('AddStudentDialog', () => {
    describe('the Car follows the Teacher', () => {
        it('leaves the Car empty for a Teacher with two Cars', async () => {
            //given
            const { fixture } = await setUp();

            //when
            await chooseTeacher(fixture, YAEL);

            //then
            expect(internals(fixture).carId()).toBeNull();
        });

        it('resets the Car when the next Teacher also has the chosen Car', async () => {
            //given
            const { fixture } = await setUp();
            await chooseTeacher(fixture, YAEL);
            await chooseCar(fixture, I20);
            expect(internals(fixture).carId()).toBe(I20);

            //when
            await chooseTeacher(fixture, RONIT);

            //then
            expect(internals(fixture).carId()).toBeNull();
        });

        it('preselects the only Car of a Teacher', async () => {
            //given
            const { fixture } = await setUp();

            //when
            await chooseTeacher(fixture, DAFNA);

            //then
            expect(internals(fixture).carId()).toBe(POLO);
        });
    });

    describe('Save', () => {
        it('is disabled while no Car is chosen', async () => {
            //given
            const { fixture } = await setUp();

            //when
            await fillRequiredFields(fixture);
            await chooseTeacher(fixture, YAEL);

            //then
            expect(saveButton(fixture).disabled).toBe(true);
        });

        it('is enabled once the form is valid and a Car is chosen', async () => {
            //given
            const { fixture } = await setUp();
            await fillRequiredFields(fixture);
            await chooseTeacher(fixture, YAEL);

            //when
            await chooseCar(fixture, GOLF);

            //then
            expect(saveButton(fixture).disabled).toBe(false);
        });

        it('is disabled while saving', async () => {
            //given
            const { fixture, isSaving } = await setUp();
            await fillRequiredFields(fixture);
            await chooseTeacher(fixture, DAFNA);
            expect(saveButton(fixture).disabled).toBe(false);

            //when
            isSaving.set(true);
            await fixture.whenStable();

            //then
            expect(saveButton(fixture).disabled).toBe(true);
        });
    });

    describe('after a stale Car refusal', () => {
        async function refuseStaleCar(): Promise<Setup> {
            const setup = await setUp(false);
            const { fixture, refusal } = setup;
            await fillRequiredFields(fixture);
            await chooseTeacher(fixture, YAEL);
            await chooseCar(fixture, I20);
            setup.confirm.mockImplementation(() => {
                refusal.set(STALE_CAR_REFUSAL);
                return Promise.resolve(false);
            });
            await internals(fixture).submit();
            await fixture.whenStable();

            return setup;
        }

        it('keeps Save disabled until another Car is chosen', async () => {
            //given
            const { fixture } = await refuseStaleCar();
            expect(saveButton(fixture).disabled).toBe(true);

            //when
            await chooseCar(fixture, GOLF);

            //then
            expect(saveButton(fixture).disabled).toBe(false);
        });

        it('keeps Save disabled until Refresh is clicked', async () => {
            //given
            const { fixture, refusal, refreshCars } = await refuseStaleCar();
            expect(saveButton(fixture).disabled).toBe(true);

            //when
            internals(fixture).onRefreshCars();
            refusal.set(null);
            await fixture.whenStable();

            //then
            expect(refreshCars).toHaveBeenCalledOnce();
            expect(saveButton(fixture).disabled).toBe(false);
        });

        it('shows no empty Car error line as soon as Refresh is clicked, before the new options arrive', async () => {
            //given
            const { fixture } = await refuseStaleCar();
            expect(errorLines(fixture)).toBe(1);

            //when
            internals(fixture).onRefreshCars();
            await fixture.whenStable();

            //then
            expect(errorLines(fixture)).toBe(0);
        });
    });

    describe('after a national ID refusal', () => {
        it('clears the banner and the field error when the national ID is edited', async () => {
            //given
            const { fixture, refusal, clearRefusal } = await setUp(false);
            await fillRequiredFields(fixture);
            await chooseTeacher(fixture, DAFNA);
            refusal.set(NATIONAL_ID_REFUSAL);
            await internals(fixture).submit();
            await fixture.whenStable();
            expect(refusalBanner(fixture)).not.toBeNull();
            expect(internals(fixture).form.controls.nationalId.hasError('refused')).toBe(true);

            //when
            await typeNationalId(fixture, '123456789');

            //then
            expect(clearRefusal).toHaveBeenCalledOnce();
            expect(refusalBanner(fixture)).toBeNull();
            expect(internals(fixture).form.controls.nationalId.hasError('refused')).toBe(false);
        });

        it('leaves a stale Car refusal alone when the national ID is edited', async () => {
            //given
            const { fixture, refusal, clearRefusal } = await setUp();
            refusal.set(STALE_CAR_REFUSAL);
            await fixture.whenStable();

            //when
            await typeNationalId(fixture, '123456789');

            //then
            expect(clearRefusal).not.toHaveBeenCalled();
            expect(refusalBanner(fixture)).not.toBeNull();
        });
    });
});
