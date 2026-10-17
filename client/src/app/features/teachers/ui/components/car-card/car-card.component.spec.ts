import { Component, provideZonelessChangeDetection, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { TranslocoTestingModule } from '@jsverse/transloco';
import { providePrimeNG } from 'primeng/config';
import { LanguageService } from '../../../../../core/language.service';
import { Car } from '../../../domain/car.model';
import { Transmission } from '../../../domain/transmission.enum';
import { CarCardComponent } from './car-card.component';

const CAR: Car = {
    id: 'car-1',
    name: 'Sandero',
    type: 'Private',
    transmission: Transmission.manual,
    assignedTeachers: [
        { id: 't-1', name: 'Osherat Peer' },
        { id: 't-2', name: 'Ziv Peer' },
    ],
};

@Component({
    imports: [CarCardComponent],
    template: `<app-car-card [car]="car" [teachers]="[]" (edit)="edits = edits + 1" />`,
})
class CarCardHost {
    car = CAR;
    edits = 0;
}

async function render(): Promise<{ card: HTMLElement; host: CarCardHost }> {
    TestBed.configureTestingModule({
        imports: [
            CarCardHost,
            TranslocoTestingModule.forRoot({
                langs: { en: {} },
                translocoConfig: { availableLangs: ['en'], defaultLang: 'en' },
            }),
        ],
        providers: [
            provideZonelessChangeDetection(),
            providePrimeNG(),
            { provide: LanguageService, useValue: { lang: signal('en'), isRtl: signal(false) } },
        ],
    });

    const fixture = TestBed.createComponent(CarCardHost);
    await fixture.whenStable();

    return { card: fixture.nativeElement as HTMLElement, host: fixture.componentInstance };
}

describe('CarCardComponent', () => {
    it('gives each assigned teacher chip their initials', async () => {
        //given
        const { card } = await render();

        //when
        const initials = [...card.querySelectorAll('.car__chip-initials')].map((chip) => chip.textContent?.trim());

        //then
        expect(initials).toEqual(['OP', 'ZP']);
    });

    it('emits edit from the compact edit button', async () => {
        //given
        const { card, host } = await render();

        //when
        card.querySelector<HTMLButtonElement>('button.car__edit--compact')!.click();

        //then
        expect(host.edits).toBe(1);
    });
});
