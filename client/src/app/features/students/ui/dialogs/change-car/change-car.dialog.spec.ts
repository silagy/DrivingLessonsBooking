import { provideZonelessChangeDetection, signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { TranslocoTestingModule } from '@jsverse/transloco';
import { DynamicDialogConfig, DynamicDialogRef } from 'primeng/dynamicdialog';
import { CarOption } from '../../../domain/car-option.model';
import { StudentRefusal, StudentRefusalKind } from '../../../domain/student-refusal';
import { Student } from '../../../domain/student.model';
import { Transmission } from '../../../domain/transmission.enum';
import { ChangeCarDialogData } from './change-car-dialog-data';
import { ChangeCarDialog } from './change-car.dialog';

const CARS: CarOption[] = [
    { id: 'car-corolla', name: 'Corolla White', transmission: Transmission.automatic, teacherIds: ['teacher-ronit'] },
    { id: 'car-i20', name: 'i20 Silver', transmission: Transmission.manual, teacherIds: ['teacher-ronit', 'teacher-yael'] },
    { id: 'car-mazda', name: 'Mazda 3 Grey', transmission: Transmission.manual, teacherIds: ['teacher-oren'] },
];

const NOA: Student = {
    id: 'student-noa',
    nationalId: '205374184',
    name: 'Noa Mizrahi',
    phone: '050-1234567',
    teacherId: 'teacher-ronit',
    teacherName: 'Ronit Avraham',
    carId: 'car-corolla',
    carName: 'Corolla White',
    carTransmission: Transmission.automatic,
    isCarOfTeacher: true,
    isActive: true,
};

const ROI: Student = {
    ...NOA,
    id: 'student-roi',
    name: 'Roi Almog',
    teacherId: 'teacher-oren',
    teacherName: 'Oren Levi',
    isCarOfTeacher: false,
};

const OMER: Student = {
    ...NOA,
    id: 'student-omer',
    name: 'Omer Shalev',
    carId: 'car-i20',
    carName: 'i20 Silver',
    carTransmission: Transmission.manual,
};

const LIA: Student = {
    ...NOA,
    id: 'student-lia',
    name: 'Lia Hadad',
    teacherId: 'teacher-oren',
    teacherName: 'Oren Levi',
    carId: 'car-mazda',
    carName: 'Mazda 3 Grey',
    carTransmission: Transmission.manual,
};

const SAME_CAR: StudentRefusal = {
    kind: StudentRefusalKind.sameCar,
    message: "This is already this Student's Car. Refresh the screen.",
    existingStudentName: null,
};

const OTHER_REFUSAL: StudentRefusal = {
    kind: StudentRefusalKind.other,
    message: 'Something went wrong.',
    existingStudentName: null,
};

interface Setup {
    fixture: ComponentFixture<ChangeCarDialog>;
    refusal: ReturnType<typeof signal<StudentRefusal | null>>;
    confirm: ReturnType<typeof vi.fn<(carId: string) => Promise<boolean>>>;
    refresh: ReturnType<typeof vi.fn>;
    clearRefusal: ReturnType<typeof vi.fn>;
    close: ReturnType<typeof vi.fn>;
}

async function setUp(student: Student = NOA, confirmResult = true): Promise<Setup> {
    const refusal = signal<StudentRefusal | null>(null);
    const confirm = vi.fn<(carId: string) => Promise<boolean>>(() => Promise.resolve(confirmResult));
    const refresh = vi.fn();
    const clearRefusal = vi.fn(() => refusal.set(null));
    const close = vi.fn();
    const data: ChangeCarDialogData = {
        student,
        cars: signal(CARS),
        refusal,
        isSaving: signal(false),
        confirm,
        refresh,
        clearRefusal,
    };

    TestBed.configureTestingModule({
        imports: [
            ChangeCarDialog,
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

    const fixture = TestBed.createComponent(ChangeCarDialog);
    await fixture.whenStable();

    return { fixture, refusal, confirm, refresh, clearRefusal, close };
}

interface DialogInternals {
    selectedCarId: () => string | null;
    canSave: () => boolean;
    selectCar: (carId: string) => void;
    submit: () => Promise<void>;
}

function internals(fixture: ComponentFixture<ChangeCarDialog>): DialogInternals {
    return fixture.componentInstance as unknown as DialogInternals;
}

function host(fixture: ComponentFixture<ChangeCarDialog>): HTMLElement {
    return fixture.nativeElement as HTMLElement;
}

function cards(fixture: ComponentFixture<ChangeCarDialog>): HTMLElement[] {
    return Array.from(host(fixture).querySelectorAll<HTMLElement>('.change-car__card'));
}

describe('ChangeCarDialog', () => {
    it("lists the Teacher's Cars with the current one disabled, and waits for a choice", async () => {
        //given
        const { fixture } = await setUp();

        //then
        expect(cards(fixture).map((card) => card.textContent?.trim().split(/\s+/)[0])).toEqual(['Corolla', 'i20']);
        expect(cards(fixture)[0].classList).toContain('change-car__card--current');
        expect(cards(fixture)[1].classList).not.toContain('change-car__card--current');
        expect(internals(fixture).canSave()).toBe(false);
        expect(host(fixture).querySelector('.change-car__flag')).toBeNull();
    });

    it('keeps the other cards selectable while the current one is disabled', async () => {
        //given
        const { fixture } = await setUp();
        await fixture.whenStable();

        //then
        const inputs = cards(fixture).map((card) => card.querySelector('input') as HTMLInputElement);
        expect(inputs.map((input) => input.disabled)).toEqual([true, false]);
    });

    it('keeps the other cards selectable when the current Car is listed last', async () => {
        //given
        const { fixture } = await setUp(OMER);
        await fixture.whenStable();

        //then
        const inputs = cards(fixture).map((card) => card.querySelector('input') as HTMLInputElement);
        expect(inputs.map((input) => input.disabled)).toEqual([false, true]);
    });

    it('changes the Car and closes', async () => {
        //given
        const { fixture, confirm, close } = await setUp();
        internals(fixture).selectCar('car-i20');
        await fixture.whenStable();

        //when
        await internals(fixture).submit();

        //then
        expect(confirm).toHaveBeenCalledWith('car-i20');
        expect(close).toHaveBeenCalled();
    });

    it('enables every card for a flagged Student and says why', async () => {
        //given
        const { fixture } = await setUp(ROI);

        //then
        expect(cards(fixture).length).toBe(1);
        expect(cards(fixture)[0].classList).not.toContain('change-car__card--current');
        expect(host(fixture).querySelector('.change-car__flag')?.textContent).toContain('students.changeCar.flagTitle');
    });

    it('shows Close only when the Teacher has no other Cars', async () => {
        //given
        const { fixture } = await setUp(LIA);

        //then
        expect(host(fixture).querySelector('.change-car__none')?.textContent).toContain('students.changeCar.none');
        expect(cards(fixture).length).toBe(0);
        expect(host(fixture).querySelector('button[type="submit"]')).toBeNull();
        expect(host(fixture).querySelector('.dialog-form__actions')?.textContent).toContain('general.close');
    });

    it('shows a stale refusal with Refresh, and Refresh closes the dialog and reloads', async () => {
        //given
        const { fixture, refusal, refresh, close } = await setUp(NOA, false);
        internals(fixture).selectCar('car-i20');
        refusal.set(SAME_CAR);
        await fixture.whenStable();

        //when
        (host(fixture).querySelector('.change-car__refresh') as HTMLButtonElement).click();

        //then
        expect(host(fixture).querySelector('app-dialog-refusal')?.textContent).toContain('students.changeCar.sameCar');
        expect(refresh).toHaveBeenCalled();
        expect(close).toHaveBeenCalled();
    });

    it('forgets the refusal when another Car is chosen', async () => {
        //given
        const { fixture, refusal, clearRefusal } = await setUp(NOA, false);
        refusal.set(SAME_CAR);
        await fixture.whenStable();

        //when
        internals(fixture).selectCar('car-i20');
        await fixture.whenStable();

        //then
        expect(clearRefusal).toHaveBeenCalled();
        expect(internals(fixture).canSave()).toBe(true);
    });

    it('keeps Save enabled after a refusal that is not stale so the Administrator can retry', async () => {
        //given
        const { fixture, refusal } = await setUp(NOA, false);
        internals(fixture).selectCar('car-i20');

        //when
        refusal.set(OTHER_REFUSAL);
        await fixture.whenStable();

        //then
        expect(internals(fixture).canSave()).toBe(true);
    });

    it('keeps Save disabled while a stale refusal shows', async () => {
        //given
        const { fixture, refusal } = await setUp(NOA, false);
        internals(fixture).selectCar('car-i20');

        //when
        refusal.set(SAME_CAR);
        await fixture.whenStable();

        //then
        expect(internals(fixture).canSave()).toBe(false);
    });
});
