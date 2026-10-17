import { provideZonelessChangeDetection } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslocoTestingModule } from '@jsverse/transloco';
import { providePrimeNG } from 'primeng/config';
import { CarMarker } from '../../../domain/car-marker.enum';
import { StudentRow } from '../../../domain/student-row.model';
import { Transmission } from '../../../domain/transmission.enum';
import { StudentCardComponent } from './student-card.component';

const STUDENT: StudentRow = {
    id: 'student-noa',
    nationalId: '205374188',
    name: 'Noa Mizrahi',
    phone: '054-2218830',
    teacherId: 'teacher-peer',
    teacherName: 'Ziv Peer',
    carId: 'car-tesla',
    carName: 'Tesla Ziv',
    carTransmission: Transmission.automatic,
    isCarOfTeacher: true,
    isTeacherDeleted: false,
    isCarDeleted: false,
    isActive: true,
    isNew: false,
    carMarker: null,
};

async function render(overrides: Partial<StudentRow>, disabled = false): Promise<ComponentFixture<StudentCardComponent>> {
    TestBed.configureTestingModule({
        imports: [
            StudentCardComponent,
            TranslocoTestingModule.forRoot({
                langs: { en: {} },
                translocoConfig: { availableLangs: ['en'], defaultLang: 'en' },
            }),
        ],
        providers: [provideZonelessChangeDetection(), providePrimeNG()],
    });

    const fixture = TestBed.createComponent(StudentCardComponent);
    fixture.componentRef.setInput('student', { ...STUDENT, ...overrides });
    fixture.componentRef.setInput('disabled', disabled);
    await fixture.whenStable();

    return fixture;
}

function card(fixture: ComponentFixture<StudentCardComponent>): HTMLElement {
    return (fixture.nativeElement as HTMLElement).querySelector('article.student-card') as HTMLElement;
}

function menuButton(fixture: ComponentFixture<StudentCardComponent>): HTMLButtonElement {
    return card(fixture).querySelector('button.student-card__actions') as HTMLButtonElement;
}

describe('StudentCardComponent', () => {
    it('shows the name, national ID, phone, Teacher and Car', async () => {
        //when
        const fixture = await render({});

        //then
        const text = card(fixture).textContent ?? '';
        expect(card(fixture).querySelector('.student-card__name')?.textContent?.trim()).toBe('Noa Mizrahi');
        expect(text).toContain('205374188');
        expect(text).toContain('054-2218830');
        expect(text).toContain('Ziv Peer');
        expect(text).toContain('Tesla Ziv');
    });

    it('marks a new Student with the New tag and the tinted card', async () => {
        //when
        const fixture = await render({ isNew: true });

        //then
        expect(card(fixture).classList).toContain('student-card--new');
        expect(card(fixture).querySelector('.student-card__top')?.textContent).toContain('students.new');
    });

    it('mutes an Inactive Student but keeps its status and menu', async () => {
        //when
        const fixture = await render({ isActive: false });

        //then
        expect(card(fixture).classList).toContain('student-card--inactive');
        expect(card(fixture).querySelector('.student-card__status')?.textContent).toContain('students.status.inactive');
        expect(menuButton(fixture).disabled).toBe(false);
    });

    it('flags a Car that is not the Teacher\'s', async () => {
        //when
        const fixture = await render({ isCarOfTeacher: false, carMarker: CarMarker.notOfTeacher });

        //then
        expect(card(fixture).querySelector('app-car-flag')).not.toBeNull();
    });

    it('flags a deleted Teacher', async () => {
        //when
        const fixture = await render({ isTeacherDeleted: true });

        //then
        expect(card(fixture).querySelector('app-deleted-flag')).not.toBeNull();
    });

    it('asks for the row menu from its button', async () => {
        //given
        const fixture = await render({});
        const requested: Event[] = [];
        fixture.componentInstance.actionsRequested.subscribe((event) => requested.push(event));

        //when
        menuButton(fixture).click();

        //then
        expect(requested.length).toBe(1);
    });

    it('names the menu button after the Student', async () => {
        //when
        const fixture = await render({});

        //then
        expect(menuButton(fixture).getAttribute('aria-label')).toBe('students.actions.menuFor');
    });

    it('disables the menu button while a change is saving', async () => {
        //when
        const fixture = await render({}, true);

        //then
        expect(menuButton(fixture).disabled).toBe(true);
    });
});
