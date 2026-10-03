import { provideZonelessChangeDetection, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { TranslocoTestingModule } from '@jsverse/transloco';
import { providePrimeNG } from 'primeng/config';
import { LanguageService } from '../../../../../core/language.service';
import { PublicationState } from '../../../../../shared/models/publication-state.enum';
import { PublicationsStore } from '../../../state/publications.store';
import { PublicationsDashboardPage } from './publications-dashboard.page';

const SHARE_LINK = 'https://school.example/s/link-token';
const WEEK_START = '2026-10-04';
const WEEK_LABEL = '4 Oct - 9 Oct 2026';

function fakeStore(overrides: Record<string, unknown>) {
    return {
        teachers: signal([{ id: 'teacher-cohen', name: 'Teacher Cohen' }]),
        selectedTeacherId: signal('teacher-cohen'),
        weekNumber: signal(41),
        weekChoices: signal([{ weekStart: WEEK_START, label: WEEK_LABEL }]),
        selectedWeekStart: signal(WEEK_START),
        weekLabel: signal(WEEK_LABEL),
        state: signal<PublicationState | undefined>(PublicationState.open),
        publication: signal({
            id: 'publication-1',
            weekStart: WEEK_START,
            weekNumber: 41,
            state: PublicationState.open,
            linkToken: 'link-token',
            windowStartUtc: '2026-10-01T16:00:00Z',
            windowEndUtc: '2026-10-09T11:00:00Z',
        }),
        hasPublication: signal(true),
        isLoading: signal(false),
        loadError: signal<string | undefined>(undefined),
        dashboard: signal(undefined),
        shareLink: signal(SHARE_LINK),
        dataAsOf: signal(''),
        slotCounts: signal([]),
        windowTimes: signal({}),
        selectTeacher: () => undefined,
        selectWeek: () => undefined,
        copyLink: async () => undefined,
        refresh: () => undefined,
        downloadExcel: async () => undefined,
        ...overrides,
    };
}

async function render(store: ReturnType<typeof fakeStore>): Promise<HTMLElement> {
    TestBed.configureTestingModule({
        imports: [
            PublicationsDashboardPage,
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

    const fixture = TestBed.createComponent(PublicationsDashboardPage);
    await fixture.whenStable();

    return fixture.nativeElement as HTMLElement;
}

describe('PublicationsDashboardPage', () => {
    it('shows the share link while the week is open', async () => {
        //given
        const store = fakeStore({});

        //when
        const page = await render(store);

        //then
        expect(page.querySelector('.share-link__value')?.textContent?.trim()).toBe(SHARE_LINK);
    });

    it('does not offer the share link once the week is closed', async () => {
        //given
        const store = fakeStore({ state: signal(PublicationState.closed) });

        //when
        const page = await render(store);

        //then
        expect(page.querySelector('app-share-link-box')).toBeNull();
    });
});
