# Task 1 of 5: Share link while the week is Open (P1, TDD)

> Part of [#76: Publications Dashboard - Share Link While Open, Week Picker, Empty State](README.md). Sub-issue [#77](https://github.com/silagy/DrivingLessonsBooking/issues/77). Work on branch `76-dashboard-link-and-week`, which already has the plan committed.

**Files:**
- Create: `client\src\app\features\publications\ui\pages\publications-dashboard\publications-dashboard.page.spec.ts`
- Modify: `client\src\app\features\publications\ui\pages\publications-dashboard\publications-dashboard.page.html` (Open case, after the `dashboard__stats` div)
- Modify: `client\src\app\features\publications\ui\pages\publications-dashboard\publications-dashboard.page.scss`

**Interfaces:**
- Consumes: `PublicationsStore.shareLink(): string` and `PublicationsStore.copyLink(): Promise<void>` (both exist); `ShareLinkBoxComponent` (`link` input, `copyClicked` output; already imported by the page).
- Produces: the spec helpers `fakeStore(overrides)` and `render(store)` plus the constants `SHARE_LINK`, `WEEK_START` and `WEEK_LABEL`. Tasks 2 and 3 add tests to the same spec file. The fake already carries every store member the page reads after task 4 (`weekChoices`, `hasPublication`, ...), so later tasks only override values.

**Why:** README P1. The link is distributed outside the system and students use it for the whole window (requirements §6.2), but the dashboard drops it the moment the week opens.

**Run the tests** (PowerShell, from `client\`):

```powershell
& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js test --watch=false --include "src/app/features/publications/**/*.spec.ts"
```

- [ ] **Step 1: Write the failing test**

Create `publications-dashboard.page.spec.ts`:

```typescript
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
```

- [ ] **Step 2: Run the tests to verify the first one fails**

Run the test command above.
Expected: `shows the share link while the week is open` FAILS (`.share-link__value` is `null`). `does not offer the share link once the week is closed` already PASSES.

- [ ] **Step 3: Render the share link in the Open case**

In `publications-dashboard.page.html`, inside `@case (PublicationState.open) {`, insert this directly after the closing `</div>` of `<div class="dashboard__stats">` and before `<section class="dashboard__card">`:

```html
                <app-share-link-box
                    class="dashboard__share"
                    [link]="store.shareLink()"
                    (copyClicked)="store.copyLink()" />
```

In `publications-dashboard.page.scss`, add after the `.dashboard__stamp` rule:

```scss
.dashboard__share {
    display: block;
    margin-block-start: 14px;
}
```

- [ ] **Step 4: Run the tests to verify they pass**

Run the test command above.
Expected: both `PublicationsDashboardPage` tests PASS.

- [ ] **Step 5: Commit**

```bash
git add client/src/app/features/publications/ui/pages/publications-dashboard
git commit -m "fix(publications): show the share link while the week is open" -m "Closes #77"
```
