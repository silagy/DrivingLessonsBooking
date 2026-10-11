import { Component, provideZonelessChangeDetection, Type } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { LockedFieldComponent } from './locked-field.component';

@Component({
    imports: [LockedFieldComponent],
    template: `<app-locked-field label="Teacher" value="Yael Carmi" />`,
})
class LockedFieldHost {}

@Component({
    imports: [LockedFieldComponent],
    template: `<app-locked-field label="Teacher" value="Yael Carmi">cannot be changed</app-locked-field>`,
})
class LockedFieldWithNoteHost {}

async function render(host: Type<unknown>): Promise<HTMLElement> {
    TestBed.configureTestingModule({
        imports: [host],
        providers: [provideZonelessChangeDetection()],
    });

    const fixture = TestBed.createComponent(host);
    await fixture.whenStable();

    return fixture.nativeElement as HTMLElement;
}

describe('LockedFieldComponent', () => {
    it('shows the label above the locked value', async () => {
        //given
        const element = await render(LockedFieldHost);

        //when
        const field = element.querySelector('app-locked-field');

        //then
        expect(field?.firstElementChild?.classList).toContain('field__label');
        expect(field?.firstElementChild?.textContent?.trim()).toBe('Teacher');
        expect(element.querySelector('.locked-field__value')?.textContent?.trim()).toBe('Yael Carmi');
    });

    it('shows a lock icon that screen readers skip', async () => {
        //given
        const element = await render(LockedFieldHost);

        //when
        const icon = element.querySelector('.locked-field .pi-lock');

        //then
        expect(icon).not.toBeNull();
        expect(icon?.getAttribute('aria-hidden')).toBe('true');
    });

    it('shows a projected note after the value', async () => {
        //given
        const element = await render(LockedFieldWithNoteHost);

        //when
        const note = element.querySelector('.locked-field .locked-field__value + .locked-field__note');

        //then
        expect(note?.textContent?.trim()).toBe('cannot be changed');
    });

    it('leaves the note empty when none is projected', async () => {
        //given
        const element = await render(LockedFieldHost);

        //when
        const note = element.querySelector('.locked-field__note');

        //then
        expect(note?.childNodes.length).toBe(0);
    });
});
