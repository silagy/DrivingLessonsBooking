import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TranslationsRevision, TranslocoHttpLoader } from './transloco-loader';

describe('TranslocoHttpLoader', () => {
    let loader: TranslocoHttpLoader;
    let http: HttpTestingController;

    beforeEach(() => {
        TestBed.configureTestingModule({
            providers: [provideHttpClient(), provideHttpClientTesting()],
        });
        loader = TestBed.inject(TranslocoHttpLoader);
        http = TestBed.inject(HttpTestingController);
    });

    afterEach(() => http.verify());

    it('requests the language file at the current revision so a stale cached copy is never reused', () => {
        loader.getTranslation('he').subscribe();

        const request = http.expectOne(`/i18n/he.json?revision=${TranslationsRevision}`);
        expect(request.request.method).toBe('GET');
        request.flush({});
    });
});
