import { provideZonelessChangeDetection } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslocoTestingModule } from '@jsverse/transloco';
import { RemovedPart } from '../../../domain/removed-part.enum';
import { DeletedFlagComponent } from './deleted-flag.component';

async function render(part: RemovedPart): Promise<ComponentFixture<DeletedFlagComponent>> {
    TestBed.configureTestingModule({
        imports: [
            DeletedFlagComponent,
            TranslocoTestingModule.forRoot({
                langs: { en: {} },
                translocoConfig: { availableLangs: ['en'], defaultLang: 'en' },
            }),
        ],
        providers: [provideZonelessChangeDetection()],
    });

    const fixture = TestBed.createComponent(DeletedFlagComponent);
    fixture.componentRef.setInput('part', part);
    await fixture.whenStable();

    return fixture;
}

function flag(fixture: ComponentFixture<DeletedFlagComponent>): HTMLElement {
    return (fixture.nativeElement as HTMLElement).querySelector('.deleted-flag') as HTMLElement;
}

describe('DeletedFlagComponent', () => {
    it.each([RemovedPart.teacher, RemovedPart.car])('says in words that the %s was deleted', async (part) => {
        //when
        const fixture = await render(part);

        //then
        expect(flag(fixture).textContent).toContain(`students.deletedFlag.${part}`);
        expect(flag(fixture).querySelector('.pi-trash')?.getAttribute('aria-hidden')).toBe('true');
    });

    it('is reachable from the keyboard and explains the fix to screen readers', async () => {
        //when
        const fixture = await render(RemovedPart.teacher);

        //then
        expect(flag(fixture).getAttribute('tabindex')).toBe('0');
        expect(flag(fixture).getAttribute('role')).toBe('note');
        expect(flag(fixture).getAttribute('aria-label')).toBe('students.deletedFlag.tip');
    });
});
