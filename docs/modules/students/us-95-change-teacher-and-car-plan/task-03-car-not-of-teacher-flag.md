# Task 3 of 7: Car-not-of-Teacher flag (backend projection + list marker)

> Part of [#95: Change Teacher and Change Car for a Student](README.md). Requires task 2 committed. Work on branch `95-change-teacher-and-car`. Read README decisions 10, 11 and 19 first.

**Files:**
- Modify: `src\DrivingLessons.Application\Queries\FindStudents\ItemForFindStudentsResponse.cs`
- Modify: `src\DrivingLessons.Application\Queries\GetStudent\GetStudentResponse.cs`
- Modify: `src\DrivingLessons.Infrastructure\EntityFramework\Queries\StudentQueries.cs` (`FindAsync`, `GetAsync`)
- Modify: `client\src\app\features\students\domain\student.model.ts`
- Modify: `client\src\app\features\students\data\item-for-find-students.response.ts`, `get-student.response.ts`
- Modify (fixtures only): `client\src\app\features\students\state\students.store.spec.ts`, `domain\student-details.spec.ts`, `ui\dialogs\deactivate-student\deactivate-student.dialog.spec.ts`, `ui\dialogs\edit-student\edit-student.dialog.spec.ts`
- Create: `client\src\app\features\students\ui\components\car-flag\car-flag.component.ts`, `.html`, `.scss`, `.spec.ts`
- Modify: `client\src\app\features\students\ui\pages\students\students.page.ts`, `.html`, `.scss`
- Modify: `client\src\app\features\students\ui\components\student-who-card\student-who-card.component.ts`, `.html`, `.scss`
- Modify: `client\public\i18n\he.json`, `en.json` (`students.flag`)

**Interfaces:**
- Consumes: task 2 (nothing new from it; the flag is independent of the commands), `car.TeacherAssignments` (owned collection, each `TeacherId`), the existing `StudentsPage` row template and `StudentWhoCardComponent`, PrimeNG `TooltipModule` (`primeng/tooltip`, directive `pTooltip`).
- Produces (tasks 4 to 6 rely on these):
  - JSON `isCarOfTeacher: boolean` on every item of `GET api/students/find` and on `GET api/students/{id}`.
  - Client `Student.isCarOfTeacher: boolean` (so also `StudentRow`, `StudentDetails`).
  - `CarFlagComponent` (`app-car-flag`), inputs `teacherName: string` (required), `isActive: boolean` (required).
  - Translations `students.flag.car`, `students.flag.tip`, `students.flag.tipInactive` (params `teacher`).

- [ ] **Step 1: Write the failing client spec**

`client\src\app\features\students\ui\components\car-flag\car-flag.component.spec.ts`:

```ts
import { provideZonelessChangeDetection } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslocoTestingModule } from '@jsverse/transloco';
import { CarFlagComponent } from './car-flag.component';

async function render(isActive: boolean): Promise<ComponentFixture<CarFlagComponent>> {
    TestBed.configureTestingModule({
        imports: [
            CarFlagComponent,
            TranslocoTestingModule.forRoot({
                langs: { en: {} },
                translocoConfig: { availableLangs: ['en'], defaultLang: 'en' },
            }),
        ],
        providers: [provideZonelessChangeDetection()],
    });

    const fixture = TestBed.createComponent(CarFlagComponent);
    fixture.componentRef.setInput('teacherName', 'Yael Carmi');
    fixture.componentRef.setInput('isActive', isActive);
    await fixture.whenStable();

    return fixture;
}

function flag(fixture: ComponentFixture<CarFlagComponent>): HTMLElement {
    return (fixture.nativeElement as HTMLElement).querySelector('.car-flag') as HTMLElement;
}

describe('CarFlagComponent', () => {
    it('says in words, not colour alone, that the Car is not the Teacher\'s', async () => {
        //when
        const fixture = await render(true);

        //then
        expect(flag(fixture).textContent).toContain('students.flag.car');
        expect(flag(fixture).querySelector('.pi-exclamation-circle')?.getAttribute('aria-hidden')).toBe('true');
    });

    it('is reachable from the keyboard and explains the fix to screen readers', async () => {
        //when
        const fixture = await render(true);

        //then
        expect(flag(fixture).getAttribute('tabindex')).toBe('0');
        expect(flag(fixture).getAttribute('aria-label')).toBe('students.flag.tip');
    });

    it('tells an Inactive Student\'s Administrator to mark them active first', async () => {
        //when
        const fixture = await render(false);

        //then
        expect(flag(fixture).getAttribute('aria-label')).toBe('students.flag.tipInactive');
        expect(flag(fixture).classList).toContain('car-flag--muted');
    });
});
```

- [ ] **Step 2: Run the spec to verify it fails**

Run from `client\`: `& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js test --watch=false --include src/app/features/students/ui/components/car-flag/car-flag.component.spec.ts`
Expected: FAIL, `Cannot find module './car-flag.component'`.

- [ ] **Step 3: Write the component**

`car-flag.component.ts`:

```ts
import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { TranslocoPipe } from '@jsverse/transloco';
import { TooltipModule } from 'primeng/tooltip';
import { isolateDirection } from '../../../../../shared/text/isolate-direction';

@Component({
    selector: 'app-car-flag',
    imports: [TranslocoPipe, TooltipModule],
    templateUrl: './car-flag.component.html',
    styleUrl: './car-flag.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CarFlagComponent {
    readonly teacherName = input.required<string>();
    readonly isActive = input.required<boolean>();

    protected readonly tipKey = computed(() => (this.isActive() ? 'students.flag.tip' : 'students.flag.tipInactive'));
    protected readonly tipParams = computed(() => ({ teacher: isolateDirection(this.teacherName()) }));
}
```

`car-flag.component.html`:

```html
@let tip = tipKey() | transloco: tipParams();
<span
  class="car-flag"
  [class.car-flag--muted]="!isActive()"
  tabindex="0"
  [attr.aria-label]="tip"
  [pTooltip]="tip"
  tooltipPosition="bottom"
  tooltipEvent="both">
  <i class="pi pi-exclamation-circle" aria-hidden="true"></i>
  {{ 'students.flag.car' | transloco }}
</span>
```

If PrimeNG 21's `Tooltip` has no `tooltipEvent="both"` (check `node_modules\primeng\types\primeng-tooltip.d.ts` for `tooltipEvent`), use its value that shows on hover and on focus; the tooltip must open for keyboard users.

`car-flag.component.scss` (design 2c: plum text and icon, 12.5px semibold, dotted underline as the tooltip affordance):

```scss
.car-flag {
    display: inline-flex;
    align-items: center;
    gap: 0.3125rem;
    padding-block-end: 1px;
    border-block-end: 1px dotted var(--p-red-600);
    font-size: 0.78rem;
    font-weight: 600;
    color: var(--p-red-600);
    white-space: nowrap;
    cursor: help;
}

.car-flag:focus-visible {
    outline: 2px solid var(--p-sky-500);
    outline-offset: 2px;
    border-radius: 2px;
}

.car-flag .pi {
    font-size: 0.875rem;
}

.car-flag--muted {
    opacity: 0.75;
}
```

- [ ] **Step 4: Add the translations**

In `client\public\i18n\he.json`, inside `students`, after `"new"`:

```json
    "flag": {
      "car": "לא רכב של המורה",
      "tip": "הרכב הזה אינו אחד מהרכבים של {{teacher}}. כנראה נקבע לפני שהכלל נכנס לתוקף. אפשר לתקן ב\"החלפת רכב\".",
      "tipInactive": "הרכב הזה אינו אחד מהרכבים של {{teacher}}. כדי לתקן ב\"החלפת רכב\", יש לסמן את התלמיד כפעיל."
    },
```

In `en.json`, at the same place:

```json
    "flag": {
      "car": "Not this Teacher's Car",
      "tip": "This Car isn't one of {{teacher}}'s Cars. It was probably set before the rule existed. Fix it with Change Car.",
      "tipInactive": "This Car isn't one of {{teacher}}'s Cars. To fix it with Change Car, mark the Student as active first."
    },
```

- [ ] **Step 5: Run the spec to verify it passes**

Run: the step 2 command.
Expected: PASS, 3 tests.

- [ ] **Step 6: Project `IsCarOfTeacher` in the backend**

In `ItemForFindStudentsResponse.cs` and `GetStudentResponse.cs`, add after `CarTransmission`:

```csharp
    public bool IsCarOfTeacher { get; init; }
```

In `StudentQueries.cs`, in **both** `FindAsync` and `GetAsync` projections, add after `CarTransmission = car.Transmission,`:

```csharp
                        IsCarOfTeacher = car.TeacherAssignments.Any(assignment => assignment.TeacherId == teacher.Id),
```

Run: `dotnet build` and `dotnet ef migrations has-pending-model-changes --project src\DrivingLessons.Infrastructure --startup-project src\DrivingLessons.Presentation.Web`
Expected: `Build succeeded`; `No changes have been made to the model since the last migration.`

- [ ] **Step 7: Add `isCarOfTeacher` to the client model and fixtures**

In `domain\student.model.ts`, `data\item-for-find-students.response.ts` and `data\get-student.response.ts`, add after `carTransmission: Transmission;`:

```ts
    isCarOfTeacher: boolean;
```

Then every fixture that builds one of these types needs the field. Add `isCarOfTeacher: true,` after `carTransmission: ...,` in each object literal in:
- `state\students.store.spec.ts`: `DANA`, `NOA`, `TAMAR`, `YONI`, `SHAKED` (and `NOA_DETAILS` inherits it from `...NOA`).
- `domain\student-details.spec.ts`, `ui\dialogs\deactivate-student\deactivate-student.dialog.spec.ts`, `ui\dialogs\edit-student\edit-student.dialog.spec.ts`: each `Student` / `StudentDetails` / `GetStudentResponse` literal.

Find them all with: `rg -n "carTransmission:" client/src --glob "*.spec.ts"`; the build in step 10 fails on any one left out.

- [ ] **Step 8: Show the marker in the table**

In `students.page.ts`, import `CarFlagComponent` from `'../../components/car-flag/car-flag.component'` and add it to `imports` after `TransmissionTagComponent`.

In `students.page.html`, the Car appears twice (compact line inside the Teacher cell, and the wide Car cell). Replace the compact block

```html
              <div class="students__car-line students__compact-only">
                <span class="students__car">
                  {{ row.carName }}
                  <app-transmission-tag [transmission]="row.carTransmission" [muted]="!row.isActive" />
                </span>
              </div>
```

with

```html
              <div class="students__car-line students__compact-only">
                <span class="students__car">
                  {{ row.carName }}
                  <app-transmission-tag [transmission]="row.carTransmission" [muted]="!row.isActive" />
                </span>
                @if (!row.isCarOfTeacher) {
                  <app-car-flag [teacherName]="row.teacherName" [isActive]="row.isActive" />
                }
              </div>
```

and the wide cell

```html
            <td class="students__wide-only">
              <span class="students__car">
                {{ row.carName }}
                <app-transmission-tag [transmission]="row.carTransmission" [muted]="!row.isActive" />
              </span>
            </td>
```

with

```html
            <td class="students__wide-only">
              <div class="students__car-cell">
                <span class="students__car">
                  {{ row.carName }}
                  <app-transmission-tag [transmission]="row.carTransmission" [muted]="!row.isActive" />
                </span>
                @if (!row.isCarOfTeacher) {
                  <app-car-flag [teacherName]="row.teacherName" [isActive]="row.isActive" />
                }
              </div>
            </td>
```

In `students.page.scss`, after the `.students__car-line` rule:

```scss
.students__car-cell {
    display: flex;
    flex-direction: column;
    align-items: flex-start;
    gap: 0.3125rem;
}

.students__car-line {
    flex-direction: column;
    align-items: flex-start;
    gap: 0.3125rem;
}
```

(The existing `.students__car-line.students__compact-only { display: flex; }` in the 768px media query already turns the compact line on; the new rule only stacks the marker under the Car.)

- [ ] **Step 9: Flag the who card**

`student-who-card.component.html`: replace

```html
      <app-transmission-tag [transmission]="student().carTransmission" />
    </span>
```

with

```html
      <app-transmission-tag [transmission]="student().carTransmission" />
      @if (!student().isCarOfTeacher) {
        <i
          class="pi pi-exclamation-circle who-card__flag"
          role="img"
          [attr.title]="'students.flag.car' | transloco"
          [attr.aria-label]="'students.flag.car' | transloco"></i>
      }
    </span>
```

`student-who-card.component.scss`, at the end:

```scss
.who-card__flag {
    font-size: 0.875rem;
    color: var(--p-red-600);
}
```

Add to `deactivate-student.dialog.spec.ts` (it renders the who card) a test after the existing first test:

```ts
    it('marks a Car that is not the Teacher\'s in the who card', async () => {
        //given
        const { fixture } = await setUp(true, { ...ITAI, isCarOfTeacher: false });

        //then
        expect(host(fixture).querySelector('.who-card__flag')?.getAttribute('aria-label')).toBe('students.flag.car');
    });
```

If `setUp` in that spec doesn't take a Student as its second argument, add one the same way `edit-student.dialog.spec.ts`'s `setUp(confirmResult = true, details = NOA)` does, defaulting to the existing fixture; use the fixture's actual constant name in place of `ITAI`.

- [ ] **Step 10: Run the client suite and build**

Run from `client\`:

```powershell
& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js test --watch=false
& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js build --project client
```

Expected: every spec PASS; build succeeds with no new warnings.

Run: `dotnet test tests\DrivingLessons.Application.Test\DrivingLessons.Application.Test.csproj`
Expected: PASS.

- [ ] **Step 11: Commit**

```bash
git add src/DrivingLessons.Application/Queries/FindStudents/ItemForFindStudentsResponse.cs src/DrivingLessons.Application/Queries/GetStudent/GetStudentResponse.cs src/DrivingLessons.Infrastructure/EntityFramework/Queries/StudentQueries.cs client/src/app/features/students client/public/i18n/he.json client/public/i18n/en.json
git commit -m "feat(students): flag Students whose Car is not one of their Teacher's Cars (#95)

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```
