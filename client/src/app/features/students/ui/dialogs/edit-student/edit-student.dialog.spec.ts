import { provideZonelessChangeDetection, signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslocoTestingModule } from '@jsverse/transloco';
import { DynamicDialogConfig, DynamicDialogRef } from 'primeng/dynamicdialog';
import { StudentDetailsChange } from '../../../domain/student-details-change.model';
import { StudentDetails } from '../../../domain/student-details.model';
import { StudentRefusal, StudentRefusalKind } from '../../../domain/student-refusal';
import { Transmission } from '../../../domain/transmission.enum';
import { EditStudentDialogData } from './edit-student-dialog-data';
import { EditStudentDialog } from './edit-student.dialog';

const NOA: StudentDetails = {
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
    address: '12 HaRimon St, Modiin',
    startDate: '2026-09-01',
    licenseType: 'B',
    isActive: true,
};

const NATIONAL_ID_IN_USE: StudentRefusal = {
    kind: StudentRefusalKind.nationalIdInUse,
    message: 'A Student with this national ID already exists: Omer Shalev.',
    existingStudentName: 'Omer Shalev',
};

interface Setup {
    fixture: ComponentFixture<EditStudentDialog>;
    refusal: ReturnType<typeof signal<StudentRefusal | null>>;
    isSaving: ReturnType<typeof signal<boolean>>;
    confirm: ReturnType<typeof vi.fn<(change: StudentDetailsChange) => Promise<boolean>>>;
    clearRefusal: ReturnType<typeof vi.fn>;
    close: ReturnType<typeof vi.fn>;
}

async function setUp(confirmResult = true, details: StudentDetails = NOA): Promise<Setup> {
    const refusal = signal<StudentRefusal | null>(null);
    const isSaving = signal(false);
    const confirm = vi.fn<(change: StudentDetailsChange) => Promise<boolean>>(() => Promise.resolve(confirmResult));
    const clearRefusal = vi.fn(() => refusal.set(null));
    const close = vi.fn();
    const data: EditStudentDialogData = { details, refusal, isSaving, confirm, clearRefusal };

    TestBed.configureTestingModule({
        imports: [
            EditStudentDialog,
            TranslocoTestingModule.forRoot({
                langs: { en: {} },
                translocoConfig: { availableLangs: ['en'], defaultLang: 'en' },
            }),
        ],
        providers: [
            provideZonelessChangeDetection(),
            { provide: DynamicDialogConfig, useValue: { data } },
            { provide: DynamicDialogRef, useValue: { close } },
        ],
    });

    const fixture = TestBed.createComponent(EditStudentDialog);
    await fixture.whenStable();

    return { fixture, refusal, isSaving, confirm, clearRefusal, close };
}

interface DialogInternals {
    form: EditStudentDialog['form'];
    submit: () => Promise<void>;
}

function internals(fixture: ComponentFixture<EditStudentDialog>): DialogInternals {
    return fixture.componentInstance as unknown as DialogInternals;
}

function host(fixture: ComponentFixture<EditStudentDialog>): HTMLElement {
    return fixture.nativeElement as HTMLElement;
}

function saveButton(fixture: ComponentFixture<EditStudentDialog>): HTMLButtonElement {
    return host(fixture).querySelector('button[type="submit"]') as HTMLButtonElement;
}

async function typeNationalId(fixture: ComponentFixture<EditStudentDialog>, value: string): Promise<void> {
    const input = host(fixture).querySelector('#edit-student-national-id') as HTMLInputElement;
    input.value = value;
    input.dispatchEvent(new Event('input'));
    await fixture.whenStable();
}

describe('EditStudentDialog', () => {
    it("opens filled with the Student's details", async () => {
        //given
        const { fixture } = await setUp();

        //then
        expect(internals(fixture).form.getRawValue()).toEqual({
            nationalId: '205374184',
            name: 'Noa Mizrahi',
            phone: '050-1234567',
            address: '12 HaRimon St, Modiin',
            startDate: new Date(2026, 8, 1),
            licenseType: 'B',
        });
        expect(host(fixture).querySelector('.locked-field__value')?.textContent).toContain('Ronit Avraham');
        expect(host(fixture).querySelector('#edit-student-national-id')?.getAttribute('dir')).toBe('ltr');
        expect(host(fixture).querySelector('.edit-student__teacher-and-car-hint')?.textContent).toContain('students.edit.teacherAndCarHint');
    });

    it('saves the changed details and closes', async () => {
        //given
        const { fixture, confirm, close } = await setUp();
        internals(fixture).form.patchValue({ phone: ' 050-1234568 ', address: '  ' });

        //when
        await internals(fixture).submit();

        //then
        expect(confirm).toHaveBeenCalledWith({
            nationalId: '205374184',
            name: 'Noa Mizrahi',
            phone: '050-1234568',
            address: null,
            startDate: '2026-09-01',
            licenseType: 'B',
        });
        expect(close).toHaveBeenCalled();
    });

    it('disables Save while a required detail is empty', async () => {
        //given
        const { fixture } = await setUp();

        //when
        internals(fixture).form.controls.name.setValue('');
        await fixture.whenStable();

        //then
        expect(saveButton(fixture).disabled).toBe(true);
    });

    it('disables Save while saving', async () => {
        //given
        const { fixture, isSaving } = await setUp();

        //when
        isSaving.set(true);
        await fixture.whenStable();

        //then
        expect(saveButton(fixture).disabled).toBe(true);
    });

    describe('after a national ID refusal', () => {
        async function refusedWith(refusal: StudentRefusal): Promise<Setup> {
            const setup = await setUp(false);
            setup.confirm.mockImplementation(() => {
                setup.refusal.set(refusal);
                return Promise.resolve(false);
            });
            await internals(setup.fixture).submit();
            await setup.fixture.whenStable();

            return setup;
        }

        it('stays open, shows the refusal first and marks the national ID', async () => {
            //given
            const { fixture, close } = await refusedWith(NATIONAL_ID_IN_USE);

            //then
            expect(close).not.toHaveBeenCalled();
            expect(host(fixture).querySelector('app-dialog-refusal')).not.toBeNull();
            expect(host(fixture).querySelectorAll('.field__error').length).toBe(1);
            expect(saveButton(fixture).disabled).toBe(true);
        });

        it('clears the refusal when the national ID is edited', async () => {
            //given
            const { fixture, clearRefusal } = await refusedWith(NATIONAL_ID_IN_USE);

            //when
            await typeNationalId(fixture, '205374184');

            //then
            expect(clearRefusal).toHaveBeenCalled();
            expect(host(fixture).querySelector('app-dialog-refusal')).toBeNull();
            expect(saveButton(fixture).disabled).toBe(false);
        });

        it('marks the national ID for an invalid one too', async () => {
            //given
            const { fixture } = await refusedWith({
                kind: StudentRefusalKind.nationalIdInvalid,
                message: "This national ID isn't valid.",
                existingStudentName: null,
            });

            //then
            expect(host(fixture).querySelectorAll('.field__error').length).toBe(1);
        });
    });

    it('opens with blank optional details for a Student who has none', async () => {
        //given
        const { fixture } = await setUp(true, { ...NOA, address: null, startDate: null, licenseType: null });

        //then
        const value = internals(fixture).form.getRawValue();
        expect([value.address, value.startDate, value.licenseType]).toEqual(['', null, '']);
    });
});
