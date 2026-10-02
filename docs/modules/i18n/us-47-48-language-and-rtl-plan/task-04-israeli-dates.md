# Task 4 of 8: Dates and times read the Israeli way in both languages; history and "data as of" built from keys (L4, R5, TDD)

> Part of [US-47 + US-48: Hebrew/English Toggle With No Leftovers, Mirror-Correct RTL](README.md). Requires tasks 1–3 committed. Work on branch `47-us-47-48-language-and-rtl`.

**Files:**
- Create: `client\src\app\shared\components\week-grid\week-grid.component.spec.ts`
- Modify: `client\src\app\shared\components\week-grid\week-grid.component.ts` (one line)
- Modify: `client\src\app\features\student-form\state\student-form.store.ts` (three call sites)
- Modify: `client\src\app\features\student-form\state\student-form.store.spec.ts` (fake + one spec)
- Modify: `client\src\app\features\student-form\student-form.routes.spec.ts`, `client\src\app\features\student-form\ui\pages\student-form\student-form.page.spec.ts` (fakes only)
- Modify: `client\src\app\features\publications\state\publications.store.ts`, `…\publications-dashboard\publications-dashboard.page.ts` + `.html`, `…\publications-history\publications-history.page.ts` + `.html`
- Modify: `client\src\app\features\roster\ui\pages\roster\roster.page.ts`, `client\src\app\features\week-schedules\state\week-schedules.store.ts` (one call site each)
- Modify: `client\src\app\features\publications\ui\dialogs\publish-week\publish-week.dialog.html`, `…\extend-window\extend-window.dialog.html`, `…\reopen-window\reopen-window.dialog.html` (date-picker format)
- Modify: `client\public\i18n\en.json`, `client\public\i18n\he.json` (+ `publications.history.windowRange`, − `publications.dashboard.dataAsOf`)

**Interfaces:**
- Consumes: task 3's `LanguageService.locale: Signal<'he-IL' | 'en-IL'>`. The existing formatters, unchanged: `formatInstantInJerusalem(utcIso, locale)` (publications and roster domains), `formatWindowInstant(utcIso, locale)`, `weekRangeLabel(weekStart: string, locale)` and `groupSlotsByDay(slots, weekStart, locale, picks)` (student-form domain), `buildWeekOptions(locale)` and `weekRangeLabel(weekStart: Date, locale)` (publications / week-schedules domains). The existing key `publications.dataAsOf` (`Data as of {{time}}` / `הנתונים נכון ל-{{time}}`).
- Produces: every displayed date and time goes through `language.locale()`; `language.lang()` is read only by `LanguageService` itself and the toggle. New key `publications.history.windowRange` = `{{start}} – {{end}}` in both files. The key `publications.dashboard.dataAsOf` is removed. Every `LanguageService` test fake carries `locale`.

**Why:** README defects L4 and R5, roadmap decision 2, README decision 8. `'en'` resolves to en-US: `10/4` for 4 October and a 12-hour clock, while the slot windows are 24-hour wall-clock labels. `en-IL` gives `04/10` and `14:00`; `he-IL` gives the same output as `he` today (`4.10`, `14:00`), so Hebrew does not change. The history page builds `start → end` in TypeScript, an arrow that points backwards in Hebrew, and prints the raw ISO week. The dashboard stamp glues a fragment (`נתונים נכון ל־`) to an isolated value, which leaves a space after the maqaf, while a parameterised key for the same sentence already exists.

- [ ] **Step 1: Write the failing week-grid spec**

Create `client\src\app\shared\components\week-grid\week-grid.component.spec.ts`:

```ts
import { Component, provideZonelessChangeDetection, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { TranslocoTestingModule } from '@jsverse/transloco';
import { LanguageService } from '../../../core/language.service';
import { WeekGridCell } from '../../models/week-grid-cell';
import { WeekGridComponent } from './week-grid.component';

@Component({
    imports: [WeekGridComponent],
    template: `
        <app-week-grid [cells]="cells" weekStart="2026-10-04">
            <ng-template let-cell>{{ cell.window }}</ng-template>
        </app-week-grid>
    `,
})
class WeekGridHost {
    readonly cells: WeekGridCell[] = [];
}

describe('WeekGridComponent', () => {
    it('dates each day day-first in Israeli English', async () => {
        //given
        TestBed.configureTestingModule({
            imports: [
                WeekGridHost,
                TranslocoTestingModule.forRoot({
                    langs: { en: {} },
                    translocoConfig: { availableLangs: ['en'], defaultLang: 'en' },
                }),
            ],
            providers: [
                provideZonelessChangeDetection(),
                { provide: LanguageService, useValue: { lang: signal('en'), locale: signal('en-IL') } },
            ],
        });

        //when
        const fixture = TestBed.createComponent(WeekGridHost);
        await fixture.whenStable();

        //then
        const dates = [...(fixture.nativeElement as HTMLElement).querySelectorAll('.week-grid__day-date')]
            .map(date => date.textContent?.trim());
        expect(dates).toEqual(['04/10', '05/10', '06/10', '07/10', '08/10', '09/10']);
    });
});
```

- [ ] **Step 2: Give every student-form fake a locale, and write the failing store spec**

1. In each of these three files, change the line

```ts
{ provide: LanguageService, useValue: { lang: signal('en') } },
```

to

```ts
{ provide: LanguageService, useValue: { lang: signal('en'), locale: signal('en-IL') } },
```

   (keep the line's existing indentation):
   - `client\src\app\features\student-form\state\student-form.store.spec.ts`
   - `client\src\app\features\student-form\student-form.routes.spec.ts`
   - `client\src\app\features\student-form\ui\pages\student-form\student-form.page.spec.ts`

2. At the end of `client\src\app\features\student-form\state\student-form.store.spec.ts`, change

```ts
            expect(store.pickSheet()).toBeNull();
        });
    });
});
```

to

```ts
            expect(store.pickSheet()).toBeNull();
        });
    });

    describe('slotDays', () => {
        it('dates each day day-first in Israeli English', async () => {
            //given
            const store = await identifiedStore();

            //when
            const [sunday] = store.slotDays();

            //then
            expect(sunday.dateLabel).toBe('15/11');
        });
    });
});
```

   The fake publication's week starts on Sunday 15 November 2026 (`weekStart: '2026-11-15'` in `identifiedStore`).

- [ ] **Step 3: Run the specs to verify they fail**

Run (in `client\`): `npm test -- --watch=false`
Expected: FAIL. Exactly two specs fail: `WeekGridComponent › dates each day day-first in Israeli English` (`expected [ '10/4', '10/5', … ] to deeply equal [ '04/10', … ]`) and `StudentFormStore › slotDays › dates each day day-first in Israeli English` (`expected '11/15' to be '15/11'`). Every other spec passes, so adding `locale` to the fakes changed nothing yet.

- [ ] **Step 4: Switch every formatter to the locale**

Make exactly these edits. Each replaces `this.language.lang()` with `this.language.locale()` and nothing else on the line:

1. `client\src\app\shared\components\week-grid\week-grid.component.ts`:
   `return date.toLocaleDateString(this.language.lang(), { day: 'numeric', month: 'numeric' });`
   → `return date.toLocaleDateString(this.language.locale(), { day: 'numeric', month: 'numeric' });`
2. `client\src\app\features\student-form\state\student-form.store.ts`: all **three** occurrences (`weekRangeLabel(publication.weekStart, …)`, `groupSlotsByDay(student.slots, publication.weekStart, …, this.picks())`, `formatWindowInstant(utcIso, …)`).
3. `client\src\app\features\publications\state\publications.store.ts`: both occurrences (`buildWeekOptions(…)` and `formatInstantInJerusalem(loadedAt, …)`).
4. `client\src\app\features\publications\ui\pages\publications-dashboard\publications-dashboard.page.ts`:
   `return formatInstantInJerusalem(utcIso, this.language.lang());` → `return formatInstantInJerusalem(utcIso, this.language.locale());`
5. `client\src\app\features\roster\ui\pages\roster\roster.page.ts`:
   `return latest ? formatInstantInJerusalem(latest.importedAtUtc, this.language.lang()) : '';`
   → `return latest ? formatInstantInJerusalem(latest.importedAtUtc, this.language.locale()) : '';`
6. `client\src\app\features\week-schedules\state\week-schedules.store.ts`:
   `readonly weekOptions = computed<WeekOption[]>(() => buildWeekOptions(this.language.lang()));`
   → `readonly weekOptions = computed<WeekOption[]>(() => buildWeekOptions(this.language.locale()));`

The history page is rewritten in Step 6. Then check that no formatter is left on the bare language. Run (repo root):

```bash
grep -rn "language.lang()" client/src/app --include=*.ts | grep -v "\.spec\.ts"
```

Expected: exactly three lines: one in `publications-history.page.ts` (`formatInstantInJerusalem(utcIso, this.language.lang())`, which Step 6 replaces) and two in `language-toggle.component.ts` (`language.lang() === option.lang` in the template, which stays).

- [ ] **Step 5: Run the specs**

Run (in `client\`): `npm test -- --watch=false`
Expected: PASS, every spec, including the two new ones.

- [ ] **Step 6: Rebuild the history page's week and window cells**

1. Replace the whole of `client\src\app\features\publications\ui\pages\publications-history\publications-history.page.ts` with:

```ts
import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { Router } from '@angular/router';
import { TranslocoPipe } from '@jsverse/transloco';
import { ButtonModule } from 'primeng/button';
import { TableModule } from 'primeng/table';
import { ProgressSpinnerModule } from 'primeng/progressspinner';
import { AppRoutes } from '../../../../../shared/config/app-routes';
import { PublicationStateTagComponent } from '../../../../../shared/components/publication-state-tag/publication-state-tag.component';
import { PublicationState } from '../../../../../shared/models/publication-state.enum';
import { formatInstantInJerusalem } from '../../../domain/jerusalem-time';
import { weekRangeLabel } from '../../../domain/week-options';
import { LanguageService } from '../../../../../core/language.service';
import { PublicationsStore } from '../../../state/publications.store';
import { ItemForFindPublicationHistoryResponse } from '../../../data/item-for-find-publication-history.response';

@Component({
    selector: 'app-publications-history-page',
    imports: [
        TranslocoPipe,
        ButtonModule,
        TableModule,
        ProgressSpinnerModule,
        PublicationStateTagComponent,
    ],
    templateUrl: './publications-history.page.html',
    styleUrl: './publications-history.page.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PublicationsHistoryPage {
    protected readonly store = inject(PublicationsStore);
    private readonly router = inject(Router);
    private readonly language = inject(LanguageService);

    protected readonly PublicationState = PublicationState;

    protected weekLabel(row: ItemForFindPublicationHistoryResponse): string {
        return weekRangeLabel(new Date(row.weekStart), this.language.locale());
    }

    protected canRedownload(row: ItemForFindPublicationHistoryResponse): boolean {
        return row.state === PublicationState.closed && row.latestExcelVersion !== null;
    }

    protected onRedownload(row: ItemForFindPublicationHistoryResponse): void {
        void this.store.downloadExcel(row.publicationId, row.teacherId);
    }

    protected onViewDashboard(row: ItemForFindPublicationHistoryResponse): void {
        void this.router.navigate(['/', AppRoutes.publications], {
            queryParams: { teacherId: row.teacherId, week: row.weekStart },
        });
    }

    protected formatInstant(utcIso: string): string {
        return formatInstantInJerusalem(utcIso, this.language.locale());
    }
}
```

   `weekRangeLabel(Date, locale)` is the same function the week pickers use (`Oct 4 – Oct 9, 2026` today, `4 Oct – 9 Oct 2026` in en-IL, `4 באוק׳ – 9 באוק׳ 2026` in Hebrew), and `new Date(isoDate)` is how `buildWeekOptions` already parses a week start. `windowLabel` is gone: the template builds the range from a key.

2. In `client\src\app\features\publications\ui\pages\publications-history\publications-history.page.html`, change

```html
                        <td><bdi>{{ row.weekStart }}</bdi></td>
                        <td>{{ row.teacherName }}</td>
                        <td class="history__window"><bdi>{{ windowLabel(row) }}</bdi></td>
```

to

```html
                        <td><bdi>{{ weekLabel(row) }}</bdi></td>
                        <td>{{ row.teacherName }}</td>
                        <td class="history__window">
                            @if (row.windowStartUtc && row.windowEndUtc) {
                                <bdi>{{ 'publications.history.windowRange' | transloco: { start: formatInstant(row.windowStartUtc), end: formatInstant(row.windowEndUtc) } }}</bdi>
                            } @else {
                                <span class="history__dash">—</span>
                            }
                        </td>
```

   `history__dash` is the class the Excel column already uses for its `—`. The en dash between the two dates has no direction, so the range reads start → end in either language (README R5).

3. In `client\public\i18n\en.json`, change

```json
      "empty": "No publications yet.",
      "loadFailed": "Failed to load history."
    },
```

to

```json
      "empty": "No publications yet.",
      "loadFailed": "Failed to load history.",
      "windowRange": "{{start}} – {{end}}"
    },
```

4. In `client\public\i18n\he.json`, change

```json
      "empty": "אין פרסומים עדיין.",
      "loadFailed": "טעינת ההיסטוריה נכשלה."
    },
```

to

```json
      "empty": "אין פרסומים עדיין.",
      "loadFailed": "טעינת ההיסטוריה נכשלה.",
      "windowRange": "{{start}} – {{end}}"
    },
```

- [ ] **Step 7: One sentence for the dashboard's "data as of" stamp**

1. In `client\src\app\features\publications\ui\pages\publications-dashboard\publications-dashboard.page.html`, change

```html
                        {{ 'publications.dashboard.dataAsOf' | transloco }} <bdi>{{ store.dataAsOf() }}</bdi>
```

to

```html
                        {{ 'publications.dataAsOf' | transloco: { time: store.dataAsOf() } }}
```

2. In `client\public\i18n\en.json`, change

```json
      "lastSubmission": "last submission",
      "dataAsOf": "Data as of",
```

to

```json
      "lastSubmission": "last submission",
```

3. In `client\public\i18n\he.json`, change

```json
      "lastSubmission": "הגשה אחרונה",
      "dataAsOf": "נתונים נכון ל־",
```

to

```json
      "lastSubmission": "הגשה אחרונה",
```

   The stamp now reads `Data as of 2 Oct 2026, 14:05` / `הנתונים נכון ל-2 באוק׳ 2026, 14:05`. The time is formatted in the active language, so it needs no isolation of its own.

- [ ] **Step 8: Day before month in the date pickers**

In each of these files, change every `dateFormat="D, M d"` to `dateFormat="D, d M"` (four occurrences in total):
- `client\src\app\features\publications\ui\dialogs\publish-week\publish-week.dialog.html` (two)
- `client\src\app\features\publications\ui\dialogs\extend-window\extend-window.dialog.html` (one)
- `client\src\app\features\publications\ui\dialogs\reopen-window\reopen-window.dialog.html` (one)

Then run (repo root): `grep -rn 'dateFormat=' client/src/app --include=*.html`
Expected: four lines, all `dateFormat="D, d M"`.

- [ ] **Step 9: Check the keys stay mirrored**

Run (repo root):

```bash
node -e "
const path = require('path');
const flat = (o, p = '') => Object.entries(o).flatMap(([k, v]) => typeof v === 'object' ? flat(v, p + k + '.') : [p + k]);
const [en, he] = ['en', 'he'].map(l => flat(require(path.resolve('client/public/i18n/' + l + '.json'))));
console.log('en', en.length, 'he', he.length, 'only en', JSON.stringify(en.filter(k => !he.includes(k))), 'only he', JSON.stringify(he.filter(k => !en.includes(k))));
console.log('dashboard.dataAsOf still used:', require('child_process').execSync('grep -rln \"dashboard.dataAsOf\" client/src || true').toString().trim() || 'no');
"
```

Expected: `en 350 he 350 only en [] only he []` (291 before the slice, + 58 `errors.*` in task 2, + `shell.language` in task 3, + `windowRange`, − `dashboard.dataAsOf`) and `dashboard.dataAsOf still used: no`.

- [ ] **Step 10: Run the suite and the build**

Run (in `client\`): `npm test -- --watch=false`
Expected: PASS, every spec.

Run (in `client\`): `npm run build`
Expected: builds clean, with no new warnings. (The history template no longer calls `windowLabel`; a leftover call fails the build here.)

- [ ] **Step 11: Commit**

```bash
git add client/src/app/shared/components/week-grid client/src/app/features/student-form/state client/src/app/features/student-form/student-form.routes.spec.ts client/src/app/features/student-form/ui/pages/student-form/student-form.page.spec.ts client/src/app/features/publications client/src/app/features/roster/ui/pages/roster/roster.page.ts client/src/app/features/week-schedules/state/week-schedules.store.ts client/public/i18n/en.json client/public/i18n/he.json
git commit -m "feat(i18n): Israeli date and time formats in both languages

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```
