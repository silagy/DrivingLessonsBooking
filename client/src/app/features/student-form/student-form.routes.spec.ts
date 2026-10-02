import { Component, provideZonelessChangeDetection, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter, withComponentInputBinding } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { TranslocoTestingModule } from '@jsverse/transloco';
import { NEVER } from 'rxjs';
import { LanguageService } from '../../core/language.service';
import { SubmissionsApiService } from './data/submissions-api.service';
import { StudentFormStore } from './state/student-form.store';
import { StudentFormPage } from './ui/pages/student-form/student-form.page';

@Component({ template: '' })
class ElsewherePage {}

describe('student-form routes', () => {
    beforeEach(() => {
        TestBed.configureTestingModule({
            imports: [TranslocoTestingModule.forRoot({ langs: { en: {} } })],
            providers: [
                provideZonelessChangeDetection(),
                provideRouter(
                    [
                        { path: 's', loadChildren: () => import('./student-form.routes') },
                        { path: 'elsewhere', component: ElsewherePage },
                    ],
                    withComponentInputBinding(),
                ),
                { provide: SubmissionsApiService, useValue: { getPublicationByLink: () => NEVER } },
                { provide: LanguageService, useValue: { lang: signal('en'), locale: signal('en-IL') } },
            ],
        });
    });

    it('gives every visit to the student link a fresh store', async () => {
        //given
        const harness = await RouterTestingHarness.create();
        await harness.navigateByUrl('/s/token-a', StudentFormPage);
        const firstVisitStore = harness.routeDebugElement!.injector.get(StudentFormStore);

        //when
        await harness.navigateByUrl('/elsewhere');
        await harness.navigateByUrl('/s/token-a', StudentFormPage);

        //then
        expect(harness.routeDebugElement!.injector.get(StudentFormStore)).not.toBe(firstVisitStore);
    });
});
