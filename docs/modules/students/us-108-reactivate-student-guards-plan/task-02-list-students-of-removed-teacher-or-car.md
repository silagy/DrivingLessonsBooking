# Task 2: Keep Students of a Deleted Teacher or Car on the Students Screen

Part of [#108 plan](README.md). Read the README's Decisions and Global Constraints first.

**Why:** `StudentQueries.FindAsync` and `GetAsync` inner-join `dbContext.Teachers` / `dbContext.Cars`. Both have `HasQueryFilter(x => !x.IsDeleted)`, so a Student whose Teacher or Car is deleted drops out of the list, even under "All", and `GET api/students/{id}` answers 404. #91 allows deleting a Teacher or Car whose Students are all inactive, so those Students become invisible. The Administrator can't find them, and the task 1 refusals for the deleted cases could never be triggered from the screen. (Found reviewing #105.)

**Files:**
- Modify: `src\DrivingLessons.Application\Queries\FindStudents\ItemForFindStudentsResponse.cs`, `src\DrivingLessons.Application\Queries\GetStudent\GetStudentResponse.cs`
- Modify: `src\DrivingLessons.Infrastructure\EntityFramework\Queries\StudentQueries.cs` (`FindAsync`, `GetAsync`)
- Modify: `client\src\app\features\students\data\item-for-find-students.response.ts`, `data\get-student.response.ts`, `domain\student.model.ts`
- Create: `client\src\app\features\students\domain\removed-part.enum.ts`
- Create: `client\src\app\features\students\ui\components\deleted-flag\deleted-flag.component.{ts,html,scss,spec.ts}`
- Modify: `client\src\app\features\students\ui\pages\students\students.page.{ts,html}`
- Modify: `client\public\i18n\he.json`, `en.json` (`students.deletedFlag.*`, `students.flag.tipInactive`)
- Modify: every Student / response fixture in `client\src\app\features\students\**\*.spec.ts` (`git grep -n "isCarOfTeacher:" -- client/src`)

**Interfaces:**
- Consumes: #95's `IsCarOfTeacher` on both responses, `CarFlagComponent`, the row template's Teacher and Car cells.
- Produces: `IsTeacherDeleted` / `IsCarDeleted` (`bool`) on `ItemForFindStudentsResponse` and `GetStudentResponse` → `isTeacherDeleted` / `isCarDeleted` on the client `Student` model; `RemovedPart` enum (`teacher`, `car`); `<app-deleted-flag [part]>`.

- [ ] **Step 1: Include deleted Teachers and Cars in both queries**

In `StudentQueries.FindAsync` and `GetAsync`, make both joins ignore the soft-delete filter and project the flags:

```csharp
        var query = from student in students
                    join teacher in dbContext.Teachers.IgnoreQueryFilters()
                        on student.TeacherId equals teacher.Id
                    join car in dbContext.Cars.IgnoreQueryFilters()
                        on student.CarId equals car.Id
```

In each `select`, after `IsCarOfTeacher = ...`:

```csharp
                        IsTeacherDeleted = teacher.IsDeleted,
                        IsCarDeleted = car.IsDeleted,
```

Add to both response classes, after `IsCarOfTeacher`:

```csharp
    public bool IsTeacherDeleted { get; init; }
    public bool IsCarDeleted { get; init; }
```

`IgnoreQueryFilters()` on a joined `DbSet` only affects that source. `UserQueries` and `SubmissionQueries` already use it the same way. The Teacher-role scoping in `FindAsync(teacherId)` is unchanged: a Teacher with an active User can't be deleted.

- [ ] **Step 2: Build and run the backend tests**

Run: `dotnet build` then `dotnet test`
Expected: green. There are no query unit tests. Task 4's Postgres smoke proves the projection (Review Focus).

- [ ] **Step 3: Carry the flags into the client model**

Add to `ItemForFindStudentsResponse`, `GetStudentResponse` (TS) and `Student`, after `isCarOfTeacher`:

```ts
    isTeacherDeleted: boolean;
    isCarDeleted: boolean;
```

The store passes rows through unchanged. Then add `isTeacherDeleted: false, isCarDeleted: false` after `isCarOfTeacher` in every fixture that `git grep -n "isCarOfTeacher:" -- client/src` lists, so the specs compile.

`client\src\app\features\students\domain\removed-part.enum.ts`:

```ts
export enum RemovedPart {
    teacher = 'teacher',
    car = 'car',
}
```

- [ ] **Step 4: Write the failing component spec**

`deleted-flag.component.spec.ts`, mirroring `car-flag.component.spec.ts`:

```ts
import { provideZonelessChangeDetection } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslocoTestingModule } from '@jsverse/transloco';
import { RemovedPart } from '../../../domain/removed-part.enum';
import { DeletedFlagComponent } from './deleted-flag.component';

async function render(part: RemovedPart): Promise<ComponentFixture<DeletedFlagComponent>> {
    TestBed.configureTestingModule({
        imports: [
            DeletedFlagComponent,
            TranslocoTestingModule.forRoot({
                langs: { en: {} },
                translocoConfig: { availableLangs: ['en'], defaultLang: 'en' },
            }),
        ],
        providers: [provideZonelessChangeDetection()],
    });

    const fixture = TestBed.createComponent(DeletedFlagComponent);
    fixture.componentRef.setInput('part', part);
    await fixture.whenStable();

    return fixture;
}

function flag(fixture: ComponentFixture<DeletedFlagComponent>): HTMLElement {
    return (fixture.nativeElement as HTMLElement).querySelector('.deleted-flag') as HTMLElement;
}

describe('DeletedFlagComponent', () => {
    it.each([RemovedPart.teacher, RemovedPart.car])('says in words that the %s was deleted', async (part) => {
        //when
        const fixture = await render(part);

        //then
        expect(flag(fixture).textContent).toContain(`students.deletedFlag.${part}`);
        expect(flag(fixture).querySelector('.pi-trash')?.getAttribute('aria-hidden')).toBe('true');
    });

    it('is reachable from the keyboard and explains the fix to screen readers', async () => {
        //when
        const fixture = await render(RemovedPart.teacher);

        //then
        expect(flag(fixture).getAttribute('tabindex')).toBe('0');
        expect(flag(fixture).getAttribute('role')).toBe('note');
        expect(flag(fixture).getAttribute('aria-label')).toBe('students.deletedFlag.tip');
    });
});
```

Run: `npx ng test --watch=false --include="**/deleted-flag.component.spec.ts"` (from `client\`)
Expected: FAIL, because the component doesn't exist.

- [ ] **Step 5: Build the component**

`deleted-flag.component.ts`:

```ts
import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { TranslocoPipe } from '@jsverse/transloco';
import { TooltipModule } from 'primeng/tooltip';
import { RemovedPart } from '../../../domain/removed-part.enum';

@Component({
    selector: 'app-deleted-flag',
    imports: [TranslocoPipe, TooltipModule],
    templateUrl: './deleted-flag.component.html',
    styleUrl: './deleted-flag.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DeletedFlagComponent {
    readonly part = input.required<RemovedPart>();

    protected readonly labelKey = computed(() => `students.deletedFlag.${this.part()}`);
}
```

`deleted-flag.component.html`:

```html
@let tip = 'students.deletedFlag.tip' | transloco;
<span
  class="deleted-flag"
  role="note"
  tabindex="0"
  [attr.aria-label]="tip"
  [pTooltip]="tip"
  tooltipPosition="bottom"
  tooltipEvent="both">
  <i class="pi pi-trash" aria-hidden="true"></i>
  {{ labelKey() | transloco }}
</span>
```

`deleted-flag.component.scss` is the car flag's shape in a neutral tone. The Student is inactive, so this is information, not an alarm:

```scss
.deleted-flag {
    display: inline-flex;
    align-items: center;
    gap: 0.3125rem;
    padding-block-end: 1px;
    border-block-end: 1px dotted var(--p-surface-400);
    font-size: 0.78rem;
    font-weight: 600;
    color: var(--p-surface-600);
    white-space: nowrap;
    cursor: help;
}

.deleted-flag:focus-visible {
    outline: 2px solid var(--p-sky-500);
    outline-offset: 2px;
    border-radius: 2px;
}

.deleted-flag .pi {
    font-size: 0.875rem;
}
```

Run the spec from step 4 again. Expected: PASS.

- [ ] **Step 6: Show the flags in the Students table**

In `students.page.ts`, import `DeletedFlagComponent` into `imports` and expose the enum: `protected readonly removedParts = RemovedPart;`.

In `students.page.html`, Teacher cell: right after `<span>{{ row.teacherName }}</span>`:

```html
              @if (row.isTeacherDeleted) {
                <app-deleted-flag [part]="removedParts.teacher" />
              }
```

In **both** Car places (the compact line inside the Teacher cell and the wide Car cell), replace the existing `@if (!row.isCarOfTeacher) { <app-car-flag ... /> }` with:

```html
                @if (row.isCarDeleted) {
                  <app-deleted-flag [part]="removedParts.car" />
                } @else if (!row.isCarOfTeacher && !row.isTeacherDeleted) {
                  <app-car-flag [teacherName]="row.teacherName" [isActive]="row.isActive" />
                }
```

A deleted Teacher or Car takes precedence over "Not this Teacher's Car". The deletion is the real reason the Student can't come back, and it is fixed differently (Decision 2).

- [ ] **Step 7: Translations**

`he.json`, under `students` (next to `flag`):

```json
    "deletedFlag": {
      "teacher": "המורה נמחק",
      "car": "הרכב נמחק",
      "tip": "כדי לסמן את התלמיד כפעיל, יש לייבא אותו שוב ברשימת התלמידים עם מורה ורכב קיימים."
    },
```

and reword `students.flag.tipInactive`. Today it says to mark the Student active first, which task 1 now refuses:

```json
      "tipInactive": "הרכב הזה אינו אחד מהרכבים של {{teacher}}. יש לשייך אותו למורה {{teacher}} במסך רכבים ומורים, ואז לסמן את התלמיד כפעיל."
```

`en.json`:

```json
    "deletedFlag": {
      "teacher": "Teacher deleted",
      "car": "Car deleted",
      "tip": "To reactivate the Student, import them again in the Roster with a current Teacher and Car."
    },
```

```json
      "tipInactive": "This Car isn't one of {{teacher}}'s Cars. Assign it to {{teacher}} on Cars & teachers, then mark the Student as active."
```

- [ ] **Step 8: Run the client suite**

Run (memory "Client build / npm workaround"), from `client\`: `npx ng test --watch=false`
Expected: all green, including `car-flag.component.spec.ts` (if it asserts the `tipInactive` key, not its text, it stays green) and `translations.spec.ts`.

- [ ] **Step 9: Commit**

```bash
git add src client
git commit -m "feat(students): keep Students of a deleted Teacher or Car on the Students screen (#108)"
```
