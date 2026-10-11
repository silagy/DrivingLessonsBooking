# Task 3 of 7: An Inactive Student on the student form is told their registration isn't active

> Part of [#94: Edit, deactivate and reactivate a Student](README.md). Requires task 2 committed. Work on branch `94-edit-deactivate-reactivate-students`. Read README decisions 8 and 9 first.

**Files:**
- Modify: `src\DrivingLessons.Application\Queries\IStudentQueries.cs`
- Modify: `src\DrivingLessons.Infrastructure\EntityFramework\Queries\StudentQueries.cs`
- Modify: `src\DrivingLessons.Application\Queries\IdentifyStudent\IdentifyStudentInteractor.cs`
- Modify: `client\src\app\features\student-form\domain\identify-status.enum.ts`
- Modify: `client\src\app\features\student-form\state\student-form.store.ts`
- Modify: `client\src\app\features\student-form\ui\components\identify-step\identify-step.component.html`, `.scss`
- Modify: `client\public\i18n\he.json`, `client\public\i18n\en.json`
- Test: `tests\DrivingLessons.Application.Test\Queries\IdentifyStudentInteractorTest.cs`
- Test: `tests\DrivingLessons.Application.Test\Filters\ApiExceptionFilterTest.cs`
- Test: `client\src\app\features\student-form\ui\pages\student-form\student-form.page.spec.ts`

**Interfaces:**
- Consumes: `IStudentQueries.GetActiveByNationalIdAsync(NationalId, DateOnly)`, `SubmissionStudentMustBeActiveException()` (`DrivingLessons.Domain.Exceptions`, 409 `submissionStudentMustBeActive`), `StudentNotFoundException()`, the page spec helpers `provideOpenLinkIdentifying`, `renderPage`, `typeNationalId`, `page`, `continueButton`, `failingWith`, constants `HTTP_CONFLICT`, `HTTP_NOT_FOUND`, `ROSTER_NATIONAL_ID`.
- Produces:
  - `Task<bool> IStudentQueries.IsInactiveAsync(NationalId nationalId)`: true when a Student with that ID exists and is Inactive.
  - The student-form identify endpoint (route and controller unchanged) answers 409 `submissionStudentMustBeActive` for an Inactive Student, 404 `studentNotFound` only for an unknown ID.
  - `IdentifyStatus.inactive = 'inactive'`.
  - Keys `studentForm.identify.inactiveTitle`, `inactiveBody`, `inactiveLocked` in both languages.

**Why:** AC "Student form: an Inactive Student identifying gets a clear translated 'cannot submit - contact the school' message (verify current behaviour, adjust only if missing)". Verified current behaviour: an Inactive Student gets 404 and the "not on the roster, ask to be added" message, which tells them something false (they *are* on file). The design (frame 8b) asks for a distinct, calmer notice. Review Focus 4.

- [ ] **Step 1: Write the failing interactor and filter tests**

In `tests\DrivingLessons.Application.Test\Queries\IdentifyStudentInteractorTest.cs`, add `using DrivingLessons.Domain.Exceptions;` to the usings and add these three tests after `Student_Must_Be_Active_On_The_Roster`:

```csharp
    [TestMethod]
    public async Task Inactive_Student_Is_Told_To_Contact_The_School()
    {
        //given
        var nationalId = NationalId.Of(RosterNationalId);

        A.CallTo(() => studentQueries.GetActiveByNationalIdAsync(nationalId, weekStart))
            .Returns((IdentifyStudentResponse?)null);
        A.CallTo(() => studentQueries.IsInactiveAsync(nationalId)).Returns(true);

        var request = new IdentifyStudentRequest(RosterNationalId);

        //when
        var act = () => interactor.ExecuteAsync(linkToken, request);

        //then
        await Should.ThrowAsync<SubmissionStudentMustBeActiveException>(act);
    }

    [TestMethod]
    public async Task Unknown_National_Id_Is_Still_Not_Found()
    {
        //given
        var nationalId = NationalId.Of(RosterNationalId);

        A.CallTo(() => studentQueries.GetActiveByNationalIdAsync(nationalId, weekStart))
            .Returns((IdentifyStudentResponse?)null);
        A.CallTo(() => studentQueries.IsInactiveAsync(nationalId)).Returns(false);

        var request = new IdentifyStudentRequest(RosterNationalId);

        //when
        var act = () => interactor.ExecuteAsync(linkToken, request);

        //then
        await Should.ThrowAsync<StudentNotFoundException>(act);
    }

    [TestMethod]
    public async Task Active_Student_Is_Found_Without_The_Inactive_Lookup()
    {
        //given
        var nationalId = NationalId.Of(RosterNationalId);
        var student = new IdentifyStudentResponse { StudentName = "Test Student" };

        A.CallTo(() => studentQueries.GetActiveByNationalIdAsync(nationalId, weekStart))
            .Returns(student);

        var request = new IdentifyStudentRequest(RosterNationalId);

        //when
        await interactor.ExecuteAsync(linkToken, request);

        //then
        A.CallTo(() => studentQueries.IsInactiveAsync(A<NationalId>._)).MustNotHaveHappened();
    }
```

(The existing `Student_Must_Be_Active_On_The_Roster` and `Not_Found_Message_Does_Not_Reveal_The_National_Id` keep passing: a FakeItEasy `Task<bool>` returns `false` by default, so an unconfigured ID is unknown.)

In `tests\DrivingLessons.Application.Test\Filters\ApiExceptionFilterTest.cs`, add after `Student_Not_Found_By_Id_Is_A_Not_Found_With_Its_Code` (task 2):

```csharp
    [TestMethod]
    public void Inactive_Student_Submitting_Is_A_Conflict_With_Its_Rule_As_Code()
    {
        //given
        var context = ContextFor(new SubmissionStudentMustBeActiveException());

        //when
        new ApiExceptionFilter().OnException(context);

        //then
        var problem = ProblemOf(context);
        problem.Status.ShouldBe(StatusCodes.Status409Conflict);
        problem.Extensions.ShouldContainKeyAndValue("code", "submissionStudentMustBeActive");
    }
```

- [ ] **Step 2: Run them to see them fail**

Run: `dotnet test tests\DrivingLessons.Application.Test\DrivingLessons.Application.Test.csproj --filter "FullyQualifiedName~IdentifyStudentInteractorTest|FullyQualifiedName~ApiExceptionFilterTest"`
Expected: build error `'IStudentQueries' does not contain a definition for 'IsInactiveAsync'`.

- [ ] **Step 3: Add the query and tell the Inactive Student apart**

Replace `src\DrivingLessons.Application\Queries\IStudentQueries.cs` with:

```csharp
using DrivingLessons.Application.Queries.FindStudents;
using DrivingLessons.Application.Queries.GetStudent;
using DrivingLessons.Application.Queries.IdentifyStudent;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Application.Queries;

public interface IStudentQueries
{
    Task<IReadOnlyCollection<ItemForFindStudentsResponse>> FindAsync(Guid? teacherId);

    Task<GetStudentResponse?> GetAsync(Guid id);

    Task<IdentifyStudentResponse?> GetActiveByNationalIdAsync(NationalId nationalId, DateOnly weekStart);

    Task<bool> IsInactiveAsync(NationalId nationalId);
}
```

In `src\DrivingLessons.Infrastructure\EntityFramework\Queries\StudentQueries.cs`, add after the `GetActiveByNationalIdAsync` method (the file already uses `Microsoft.EntityFrameworkCore`):

```csharp
    public async Task<bool> IsInactiveAsync(NationalId nationalId)
    {
        return await dbContext
                         .Students
                         .AnyAsync(x => x.NationalId == nationalId && !x.IsActive);
    }
```

Replace `src\DrivingLessons.Application\Queries\IdentifyStudent\IdentifyStudentInteractor.cs` with:

```csharp
using DrivingLessons.Application.Common.Exceptions;
using DrivingLessons.Domain.Exceptions;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Application.Queries.IdentifyStudent;

public class IdentifyStudentInteractor
{
    private readonly IPublicationQueries publicationQueries;
    private readonly IStudentQueries studentQueries;

    public IdentifyStudentInteractor(IPublicationQueries publicationQueries, IStudentQueries studentQueries)
    {
        this.publicationQueries = publicationQueries;
        this.studentQueries = studentQueries;
    }

    public async Task<IdentifyStudentResponse> ExecuteAsync(string linkToken, IdentifyStudentRequest request)
    {
        var publication = await publicationQueries.GetByLinkTokenExcludingDraftsAsync(linkToken)
                          ?? throw new PublicationLinkNotFoundException();

        var nationalId = NationalId.Of(request.NationalId);

        var student = await studentQueries.GetActiveByNationalIdAsync(nationalId, publication.WeekStart);

        if (student is not null)
        {
            return student;
        }

        await StudentMustNotBeInactiveAsync(nationalId);

        throw new StudentNotFoundException();
    }

    private async Task StudentMustNotBeInactiveAsync(NationalId nationalId)
    {
        var isInactive = await studentQueries.IsInactiveAsync(nationalId);

        if (isInactive)
        {
            throw new SubmissionStudentMustBeActiveException();
        }
    }
}
```

- [ ] **Step 4: Run the backend suites**

```bash
dotnet build
dotnet test tests\DrivingLessons.Application.Test\DrivingLessons.Application.Test.csproj
```

Expected: PASS, including the three new identify tests and the new filter test.

- [ ] **Step 5: Write the failing page spec**

In `client\src\app\features\student-form\ui\pages\student-form\student-form.page.spec.ts`, add this helper right after `failingWith`:

```ts
function refusedWith(status: number, code: string): () => Observable<never> {
    return () => throwError(() => new HttpErrorResponse({ status, error: { status, title: 'Conflict', code } }));
}
```

and add this test right after `keeps an ID that is not on the roster at the ID step with a contact-your-school message`:

```ts
        it("tells an Inactive Student their registration isn't active and keeps Continue locked", async () => {
            //given
            provideOpenLinkIdentifying(refusedWith(HTTP_CONFLICT, 'submissionStudentMustBeActive'));
            const fixture = await renderPage();

            //when
            await typeNationalId(fixture, ROSTER_NATIONAL_ID);

            //then
            expect(page(fixture).querySelector('.identify__inactive')).not.toBeNull();
            expect(page(fixture).querySelector('.identify__locked')).not.toBeNull();
            expect(page(fixture).querySelector('.identify__not-on-roster')).toBeNull();
            expect(page(fixture).querySelector('.identify__invalid')).toBeNull();
            expect(continueButton(fixture)!.disabled).toBe(true);
            expect(page(fixture).querySelector('app-details-step')).toBeNull();
        });
```

The existing `flags an ID the school system rejects as malformed` (a 409 without that code) must keep showing `.identify__invalid`.

Run from `client\` (PowerShell): `& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js test --watch=false --include src/app/features/student-form/ui/pages/student-form/student-form.page.spec.ts`
Expected: FAIL in the new test (`.identify__inactive` is null: the 409 is shown as an invalid ID).

- [ ] **Step 6: Add the `inactive` status**

Replace `client\src\app\features\student-form\domain\identify-status.enum.ts` with:

```ts
export enum IdentifyStatus {
    idle = 'idle',
    checking = 'checking',
    found = 'found',
    notOnRoster = 'notOnRoster',
    inactive = 'inactive',
    invalidId = 'invalidId',
    failed = 'failed',
}
```

In `client\src\app\features\student-form\state\student-form.store.ts`:

1. Add the import (after the `isolateDirection` import at the end of the import block):

```ts
import { ProblemDetails } from '../../../shared/models/problem-details';
```

2. Add this constant right after the `CAPTION_KEY_BY_STEP` constant:

```ts
const INACTIVE_STUDENT_CODE = 'submissionStudentMustBeActive';
```

3. Widen the result type:

```ts
type IdentifyResult =
    | { status: IdentifyStatus.found; student: IdentifyStudentResponse }
    | { status: IdentifyStatus.notOnRoster | IdentifyStatus.inactive | IdentifyStatus.invalidId };
```

4. In the private `identify` method, replace

```ts
            if (isStatus(error, HTTP_CONFLICT)) {
                return { status: IdentifyStatus.invalidId };
            }
```

with

```ts
            if (isStatus(error, HTTP_CONFLICT)) {
                const status = problemCodeOf(error) === INACTIVE_STUDENT_CODE
                    ? IdentifyStatus.inactive
                    : IdentifyStatus.invalidId;

                return { status };
            }
```

5. Add this function right after the `isStatus` function at the bottom of the file:

```ts
function problemCodeOf(error: unknown): string | undefined {
    if (!(error instanceof HttpErrorResponse)) {
        return undefined;
    }

    const problem = error.error as ProblemDetails | null;

    return problem?.code;
}
```

(The identify step's `isRejected` set stays `notOnRoster` + `invalidId`: in frame 8b the ID field is not red, the ID is right, the registration is what's off. `canSubmit` already keeps Continue disabled for any status other than `found`, `failed` or a fresh `idle` candidate.)

- [ ] **Step 7: Show the notice**

In `client\src\app\features\student-form\ui\components\identify-step\identify-step.component.html`, add this case right after the `@case (statuses.notOnRoster) { ... }` block:

```html
                @case (statuses.inactive) {
                    <p-message severity="secondary" icon="pi pi-pause-circle" class="identify__inactive">
                        <span>
                            <strong class="identify__inactive-title">{{ 'studentForm.identify.inactiveTitle' | transloco }}</strong>
                            {{ 'studentForm.identify.inactiveBody' | transloco }}
                        </span>
                    </p-message>
                }
```

and replace the footer

```html
        <div wizardFooter>
            <p-button type="submit" fluid [label]="continueKey() | transloco" [disabled]="!canSubmit()" />
        </div>
```

with

```html
        <div wizardFooter>
            <p-button type="submit" fluid [label]="continueKey() | transloco" [disabled]="!canSubmit()" />
            @if (status() === statuses.inactive) {
                <p class="identify__locked">{{ 'studentForm.identify.inactiveLocked' | transloco }}</p>
            }
        </div>
```

Append to `client\src\app\features\student-form\ui\components\identify-step\identify-step.component.scss`:

```scss
.identify__inactive {
    --p-message-secondary-background: var(--app-bg-muted);
    --p-message-secondary-border-color: var(--app-steel-light);
    --p-message-secondary-color: var(--app-text-secondary);
}

.identify__inactive-title {
    display: block;
    color: var(--app-whale);
}

.identify__locked {
    margin: 0.5rem 0 0;
    font-size: 0.7rem;
    color: var(--app-text-muted);
    text-align: center;
}
```

(Frame 8b: border `#C3CFE5` = `--app-steel-light`, background `#F5F6FF` = `--app-bg-muted`, title `#005389` = `--app-whale`, caption 11px muted. If the Aura message tokens use other names in this PrimeNG version, task 7 step 7 checks the computed colours; fix the variable names then.)

- [ ] **Step 8: Add the translations**

In `client\public\i18n\he.json`, inside `studentForm.identify`, replace

```json
      "notOnRosterBody": "תעודת הזהות הזו לא מופיעה ברשימת התלמידים של בית הספר, ולכן אין מה להגיש. פנו לבית הספר כדי שיוסיפו אתכם.",
```

with

```json
      "notOnRosterBody": "תעודת הזהות הזו לא מופיעה ברשימת התלמידים של בית הספר, ולכן אין מה להגיש. פנו לבית הספר כדי שיוסיפו אתכם.",
      "inactiveTitle": "ההרשמה שלכם לא פעילה.",
      "inactiveBody": "לכן אי אפשר להגיש בקשות השבוע. כדי לחזור לשיעורים, פנו לבית הספר.",
      "inactiveLocked": "אפשר להמשיך רק עם הרשמה פעילה",
```

In `client\public\i18n\en.json`, inside `studentForm.identify`, replace

```json
      "notOnRosterBody": "This ID isn't on the school's roster, so there's nothing to submit. Please contact your school to be added.",
```

with

```json
      "notOnRosterBody": "This ID isn't on the school's roster, so there's nothing to submit. Please contact your school to be added.",
      "inactiveTitle": "Your registration isn't active.",
      "inactiveBody": "So you can't submit requests this week. To get back to lessons, contact the school.",
      "inactiveLocked": "You can continue only with an active registration",
```

(Hebrew in the form's plural voice, README decision 9; the titles end with a period like `notOnRosterTitle`, since the body follows inline in the same Message.)

- [ ] **Step 9: Run the client suites**

From `client\` (PowerShell):

```powershell
& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js test --watch=false
& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js build --project client
```

Expected: every spec PASS (the new page test, `translations.spec.ts` with matching keys in both files, `source-text.spec.ts`); the build succeeds.

- [ ] **Step 10: Commit**

```bash
git add src/DrivingLessons.Application/Queries/IStudentQueries.cs src/DrivingLessons.Infrastructure/EntityFramework/Queries/StudentQueries.cs src/DrivingLessons.Application/Queries/IdentifyStudent/IdentifyStudentInteractor.cs tests/DrivingLessons.Application.Test/Queries/IdentifyStudentInteractorTest.cs tests/DrivingLessons.Application.Test/Filters/ApiExceptionFilterTest.cs client/src/app/features/student-form client/public/i18n/he.json client/public/i18n/en.json
git commit -m "feat(student-form): tell an Inactive Student their registration isn't active instead of 'not on file' (#94)

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```
