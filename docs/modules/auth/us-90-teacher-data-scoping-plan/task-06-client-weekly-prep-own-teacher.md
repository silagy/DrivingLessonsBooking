# Task 6 of 8: Weekly prep opens the signed-in User's own Teacher (client)

> Part of [#90: Teacher-role Users See and Change Only Their Own Teacher's Data](README.md). Requires task 5 committed. Work on branch `90-teacher-data-scoping`. Read README decisions 9, 10 and 11 first.

**Files:**
- Modify: `client\src\app\features\week-schedules\domain\teacher-option.model.ts`
- Modify: `client\src\app\features\week-schedules\state\week-schedules.store.ts`
- Test: `client\src\app\features\week-schedules\state\week-schedules.store.spec.ts`
- Modify: `client\src\app\features\week-schedules\ui\pages\weekly-prep\weekly-prep.page.html`, `weekly-prep.page.ts`
- Modify: `client\public\i18n\he.json`, `client\public\i18n\en.json`

**Interfaces:**
- Consumes: `AuthService.isAdministrator`, `AuthService.isTeacher`, `AuthService.teacherId` (#89; `teacherId` reads the token's `teacher_id`, which is present whenever the User is linked, for any Role). `SignedInUserStore.teacherName: Signal<string | null>` (task 5, `client\src\app\core\signed-in-user\signed-in-user.store.ts`). `LockedFieldComponent` (task 5, selector `app-locked-field`, inputs `label` and `value`, `client\src\app\shared\components\locked-field\locked-field.component.ts`).
- Produces:
  - `TeacherOption { id: string; name: string; isMe: boolean }` (week-schedules feature), `isMe` is `id === auth.teacherId()`
  - `WeekSchedulesStore.selectedTeacherId` defaults to `auth.teacherId()` for every Role: a Teacher's linked Teacher, a linked Administrator's own Teacher, `null` for an unlinked Administrator. `selectTeacher` still works only for an Administrator, so a linked Administrator can switch
  - `WeekSchedulesStore.ownTeacherName: Signal<string | null>`: the signed-in User's linked Teacher's name from `api/me`, for the locked field
  - i18n key `weekSchedules.me`: "(אני)" / "(me)"

**Design (frames 10a, 10c):**
- 10a, Teacher: in the selectors row, the Teacher picker is replaced by the locked field. Label "מורה" / "Teacher" above (the existing key `weekSchedules.teacher`), then a 42px-high chip with a lock icon and the Teacher's name, grey surface and border (all styling comes from `LockedFieldComponent`, task 5). The Week picker beside it is unchanged. No "Create Week Schedule" and no "Publish week" (already #89).
- 10c, Administrator linked to a Teacher: the picker opens on their own Teacher, the closed value reads "{name} (אני)" / "{name} (me)", and the open list shows the same "(me)" after that option's name. Every other option is the plain name. An unlinked Administrator sees no "(me)" anywhere and starts with no Teacher, as today.

**Why:** AC 1 (a Teacher sees their own Teacher's Week Schedule, named) and AC 6 (an Administrator linked to a Teacher defaults to their own Teacher's view and can still switch); decisions 10 and 11.

- [ ] **Step 1: Write the failing store specs**

Replace `client\src\app\features\week-schedules\state\week-schedules.store.spec.ts` with the version below. The 9 existing specs are kept unchanged in behavior; the fixture now also provides a fake `SignedInUserStore` (the store injects it) and the teacher list has a second Teacher, so a linked Administrator has someone else to switch to. `resets the selected Teacher when a Teacher signs out and an Administrator signs in` still expects `null`, because `ADMINISTRATOR` is unlinked (`teacherId: null`). Four specs are new.

```typescript
import { HttpErrorResponse } from '@angular/common/http';
import { ApplicationRef, provideZonelessChangeDetection, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { AuthService } from '../../../core/auth.service';
import { LanguageService } from '../../../core/language.service';
import { ToastService } from '../../../core/services/toast.service';
import { SignedInUserStore } from '../../../core/signed-in-user/signed-in-user.store';
import { PublicationState } from '../../../shared/models/publication-state.enum';
import { GetPublicationResponse } from '../data/get-publication.response';
import { GetWeekScheduleResponse } from '../data/get-week-schedule.response';
import { PublicationStatusApiService } from '../data/publication-status-api.service';
import { TeacherOptionsApiService } from '../data/teacher-options-api.service';
import { WeekSchedulesApiService } from '../data/week-schedules-api.service';
import { WeekSchedulesStore } from './week-schedules.store';

const HTTP_NOT_FOUND = 404;
const HTTP_INTERNAL_SERVER_ERROR = 500;

interface SignedInAs {
    isAdministrator: boolean;
    isTeacher: boolean;
    teacherId: string | null;
}

const ADMINISTRATOR: SignedInAs = { isAdministrator: true, isTeacher: false, teacherId: null };
const LINKED_ADMINISTRATOR: SignedInAs = { isAdministrator: true, isTeacher: false, teacherId: 'teacher-cohen' };
const TEACHER: SignedInAs = { isAdministrator: false, isTeacher: true, teacherId: 'teacher-yael' };

const TEACHER_NAMES: Record<string, string> = {
    'teacher-yael': 'Yael Carmi',
    'teacher-cohen': 'Teacher Cohen',
};

const WEEK_SCHEDULE: GetWeekScheduleResponse = {
    id: 'schedule-1',
    teacherId: 'teacher-yael',
    weekStart: '2026-10-11',
    slots: [],
};

const DRAFT_PUBLICATION: GetPublicationResponse = {
    id: 'publication-1',
    weekStart: '2026-10-11',
    state: PublicationState.draft,
    linkToken: 'link-token',
    windowStartUtc: null,
    windowEndUtc: null,
};

function notFound() {
    return throwError(() => new HttpErrorResponse({ status: HTTP_NOT_FOUND }));
}

function serverError() {
    return throwError(() => new HttpErrorResponse({ status: HTTP_INTERNAL_SERVER_ERROR }));
}

function teacherNameOf(user: SignedInAs): string | null {
    return user.teacherId ? TEACHER_NAMES[user.teacherId] : null;
}

function storeSignedInAs(
    user: SignedInAs,
    getByTeacherAndWeek: ReturnType<typeof vi.fn> = vi.fn(() => of(WEEK_SCHEDULE)),
) {
    const findTeachers = vi.fn(() =>
        of([
            { id: 'teacher-levi', name: 'Teacher Levi' },
            { id: 'teacher-cohen', name: 'Teacher Cohen' },
        ]),
    );
    const create = vi.fn(() => of({ id: 'schedule-1' }));
    const isAdministrator = signal(user.isAdministrator);
    const isTeacher = signal(user.isTeacher);
    const teacherId = signal(user.teacherId);
    const teacherName = signal<string | null>(teacherNameOf(user));

    TestBed.configureTestingModule({
        providers: [
            provideZonelessChangeDetection(),
            { provide: LanguageService, useValue: { lang: signal('en'), locale: signal('en-IL') } },
            {
                provide: AuthService,
                useValue: { isAdministrator, isTeacher, teacherId },
            },
            { provide: SignedInUserStore, useValue: { name: signal<string | null>('Signed In User'), teacherName } },
            { provide: TeacherOptionsApiService, useValue: { findTeachers } },
            { provide: WeekSchedulesApiService, useValue: { getByTeacherAndWeek, create } },
            { provide: PublicationStatusApiService, useValue: { getByWeek: () => of(DRAFT_PUBLICATION) } },
            { provide: ToastService, useValue: { success: () => undefined, apiError: () => undefined } },
        ],
    });

    const signIn = (next: SignedInAs) => {
        isAdministrator.set(next.isAdministrator);
        isTeacher.set(next.isTeacher);
        teacherId.set(next.teacherId);
        teacherName.set(teacherNameOf(next));
    };

    return { store: TestBed.inject(WeekSchedulesStore), findTeachers, getByTeacherAndWeek, create, signIn };
}

async function settle(): Promise<void> {
    await TestBed.inject(ApplicationRef).whenStable();
}

describe('WeekSchedulesStore', () => {
    it('resets the selected Teacher when a Teacher signs out and an Administrator signs in', async () => {
        //given
        const { store, signIn } = storeSignedInAs(TEACHER);
        await settle();
        expect(store.selectedTeacherId()).toBe('teacher-yael');

        //when
        signIn(ADMINISTRATOR);
        await settle();

        //then
        expect(store.selectedTeacherId()).toBeNull();
        expect(store.canChooseTeacher()).toBe(true);
    });

    it('reports the load error instead of throwing when the week fails to load', async () => {
        //given
        const { store } = storeSignedInAs(ADMINISTRATOR, vi.fn(serverError));
        store.selectTeacher('teacher-cohen');

        //when
        await settle();

        //then
        expect(store.weekSchedule()).toBeUndefined();
        expect(store.slots()).toEqual([]);
        expect(store.unavailableCount()).toBe(0);
        expect(store.isNotCreatedYet()).toBe(false);
        expect(store.loadError()).toBe('weekSchedules.loadFailed');
    });

    it('lets an Administrator choose a Teacher and publish a draft week', async () => {
        //given
        const { store, findTeachers } = storeSignedInAs(ADMINISTRATOR);

        //when
        store.selectTeacher('teacher-cohen');
        await settle();

        //then
        expect(findTeachers).toHaveBeenCalled();
        expect(store.canChooseTeacher()).toBe(true);
        expect(store.selectedTeacherId()).toBe('teacher-cohen');
        expect(store.canPublish()).toBe(true);
    });

    it('still creates a missing week for an Administrator', async () => {
        //given
        const getByTeacherAndWeek = vi.fn().mockReturnValueOnce(notFound()).mockReturnValue(of(WEEK_SCHEDULE));
        const { store, create } = storeSignedInAs(ADMINISTRATOR, getByTeacherAndWeek);

        //when
        store.selectTeacher('teacher-cohen');
        await settle();

        //then
        expect(create).toHaveBeenCalledWith({ teacherId: 'teacher-cohen', weekStart: store.selectedWeekStart() });
        expect(store.weekSchedule()?.id).toBe('schedule-1');
        expect(store.isNotCreatedYet()).toBe(false);
    });

    it('opens a linked Administrator\'s own Teacher by default', async () => {
        //given
        const { store, getByTeacherAndWeek } = storeSignedInAs(LINKED_ADMINISTRATOR);

        //when
        await settle();

        //then
        expect(store.canChooseTeacher()).toBe(true);
        expect(store.selectedTeacherId()).toBe('teacher-cohen');
        expect(getByTeacherAndWeek).toHaveBeenCalledWith('teacher-cohen', store.selectedWeekStart());
        expect(store.weekSchedule()?.id).toBe('schedule-1');
    });

    it('still lets a linked Administrator choose another Teacher', async () => {
        //given
        const { store, getByTeacherAndWeek } = storeSignedInAs(LINKED_ADMINISTRATOR);
        await settle();

        //when
        store.selectTeacher('teacher-levi');
        await settle();

        //then
        expect(store.selectedTeacherId()).toBe('teacher-levi');
        expect(getByTeacherAndWeek).toHaveBeenLastCalledWith('teacher-levi', store.selectedWeekStart());
    });

    it('marks the signed-in User\'s own Teacher as me', async () => {
        //given
        const { store } = storeSignedInAs(LINKED_ADMINISTRATOR);

        //when
        await settle();

        //then
        expect(store.teachers()).toEqual([
            { id: 'teacher-cohen', name: 'Teacher Cohen', isMe: true },
            { id: 'teacher-levi', name: 'Teacher Levi', isMe: false },
        ]);
    });

    it('never asks for the teacher list for a Teacher', async () => {
        //given
        const { store, findTeachers } = storeSignedInAs(TEACHER);

        //when
        await settle();

        //then
        expect(findTeachers).not.toHaveBeenCalled();
        expect(store.teachers()).toEqual([]);
        expect(store.canChooseTeacher()).toBe(false);
        expect(store.loadError()).toBeUndefined();
    });

    it('opens the linked Teacher\'s week for a Teacher', async () => {
        //given
        const { store, getByTeacherAndWeek } = storeSignedInAs(TEACHER);

        //when
        await settle();

        //then
        expect(store.selectedTeacherId()).toBe('teacher-yael');
        expect(getByTeacherAndWeek).toHaveBeenCalledWith('teacher-yael', store.selectedWeekStart());
        expect(store.weekSchedule()?.id).toBe('schedule-1');
    });

    it('names the Teacher\'s own Teacher for the locked field', async () => {
        //given
        const { store } = storeSignedInAs(TEACHER);

        //when
        await settle();

        //then
        expect(store.canChooseTeacher()).toBe(false);
        expect(store.ownTeacherName()).toBe('Yael Carmi');
    });

    it('ignores a Teacher picking another Teacher', async () => {
        //given
        const { store } = storeSignedInAs(TEACHER);

        //when
        store.selectTeacher('teacher-cohen');
        await settle();

        //then
        expect(store.selectedTeacherId()).toBe('teacher-yael');
    });

    it('shows a missing week as not prepared instead of creating it, for a Teacher', async () => {
        //given
        const { store, create } = storeSignedInAs(TEACHER, vi.fn(notFound));

        //when
        await settle();

        //then
        expect(create).not.toHaveBeenCalled();
        expect(store.isNotCreatedYet()).toBe(true);
        expect(store.weekSchedule()).toBeUndefined();
        expect(store.loadError()).toBeUndefined();
    });

    it('never offers publishing to a Teacher', async () => {
        //given
        const { store } = storeSignedInAs(TEACHER);

        //when
        await settle();

        //then
        expect(store.publicationState()).toBe(PublicationState.draft);
        expect(store.canPublish()).toBe(false);
    });
});
```

- [ ] **Step 2: Run it to verify it fails**

From `client\` (PowerShell):

```powershell
& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js test --watch=false --include src/app/features/week-schedules/state/week-schedules.store.spec.ts
```

Expected: FAIL to compile: `Property 'ownTeacherName' does not exist on type 'WeekSchedulesStore'`. Once that compiles, `opens a linked Administrator's own Teacher by default`, `still lets a linked Administrator choose another Teacher` (the store would start on `null`) and `marks the signed-in User's own Teacher as me` (no `isMe`) also fail.

- [ ] **Step 3: Give the Teacher option `isMe`**

Replace `client\src\app\features\week-schedules\domain\teacher-option.model.ts`:

```typescript
export interface TeacherOption {
    id: string;
    name: string;
    isMe: boolean;
}
```

- [ ] **Step 4: Default to the signed-in User's own Teacher and expose its name**

In `client\src\app\features\week-schedules\state\week-schedules.store.ts`:

1. Imports: add, after the `AuthService` import:

```typescript
import { SignedInUserStore } from '../../../core/signed-in-user/signed-in-user.store';
```

2. Inject the signed-in User store right after `private readonly auth = inject(AuthService);`:

```typescript
    private readonly signedInUser = inject(SignedInUserStore);
```

3. Replace the `selectedTeacherIdState` field (every Role now starts on the token's linked Teacher; an unlinked Administrator still starts on `null`, and the `linkedSignal` still resets on every sign-in change):

```typescript
    private readonly selectedTeacherIdState = linkedSignal<string | null>(() => this.auth.teacherId());
```

4. Replace `teachers`:

```typescript
    readonly teachers = computed<TeacherOption[]>(() => {
        const items = this.teachersResource.hasValue() ? this.teachersResource.value() : [];
        const ownTeacherId = this.auth.teacherId();

        return items
            .map((item) => ({ id: item.id, name: item.name, isMe: item.id === ownTeacherId }))
            .sort((a, b) => a.name.localeCompare(b.name));
    });
```

5. Add `ownTeacherName` right after `selectedTeacherId`:

```typescript
    readonly ownTeacherName = this.signedInUser.teacherName;
```

`selectTeacher`, `canChooseTeacher`, `loadOrCreate` and the resources are unchanged: `canChooseTeacher` stays Administrator-only, so a linked Administrator can switch and a Teacher still can't.

- [ ] **Step 5: Run it to verify it passes**

```powershell
& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js test --watch=false --include src/app/features/week-schedules/state/week-schedules.store.spec.ts
```

Expected: PASS (13 specs).

- [ ] **Step 6: Locked field for a Teacher, "(me)" in the picker**

In `client\src\app\features\week-schedules\ui\pages\weekly-prep\weekly-prep.page.ts`, add the import after the `PublicationStateTagComponent` import:

```typescript
import { LockedFieldComponent } from '../../../../../shared/components/locked-field/locked-field.component';
```

and add `LockedFieldComponent` to the end of the `imports` array, after `PublicationStateTagComponent`:

```typescript
        PublicationStateTagComponent,
        LockedFieldComponent,
    ],
```

In `client\src\app\features\week-schedules\ui\pages\weekly-prep\weekly-prep.page.html`, replace the Teacher block at the top of `.weekly-prep__selectors` (the `@if (store.canChooseTeacher()) { ... }` around the `inputId="teacher"` picker) with:

```html
  <div class="weekly-prep__selectors">
    @if (store.canChooseTeacher()) {
      <div class="field">
        <label for="teacher">{{ 'weekSchedules.teacher' | transloco }}</label>
        <p-select
          inputId="teacher"
          [options]="store.teachers()"
          optionLabel="name"
          optionValue="id"
          [ngModel]="store.selectedTeacherId()"
          (ngModelChange)="store.selectTeacher($event)"
          [placeholder]="'weekSchedules.selectTeacher' | transloco">
          <ng-template pTemplate="selectedItem" let-teacher>
            <span>{{ teacher.name }}</span>
            @if (teacher.isMe) {
              <span>&nbsp;{{ 'weekSchedules.me' | transloco }}</span>
            }
          </ng-template>
          <ng-template pTemplate="item" let-teacher>
            <span>{{ teacher.name }}</span>
            @if (teacher.isMe) {
              <span>&nbsp;{{ 'weekSchedules.me' | transloco }}</span>
            }
          </ng-template>
        </p-select>
      </div>
    } @else {
      <app-locked-field
        class="field"
        [label]="'weekSchedules.teacher' | transloco"
        [value]="store.ownTeacherName() ?? ''" />
    }
    <div class="field">
```

(The Week field after it is unchanged.)

Notes:
- `pTemplate="item"` / `pTemplate="selectedItem"` is the syntax the client already uses on `p-select` (`add-user.dialog.html`, `roster.page.html`); `SelectModule` re-exports `PrimeTemplate`, so no extra import. PrimeNG 21 renders the `selectedItem` template only when an option is selected and falls back to the placeholder otherwise, so an unlinked Administrator still sees "Select a teacher".
- `class="field"` on the host puts the locked field under the existing `.weekly-prep__selectors .field` rule (column, 6px gap), so its label sits above the chip like the Week field's label. The chip's look (lock icon, 42px block size, grey surface and border) is all `LockedFieldComponent`'s; `weekly-prep.page.scss` doesn't change.
- `&nbsp;` keeps "Yael Carmi (me)" / "(אני) Yael Carmi" on one line in both directions; the name and the "(me)" are separate spans so bidi orders them correctly in RTL.

- [ ] **Step 7: Add the translation**

Inside the `weekSchedules` object, right after `"selectTeacher"`:

`client\public\i18n\he.json`:

```json
    "me": "(אני)",
```

`client\public\i18n\en.json`:

```json
    "me": "(me)",
```

- [ ] **Step 8: Run the client suite and build**

```powershell
& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js test --watch=false
& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js build --project client
```

Expected: all PASS (including `translations.spec.ts`, which checks `weekSchedules.me` exists in both files, and `source-text.spec.ts`), build succeeds.

- [ ] **Step 9: Commit**

```bash
git add client/src/app/features/week-schedules client/public/i18n/he.json client/public/i18n/en.json
git commit -m "feat(client): Weekly prep opens the signed-in User's own Teacher, locked for a Teacher (#90)

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```
