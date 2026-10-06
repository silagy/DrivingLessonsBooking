import { provideZonelessChangeDetection } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslocoTestingModule } from '@jsverse/transloco';
import { CarFlagComponent } from './car-flag.component';

async function render(isActive: boolean): Promise<ComponentFixture<CarFlagComponent>> {
    TestBed.configureTestingModule({
        imports: [
            CarFlagComponent,
            TranslocoTestingModule.forRoot({
                langs: { en: {} },
                translocoConfig: { availableLangs: ['en'], defaultLang: 'en' },
            }),
        ],
        providers: [provideZonelessChangeDetection()],
    });

    const fixture = TestBed.createComponent(CarFlagComponent);
    fixture.componentRef.setInput('teacherName', 'Yael Carmi');
    fixture.componentRef.setInput('isActive', isActive);
    await fixture.whenStable();

    return fixture;
}

function flag(fixture: ComponentFixture<CarFlagComponent>): HTMLElement {
    return (fixture.nativeElement as HTMLElement).querySelector('.car-flag') as HTMLElement;
}

describe('CarFlagComponent', () => {
    it('says in words, not colour alone, that the Car is not the Teacher\'s', async () => {
        //when
        const fixture = await render(true);

        //then
        expect(flag(fixture).textContent).toContain('students.flag.car');
        expect(flag(fixture).querySelector('.pi-exclamation-circle')?.getAttribute('aria-hidden')).toBe('true');
    });

    it('is reachable from the keyboard and explains the fix to screen readers', async () => {
        //when
        const fixture = await render(true);

        //then
        expect(flag(fixture).getAttribute('role')).toBe('note');
        expect(flag(fixture).getAttribute('tabindex')).toBe('0');
        expect(flag(fixture).getAttribute('aria-label')).toBe('students.flag.tip');
    });

    it('tells an Inactive Student\'s Administrator to mark them active first', async () => {
        //when
        const fixture = await render(false);

        //then
        expect(flag(fixture).getAttribute('aria-label')).toBe('students.flag.tipInactive');
        expect(flag(fixture).classList).toContain('car-flag--muted');
    });
});
