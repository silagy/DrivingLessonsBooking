# Task 5 of 7: Weekly prep for a Teacher-role User

> Part of [#89: Teacher-role Users Reach Only Week Schedules and Publications](README.md). Requires task 4 committed. Work on branch `89-teacher-role-navigation`. Read README decisions 10 and 11 first.

**Files:**
- Modify: `client\src\app\features\week-schedules\state\week-schedules.store.ts`
- Test: `client\src\app\features\week-schedules\state\week-schedules.store.spec.ts`
- Modify: `client\src\app\features\week-schedules\ui\pages\weekly-prep\weekly-prep.page.html`
- Modify: `client\public\i18n\he.json`, `client\public\i18n\en.json`

**Interfaces:**
- Consumes: `AuthService.isAdministrator`, `AuthService.isTeacher`, `AuthService.teacherId` (task 2). Server behavior from task 1: a Teacher-role User gets 403 from `GET api/teachers/find` and `POST api/week-schedules`, and 200 / 404 from `GET api/week-schedules/by-teacher-and-week`.
- Produces (on `WeekSchedulesStore`, all readonly signals):
  - `canChooseTeacher: Signal<boolean>`: `true` only for an Administrator
  - `canPublish: Signal<boolean>`: now `Draft` **and** Administrator
  - `isNotCreatedYet: Signal<boolean>`: the selected week has no Week Schedule and the signed-in User may not create it
  - `selectTeacher(teacherId)` is ignored for a Teacher-role User; their selected Teacher is always the token's `teacher_id`

**Design (frame 10a):** for a Teacher, the page header has no "יצירת מערכת שבועית" and no "פרסום שבוע..." button: they're hidden, not disabled. The Week picker, the Publication state tag, the grid and the legend look as they do for an Administrator. The locked Teacher chip of 10a (lock icon + the Teacher's name) is **#90**; in #89 the Teacher picker is simply not rendered for a Teacher.

**Why:** AC 3 ("Creating a Week Schedule ... remain Administrator-only") and AC 7 for the "Publish week" link; and decision 10, without which the page fails to load for a Teacher (403 on the picker and on the auto-create).

- [ ] **Step 1: Write the failing store specs**

Replace `client\src\app\features\week-schedules\state\week-schedules.store.spec.ts` with the version below. The existing spec (`reports the load error ...`) is kept, now signed in as an Administrator, because the store injects `AuthService`.

```typescript
import { HttpErrorResponse } from '@angular/common/http';
import { ApplicationRef, provideZonelessChangeDetection, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { AuthService } from '../../../core/auth.service';
import { LanguageService } from '../../../core/language.service';
import { ToastService } from '../../../core/services/toast.service';
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
const TEACHER: SignedInAs = { isAdministrator: false, isTeacher: true, teacherId: 'teacher-yael' };

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

function storeSignedInAs(
    user: SignedInAs,
    getByTeacherAndWeek: ReturnType<typeof vi.fn> = vi.fn(() => of(WEEK_SCHEDULE)),
) {
    const findTeachers = vi.fn(() => of([{ id: 'teacher-cohen', name: 'Teacher Cohen' }]));
    const create = vi.fn(() => of({ id: 'schedule-1' }));

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
            { provide: TeacherOptionsApiService, useValue: { findTeachers } },
            { provide: WeekSchedulesApiService, useValue: { getByTeacherAndWeek, create } },
            { provide: PublicationStatusApiService, useValue: { getByWeek: () => of(DRAFT_PUBLICATION) } },
            { provide: ToastService, useValue: { success: () => undefined, apiError: () => undefined } },
        ],
    });

    return { store: TestBed.inject(WeekSchedulesStore), findTeachers, getByTeacherAndWeek, create };
}

async function settle(): Promise<void> {
    await TestBed.inject(ApplicationRef).whenStable();
}

describe('WeekSchedulesStore', () => {
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

Expected: FAIL to compile: `Property 'isNotCreatedYet' does not exist` and `Property 'canChooseTeacher' does not exist`.

- [ ] **Step 3: Make the store Role-aware**

In `client\src\app\features\week-schedules\state\week-schedules.store.ts`:

1. Imports: add `linkedSignal` to the `@angular/core` import, and add:

```typescript
import { AuthService } from '../../../core/auth.service';
```

2. Inject the auth service with the other injections:

```typescript
    private readonly auth = inject(AuthService);
```

3. Replace the `selectedTeacherIdState` field:

```typescript
    private readonly selectedTeacherIdState = linkedSignal<string | null>(() =>
        this.auth.isTeacher() ? this.auth.teacherId() : null,
    );
```

4. Replace `teachersResource` so it only loads for an Administrator (a resource whose `params` return `undefined` stays idle: no request, not loading, no error):

```typescript
    private readonly teachersResource = resource({
        params: () => (this.auth.isAdministrator() ? true : undefined),
        loader: () => firstValueFrom(this.teachersApi.findTeachers()),
    });
```

5. Replace `canPublish` and add `canChooseTeacher` next to it:

```typescript
    readonly canChooseTeacher = computed(() => this.auth.isAdministrator());
    readonly canPublish = computed(
        () => this.publicationState() === PublicationState.draft && this.auth.isAdministrator(),
    );
```

6. Replace `weekSchedule` and add `isNotCreatedYet` after it (the schedule resource can now resolve to `null`):

```typescript
    readonly weekSchedule = computed<WeekSchedule | undefined>(() =>
        this.scheduleResource.hasValue() ? (this.scheduleResource.value() ?? undefined) : undefined,
    );

    readonly isNotCreatedYet = computed(
        () => this.scheduleResource.hasValue() && this.scheduleResource.value() === null,
    );
```

7. Replace `selectTeacher`:

```typescript
    selectTeacher(teacherId: string): void {
        if (!this.canChooseTeacher()) {
            return;
        }

        this.selectedTeacherIdState.set(teacherId);
    }
```

8. Replace `loadOrCreate` (the only change: a User who may not create gets `null` instead of a `POST`):

```typescript
    private async loadOrCreate(teacherId: string, weekStart: string): Promise<GetWeekScheduleResponse | null> {
        try {
            return await firstValueFrom(this.api.getByTeacherAndWeek(teacherId, weekStart));
        } catch (error) {
            if (!isStatus(error, HTTP_NOT_FOUND)) {
                throw error;
            }
        }

        if (!this.auth.isAdministrator()) {
            return null;
        }

        try {
            await firstValueFrom(this.api.create({ teacherId, weekStart }));
        } catch (error) {
            if (!isStatus(error, HTTP_CONFLICT)) {
                throw error;
            }
        }

        return firstValueFrom(this.api.getByTeacherAndWeek(teacherId, weekStart));
    }
```

Note the order of fields: `selectedTeacherIdState` must stay declared **before** `scheduleResource` (it already is), because field initializers run top to bottom and `auth` must be injected before both.

- [ ] **Step 4: Run it to verify it passes**

```powershell
& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js test --watch=false --include src/app/features/week-schedules/state/week-schedules.store.spec.ts
```

Expected: PASS (8 specs).

- [ ] **Step 5: Hide the picker and show the not-prepared state**

In `client\src\app\features\week-schedules\ui\pages\weekly-prep\weekly-prep.page.html`:

Wrap the Teacher field (the first `<div class="field">` inside `.weekly-prep__selectors`, the one with `inputId="teacher"`) in a Role check:

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
          [placeholder]="'weekSchedules.selectTeacher' | transloco" />
      </div>
    }
    <div class="field">
```

(The Week field after it is unchanged.)

In the state chain below, add a branch between `!store.hasSelection()` and `store.weekSchedule()`:

```html
  } @else if (!store.hasSelection()) {
    <div class="weekly-prep__state">{{ 'weekSchedules.selectTeacherPrompt' | transloco }}</div>
  } @else if (store.isNotCreatedYet()) {
    <div class="weekly-prep__state">{{ 'weekSchedules.notCreatedYet' | transloco }}</div>
  } @else if (store.weekSchedule()) {
```

The "Publish week" link already renders only `@if (store.canPublish())`, so it disappears for a Teacher with no template change.

- [ ] **Step 6: Add the translation**

Inside the `weekSchedules` object, right after `"selectTeacherPrompt"`:

`client\public\i18n\he.json`:

```json
    "notCreatedYet": "מנהל המערכת עדיין לא הכין את השבוע הזה.",
```

`client\public\i18n\en.json`:

```json
    "notCreatedYet": "The Administrator hasn't prepared this week yet.",
```

- [ ] **Step 7: Run the client suite and build**

```powershell
& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js test --watch=false
& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js build --project client
```

Expected: all PASS, build succeeds.

- [ ] **Step 8: Commit**

```bash
git add client/src/app/features/week-schedules client/public/i18n/he.json client/public/i18n/en.json
git commit -m "feat(client): Weekly prep opens a Teacher's own week without the picker, creation or publishing (#89)

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```
