import { Component, provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { TranslocoTestingModule } from '@jsverse/transloco';
import { providePrimeNG } from 'primeng/config';
import { Teacher } from '../../../domain/teacher.model';
import { TeacherCardComponent } from './teacher-card.component';

const TEACHER: Teacher = { id: 't-2', name: 'Ziv Peer', contactEmail: 'ziv@drivecal.co.il' };

@Component({
    imports: [TeacherCardComponent],
    template: `<app-teacher-card [teacher]="teacher" [cars]="[]" (edit)="edits = edits + 1" />`,
})
class TeacherCardHost {
    teacher = TEACHER;
    edits = 0;
}

async function render(): Promise<{ card: HTMLElement; host: TeacherCardHost }> {
    TestBed.configureTestingModule({
        imports: [
            TeacherCardHost,
            TranslocoTestingModule.forRoot({
                langs: { en: {} },
                translocoConfig: { availableLangs: ['en'], defaultLang: 'en' },
            }),
        ],
        providers: [provideZonelessChangeDetection(), providePrimeNG()],
    });

    const fixture = TestBed.createComponent(TeacherCardHost);
    await fixture.whenStable();

    return { card: fixture.nativeElement as HTMLElement, host: fixture.componentInstance };
}

describe('TeacherCardComponent', () => {
    it('labels the compact edit button for screen readers', async () => {
        //given
        const { card } = await render();

        //when
        const button = card.querySelector('button.card__edit--compact');

        //then
        expect(button?.getAttribute('aria-label')).toBe('teachers.editTeacherAria');
    });

    it('emits edit from the compact edit button', async () => {
        //given
        const { card, host } = await render();

        //when
        card.querySelector<HTMLButtonElement>('button.card__edit--compact')!.click();

        //then
        expect(host.edits).toBe(1);
    });
});
