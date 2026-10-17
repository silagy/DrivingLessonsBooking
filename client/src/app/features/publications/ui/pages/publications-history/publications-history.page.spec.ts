import { provideZonelessChangeDetection, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { TranslocoTestingModule } from '@jsverse/transloco';
import { providePrimeNG } from 'primeng/config';
import { LanguageService } from '../../../../../core/language.service';
import { PublicationState } from '../../../../../shared/models/publication-state.enum';
import { ItemForFindPublicationHistoryResponse } from '../../../data/item-for-find-publication-history.response';
import { PublicationsStore } from '../../../state/publications.store';
import { PublicationsHistoryPage } from './publications-history.page';

const ROWS: ItemForFindPublicationHistoryResponse[] = [
    {
        publicationId: 'publication-draft',
        weekStart: '2026-10-18',
        teacherId: 'teacher-peer',
        teacherName: 'Ziv Peer',
        state: PublicationState.draft,
        windowStartUtc: null,
        windowEndUtc: null,
        latestExcelVersion: null,
    },
    {
        publicationId: 'publication-closed',
        weekStart: '2026-10-11',
        teacherId: 'teacher-peer',
        teacherName: 'Ziv Peer',
        state: PublicationState.closed,
        windowStartUtc: '2026-10-07T15:00:00Z',
        windowEndUtc: '2026-10-09T11:00:00Z',
        latestExcelVersion: 2,
    },
];

function fakeStore(overrides: Record<string, unknown>) {
    return {
        history: signal(ROWS),
        historyIsLoading: signal(false),
        historyError: signal<string | null>(null),
        historyIsEmpty: signal(false),
        downloadExcel: vi.fn(async () => undefined),
        ...overrides,
    };
}

async function render(store: ReturnType<typeof fakeStore>): Promise<HTMLElement> {
    TestBed.configureTestingModule({
        imports: [
            PublicationsHistoryPage,
            TranslocoTestingModule.forRoot({
                langs: { en: {} },
                translocoConfig: { availableLangs: ['en'], defaultLang: 'en' },
            }),
        ],
        providers: [
            provideZonelessChangeDetection(),
            provideRouter([]),
            providePrimeNG(),
            { provide: PublicationsStore, useValue: store },
            { provide: LanguageService, useValue: { lang: signal('en'), locale: signal('en-IL') } },
        ],
    });

    const fixture = TestBed.createComponent(PublicationsHistoryPage);
    await fixture.whenStable();

    return fixture.nativeElement as HTMLElement;
}

describe('PublicationsHistoryPage', () => {
    it('renders one card per history row with its teacher', async () => {
        //given
        const page = await render(fakeStore({}));

        //when
        const teachers = [...page.querySelectorAll('li.history-card .history-card__teacher')].map((name) =>
            name.textContent?.trim(),
        );

        //then
        expect(teachers).toEqual(['Ziv Peer', 'Ziv Peer']);
    });

    it('offers the dashboard on an unclosed week and the download on a closed one', async () => {
        //given
        const page = await render(fakeStore({}));

        //when
        const actions = [...page.querySelectorAll('li.history-card .history-card__action .p-button-label')].map(
            (label) => label.textContent?.trim(),
        );

        //then
        expect(actions).toEqual(['publications.history.viewDashboard', 'publications.history.redownload']);
    });

    it('downloads the closed week from its card', async () => {
        //given
        const store = fakeStore({});
        const page = await render(store);

        //when
        page.querySelectorAll<HTMLButtonElement>('li.history-card button.history-card__action')[1].click();

        //then
        expect(store.downloadExcel).toHaveBeenCalledWith('publication-closed', 'teacher-peer', '2026-10-11');
    });

    it('shows the latest Excel version on the card', async () => {
        //given
        const page = await render(fakeStore({}));

        //when
        const details = [...page.querySelectorAll('li.history-card .history-card__details')].map(
            (detail) => detail.textContent,
        );

        //then
        expect(details[1]).toContain('v2');
    });
});
