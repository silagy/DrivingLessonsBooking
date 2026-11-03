import { provideZonelessChangeDetection } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { NgModel } from '@angular/forms';
import { By } from '@angular/platform-browser';
import { TranslocoTestingModule } from '@jsverse/transloco';
import { providePrimeNG } from 'primeng/config';
import { ReminderRow } from '../../../domain/reminder-row.model';
import { TeacherOption } from '../../../domain/teacher-option.model';
import { StudentsWithoutSubmissionComponent } from './students-without-submission.component';

const NOA: ReminderRow = {
    studentId: 'student-noa',
    studentName: 'Noa Sample',
    teacherName: 'Teacher Cohen',
    phone: '050-0000001',
    whatsAppUrl: 'https://wa.me/972500000001?text=reminder',
};

const OMER: ReminderRow = {
    studentId: 'student-omer',
    studentName: 'Omer Sample',
    teacherName: 'Teacher Levi',
    phone: '03-0000002',
    whatsAppUrl: null,
};

const TEACHERS: TeacherOption[] = [
    { id: 'teacher-cohen', name: 'Teacher Cohen', isMe: false },
    { id: 'teacher-levi', name: 'Teacher Levi', isMe: false },
];

interface SectionOptions {
    rows?: ReminderRow[];
    isLoading?: boolean;
    errorKey?: string | null;
    showTeacher?: boolean;
    selectedTeacherId?: string | null;
}

async function render(options: SectionOptions): Promise<ComponentFixture<StudentsWithoutSubmissionComponent>> {
    TestBed.configureTestingModule({
        imports: [
            StudentsWithoutSubmissionComponent,
            TranslocoTestingModule.forRoot({
                langs: { en: { publications: { reminders: { title: 'Not sent ({{count}})' } } } },
                translocoConfig: { availableLangs: ['en'], defaultLang: 'en' },
            }),
        ],
        providers: [provideZonelessChangeDetection(), providePrimeNG()],
    });

    const fixture = TestBed.createComponent(StudentsWithoutSubmissionComponent);
    fixture.componentRef.setInput('rows', options.rows ?? [NOA, OMER]);
    fixture.componentRef.setInput('isLoading', options.isLoading ?? false);
    fixture.componentRef.setInput('errorKey', options.errorKey ?? null);
    fixture.componentRef.setInput('showTeacher', options.showTeacher ?? true);
    fixture.componentRef.setInput('teachers', TEACHERS);
    fixture.componentRef.setInput('selectedTeacherId', options.selectedTeacherId ?? null);
    fixture.componentRef.setInput('dataAsOf', '3 Nov 2026, 12:04');
    await fixture.whenStable();

    return fixture;
}

function host(fixture: ComponentFixture<StudentsWithoutSubmissionComponent>): HTMLElement {
    return fixture.nativeElement as HTMLElement;
}

function rowOf(fixture: ComponentFixture<StudentsWithoutSubmissionComponent>, name: string): HTMLElement {
    const rows = Array.from(host(fixture).querySelectorAll<HTMLElement>('li'));

    return rows.find((row) => row.textContent?.includes(name))!;
}

function teacherFilter(fixture: ComponentFixture<StudentsWithoutSubmissionComponent>): NgModel {
    return fixture.debugElement.query(By.directive(NgModel)).injector.get(NgModel);
}

describe('StudentsWithoutSubmissionComponent', () => {
    beforeEach(() => {
        Object.defineProperty(globalThis, 'matchMedia', {
            configurable: true,
            value: () => ({ matches: false, addEventListener: () => undefined, removeEventListener: () => undefined }),
        });
    });

    it('lists each student with a WhatsApp link that opens in a new tab', async () => {
        //given
        const fixture = await render({});

        //when
        const link = rowOf(fixture, NOA.studentName).querySelector('a');

        //then
        expect(link?.getAttribute('href')).toBe(NOA.whatsAppUrl);
        expect(link?.getAttribute('target')).toBe('_blank');
        expect(link?.getAttribute('rel')).toContain('noopener');
        expect(link?.getAttribute('aria-label')).toBe('publications.reminders.sendAria');
    });

    it('shows the phone and no link when the student has no WhatsApp number', async () => {
        //given
        const fixture = await render({});

        //when
        const row = rowOf(fixture, OMER.studentName);

        //then
        expect(row.querySelector('a')).toBeNull();
        expect(row.querySelector('bdi[dir="ltr"]')?.textContent?.trim()).toBe(OMER.phone);
        expect(row.textContent).toContain('publications.reminders.noWhatsApp');
    });

    it('shows each row\'s teacher and the filter only when asked', async () => {
        //given
        const withTeacher = await render({ showTeacher: true });
        const filterLabel = host(withTeacher).querySelector('label');
        const combobox = host(withTeacher).querySelector('[role="combobox"]');
        host(withTeacher).querySelector<HTMLElement>('.p-select')?.click();
        await withTeacher.whenStable();
        const firstOption = document.querySelector('[role="option"]');

        //then
        expect(rowOf(withTeacher, NOA.studentName).textContent).toContain(NOA.teacherName);
        expect(filterLabel?.textContent?.trim()).toBe('publications.reminders.teacherFilter');
        expect(combobox?.getAttribute('aria-labelledby')).toBe(filterLabel?.id);
        expect(firstOption?.textContent?.trim()).toBe('publications.reminders.allTeachers');

        //given
        TestBed.resetTestingModule();

        //when
        const withoutTeacher = await render({ showTeacher: false });

        //then
        expect(rowOf(withoutTeacher, NOA.studentName).textContent).not.toContain(NOA.teacherName);
        expect(host(withoutTeacher).querySelector('p-select')).toBeNull();
    });

    it('emits the chosen teacher, and null for all teachers', async () => {
        //given
        const fixture = await render({ selectedTeacherId: 'teacher-levi' });
        const filtered: (string | null)[] = [];
        fixture.componentInstance.teacherFiltered.subscribe((teacherId) => filtered.push(teacherId));

        //when
        teacherFilter(fixture).viewToModelUpdate('teacher-cohen');
        host(fixture).querySelector<HTMLElement>('.p-select')?.click();
        await fixture.whenStable();
        document.querySelector<HTMLElement>('[role="option"]')?.click();

        //then
        expect(filtered).toEqual(['teacher-cohen', null]);
    });

    it('counts the students it lists in the title', async () => {
        //given
        const fixture = await render({ rows: [NOA, OMER] });

        //when
        const title = host(fixture).querySelector('h2');

        //then
        expect(title?.textContent?.trim()).toBe('Not sent (2)');
    });

    it('says everyone has submitted when the list is empty', async () => {
        //given
        const fixture = await render({ rows: [] });

        //when
        const text = host(fixture).textContent;

        //then
        expect(text).toContain('publications.reminders.empty');
        expect(host(fixture).querySelector('li')).toBeNull();
    });

    it('shows the load error instead of the list', async () => {
        //given
        const fixture = await render({ errorKey: 'publications.reminders.loadFailed' });

        //when
        const alert = host(fixture).querySelector('[role="alert"]');

        //then
        expect(alert?.textContent?.trim()).toBe('publications.reminders.loadFailed');
        expect(host(fixture).querySelector('li')).toBeNull();
    });
});
