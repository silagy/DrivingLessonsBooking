# Task 11 of 14: Client — signal store, routing, i18n

> Part of [US-08–21: Publications Module](README.md). Requires tasks 1–10 complete. Work on branch `9-us-08-21-publications-module`, commands from `client\` unless noted.
>
> **This task does not commit on its own** — a store and routes with no page to render them are dead code. Commit at the end of [task 12](task-12-client-dashboard.md) (see the commit note below). Because `publications.routes.ts` references the dashboard/history pages created in task 12, `npm run build` will not succeed until task 12 lands; the build gate lives there.

Signals only (rule 10). The store mirrors `TeachersStore`'s `executeCommand` helper and `WeekSchedulesStore`'s parameterized `resource()` reads: a `by-week` publication read that maps 404 → `undefined`, a dashboard read keyed on `publication + teacher`, plus `copyLink`/`downloadExcel`/`refresh`. Errors are i18n keys; 409s surface via `toast.apiError`.

**Files:**
- Create: `client\src\app\features\publications\state\publications.store.ts`
- Create: `client\src\app\features\publications\publications.routes.ts`
- Modify: `client\src\app\shared\config\app-routes.ts`
- Modify: `client\src\app\features\admin-shell\admin.routes.ts`
- Modify: `client\src\app\features\admin-shell\admin-shell.component.html`
- Modify: `client\src\app\app.config.ts`
- Modify: `client\public\i18n\en.json`, `client\public\i18n\he.json`

---

- [ ] **Step 1: Signal store**

`features\publications\state\publications.store.ts`:

```typescript
import { HttpErrorResponse } from '@angular/common/http';
import { computed, inject, Injectable, resource, signal } from '@angular/core';
import { firstValueFrom, Observable } from 'rxjs';
import { LanguageService } from '../../../core/language.service';
import { ClipboardService } from '../../../core/services/clipboard.service';
import { FileDownloadService } from '../../../core/services/file-download.service';
import { ToastService } from '../../../core/services/toast.service';
import { ExtendPublicationWindowRequest } from '../data/extend-publication-window.request';
import { GetPublicationDashboardResponse } from '../data/get-publication-dashboard.response';
import { GetPublicationResponse } from '../data/get-publication.response';
import { PublicationsApiService } from '../data/publications-api.service';
import { PublishPublicationRequest } from '../data/publish-publication.request';
import { ReopenPublicationRequest } from '../data/reopen-publication.request';
import { TeacherOptionsApiService } from '../data/teacher-options-api.service';
import { SlotCountForGetPublicationDashboardResponse } from '../data/get-publication-dashboard.response';
import { TeacherOption } from '../domain/teacher-option.model';
import { buildWeekOptions, WeekOption } from '../domain/week-options';

const HTTP_NOT_FOUND = 404;

@Injectable({ providedIn: 'root' })
export class PublicationsStore {
    private readonly api = inject(PublicationsApiService);
    private readonly teachersApi = inject(TeacherOptionsApiService);
    private readonly toast = inject(ToastService);
    private readonly language = inject(LanguageService);
    private readonly clipboard = inject(ClipboardService);
    private readonly fileDownload = inject(FileDownloadService);

    private readonly selectedTeacherIdState = signal<string | null>(null);
    private readonly selectedWeekStartState = signal<string>(defaultWeekStart());
    private readonly mutating = signal(false);

    private readonly teachersResource = resource({
        loader: () => firstValueFrom(this.teachersApi.findTeachers()),
    });

    private readonly publicationResource = resource({
        params: () => ({ weekStart: this.selectedWeekStartState() }),
        loader: ({ params }) => this.loadByWeek(params.weekStart),
    });

    private readonly dashboardResource = resource({
        params: () => {
            const publication = this.publicationResource.value();
            const teacherId = this.selectedTeacherIdState();

            return publication && teacherId ? { publicationId: publication.id, teacherId } : undefined;
        },
        loader: ({ params }) => firstValueFrom(this.api.getDashboard(params.publicationId, params.teacherId)),
    });

    readonly selectedTeacherId = this.selectedTeacherIdState.asReadonly();
    readonly selectedWeekStart = this.selectedWeekStartState.asReadonly();
    readonly isMutating = this.mutating.asReadonly();

    readonly teachers = computed<TeacherOption[]>(() => {
        const items = this.teachersResource.value() ?? [];

        return items
            .map((item) => ({ id: item.id, name: item.name }))
            .sort((a, b) => a.name.localeCompare(b.name));
    });

    readonly weekOptions = computed<WeekOption[]>(() => buildWeekOptions(this.language.lang()));

    readonly publication = computed<GetPublicationResponse | undefined>(() => this.publicationResource.value());

    readonly dashboard = computed<GetPublicationDashboardResponse | undefined>(() => this.dashboardResource.value());

    readonly slotCounts = computed<SlotCountForGetPublicationDashboardResponse[]>(
        () => this.dashboard()?.slotCounts ?? [],
    );

    readonly hasPublication = computed(() => this.publication() !== undefined);

    readonly isLoading = computed(
        () => this.publicationResource.isLoading() || this.dashboardResource.isLoading(),
    );

    readonly loadError = computed(() => {
        const failed =
            this.publicationResource.error() ||
            this.dashboardResource.error() ||
            this.teachersResource.error();

        return failed ? 'publications.loadFailed' : undefined;
    });

    selectTeacher(teacherId: string): void {
        this.selectedTeacherIdState.set(teacherId);
    }

    selectWeek(weekStart: string): void {
        this.selectedWeekStartState.set(weekStart);
    }

    async publish(request: PublishPublicationRequest): Promise<void> {
        const id = this.publication()?.id;

        if (!id) {
            return;
        }

        await this.executeCommand(() => this.api.publish(id, request), 'publications.published');
    }

    async extendWindow(request: ExtendPublicationWindowRequest): Promise<void> {
        const id = this.publication()?.id;

        if (!id) {
            return;
        }

        await this.executeCommand(() => this.api.extendWindow(id, request), 'publications.extended');
    }

    async reopen(request: ReopenPublicationRequest): Promise<void> {
        const id = this.publication()?.id;

        if (!id) {
            return;
        }

        await this.executeCommand(() => this.api.reopen(id, request), 'publications.reopened');
    }

    async copyLink(): Promise<void> {
        const token = this.publication()?.linkToken;

        if (!token) {
            return;
        }

        const copied = await this.clipboard.copy(token);

        if (copied) {
            this.toast.success('publications.linkCopied');

            return;
        }

        this.toast.apiError(undefined);
    }

    async downloadExcel(): Promise<void> {
        const id = this.publication()?.id;
        const teacherId = this.selectedTeacherIdState();

        if (!id || !teacherId) {
            return;
        }

        this.mutating.set(true);

        try {
            const blob = await firstValueFrom(this.api.downloadExcel(id, teacherId));
            this.fileDownload.download(blob, `week-${this.selectedWeekStartState()}.xlsx`);
        } catch (error) {
            this.toast.apiError(error);
        } finally {
            this.mutating.set(false);
        }
    }

    refresh(): void {
        this.publicationResource.reload();
        this.dashboardResource.reload();
    }

    private async loadByWeek(weekStart: string): Promise<GetPublicationResponse | undefined> {
        try {
            return await firstValueFrom(this.api.getByWeek(weekStart));
        } catch (error) {
            if (isStatus(error, HTTP_NOT_FOUND)) {
                return undefined;
            }

            throw error;
        }
    }

    private async executeCommand(command: () => Observable<void>, successKey: string): Promise<void> {
        this.mutating.set(true);

        try {
            await firstValueFrom(command());
            this.toast.success(successKey);
            this.refresh();
        } catch (error) {
            this.toast.apiError(error);
        } finally {
            this.mutating.set(false);
        }
    }
}

function defaultWeekStart(): string {
    const options = buildWeekOptions('en');

    return options[0].weekStart;
}

function isStatus(error: unknown, status: number): boolean {
    return error instanceof HttpErrorResponse && error.status === status;
}
```

> `toast.apiError(undefined)` on a failed clipboard write resolves to the generic `general.unexpectedError` message (the `ToastService` only translates keys for `success`). Default week = `options[0]` = **current week** (the publish/dashboard flow works on the week that is live now, unlike the prep page which defaults to next week).

- [ ] **Step 2: Feature routes**

`features\publications\publications.routes.ts` (pages arrive in tasks 12/13):

```typescript
import { Routes } from '@angular/router';
import { PublicationsDashboardPage } from './ui/pages/publications-dashboard/publications-dashboard.page';
import { PublicationsHistoryPage } from './ui/pages/publications-history/publications-history.page';

export default [
    { path: '', component: PublicationsDashboardPage },
    { path: 'history', component: PublicationsHistoryPage },
] satisfies Routes;
```

- [ ] **Step 3: Route constant**

`shared\config\app-routes.ts` — add the `publications` path:

```typescript
export const AppRoutes = {
    login: 'login',
    teachers: 'teachers',
    weekSchedules: 'week-schedules',
    publications: 'publications',
} as const;
```

- [ ] **Step 4: Lazy child route in the admin shell**

`features\admin-shell\admin.routes.ts` — add a lazy child after `weekSchedules`:

```typescript
      {
        path: AppRoutes.weekSchedules,
        loadChildren: () => import('../week-schedules/week-schedules.routes'),
      },
      {
        path: AppRoutes.publications,
        loadChildren: () => import('../publications/publications.routes'),
      },
```

- [ ] **Step 5: Nav links**

`features\admin-shell\admin-shell.component.html` — add two links after the Weekly-prep link (before the closing `</nav>`). The dashboard link uses `exact` so it does not stay active on the history sub-route:

```html
      <a
        [routerLink]="['/', appRoutes.publications]"
        routerLinkActive="shell__nav-link--active"
        [routerLinkActiveOptions]="{ exact: true }"
        class="shell__nav-link">
        {{ 'shell.nav.publications' | transloco }}
      </a>
      <a
        [routerLink]="['/', appRoutes.publications, 'history']"
        routerLinkActive="shell__nav-link--active"
        class="shell__nav-link">
        {{ 'shell.nav.history' | transloco }}
      </a>
```

- [ ] **Step 6: Component input binding**

`app.config.ts` — the dashboard/history pages bind `teacherId`/`week` query params to `input()`s (task 12), so enable `withComponentInputBinding()`:

```typescript
import { provideRouter, withComponentInputBinding } from '@angular/router';
```

```typescript
    provideRouter(routes, withComponentInputBinding()),
```

- [ ] **Step 7: i18n — `en.json`**

Add `publications` and `history` under `shell.nav`:

```json
  "shell": {
    "title": "Driving Lessons Planner",
    "logout": "Sign out",
    "languageToggle": "עברית",
    "nav": {
      "dashboard": "Dashboard",
      "teachers": "Teachers & cars",
      "weeklyPrep": "Weekly prep",
      "publications": "Publications",
      "history": "History"
    }
  },
```

Add the full `publications` namespace (lowercase namespace; `state` subgroup keyed by the camelCase enum values):

```json
  "publications": {
    "title": "Publications",
    "subtitle": "Publish the week, track requests, and share the link.",
    "teacher": "Teacher",
    "week": "Week",
    "selectTeacher": "Select a teacher",
    "state": {
      "draft": "Draft",
      "published": "Published",
      "open": "Open",
      "closed": "Closed"
    },
    "publish": "Publish week",
    "extendWindow": "Extend deadline",
    "reopen": "Reopen window",
    "copyLink": "Copy link",
    "downloadExcel": "Download Excel",
    "refresh": "Refresh",
    "shareLink": "Shareable link",
    "windowStart": "Opens",
    "windowEnd": "Closes",
    "studentsSubmitted": "Students submitted",
    "totalPicks": "Total picks",
    "lastSubmission": "Last submission",
    "latestVersion": "Latest version",
    "dataAsOf": "Data as of {{time}}",
    "requestCount": "{{count}} requests",
    "noPublication": "This week has not been prepared yet.",
    "published": "Week published",
    "extended": "Deadline extended",
    "reopened": "Window reopened",
    "linkCopied": "Link copied",
    "excelDownloaded": "Excel downloaded",
    "loadFailed": "Failed to load the publication.",
    "history": {
      "title": "Publications history",
      "week": "Week",
      "teacher": "Teacher",
      "state": "State",
      "version": "Version",
      "reDownload": "Download",
      "viewDashboard": "View",
      "empty": "No past publications yet."
    },
    "dialogs": {
      "publishTitle": "Publish week",
      "extendTitle": "Extend deadline",
      "reopenTitle": "Reopen window",
      "windowStart": "Window opens",
      "windowEnd": "Window closes",
      "newEnd": "New closing time"
    }
  },
```

- [ ] **Step 8: i18n — `he.json` (mirror, translated by meaning)**

Add `publications` and `history` under `shell.nav`:

```json
  "shell": {
    "title": "מערכת תכנון שיעורי נהיגה",
    "logout": "התנתקות",
    "languageToggle": "English",
    "nav": {
      "dashboard": "לוח בקרה",
      "teachers": "מורים ורכבים",
      "weeklyPrep": "הכנת שבוע",
      "publications": "פרסומים",
      "history": "היסטוריה"
    }
  },
```

Add the mirrored `publications` namespace:

```json
  "publications": {
    "title": "פרסומים",
    "subtitle": "פרסמו את השבוע, עקבו אחר הבקשות ושתפו את הקישור.",
    "teacher": "מורה",
    "week": "שבוע",
    "selectTeacher": "בחרו מורה",
    "state": {
      "draft": "טיוטה",
      "published": "פורסם",
      "open": "פתוח",
      "closed": "סגור"
    },
    "publish": "פרסום השבוע",
    "extendWindow": "הארכת מועד",
    "reopen": "פתיחה מחדש",
    "copyLink": "העתקת קישור",
    "downloadExcel": "הורדת אקסל",
    "refresh": "רענון",
    "shareLink": "קישור לשיתוף",
    "windowStart": "נפתח",
    "windowEnd": "נסגר",
    "studentsSubmitted": "תלמידים שהגישו",
    "totalPicks": "סך הבחירות",
    "lastSubmission": "הגשה אחרונה",
    "latestVersion": "גרסה אחרונה",
    "dataAsOf": "הנתונים נכון ל-{{time}}",
    "requestCount": "{{count}} בקשות",
    "noPublication": "השבוע הזה עדיין לא הוכן.",
    "published": "השבוע פורסם",
    "extended": "המועד הוארך",
    "reopened": "החלון נפתח מחדש",
    "linkCopied": "הקישור הועתק",
    "excelDownloaded": "האקסל הורד",
    "loadFailed": "טעינת הפרסום נכשלה.",
    "history": {
      "title": "היסטוריית פרסומים",
      "week": "שבוע",
      "teacher": "מורה",
      "state": "מצב",
      "version": "גרסה",
      "reDownload": "הורדה",
      "viewDashboard": "צפייה",
      "empty": "אין עדיין פרסומים קודמים."
    },
    "dialogs": {
      "publishTitle": "פרסום השבוע",
      "extendTitle": "הארכת מועד",
      "reopenTitle": "פתיחה מחדש",
      "windowStart": "החלון נפתח",
      "windowEnd": "החלון נסגר",
      "newEnd": "מועד סגירה חדש"
    }
  },
```

> Every key in `en.json` has its `he.json` mirror in the same change (rule 11). Numbers, dates, and the link render inside `<bdi>` in the templates (task 12), not here.

- [ ] **Step 9: Commit note**

⏳ **Do not commit yet.** The store + routes are consumed by the task-12 dashboard page; `npm run build` first passes at the end of task 12, which commits tasks 11 and 12 together:

```bash
git add client/src/app/features/publications client/src/app/shared/config/app-routes.ts \
        client/src/app/features/admin-shell client/src/app/app.config.ts client/public/i18n
git commit -m "feat(client): publications store, routing, dashboard and dialogs"
```

---

**Next:** [task-12-client-dashboard.md](task-12-client-dashboard.md)
