# Task 6 of 7: Publications for a Teacher-role User

> Part of [#89: Teacher-role Users Reach Only Week Schedules and Publications](README.md). Requires task 5 committed. Work on branch `89-teacher-role-navigation`. Read README decisions 10 and 11 first.

**Files:**
- Modify: `client\src\app\features\publications\state\publications.store.ts`
- Test: `client\src\app\features\publications\state\publications.store.spec.ts`
- Modify: `client\src\app\features\publications\ui\pages\publications-dashboard\publications-dashboard.page.html`
- Modify: `client\src\app\features\publications\ui\pages\publications-dashboard\publications-dashboard.page.ts`
- Test: `client\src\app\features\publications\ui\pages\publications-dashboard\publications-dashboard.page.spec.ts`
- Modify: `client\public\i18n\he.json`, `client\public\i18n\en.json`

**Interfaces:**
- Consumes: `AuthService.isAdministrator`, `AuthService.isTeacher`, `AuthService.teacherId` (task 2). Server behavior from task 1: a Teacher-role User gets 403 from `GET api/teachers/find` and from publish / extend-window / reopen, and normal responses from the four Publication queries.
- Produces (on `PublicationsStore`, readonly signals):
  - `canChooseTeacher: Signal<boolean>`: `true` only for an Administrator
  - `canManageLifecycle: Signal<boolean>`: `true` only for an Administrator
  - `selectTeacher(teacherId)` is ignored for a Teacher-role User; their selected Teacher is always the token's `teacher_id`

**Design (frame 10b):** a Teacher sees the Publications screen without lifecycle controls. On our dashboard that means, by Publication state:

| State | Administrator (unchanged) | Teacher |
|-------|---------------------------|---------|
| No Publication | notice + "להכנת השבוע" link | notice only |
| Draft | "פרסום" button; prompt "...פרסמו אותו כדי לפתוח..." | no button; prompt "...מנהל המערכת יפרסם אותו..." |
| Published | share link | share link (unchanged) |
| Open | "הארכת החלון", "רענון", "הורדת אקסל" | "רענון", "הורדת אקסל" |
| Closed | "פתיחה מחדש", "הורדת אקסל" | "הורדת אקסל" |

The Teacher picker in the title row is not rendered for a Teacher (the locked Teacher of #90 replaces it later). The design's per-week table layout of 10b is a different screen shape and is **not** part of #89; note the difference in the PR (task 7).

**Why:** AC 7 ("Publication lifecycle controls are hidden for Teacher-role Users"), and decision 10, without which the dashboard fails to load for a Teacher (403 on the picker).

- [ ] **Step 1: Write the failing store specs**

In `client\src\app\features\publications\state\publications.store.spec.ts`:

Add the import:

```typescript
import { AuthService } from '../../../core/auth.service';
```

Add after the `TEACHERS` constant:

```typescript
interface SignedInAs {
    isAdministrator: boolean;
    isTeacher: boolean;
    teacherId: string | null;
}

const ADMINISTRATOR: SignedInAs = { isAdministrator: true, isTeacher: false, teacherId: null };
const TEACHER: SignedInAs = { isAdministrator: false, isTeacher: true, teacherId: 'teacher-yael' };
```

Replace `createStore` with this version (a new `user` parameter defaulting to the Administrator, so every existing spec keeps its meaning; `findTeachers` and `downloadExcel` become spies):

```typescript
function createStore(
    teachers: Observable<TeacherOption[]>,
    publication: Observable<GetPublicationResponse>,
    user: SignedInAs = ADMINISTRATOR,
): PublicationsStore {
    TestBed.configureTestingModule({
        providers: [
            provideZonelessChangeDetection(),
            { provide: LanguageService, useValue: { lang: signal('en'), locale: signal('en-IL') } },
            {
                provide: AuthService,
                useValue: {
                    isAdministrator: signal(user.isAdministrator),
                    isTeacher: signal(user.isTeacher),
                    teacherId: signal(user.teacherId),
                },
            },
            { provide: TeacherOptionsApiService, useValue: { findTeachers: vi.fn(() => teachers) } },
            {
                provide: PublicationsApiService,
                useValue: {
                    getByWeek: () => publication,
                    getDashboard: () => NEVER,
                    findHistory: () => of([]),
                    downloadExcel: vi.fn(() => of(new Blob())),
                },
            },
            { provide: ToastService, useValue: { success: () => undefined, apiError: () => undefined } },
            { provide: ClipboardService, useValue: { copy: async () => true } },
            { provide: FileDownloadService, useValue: { download: vi.fn() } },
        ],
    });

    return TestBed.inject(PublicationsStore);
}
```

Add these specs at the end of `describe('PublicationsStore', ...)`:

```typescript
    it('offers the Teacher picker and the lifecycle controls to an Administrator', async () => {
        //given
        const store = await loadedStore();

        //expected
        expect(store.canChooseTeacher()).toBe(true);
        expect(store.canManageLifecycle()).toBe(true);
    });

    it('never asks for the teacher list for a Teacher', async () => {
        //given
        const store = createStore(of(TEACHERS), of(OPEN_PUBLICATION), TEACHER);

        //when
        await vi.waitFor(() => expect(store.publication()).toBeDefined());

        //then
        expect(TestBed.inject(TeacherOptionsApiService).findTeachers).not.toHaveBeenCalled();
        expect(store.teachers()).toEqual([]);
        expect(store.canChooseTeacher()).toBe(false);
        expect(store.canManageLifecycle()).toBe(false);
    });

    it('selects the linked Teacher for a Teacher and ignores picking another', async () => {
        //given
        const store = createStore(of(TEACHERS), of(OPEN_PUBLICATION), TEACHER);
        await vi.waitFor(() => expect(store.publication()).toBeDefined());

        //when
        store.selectTeacher('teacher-levi');

        //then
        expect(store.selectedTeacherId()).toBe('teacher-yael');
    });

    it('downloads the linked Teacher\'s Excel workbook for a Teacher', async () => {
        //given
        const store = createStore(of(TEACHERS), of(OPEN_PUBLICATION), TEACHER);
        await vi.waitFor(() => expect(store.publication()).toBeDefined());

        //when
        await store.downloadExcel();

        //then
        expect(TestBed.inject(PublicationsApiService).downloadExcel).toHaveBeenCalledWith('publication-1', 'teacher-yael');
    });
```

- [ ] **Step 2: Run them to verify they fail**

From `client\` (PowerShell):

```powershell
& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js test --watch=false --include src/app/features/publications/state/publications.store.spec.ts
```

Expected: FAIL to compile: `Property 'canChooseTeacher' does not exist` and `Property 'canManageLifecycle' does not exist`.

- [ ] **Step 3: Make the store Role-aware**

In `client\src\app\features\publications\state\publications.store.ts`:

1. Add the import:

```typescript
import { AuthService } from '../../../core/auth.service';
```

2. Inject it as the **first** injected field (field initializers below read it):

```typescript
    private readonly auth = inject(AuthService);
```

3. Replace `teachersResource` (idle for a Teacher: no request, not loading, no error):

```typescript
    private readonly teachersResource = resource({
        params: () => (this.auth.isAdministrator() ? true : undefined),
        loader: () => firstValueFrom(this.teachersApi.findTeachers()),
    });
```

4. Replace `selectedTeacherIdState`:

```typescript
    private readonly selectedTeacherIdState = linkedSignal<TeacherOption[], string | null>({
        source: this.teachers,
        computation: (teachers, previous) =>
            this.auth.isTeacher() ? this.auth.teacherId() : (previous?.value ?? teachers[0]?.id ?? null),
    });
```

5. Add after `selectedTeacherId`:

```typescript
    readonly canChooseTeacher = computed(() => this.auth.isAdministrator());
    readonly canManageLifecycle = computed(() => this.auth.isAdministrator());
```

6. Replace `selectTeacher`:

```typescript
    selectTeacher(teacherId: string): void {
        if (!this.canChooseTeacher()) {
            return;
        }

        this.selectedTeacherIdState.set(teacherId);
    }
```

- [ ] **Step 4: Run them to verify they pass**

```powershell
& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js test --watch=false --include src/app/features/publications/state/publications.store.spec.ts
```

Expected: PASS (existing specs plus 4 new).

- [ ] **Step 5: Write the failing page specs**

In `client\src\app\features\publications\ui\pages\publications-dashboard\publications-dashboard.page.spec.ts`:

Add two entries to the object returned by `fakeStore` (before `...overrides`):

```typescript
        canChooseTeacher: signal(true),
        canManageLifecycle: signal(true),
```

Add a helper after `weekPickerModel`:

```typescript
function buttonLabels(page: HTMLElement): string[] {
    return Array.from(page.querySelectorAll('.dashboard__actions .p-button-label')).map(
        (label) => label.textContent?.trim() ?? '',
    );
}

const AS_TEACHER = { canChooseTeacher: signal(false), canManageLifecycle: signal(false) };
```

Add these specs at the end of `describe('PublicationsDashboardPage', ...)` (the testing Transloco has no translations, so every label renders as its key):

```typescript
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
```

- [ ] **Step 6: Run them to verify they fail**

```powershell
& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js test --watch=false --include src/app/features/publications/ui/pages/publications-dashboard/publications-dashboard.page.spec.ts
```

Expected: the five Teacher specs FAIL (picker present, extra buttons, Administrator draft prompt, prepare link present); the Administrator spec passes.

- [ ] **Step 7: Hide the controls in the template**

In `publications-dashboard.page.html`:

1. Wrap the Teacher `<p-select styleClass="dashboard__teacher-select" ... />` (inside `<h2 class="dashboard__title">`) in:

```html
                    @if (store.canChooseTeacher()) {
                        <p-select
                            styleClass="dashboard__teacher-select"
                            [options]="store.teachers()"
                            optionLabel="name"
                            optionValue="id"
                            [ngModel]="store.selectedTeacherId()"
                            (ngModelChange)="store.selectTeacher($event)"
                            [placeholder]="'publications.selectTeacher' | transloco"
                            [ariaLabel]="'publications.selectTeacher' | transloco" />
                    }
```

2. Replace the whole `<div class="dashboard__actions"> ... </div>` with:

```html
        <div class="dashboard__actions">
            @switch (store.state()) {
                @case (PublicationState.draft) {
                    @if (store.canManageLifecycle()) {
                        <p-button [label]="'publications.publish' | transloco" (onClick)="onPublish()" />
                    }
                }
                @case (PublicationState.open) {
                    @if (store.canManageLifecycle()) {
                        <p-button [label]="'publications.extendWindow' | transloco" severity="secondary" text size="small" (onClick)="onExtend()" />
                    }
                    <p-button [label]="'general.refresh' | transloco" severity="secondary" outlined size="small" (onClick)="store.refresh()" />
                    <p-button [label]="'publications.downloadExcel' | transloco" size="small" (onClick)="store.downloadExcel()" />
                }
                @case (PublicationState.closed) {
                    @if (store.canManageLifecycle()) {
                        <p-button [label]="'publications.reopenWindow' | transloco" severity="secondary" size="small" (onClick)="onReopen()" />
                    }
                    <p-button [label]="'publications.downloadExcel' | transloco" size="small" (onClick)="store.downloadExcel()" />
                }
            }
        </div>
```

3. In the empty state, wrap the "prepare this week" link:

```html
        <section class="dashboard__card dashboard__empty">
            <p class="dashboard__notice">{{ 'publications.noPublication' | transloco }}</p>
            @if (store.canManageLifecycle()) {
                <a
                    pButton
                    [label]="'publications.dashboard.prepareWeek' | transloco"
                    [routerLink]="['/', appRoutes.weekSchedules]"></a>
            }
        </section>
```

4. In the `@case (PublicationState.draft)` notice section, choose the prompt by Role:

```html
            @case (PublicationState.draft) {
                <section class="dashboard__card dashboard__notice">
                    {{ (store.canManageLifecycle() ? 'publications.dashboard.draftPrompt' : 'publications.dashboard.draftPromptTeacher') | transloco }}
                </section>
            }
```

- [ ] **Step 8: Don't open the publish dialog for a Teacher from `?publish=1`**

In `publications-dashboard.page.ts`, change the third `effect` in the constructor to:

```typescript
        effect(() => {
            if (
                this.publish() === PUBLISH_FLAG &&
                this.store.canManageLifecycle() &&
                this.store.state() === PublicationState.draft
            ) {
                this.onPublish();
            }
        });
```

(Weekly prep only links with `publish: 1` when `canPublish` is true, which is Administrator-only since task 5; this closes the hand-typed URL.)

- [ ] **Step 9: Add the translation**

Inside `publications.dashboard`, right after `"draftPrompt"`:

`client\public\i18n\he.json`:

```json
      "draftPromptTeacher": "השבוע הוכן אך טרם פורסם. מנהל המערכת יפרסם אותו ויפתח את חלון ההגשות.",
```

`client\public\i18n\en.json`:

```json
      "draftPromptTeacher": "This week is prepared but not published yet. The Administrator publishes it to open the submission window.",
```

- [ ] **Step 10: Run the page spec, then the client suite and build**

```powershell
& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js test --watch=false --include src/app/features/publications/ui/pages/publications-dashboard/publications-dashboard.page.spec.ts
& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js test --watch=false
& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js build --project client
```

Expected: all PASS, build succeeds.

- [ ] **Step 11: Commit**

```bash
git add client/src/app/features/publications client/public/i18n/he.json client/public/i18n/en.json
git commit -m "feat(client): Publications hides the Teacher picker and lifecycle controls from Teachers (#89)

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```
