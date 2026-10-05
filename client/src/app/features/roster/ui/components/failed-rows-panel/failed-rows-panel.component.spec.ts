import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { TranslocoTestingModule } from '@jsverse/transloco';
import en from '../../../../../../../public/i18n/en.json';
import he from '../../../../../../../public/i18n/he.json';
import { FailureForGetLatestRosterImportResponse } from '../../../data/get-latest-roster-import.response';
import { RosterRowFailureReason } from '../../../domain/roster-row-failure-reason.enum';
import { FailedRowsPanelComponent } from './failed-rows-panel.component';

const CAR_NOT_OF_TEACHER: FailureForGetLatestRosterImportResponse = {
    rowNumber: 3,
    studentName: 'דנה כהן',
    reason: RosterRowFailureReason.carNotAssignedToTeacher,
};

async function reasonShownIn(lang: 'he' | 'en'): Promise<string | undefined> {
    TestBed.configureTestingModule({
        imports: [
            FailedRowsPanelComponent,
            TranslocoTestingModule.forRoot({
                langs: { he, en },
                translocoConfig: { availableLangs: ['he', 'en'], defaultLang: lang },
            }),
        ],
        providers: [provideZonelessChangeDetection()],
    });

    const fixture = TestBed.createComponent(FailedRowsPanelComponent);
    fixture.componentRef.setInput('failures', [CAR_NOT_OF_TEACHER]);
    await fixture.whenStable();

    return (fixture.nativeElement as HTMLElement).querySelector('.failed-panel__reason')?.textContent?.trim();
}

describe('FailedRowsPanelComponent', () => {
    it('explains in Hebrew that the Car is not assigned to the Teacher', async () => {
        //when
        const reason = await reasonShownIn('he');

        //then
        expect(reason).toBe('הרכב לא משויך למורה הזה');
    });

    it('explains in English that the Car is not assigned to the Teacher', async () => {
        //when
        const reason = await reasonShownIn('en');

        //then
        expect(reason).toBe('Car is not assigned to this Teacher');
    });
});
