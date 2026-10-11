# Task 7 of 8: Publications opens the signed-in User's own Teacher (client)

> Part of [#90: Teacher-role Users See and Change Only Their Own Teacher's Data](README.md). Requires task 6 committed. Work on branch `90-teacher-data-scoping`. Read README decisions 10, 11 and 12 first.

**Files:**
- Modify: `client\src\app\features\publications\domain\teacher-option.model.ts`
- Modify: `client\src\app\features\publications\state\publications.store.ts`
- Test: `client\src\app\features\publications\state\publications.store.spec.ts`
- Modify: `client\src\app\features\publications\ui\pages\publications-dashboard\publications-dashboard.page.html`
- Modify: `client\src\app\features\publications\ui\pages\publications-dashboard\publications-dashboard.page.scss`
- Test: `client\src\app\features\publications\ui\pages\publications-dashboard\publications-dashboard.page.spec.ts`
- Modify: `client\public\i18n\he.json`, `client\public\i18n\en.json`

**Interfaces:**
- Consumes: `AuthService.isAdministrator`, `AuthService.isTeacher`, `AuthService.teacherId` (#89). The token carries `teacher_id` whenever the User is linked, for any Role, so a linked Administrator has a non-null `teacherId()`.
- Produces:
  - `TeacherOption { id: string; name: string; isMe: boolean }` (publications feature), `isMe = id === auth.teacherId()`
  - `PublicationsStore.selectedTeacherId` default: a Teacher always gets `auth.teacherId()`; anyone else keeps the previous choice, else `auth.teacherId()`, else the first Teacher by name, else `null`
  - `PublicationsStore.selectTeacher(teacherId)` is unchanged: it still wins over the default for an Administrator (the page's `?teacherId=` effect calls it) and is still ignored for a Teacher
- The page's `teacherId` query-param effect is untouched. It runs on the page's first change detection, before the teacher list resource resolves, so the store must keep a choice made before the teachers load (spec 3) as well as one made after (spec 4).

**Design (frames 10b, 10c):**
- 10b, Teacher: the Teacher picker stays hidden (#89). A one-line subtitle sits under the title row, above the week picker line: "רק הפרסומים שלך: מה שהתלמידים ביקשו ממך בכל שבוע." / "Only your Publications: what Students requested from you each week." Secondary text color, 4px above, like the History page subtitle. Share link copy and Excel download stay as they are. The per-week table layout of 10b is out of scope (README decision 12).
- 10c, Administrator linked to a Teacher: the picker opens on their own Teacher. The closed value reads "{name} (אני)" / "{name} (me)", and the same option in the open list ends with "(אני)" / "(me)". "(me)" is in the secondary text color, separated from the name by 4px. Other options show the name only. An unlinked Administrator sees no "(me)" anywhere and keeps the first Teacher by name.

**Why:** AC 6 (an Administrator linked to a Teacher defaults to their own Teacher's view and can still switch to any Teacher), README review focus 5 (an explicit choice, such as a History row link `?teacherId=other`, beats the linked default), and frame 10b's copy for a Teacher.

- [ ] **Step 1: Write the failing store specs**

Replace `client\src\app\features\publications\state\publications.store.spec.ts` with the version below. It keeps all 11 existing specs unchanged, types the fake teacher list as the API response (`ItemForFindTeachersResponse`, which has no `isMe`), adds a third Teacher so a linked Administrator's own Teacher ("Teacher Levi") is neither the first by name ("Teacher Cohen") nor the query-param Teacher ("Teacher Mizrahi"), and adds `LINKED_ADMINISTRATOR` plus five specs at the end. `selects the first teacher by name once the teachers load` needs no change: an unlinked Administrator has `teacherId` `null`.

```typescript
import { HttpErrorResponse } from '@angular/common/http';
import { ApplicationRef, provideZonelessChangeDetection, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { NEVER, Observable, of, throwError } from 'rxjs';
import { AuthService } from '../../../core/auth.service';
import { LanguageService } from '../../../core/language.service';
import { ClipboardService } from '../../../core/services/clipboard.service';
import { FileDownloadService } from '../../../core/services/file-download.service';
import { ToastService } from '../../../core/services/toast.service';
import { PublicationState } from '../../../shared/models/publication-state.enum';
import { GetPublicationResponse } from '../data/get-publication.response';
import { ItemForFindTeachersResponse } from '../data/item-for-find-teachers.response';
import { PublicationsApiService } from '../data/publications-api.service';
import { TeacherOptionsApiService } from '../data/teacher-options-api.service';
import { TeacherOption } from '../domain/teacher-option.model';
import { PublicationsStore } from './publications.store';

const HTTP_NOT_FOUND = 404;
const HTTP_INTERNAL_SERVER_ERROR = 500;

const TEACHERS: ItemForFindTeachersResponse[] = [
    { id: 'teacher-levi', name: 'Teacher Levi' },
    { id: 'teacher-cohen', name: 'Teacher Cohen' },
    { id: 'teacher-mizrahi', name: 'Teacher Mizrahi' },
];

const OPEN_PUBLICATION: GetPublicationResponse = {
    id: 'publication-1',
    weekStart: '2026-10-04',
    weekNumber: 41,
    state: PublicationState.open,
    linkToken: 'link-token',
    windowStartUtc: '2026-10-01T16:00:00Z',
    windowEndUtc: '2026-10-09T11:00:00Z',
};

interface SignedInAs {
    isAdministrator: boolean;
    isTeacher: boolean;
    teacherId: string | null;
}

const ADMINISTRATOR: SignedInAs = { isAdministrator: true, isTeacher: false, teacherId: null };
const TEACHER: SignedInAs = { isAdministrator: false, isTeacher: true, teacherId: 'teacher-yael' };
const LINKED_ADMINISTRATOR: SignedInAs = { isAdministrator: true, isTeacher: false, teacherId: 'teacher-levi' };

const isAdministrator = signal(true);
const isTeacher = signal(false);
const teacherId = signal<string | null>(null);

function signIn(user: SignedInAs): void {
    isAdministrator.set(user.isAdministrator);
    isTeacher.set(user.isTeacher);
    teacherId.set(user.teacherId);
}

function createStore(
    teachers: Observable<ItemForFindTeachersResponse[]>,
    publication: Observable<GetPublicationResponse>,
    user: SignedInAs = ADMINISTRATOR,
): PublicationsStore {
    signIn(user);

    TestBed.configureTestingModule({
        providers: [
            provideZonelessChangeDetection(),
            { provide: LanguageService, useValue: { lang: signal('en'), locale: signal('en-IL') } },
            {
                provide: AuthService,
                useValue: { isAdministrator, isTeacher, teacherId },
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

async function loadedStore(user: SignedInAs = ADMINISTRATOR): Promise<PublicationsStore> {
    const store = createStore(
        of(TEACHERS),
        throwError(() => new HttpErrorResponse({ status: HTTP_NOT_FOUND })),
        user,
    );
    await TestBed.inject(ApplicationRef).whenStable();

    return store;
}

describe('PublicationsStore', () => {
    it('selects the linked Teacher when an Administrator signs out and a Teacher signs in', async () => {
        //given
        const store = await loadedStore();
        expect(store.selectedTeacherId()).toBe('teacher-cohen');

        //when
        signIn(TEACHER);
        await TestBed.inject(ApplicationRef).whenStable();

        //then
        expect(store.selectedTeacherId()).toBe('teacher-yael');
        expect(store.canChooseTeacher()).toBe(false);
    });

    it('selects the first teacher by name once the teachers load', async () => {
        //given
        const store = await loadedStore();

        //expected
        expect(store.selectedTeacherId()).toBe('teacher-cohen');
    });

    it('keeps an explicitly chosen teacher', async () => {
        //given
        const store = await loadedStore();

        //when
        store.selectTeacher('teacher-levi');

        //then
        expect(store.selectedTeacherId()).toBe('teacher-levi');
    });

    it('stays loading until the teachers arrive, so an open week shows no placeholder counts', async () => {
        //given
        const store = createStore(NEVER, of(OPEN_PUBLICATION));

        //when
        await vi.waitFor(() => expect(store.publication()).toBeDefined());

        //then
        expect(store.isLoading()).toBe(true);
    });

    it('names a history download after the row week, not the week selected on the dashboard', async () => {
        //given
        const store = await loadedStore();
        const fileDownload = TestBed.inject(FileDownloadService);
        store.selectWeek('2026-10-04');

        //when
        await store.downloadExcel('publication-week-38', 'teacher-levi', '2026-09-13');

        //then
        expect(fileDownload.download).toHaveBeenCalledWith(expect.any(Blob), 'week-2026-09-13.xlsx');
    });

    it('names a dashboard download after the selected week', async () => {
        //given
        const store = await loadedStore();
        const fileDownload = TestBed.inject(FileDownloadService);
        store.selectWeek('2026-10-04');

        //when
        await store.downloadExcel('publication-1');

        //then
        expect(fileDownload.download).toHaveBeenCalledWith(expect.any(Blob), 'week-2026-10-04.xlsx');
    });

    it('reports the load error instead of throwing when the week fails to load', async () => {
        //given
        const store = createStore(
            of(TEACHERS),
            throwError(() => new HttpErrorResponse({ status: HTTP_INTERNAL_SERVER_ERROR })),
        );

        //when
        await TestBed.inject(ApplicationRef).whenStable();

        //then
        expect(store.publication()).toBeUndefined();
        expect(store.state()).toBeUndefined();
        expect(store.weekNumber()).toBeUndefined();
        expect(store.dashboard()).toBeUndefined();
        expect(store.loadError()).toBe('publications.loadFailed');
    });

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

    it('selects a linked Administrator\'s own Teacher once the teachers load', async () => {
        //given
        const store = await loadedStore(LINKED_ADMINISTRATOR);

        //expected
        expect(store.selectedTeacherId()).toBe('teacher-levi');
        expect(store.canChooseTeacher()).toBe(true);
    });

    it('keeps the query-param Teacher over the linked default', async () => {
        //given
        const store = createStore(
            of(TEACHERS),
            throwError(() => new HttpErrorResponse({ status: HTTP_NOT_FOUND })),
            LINKED_ADMINISTRATOR,
        );
        expect(store.teachers()).toEqual([]);

        //when
        store.selectTeacher('teacher-mizrahi');
        await TestBed.inject(ApplicationRef).whenStable();

        //then
        expect(store.teachers().length).toBe(3);
        expect(store.selectedTeacherId()).toBe('teacher-mizrahi');
    });

    it('keeps the query-param Teacher over the linked default when it arrives after the teachers', async () => {
        //given
        const store = await loadedStore(LINKED_ADMINISTRATOR);

        //when
        store.selectTeacher('teacher-mizrahi');
        await TestBed.inject(ApplicationRef).whenStable();

        //then
        expect(store.selectedTeacherId()).toBe('teacher-mizrahi');
    });

    it('marks the signed-in User\'s own Teacher as me', async () => {
        //given
        const expected: TeacherOption[] = [
            { id: 'teacher-cohen', name: 'Teacher Cohen', isMe: false },
            { id: 'teacher-levi', name: 'Teacher Levi', isMe: true },
            { id: 'teacher-mizrahi', name: 'Teacher Mizrahi', isMe: false },
        ];

        //when
        const store = await loadedStore(LINKED_ADMINISTRATOR);

        //then
        expect(store.teachers()).toEqual(expected);
    });
});
```

- [ ] **Step 2: Run them to verify they fail**

From `client\` (PowerShell):

```powershell
& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js test --watch=false --include src/app/features/publications/state/publications.store.spec.ts
```

Expected: FAIL to compile in `marks the signed-in User's own Teacher as me`: `Object literal may only specify known properties, and 'isMe' does not exist in type 'TeacherOption'`. (Once the model has `isMe` and before the store sets it, that spec fails on `isMe: undefined`, and `selects a linked Administrator's own Teacher once the teachers load` fails with `teacher-cohen` instead of `teacher-levi`. The two `keeps the query-param Teacher...` specs already pass against today's store: they pin the ordering so the new default can't break it.)

- [ ] **Step 3: Give the option `isMe` and default to the signed-in User's own Teacher**

1. Replace `client\src\app\features\publications\domain\teacher-option.model.ts`:

```typescript
export interface TeacherOption {
    id: string;
    name: string;
    isMe: boolean;
}
```

2. In `client\src\app\features\publications\state\publications.store.ts`, replace `teachers`:

```typescript
    readonly teachers = computed<TeacherOption[]>(() => {
        const items = this.teachersResource.hasValue() ? this.teachersResource.value() : [];
        const ownTeacherId = this.auth.teacherId();

        return items
            .map((item) => ({ id: item.id, name: item.name, isMe: item.id === ownTeacherId }))
            .sort((a, b) => a.name.localeCompare(b.name));
    });
```

3. Replace `selectedTeacherIdState`:

```typescript
    private readonly selectedTeacherIdState = linkedSignal<TeacherOption[], string | null>({
        source: this.teachers,
        computation: (teachers, previous) =>
            this.auth.isTeacher()
                ? this.auth.teacherId()
                : (previous?.value ?? this.auth.teacherId() ?? teachers[0]?.id ?? null),
    });
```

`selectTeacher`, `canChooseTeacher` and `canManageLifecycle` stay as they are. A linked Administrator's first computation runs while `teachers` is still `[]` and already yields their own Teacher, so the dashboard loads it without waiting for the list; a `selectTeacher` from the page's query-param effect then sets the value, and the later `teachers` change keeps it through `previous.value`.

- [ ] **Step 4: Run them to verify they pass**

```powershell
& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js test --watch=false --include src/app/features/publications/state/publications.store.spec.ts
```

Expected: PASS (11 existing specs plus 4 new).

- [ ] **Step 5: Write the failing page specs**

In `client\src\app\features\publications\ui\pages\publications-dashboard\publications-dashboard.page.spec.ts`:

1. In `fakeStore`, replace the `teachers` entry so the default option has the new field:

```typescript
        teachers: signal([{ id: 'teacher-cohen', name: 'Teacher Cohen', isMe: false }]),
```

2. Add these specs at the end of `describe('PublicationsDashboardPage', ...)` (the testing Transloco has no translations, so every label renders as its key):

```typescript
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
```

- [ ] **Step 6: Run them to verify they fail**

```powershell
& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js test --watch=false --include src/app/features/publications/ui/pages/publications-dashboard/publications-dashboard.page.spec.ts
```

Expected: both new specs FAIL (no `.dashboard__only-yours` element; no `.dashboard__me` in the closed value). The existing specs pass.

- [ ] **Step 7: Add the "(me)" templates and the Teacher subtitle**

In `publications-dashboard.page.html`:

1. Give the Teacher `<p-select styleClass="dashboard__teacher-select" ... />` (inside `@if (store.canChooseTeacher())` in `<h2 class="dashboard__title">`) a body with the selected-value and option templates:

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
                            [ariaLabel]="'publications.selectTeacher' | transloco">
                            <ng-template pTemplate="selectedItem" let-teacher>
                                <span>{{ teacher.name }}</span>
                                @if (teacher.isMe) {
                                    <span class="dashboard__me">{{ 'publications.me' | transloco }}</span>
                                }
                            </ng-template>
                            <ng-template pTemplate="item" let-teacher>
                                <span>{{ teacher.name }}</span>
                                @if (teacher.isMe) {
                                    <span class="dashboard__me">{{ 'publications.me' | transloco }}</span>
                                }
                            </ng-template>
                        </p-select>
                    }
```

2. Add the Teacher subtitle between the closing `</div>` of `<div class="dashboard__title-row">` and `<div class="dashboard__subtitle">`:

```html
            @if (!store.canChooseTeacher()) {
                <p class="dashboard__only-yours">{{ 'publications.dashboard.onlyYours' | transloco }}</p>
            }
```

With both edits, the start of the heading block reads:

```html
        <div class="dashboard__heading">
            <div class="dashboard__title-row">
                <h2 class="dashboard__title">
                    @if (store.canChooseTeacher()) {
                        <p-select
                            styleClass="dashboard__teacher-select"
                            [options]="store.teachers()"
                            optionLabel="name"
                            optionValue="id"
                            [ngModel]="store.selectedTeacherId()"
                            (ngModelChange)="store.selectTeacher($event)"
                            [placeholder]="'publications.selectTeacher' | transloco"
                            [ariaLabel]="'publications.selectTeacher' | transloco">
                            <ng-template pTemplate="selectedItem" let-teacher>
                                <span>{{ teacher.name }}</span>
                                @if (teacher.isMe) {
                                    <span class="dashboard__me">{{ 'publications.me' | transloco }}</span>
                                }
                            </ng-template>
                            <ng-template pTemplate="item" let-teacher>
                                <span>{{ teacher.name }}</span>
                                @if (teacher.isMe) {
                                    <span class="dashboard__me">{{ 'publications.me' | transloco }}</span>
                                }
                            </ng-template>
                        </p-select>
                    }
                    @if (store.weekNumber(); as weekNumber) {
                        <span class="dashboard__week-title">{{ 'publications.dashboard.weekTitle' | transloco: { weekNumber } }}</span>
                    }
                </h2>
                @if (store.state(); as state) {
                    <app-publication-state-tag [state]="state" />
                }
            </div>
            @if (!store.canChooseTeacher()) {
                <p class="dashboard__only-yours">{{ 'publications.dashboard.onlyYours' | transloco }}</p>
            }
            <div class="dashboard__subtitle">
```

The rest of the template is unchanged.

In `publications-dashboard.page.scss`, add after the `.dashboard__teacher-select` block:

```scss
.dashboard__me {
    margin-inline-start: 4px;
    color: var(--app-text-secondary);
}

.dashboard__only-yours {
    margin-block: 4px 0;
    color: var(--app-text-secondary);
}
```

The option and selected-value templates are declared in this component's template, so their nodes carry its style scope and `.dashboard__me` applies inside the overlay without `::ng-deep`. The picker's display font for the closed value comes from the existing `.dashboard__teacher-select .p-select-label` rule, so "(me)" in the closed value has the same size as the name.

- [ ] **Step 8: Add the translations**

`client\public\i18n\he.json`, inside `publications`, right after `"selectTeacher"`:

```json
    "me": "(אני)",
```

and inside `publications.dashboard`, right after `"draftPromptTeacher"`:

```json
      "onlyYours": "רק הפרסומים שלך: מה שהתלמידים ביקשו ממך בכל שבוע.",
```

`client\public\i18n\en.json`, inside `publications`, right after `"selectTeacher"`:

```json
    "me": "(me)",
```

and inside `publications.dashboard`, right after `"draftPromptTeacher"`:

```json
      "onlyYours": "Only your Publications: what Students requested from you each week.",
```

- [ ] **Step 9: Run the page spec, then the client suite and build**

```powershell
& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js test --watch=false --include src/app/features/publications/ui/pages/publications-dashboard/publications-dashboard.page.spec.ts
& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js test --watch=false
& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js build --project client
```

Expected: all PASS (including `translations.spec.ts`, which checks that both files carry the same keys, and `source-text.spec.ts`), build succeeds. The "(me)" in the open option list is checked in the browser in task 8 (10c).

- [ ] **Step 10: Commit**

```bash
git add client/src/app/features/publications client/public/i18n/he.json client/public/i18n/en.json
git commit -m "feat(client): Publications opens the signed-in User's own Teacher (#90)

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```
