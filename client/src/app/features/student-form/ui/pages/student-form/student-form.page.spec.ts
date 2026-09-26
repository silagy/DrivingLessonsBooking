import { HttpErrorResponse } from '@angular/common/http';
import { provideZonelessChangeDetection, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { TranslocoTestingModule } from '@jsverse/transloco';
import { Observable, of, throwError } from 'rxjs';
import { LanguageService } from '../../../../../core/language.service';
import { PublicationState } from '../../../../../shared/models/publication-state.enum';
import { GetPublicationByLinkResponse } from '../../../data/get-publication-by-link.response';
import { SubmissionsApiService } from '../../../data/submissions-api.service';
import { StudentFormPage } from './student-form.page';

const HTTP_NOT_FOUND = 404;
const HTTP_SERVER_ERROR = 500;

function publicationIn(state: PublicationState): () => Observable<GetPublicationByLinkResponse> {
    return () =>
        of({
            weekStart: '2026-11-15',
            weekNumber: 47,
            state,
            windowStartUtc: '2026-11-11T16:00:00Z',
            windowEndUtc: '2026-11-13T12:00:00Z',
        });
}

function failingWith(status: number): () => Observable<never> {
    return () => throwError(() => new HttpErrorResponse({ status }));
}

function provideApiReturning(getPublicationByLink: () => Observable<unknown>): void {
    TestBed.configureTestingModule({
        imports: [TranslocoTestingModule.forRoot({ langs: { en: {} } })],
        providers: [
            provideZonelessChangeDetection(),
            { provide: SubmissionsApiService, useValue: { getPublicationByLink } },
            { provide: LanguageService, useValue: { lang: signal('en') } },
        ],
    });
}

async function renderPage(): Promise<HTMLElement> {
    const fixture = TestBed.createComponent(StudentFormPage);
    fixture.componentRef.setInput('token', 'link-token');
    await fixture.whenStable();

    return fixture.nativeElement as HTMLElement;
}

describe('StudentFormPage', () => {
    it.each([
        { view: 'notYetOpen', load: publicationIn(PublicationState.published), windowRows: 2, hasCaption: false },
        { view: 'open', load: publicationIn(PublicationState.open), windowRows: 1, hasCaption: true },
        { view: 'closed', load: publicationIn(PublicationState.closed), windowRows: 1, hasCaption: false },
        { view: 'invalidLink (draft)', load: publicationIn(PublicationState.draft), windowRows: 0, hasCaption: false },
        { view: 'invalidLink (unknown)', load: failingWith(HTTP_NOT_FOUND), windowRows: 0, hasCaption: false },
        { view: 'loadFailed', load: failingWith(HTTP_SERVER_ERROR), windowRows: 0, hasCaption: false },
    ])('renders the $view view read-only', async ({ load, windowRows, hasCaption }) => {
        //given
        provideApiReturning(load);

        //when
        const page = await renderPage();

        //then
        expect(page.querySelector('.status__heading')).not.toBeNull();
        expect(page.querySelectorAll('form, input, select, textarea').length).toBe(0);
        expect(page.querySelectorAll('.student-form__window dt').length).toBe(windowRows);
        expect(page.querySelector('.student-shell__caption') !== null).toBe(hasCaption);
    });
});
