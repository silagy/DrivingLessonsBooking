import { provideZonelessChangeDetection, signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslocoTestingModule } from '@jsverse/transloco';
import { DynamicDialogConfig, DynamicDialogRef } from 'primeng/dynamicdialog';
import { StudentRefusal, StudentRefusalKind } from '../../../domain/student-refusal';
import { Student } from '../../../domain/student.model';
import { Transmission } from '../../../domain/transmission.enum';
import { DeactivateStudentDialog, DeactivateStudentDialogData } from './deactivate-student.dialog';

const ITAI: Student = {
    id: 'student-itai',
    nationalId: '207815432',
    name: 'Itai Peretz',
    phone: '050-6612034',
    teacherId: 'teacher-yael',
    teacherName: 'Yael Carmi',
    carId: 'car-i20',
    carName: 'i20 Silver',
    carTransmission: Transmission.manual,
    isCarOfTeacher: true,
    isActive: true,
};

interface Setup {
    fixture: ComponentFixture<DeactivateStudentDialog>;
    refusal: ReturnType<typeof signal<StudentRefusal | null>>;
    isDeactivating: ReturnType<typeof signal<boolean>>;
    close: ReturnType<typeof vi.fn>;
}

async function setUp(confirmResult: boolean, student: Student = ITAI): Promise<Setup> {
    const refusal = signal<StudentRefusal | null>(null);
    const isDeactivating = signal(false);
    const close = vi.fn();
    const data: DeactivateStudentDialogData = {
        student,
        refusal,
        isDeactivating,
        confirm: () => Promise.resolve(confirmResult),
    };

    TestBed.configureTestingModule({
        imports: [
            DeactivateStudentDialog,
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

    const fixture = TestBed.createComponent(DeactivateStudentDialog);
    await fixture.whenStable();

    return { fixture, refusal, isDeactivating, close };
}

function host(fixture: ComponentFixture<DeactivateStudentDialog>): HTMLElement {
    return fixture.nativeElement as HTMLElement;
}

function confirmButton(fixture: ComponentFixture<DeactivateStudentDialog>): HTMLButtonElement {
    return host(fixture).querySelector('.deactivate-student__confirm button') as HTMLButtonElement;
}

describe('DeactivateStudentDialog', () => {
    it('shows who is being deactivated, with the national ID left-to-right', async () => {
        //given
        const { fixture } = await setUp(true);

        //then
        expect(host(fixture).querySelector('.who-card__name')?.textContent).toContain('Itai Peretz');
        expect(host(fixture).querySelector('.who-card__national-id')?.getAttribute('dir')).toBe('ltr');
        expect(host(fixture).querySelectorAll('.deactivate-student__effect').length).toBe(3);
    });

    it('marks a Car that is not the Teacher\'s in the who card', async () => {
        //given
        const { fixture } = await setUp(true, { ...ITAI, isCarOfTeacher: false });

        //then
        expect(host(fixture).querySelector('.who-card__flag')?.getAttribute('aria-label')).toBe('students.flag.car');
    });

    it('closes once the Student is deactivated', async () => {
        //given
        const { fixture, close } = await setUp(true);

        //when
        confirmButton(fixture).click();
        await fixture.whenStable();

        //then
        expect(close).toHaveBeenCalled();
    });

    it('stays open with the refusal when the deactivation is refused', async () => {
        //given
        const { fixture, close, refusal } = await setUp(false);

        //when
        confirmButton(fixture).click();
        refusal.set({ kind: StudentRefusalKind.other, message: 'Something went wrong.', existingStudentName: null });
        await fixture.whenStable();

        //then
        expect(close).not.toHaveBeenCalled();
        expect(host(fixture).querySelector('app-dialog-refusal')).not.toBeNull();
    });

    it('disables the confirmation while deactivating', async () => {
        //given
        const { fixture, isDeactivating } = await setUp(true);

        //when
        isDeactivating.set(true);
        await fixture.whenStable();

        //then
        expect(confirmButton(fixture).disabled).toBe(true);
    });
});
