# Task 4 of 5: Select the first teacher by default (P3, TDD)

> Part of [#76: Publications Dashboard - Share Link While Open, Week Picker, Empty State](README.md). Sub-issue [#80](https://github.com/silagy/DrivingLessonsBooking/issues/80). Requires tasks 1–3 committed. Work on branch `76-dashboard-link-and-week`.

**Files:**
- Create: `client\src\app\features\publications\state\publications.store.spec.ts`
- Modify: `client\src\app\features\publications\state\publications.store.ts`

**Interfaces:**
- Consumes: `PublicationsStore.teachers: Signal<TeacherOption[]>` (sorted by name, exists) and `selectTeacher(teacherId: string)` (exists).
- Produces: `selectedTeacherId()` returns the first sorted teacher until one is chosen explicitly. The store's public shape doesn't change.

**Why:** README P3 and decision 6. With no teacher selected, an Open week shows `0` students and `0` picks as if they were real.

**Run the tests** (PowerShell, from `client\`):

```powershell
& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js test --watch=false --include "src/app/features/publications/**/*.spec.ts"
```

- [ ] **Step 1: Write the failing tests**

Create `publications.store.spec.ts`:

```typescript
import { HttpErrorResponse } from '@angular/common/http';
import { ApplicationRef, provideZonelessChangeDetection, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { LanguageService } from '../../../core/language.service';
import { ClipboardService } from '../../../core/services/clipboard.service';
import { FileDownloadService } from '../../../core/services/file-download.service';
import { ToastService } from '../../../core/services/toast.service';
import { PublicationsApiService } from '../data/publications-api.service';
import { TeacherOptionsApiService } from '../data/teacher-options-api.service';
import { PublicationsStore } from './publications.store';

const HTTP_NOT_FOUND = 404;

async function loadedStore(): Promise<PublicationsStore> {
    TestBed.configureTestingModule({
        providers: [
            provideZonelessChangeDetection(),
            { provide: LanguageService, useValue: { lang: signal('en'), locale: signal('en-IL') } },
            {
                provide: TeacherOptionsApiService,
                useValue: {
                    findTeachers: () =>
                        of([
                            { id: 'teacher-levi', name: 'Teacher Levi' },
                            { id: 'teacher-cohen', name: 'Teacher Cohen' },
                        ]),
                },
            },
            {
                provide: PublicationsApiService,
                useValue: {
                    getByWeek: () => throwError(() => new HttpErrorResponse({ status: HTTP_NOT_FOUND })),
                    findHistory: () => of([]),
                },
            },
            { provide: ToastService, useValue: { success: () => undefined, apiError: () => undefined } },
            { provide: ClipboardService, useValue: { copy: async () => true } },
            { provide: FileDownloadService, useValue: { download: () => undefined } },
        ],
    });

    const store = TestBed.inject(PublicationsStore);
    await TestBed.inject(ApplicationRef).whenStable();

    return store;
}

describe('PublicationsStore', () => {
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
});
```

- [ ] **Step 2: Run the tests to verify the first one fails**

Run the test command above.
Expected: `selects the first teacher by name once the teachers load` FAILS (`selectedTeacherId()` is `null`). `keeps an explicitly chosen teacher` already PASSES.

- [ ] **Step 3: Make the selected teacher follow the teacher list**

In `publications.store.ts`:

1. Add `linkedSignal` to the `@angular/core` import.
2. Delete the line `private readonly selectedTeacherIdState = signal<string | null>(null);`.
3. Directly after the `readonly teachers = computed<TeacherOption[]>(...)` block, add the replacement. It must come **after** `teachers`, because it reads `this.teachers` when it's created:

```typescript
    private readonly selectedTeacherIdState = linkedSignal<TeacherOption[], string | null>({
        source: this.teachers,
        computation: (teachers, previous) => previous?.value ?? teachers[0]?.id ?? null,
    });
```

4. Move `readonly selectedTeacherId = this.selectedTeacherIdState.asReadonly();` from the block of readonly projections to directly after the new `selectedTeacherIdState` declaration. Field initializers run in order, so `asReadonly()` must not run before the linked signal exists.

`selectTeacher()` keeps calling `this.selectedTeacherIdState.set(teacherId)`, since a `linkedSignal` is writable. `dashboardResource.params` reads `this.selectedTeacherIdState()` lazily, so its earlier position in the class is fine.

- [ ] **Step 4: Run the tests to verify they pass**

Run the test command above.
Expected: both `PublicationsStore` tests PASS, and the page tests from tasks 1–3 still PASS.

- [ ] **Step 5: Commit**

```bash
git add client/src/app/features/publications/state
git commit -m "fix(publications): select the first teacher by default on the dashboard" -m "Closes #80"
```
