import { provideZonelessChangeDetection, signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { NgModel } from '@angular/forms';
import { By } from '@angular/platform-browser';
import { provideRouter, Router } from '@angular/router';
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
        teachers: signal([{ id: 'teacher-cohen', name: 'Teacher Cohen', isMe: false }]),
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
        canChooseTeacher: signal(true),
        canManageLifecycle: signal(true),
        selectTeacher: () => undefined,
        selectWeek: () => undefined,
        copyLink: async () => undefined,
        refresh: () => undefined,
        downloadExcel: async () => undefined,
        ...overrides,
    };
}

async function render(store: ReturnType<typeof fakeStore>): Promise<HTMLElement> {
    const fixture = await renderFixture(store);

    return fixture.nativeElement as HTMLElement;
}

async function renderFixture(
    store: ReturnType<typeof fakeStore>,
): Promise<ComponentFixture<PublicationsDashboardPage>> {
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

    return fixture;
}

function weekPickerModel(fixture: ComponentFixture<PublicationsDashboardPage>): NgModel {
    const weekPicker = fixture.debugElement
        .queryAll(By.directive(NgModel))
        .find((element) => (element.nativeElement as HTMLElement).matches('.dashboard__week-select'));

    return weekPicker!.injector.get(NgModel);
}

function buttonLabels(page: HTMLElement): string[] {
    return Array.from(page.querySelectorAll('.dashboard__actions .p-button-label')).map(
        (label) => label.textContent?.trim() ?? '',
    );
}

const AS_TEACHER = { canChooseTeacher: signal(false), canManageLifecycle: signal(false) };

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

    it('shows the selected week in the week picker', async () => {
        //given
        const store = fakeStore({});

        //when
        const page = await render(store);

        //then
        expect(page.querySelector('.dashboard__week-select .p-select-label')?.textContent?.trim()).toBe(WEEK_LABEL);
    });

    it('explains that the week is not prepared and links to weekly prep', async () => {
        //given
        const store = fakeStore({
            state: signal(undefined),
            publication: signal(undefined),
            hasPublication: signal(false),
            weekNumber: signal(undefined),
        });

        //when
        const page = await render(store);

        //then
        expect(page.querySelector('.dashboard__empty')).not.toBeNull();
        expect(page.querySelector('.dashboard__empty a')?.getAttribute('href')).toBe('/week-schedules');
    });

    it('shows the spinner rather than the empty state while loading', async () => {
        //given
        const store = fakeStore({
            state: signal(undefined),
            publication: signal(undefined),
            hasPublication: signal(false),
            isLoading: signal(true),
        });

        //when
        const page = await render(store);

        //then
        expect(page.querySelector('.dashboard__empty')).toBeNull();
        expect(page.querySelector('p-progressspinner')).not.toBeNull();
    });

    it('shows the spinner in the same card the empty state uses', async () => {
        //given
        const store = fakeStore({
            state: signal(undefined),
            publication: signal(undefined),
            hasPublication: signal(false),
            isLoading: signal(true),
        });

        //when
        const page = await render(store);

        //then
        expect(page.querySelector('.dashboard__card p-progressspinner')).not.toBeNull();
    });

    it('moves to the picked week through the URL and drops a pending publish request', async () => {
        //given
        const store = fakeStore({});
        const fixture = await renderFixture(store);
        const navigate = vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);

        //when
        weekPickerModel(fixture).viewToModelUpdate('2026-10-11');

        //then
        expect(navigate).toHaveBeenCalledWith(
            [],
            expect.objectContaining({
                queryParams: { week: '2026-10-11', publish: null },
                queryParamsHandling: 'merge',
            }),
        );
    });

    it('offers an Administrator the lifecycle control for each state', async () => {
        //given
        const store = fakeStore({ state: signal(PublicationState.closed) });

        //when
        const page = await render(store);

        //then
        expect(buttonLabels(page)).toEqual(['publications.reopenWindow', 'publications.downloadExcel']);
        expect(page.querySelector('.dashboard__teacher-select')).not.toBeNull();
    });

    it('hides the Teacher picker from a Teacher', async () => {
        //given
        const store = fakeStore({ ...AS_TEACHER });

        //when
        const page = await render(store);

        //then
        expect(page.querySelector('.dashboard__teacher-select')).toBeNull();
    });

    it('keeps refresh and the Excel download for a Teacher while the week is open, without extending', async () => {
        //given
        const store = fakeStore({ ...AS_TEACHER });

        //when
        const page = await render(store);

        //then
        expect(buttonLabels(page)).toEqual(['general.refresh', 'publications.downloadExcel']);
    });

    it('keeps only the Excel download for a Teacher once the week is closed', async () => {
        //given
        const store = fakeStore({ ...AS_TEACHER, state: signal(PublicationState.closed) });

        //when
        const page = await render(store);

        //then
        expect(buttonLabels(page)).toEqual(['publications.downloadExcel']);
    });

    it('tells a Teacher that the Administrator publishes a draft week, with no publish button', async () => {
        //given
        const store = fakeStore({ ...AS_TEACHER, state: signal(PublicationState.draft) });

        //when
        const page = await render(store);

        //then
        expect(buttonLabels(page)).toEqual([]);
        expect(page.textContent).toContain('publications.dashboard.draftPromptTeacher');
    });

    it('does not link a Teacher to prepare a week that has no Publication', async () => {
        //given
        const store = fakeStore({
            ...AS_TEACHER,
            state: signal(undefined),
            publication: signal(undefined),
            hasPublication: signal(false),
            weekNumber: signal(undefined),
        });

        //when
        const page = await render(store);

        //then
        expect(page.querySelector('.dashboard__empty')).not.toBeNull();
        expect(page.querySelector('.dashboard__empty a')).toBeNull();
    });

    it('tells a Teacher these are only their Publications', async () => {
        //given
        const store = fakeStore({ ...AS_TEACHER });

        //when
        const page = await render(store);

        //then
        expect(page.querySelector('.dashboard__only-yours')?.textContent?.trim()).toBe(
            'publications.dashboard.onlyYours',
        );
    });

    it('shows (me) for the signed-in User\'s own Teacher', async () => {
        //given
        const store = fakeStore({
            teachers: signal([{ id: 'teacher-cohen', name: 'Teacher Cohen', isMe: true }]),
        });

        //when
        const page = await render(store);

        //then
        const label = page.querySelector('.dashboard__teacher-select .p-select-label');
        expect(label?.textContent).toContain('Teacher Cohen');
        expect(label?.querySelector('.dashboard__me')?.textContent?.trim()).toBe('publications.me');
        expect(page.querySelector('.dashboard__only-yours')).toBeNull();
    });
});
