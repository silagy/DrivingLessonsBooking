# #94: Edit, Deactivate and Reactivate a Student - Task Index

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

Per-task breakdown of issue [#94](https://github.com/silagy/DrivingLessonsBooking/issues/94), a slice of spec [#82](https://github.com/silagy/DrivingLessonsBooking/issues/82). Each task has its own file and is self-contained. Execute the tasks **in order**, one commit each. Every commit references #94.

**Goal:** From the Students screen, Administrators edit a Student's national ID, name, phone, address, start date and license type, deactivate a Student who finished or left and reactivate a returning one; an Inactive Student who identifies on the student form is told, calmly and distinctly from "not on file", that their registration isn't active and they should contact the school.

**Architecture:**
- Backend: `Student.ChangeDetails(...)` emits the new `StudentDetailsChanged` event. A `ChangeStudentDetailsInteractor` (`PUT api/students/{id}/details`) builds the value objects, loads the Student through the new `IStudentRepository.GetAsync(StudentId)` (404 `studentNotFound`), refuses a national ID that **another** Student already uses with the existing `StudentNationalIdAlreadyInUseException` (409, the other Student's name in `params.name`), and commits. `DeactivateStudentInteractor` / `ReactivateStudentInteractor` (`POST api/students/{id}/deactivate|reactivate`) call the existing `Student.Deactivate()` / `Reactivate()`, whose existing guards answer 409 `studentAlreadyDeactivated` / `studentAlreadyActive`. `IdentifyStudentInteractor` tells an Inactive Student apart from an unknown ID through a new `IStudentQueries.IsInactiveAsync(NationalId)` and answers 409 `submissionStudentMustBeActive` (existing exception) instead of 404.
- Client: the `students` feature gains `getStudent`, `changeStudentDetails`, `deactivateStudent`, `reactivateStudent` in `StudentsApiService`; the page-scoped `StudentsStore` gains `loadDetails`, `changeDetails`, `deactivate`, `reactivate`. The Students table gets a row-actions column (kebab + PrimeNG popup `Menu`): active rows offer **Edit details** and **Deactivate**, Inactive rows **Edit details** and **Reactivate**. Edit details and Deactivate are DynamicDialogs fed with store signals (the #93 / Users pattern); Reactivate is one click. The student form's identify step gets an `inactive` status with its own notice.

**Tech Stack:**
- Backend: .NET 10, ASP.NET Core Web API, EF Core 10 + Npgsql; tests with MSTest 4 + Shouldly + FakeItEasy.
- Client: Angular 21 (standalone, zoneless, signals) + PrimeNG 21 + Transloco; Vitest via `ng test`.
- No new package and no migration.

**Spec:** issue [#94](https://github.com/silagy/DrivingLessonsBooking/issues/94) (acceptance criteria) · predecessor [#93](https://github.com/silagy/DrivingLessonsBooking/issues/93) ([plan](../us-93-students-screen-plan/README.md)) · follow-up [#95](https://github.com/silagy/DrivingLessonsBooking/issues/95) (Change Teacher, Change Car, Car-not-of-Teacher flag) · parent spec [#82](https://github.com/silagy/DrivingLessonsBooking/issues/82) · design brief [claude-design-prompt.md](../claude-design-prompt.md) · [docs/requirements.md](../../../requirements.md) §5.5 / §5.5.1 (the national ID decision is already there) · [CONTEXT.md](../../../../CONTEXT.md) glossary ("Inactive Student") · `.claude\rules\*.md`

**Design:** Claude Design project `6a0ab892-caa4-49f7-baff-bba7ca38c862`, file `Students.html`, built from `students/app.jsx` (frame list), `students/data.jsx` (copy deck `ST_GROUPS`), `students/kit.jsx` (`StTable`, `StKebab`, `StRowMenu`, `StWho`, `StTx`), `students/dialogs.jsx` (`StEditDlg`, `StDeactDlg`), `students/more.jsx` (`StForm`, `StInventory` decisions), on top of `auth/kit.jsx` (`AuShell`, `AuTag`, `AuMsg`, `AuDialog`, `AuToast`). Frames built by this story:

| Frame | What it shows |
|-------|---------------|
| 2e | Row actions, active Student: kebab (32px round, `pi-ellipsis-v`) at the inline end of the row opens a 240px menu: "עריכת פרטים" (pencil) · separator · "סימון כלא פעיל" (pause icon, danger red). Change Teacher / Change Car are #95. |
| 2f | Row actions, Inactive Student (filter All): "עריכת פרטים" · "סימון כפעיל" (replay icon). |
| 4a | Edit details dialog, 560px: header "עריכת פרטים", the Student's name under it; fields national ID (LTR, numeric, info hint "השינוי כאן לא משנה את קובץ רשימת התלמידים. קובץ מאוחר יותר עם תעודת הזהות הישנה יוסיף תלמיד שני, ולכן צריך לתקן גם את הקובץ."), full name, phone (LTR), address, start date (DatePicker dd/mm/yy) and license type side by side, then a locked "מורה ורכב" field: lock icon · Teacher · Car + transmission tag. Footer text Cancel + primary Save. |
| 4b | Refused, national ID used by another Student: error Message first in the dialog, "תעודת הזהות הזו שייכת לתלמיד אחר: {name}.", the field invalid with the same line under it. |
| 4c | Refused, national ID not valid: same layout, "תעודת הזהות לא תקינה. יש לבדוק שיש 9 ספרות ושספרת הביקורת נכונה." |
| 5a | Deactivate confirmation, 520px: header "לסמן את {name} כלא פעיל/ה?"; who card (avatar, name, national ID LTR; Teacher; Car + transmission tag); three lines with icons: pause "עד לסימון מחדש כפעיל/ה, לא תהיה אפשרות להגיש בטופס התלמידים.", check "ההגשות הקודמות נשמרות.", replay "קובץ רשימת תלמידים שעדיין כולל אותם יסמן אותם שוב כפעילים."; footer text Cancel + danger outlined "סימון כלא פעיל". |
| 5b | Deactivated toast: "התלמיד סומן כלא פעיל" / "{name} לא יוכל/תוכל להגיש עד לסימון מחדש כפעיל/ה."; the row leaves the default Active view. |
| 5c | Reactivated toast: "התלמיד סומן כפעיל" / "{name} יכול/ה להגיש שוב בטופס התלמידים."; the status Tag turns "פעיל". |
| 5d / 5e | Stale toggle: error toast "התלמיד הזה כבר לא פעיל. הרשימה רועננה." / "התלמיד הזה כבר פעיל. הרשימה רועננה." and the list reloads. |
| 8b | Student form (375px): national ID field, then a calm notice (border `--app-steel-light`, background `--app-bg-muted`, title in `--app-whale`, pause icon) "ההרשמה שלכם לא פעילה" + body; Continue stays disabled with the caption "אפשר להמשיך רק עם הרשמה פעילה" under it. 8a ("not on file") is the existing state and stays as is. |

Frames 2c, 2d (Car-not-of-Teacher marker, "Fix" badge), 6 and 7 (Change Teacher, Change Car) belong to #95. Every frame's layout, sizes, colors and copy are spelled out in the task that builds it, so the tasks can be built without the `claude_design` MCP. Task 7 compares against the frames when the MCP is connected.

**Branch:** `94-edit-deactivate-reactivate-students`, from `82-users-and-roles` at `b9c2eed` (contains #93). This plan is committed on the branch before task 1. The PR targets `82-users-and-roles` (memory: every #82 story PRs into the epic branch).

## Current State

| Piece | Today | File |
|-------|-------|------|
| Student aggregate | `Create`, `UpdateFromRoster`, `Deactivate` (guard `StudentAlreadyDeactivatedException`, event `StudentDeactivated`), `Reactivate` (guard `StudentAlreadyActiveException`, event `StudentReactivated`). No way to edit details by hand. | `src\DrivingLessons.Domain\Entities\Student.cs` |
| Student repository | `FindAllAsync`, `GetActiveByNationalIdAsync`, `GetByNationalIdAsync` (any state, #93), `Add`. **No `GetAsync(StudentId)`.** | `src\DrivingLessons.Domain\Repositories\IStudentRepository.cs`, `Infrastructure\EntityFramework\Repositories\StudentRepository.cs` |
| Student endpoints | `GET api/students/find`, `GET api/students/{id}` (`GetStudentResponse` with address, start date, license type, Car transmission), `POST api/students`. All Administrator-only by fallback; `Students_Stay_Administrator_Only` pins them. | `Presentation.Web\Controllers\Student\`, `tests\...\Auth\ControllerAuthorizationTest.cs` |
| Duplicate national ID | `StudentNationalIdAlreadyInUseException(StudentName)`, 409 `studentNationalIdAlreadyInUse`, `params.name` | `Domain\Exceptions\`, `Presentation.Web\Filters\ApiExceptionFilter.cs` |
| Not found | `StudentNotFoundException(StudentId)` → 404 `studentNotFound` | `Application\Common\Exceptions\StudentNotFoundException.cs` |
| Filter tests | No test for `studentAlreadyDeactivated`, `studentAlreadyActive`, `submissionStudentMustBeActive` | `tests\...\Filters\ApiExceptionFilterTest.cs` |
| Identify on the student form | Active lookup only; an Inactive Student gets 404 `studentNotFound`, the client shows "not on the roster" (`IdentifyStatus.notOnRoster`); any 409 is shown as an invalid ID | `Application\Queries\IdentifyStudent\IdentifyStudentInteractor.cs`, `client\...\student-form\state\student-form.store.ts` (`identify`), `ui\components\identify-step\` |
| Roster import | Reactivates an Inactive Student found in the file (`ImportRosterInteractor`, line ~188), so the Deactivate dialog's third line is true | `Application\Commands\ImportRoster\ImportRosterInteractor.cs` |
| Client Students | List, filters, search, Add Student. No row actions. Refusal type `AddStudentRefusal` / `AddStudentRefusalKind` / `refusalKindOf` in `domain\add-student-refusal.ts`; date and optional-text helpers private inside `domain\new-student.ts` | `client\src\app\features\students\` |
| Users pattern to mirror | Row kebab + popup `p-menu` (`users.page.*`), `DeleteUserDialog` (who card + effects list + danger button), `EditUserDialog` (`LockedFieldComponent`), `UsersStore.runDialogCommand` | `client\src\app\features\users\`, `shared\components\locked-field\` |
| Translations | `errors.studentAlreadyActive` / `studentAlreadyDeactivated` exist with older wording; `errors.submissionStudentMustBeActive` exists | `client\public\i18n\he.json`, `en.json` |

## Decisions (made while planning, challenge on review)

| # | Decision |
|---|----------|
| 1 | **Edit is `PUT api/students/{id}/details`** with `ChangeStudentDetailsRequest(string NationalId, string Name, string Phone, string? Address, DateOnly? StartDate, string? LicenseType)` → 204, like `PUT api/users/{id}/details`. Teacher and Car are not part of it (#95). |
| 2 | **`Student.ChangeDetails(NationalId, StudentName, PhoneNumber, Address?, LessonsStartDate?, LicenseType?)` has no state guard**: an Inactive Student can be edited (design 2f offers Edit details on Inactive rows), and saving unchanged details is not refused, like `User.ChangeDetails`. It replaces the details, it is not a state transition, so "not idempotent" doesn't apply. It emits the new `StudentDetailsChanged(StudentId, NationalId, StudentName)` (critical rule 9: a new event, `StudentUpdatedFromRoster` is untouched). |
| 3 | **Interactor order**: value objects (409 for an invalid national ID, empty name, bad phone, empty address / license type) → load the Student (404) → national ID check → `ChangeDetails` → one commit. An invalid ID never costs a lookup. |
| 4 | **The national ID check skips the Student's own ID** (compared as the `NationalId` value object, so " 18 " equals `000000018`) and otherwise refuses an ID used by **any other** Student, active or Inactive, with the existing `StudentNationalIdAlreadyInUseException(otherStudent.Name)`. No new exception: same rule, same code, same `params.name`; the client words it "belongs to another Student" in the Edit dialog (decision 12). Requirements §5.5.1 already records "a national ID corrected by hand must also be corrected in the Roster" (no guard). |
| 5 | **Deactivate / Reactivate are `POST api/students/{id}/deactivate` and `POST api/students/{id}/reactivate`** → 204, like `POST api/users/{id}/restore`. The interactors only load (404 `studentNotFound`), call the existing domain method and commit; the existing guards give 409 `studentAlreadyDeactivated` / `studentAlreadyActive`. The AC's "ApiExceptionFilterTest extended for each new exception" is met by filter tests for these two codes and `submissionStudentMustBeActive`, none of which had one; this story adds no new exception type. |
| 6 | **`IStudentRepository.GetAsync(StudentId)`** is added (critical rule 6: repositories return null, interactors throw). |
| 7 | **Authorization**: the three new actions sit on `StudentCommandController` (no attribute, Administrator fallback); `Students_Stay_Administrator_Only` gains three `[DataRow]`s. |
| 8 | **Student form, Inactive Student**: today an Inactive Student gets the same "not on the roster, ask to be added" message as an unknown ID, which is wrong (they *are* on file). `IdentifyStudentInteractor`, after the active lookup misses, asks the new `IStudentQueries.IsInactiveAsync(NationalId)` and throws the existing `SubmissionStudentMustBeActiveException` (409 `submissionStudentMustBeActive`): the Student exists but can't submit. Unknown IDs keep 404 `studentNotFound`. The client maps 409 + that code to the new `IdentifyStatus.inactive`; any other 409 stays `invalidId`. The notice never shows the Student's name (the link is open to anyone; design decision "Student form"). This reveals that an ID belongs to an Inactive Student, no more than the active path already reveals (it returns the name). |
| 9 | **Student-form copy keeps the form's own voice**: the existing identify strings address the Student in the plural ("אתכם", "פנו"), the copy deck uses the singular ("שלך", "יש לפנות"). The Hebrew is adapted to the plural: "ההרשמה שלכם לא פעילה" / "לכן אי אפשר להגיש בקשות השבוע. כדי לחזור לשיעורים, פנו לבית הספר." / "אפשר להמשיך רק עם הרשמה פעילה". English is the copy deck's, with the button-style capitals dropped. |
| 10 | **Row actions** follow design 2e / 2f: active → Edit details, separator, Deactivate (danger); Inactive → Edit details, Reactivate. Users' inline "Restore" button is not copied: the design puts Reactivate in the menu. The kebab is disabled while a command runs. The actions column header is screen-reader only ("פעולות"), 60px; the "no match" row spans 7 columns. |
| 11 | **Edit opens on fresh details**: the list lacks address, start date and license type, so the page awaits `store.loadDetails(id)` (`GET api/students/{id}`) and opens the dialog with them; a failure (deleted meanwhile → 404) is an error toast and no dialog. |
| 12 | **Refusals are one shared type**: `domain\add-student-refusal.ts` is renamed `domain\student-refusal.ts` (`StudentRefusal`, `StudentRefusalKind`, `refusalKindOf`) and gains `existingStudentName: string \| null` (from `params.name`, direction-isolated). Add Student keeps showing `message` (`errors.studentNationalIdAlreadyInUse`); Edit details shows `students.edit.nationalIdUsed` with `existingStudentName` for `nationalIdInUse`, the shared `students.add.nationalIdInvalid` for `nationalIdInvalid`, `message` otherwise. The refused field gets a `refused` error so Save stays disabled until it changes; typing in the national ID clears the refusal (as in Add). |
| 13 | **Stale toggles close and refresh**: Deactivate (dialog) and Reactivate (one click) treat `studentAlreadyDeactivated`, `studentAlreadyActive` and `studentNotFound` as "someone else got there first": error toast with the translated rule, list reloaded, the Deactivate dialog closes. Any other Deactivate failure stays in the dialog as a refusal; any other Reactivate failure is an error toast. The `errors.studentAlreadyDeactivated` / `studentAlreadyActive` texts become the copy deck's `err.alreadyInactive` / `err.alreadyActive` ("... הרשימה רועננה."), true because only this screen surfaces them and it always reloads. |
| 14 | **Success**: Edit → toast "הפרטים נשמרו" (no detail, like Users), list reloaded, dialog closed. Deactivate → toast `students.deactivated` + `students.deactivatedDetail` with the name. Reactivate → `students.reactivated` + `students.reactivatedDetail`. Names are direction-isolated. |
| 15 | **Hebrew action wording** "סימון כלא פעיל" / "סימון כפעיל" per the design's decision (mirrors the status Tag "לא פעיל", never suggests deletion). English "Deactivate" / "Reactivate". The design flags it "Spec owner to confirm"; the PR body asks. |
| 16 | **Edit details omits the copy deck's `edit.tcHint`** ("use Change Teacher or Change Car in the row menu"): those actions arrive with #95, which adds the hint. The locked "מורה ורכב" field stays. |
| 17 | **Shared form helpers move to `domain\form-values.ts`**: `optionalText`, `toIsoDate` (moved from `new-student.ts`) and the new `fromIsoDate` ("yyyy-MM-dd" → local-midnight `Date` built from its parts, never `new Date(text)`, which parses as UTC midnight and is the previous calendar day for any browser west of UTC). Add and Edit share them, so a saved date round-trips exactly in every time zone. |
| 18 | **Who card**: a new `app-student-who-card` in `students\ui\components` (avatar, name, national ID LTR; Teacher; Car + transmission tag), styled like `app-user-who-card`. Features never import each other, so the Users card isn't reused. |
| 19 | **Dialog sizes**: Edit `35rem` (560px, like Add); Deactivate `32.5rem` (520px). |
| 20 | **Smoke and browser checks run on throwaway databases** (`drivinglessons_us94_smoke`, `drivinglessons_us94_verify`) on the compose Postgres, like #93. |

## Global Constraints

- `Domain` depends on nothing; `Application` depends only on `Domain`.
- No comments in code. The only exception is the test section markers `//given //when //then //expected`.
- No long dashes or ellipsis characters in source, specs or translation files (`SourceTextTest`, `source-text.spec.ts`, `translations.spec.ts` enforce this). Use a plain `-` and three dots.
- C#: always `var`, braces on every block, multiline ternaries, no nested method calls outside tests, every parameter used, `is null` outside expression trees.
- Domain methods take resolved entities, typed IDs and value objects; raw `Guid` exists only at controller/response boundaries and in query interfaces.
- Operations are not idempotent; every refusal is a domain-specific exception mapped by `ApiExceptionFilter`.
- Prefer new domain events over modifying existing ones.
- Controllers: `[ApiController]`, `[Route]`, `[Tags]`, interactors via `[FromServices]`, `EndpointSummary` and `ProducesResponseType` on every action, zero logic.
- Client rules:
  - Standalone components, `inject()`, OnPush.
  - Signals-only stores exposing readonly signals; components never subscribe. RxJS only in `data\` services, consumed with `firstValueFrom` / `resource()`.
  - No hardcoded user-visible strings; every new key in **both** `client\public\i18n\he.json` and `en.json`, Hebrew first. Logical CSS properties only. National IDs and phones are LTR-isolated; names in translated sentences go through `isolateDirection`.
  - `core\` and `shared\` never import from `features\`; no feature imports another.
  - Business rules stay in the backend: the client shows the server's refusals and never decides who may be deactivated.
- Terminology: Student, Inactive Student, Teacher, Car, Roster, national ID, Submission. Never "delete", "archive", "assign", "transfer" or "move" a Student in user-visible copy.

**Commands** (from the repository root unless a step says otherwise):

```bash
dotnet build
dotnet test tests\DrivingLessons.Domain.Test\DrivingLessons.Domain.Test.csproj
dotnet test tests\DrivingLessons.Application.Test\DrivingLessons.Application.Test.csproj
dotnet ef migrations has-pending-model-changes --project src\DrivingLessons.Infrastructure --startup-project src\DrivingLessons.Presentation.Web
```

To run one test class, append `--filter "FullyQualifiedName~ChangeStudentDetailsInteractorTest"` (any class name).

Client commands run from `client\` in PowerShell. The default `npm` can't install on this machine, but the local Angular CLI works:

```powershell
& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js test --watch=false
& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js build --project client
```

To run one spec file, append `--include src/app/features/students/state/students.store.spec.ts` (any spec path) to the test command.

If `client\node_modules` is missing, install it from `client\` with `& "C:\Users\AdiSilagy\AppData\Roaming\nvm\v22.6.0\node.exe" "C:\Users\AdiSilagy\AppData\Roaming\nvm\v22.6.0\node_modules\npm\bin\npm-cli.js" ci`.

Before anything touches the database, switch to the compose Postgres instead of `dl-postgres`: run `docker stop dl-postgres`, then `docker compose up -d postgres`.

## Review Focus

1. **Saving a Student's details without touching the national ID is never refused as "in use"** (the lookup would find the Student itself), also when the ID was typed without its leading zeros. Covered in task 1 (`Keeps_Its_Own_National_Id_Without_A_Lookup`, `Keeps_Its_Own_National_Id_Written_Without_Leading_Zeros`) and task 7 step 5 (save Noa unchanged).
2. **Clearing an optional detail (address, start date, license type) clears it, and a spaces-only address is sent as absent, not refused as `addressMustNotBeEmpty`.** Covered in task 1 (`Clears_The_Optional_Details_Left_Blank`), task 4 (`sends cleared optional details as absent`) and task 7 step 5.
3. **The start date shown in the Edit dialog is the saved calendar day, and saving it again doesn't move it a day** (`new Date('2026-09-01')` is UTC midnight, the previous day west of UTC). Covered in task 4 (`fromIsoDate` spec: `reads the calendar day as local midnight`; `round-trips the saved start date`) and task 7 step 5.
4. **An Inactive Student identifying on the student form gets the "registration isn't active" notice, never "not on file", and Continue stays locked; an unknown ID still gets "not on file".** Covered in task 3 (`Inactive_Student_Is_Told_To_Contact_The_School`, `Unknown_National_Id_Is_Still_Not_Found`, page spec `tells an Inactive Student their registration isn't active`) and task 7 step 7.
5. **Two Administrators toggling the same Student: the second one sees a clear "already inactive / active - list refreshed" toast and the current state, never a stuck dialog or a stale row.** Covered in task 4 (`treats an already Inactive Student as stale: toast, reload, close`, `reloads when Reactivate finds the Student already active`) and task 7 step 6 (toggle from a second tab).

## File Structure

| File | Change | Task |
|------|--------|------|
| `src\DrivingLessons.Domain\Entities\Student.cs` | `ChangeDetails` | 1 |
| `src\DrivingLessons.Domain\Events\StudentDetailsChanged.cs` | **New** | 1 |
| `tests\DrivingLessons.Domain.Test\Entities\StudentTest.cs` | `Change_Details*` tests | 1 |
| `src\DrivingLessons.Domain\Repositories\IStudentRepository.cs`, `src\DrivingLessons.Infrastructure\EntityFramework\Repositories\StudentRepository.cs` | `GetAsync(StudentId)` | 1 |
| `src\DrivingLessons.Application\Commands\ChangeStudentDetails\ChangeStudentDetailsInteractor.cs`, `ChangeStudentDetailsRequest.cs` | **New** | 1 |
| `src\DrivingLessons.Application\Commands\DeactivateStudent\DeactivateStudentInteractor.cs`, `Commands\ReactivateStudent\ReactivateStudentInteractor.cs` | **New** | 2 |
| `src\DrivingLessons.Application\DependencyInjection.cs` | Register the three interactors | 1, 2 |
| `src\DrivingLessons.Presentation.Web\Controllers\Student\StudentCommandController.cs` | `ChangeDetailsAsync` (1), `DeactivateAsync`, `ReactivateAsync` (2) | 1, 2 |
| `tests\DrivingLessons.Application.Test\Commands\ChangeStudentDetailsInteractorTest.cs` | **New** | 1 |
| `tests\DrivingLessons.Application.Test\Commands\DeactivateStudentInteractorTest.cs`, `ReactivateStudentInteractorTest.cs` | **New** | 2 |
| `tests\DrivingLessons.Application.Test\Auth\ControllerAuthorizationTest.cs` | Three rows | 1, 2 |
| `tests\DrivingLessons.Application.Test\Filters\ApiExceptionFilterTest.cs` | Codes `studentAlreadyDeactivated`, `studentAlreadyActive` (2), `submissionStudentMustBeActive` (3) | 2, 3 |
| `src\DrivingLessons.Application\Queries\IStudentQueries.cs`, `src\DrivingLessons.Infrastructure\EntityFramework\Queries\StudentQueries.cs` | `IsInactiveAsync(NationalId)` | 3 |
| `src\DrivingLessons.Application\Queries\IdentifyStudent\IdentifyStudentInteractor.cs`, `tests\...\Queries\IdentifyStudentInteractorTest.cs` | Inactive → 409 | 3 |
| `client\src\app\features\student-form\domain\identify-status.enum.ts`, `state\student-form.store.ts`, `ui\components\identify-step\*`, `ui\pages\student-form\student-form.page.spec.ts` | `inactive` status and notice | 3 |
| `client\src\app\features\students\domain\student-refusal.ts` (renamed from `add-student-refusal.ts`) | `StudentRefusal*`, `existingStudentName` | 4 |
| `client\src\app\features\students\domain\form-values.ts`, `.spec.ts` | **New**: `optionalText`, `toIsoDate`, `fromIsoDate` | 4 |
| `client\src\app\features\students\domain\new-student.ts` | Uses `form-values.ts` | 4 |
| `client\src\app\features\students\domain\student-details.model.ts`, `student-details-change.model.ts`, `student-details.ts`, `student-details.spec.ts` | **New** | 4 |
| `client\src\app\features\students\data\get-student.response.ts`, `change-student-details.request.ts`, `students-api.service.ts` | **New** DTOs; four API methods | 4 |
| `client\src\app\features\students\state\students.store.ts`, `.spec.ts` | `loadDetails`, `changeDetails`, `deactivate`, `reactivate`; refusal rename | 4 |
| `client\src\app\features\students\ui\dialogs\add-student\*` | Refusal rename only | 4 |
| `client\src\app\features\students\ui\components\student-who-card\*` | **New** | 5 |
| `client\src\app\features\students\ui\dialogs\deactivate-student\*` | **New** | 5 |
| `client\src\app\features\students\ui\pages\students\students.page.*` | Actions column + menu, Deactivate, Reactivate (5); Edit (6) | 5, 6 |
| `client\src\app\features\students\ui\dialogs\edit-student\*` | **New**, with a spec | 6 |
| `client\public\i18n\he.json`, `en.json` | `errors.studentAlready*` (2); `studentForm.identify.inactive*` (3); `students.actions*`, `students.deactivate*`, `students.reactivate*` (5); `students.edit*`, `students.detailsSaved` (6) | 2, 3, 5, 6 |

## Execution Order

| # | File | Task | Commit point |
|---|------|------|--------------|
| 1 | [task-01-change-student-details.md](task-01-change-student-details.md) | `Student.ChangeDetails` + event, `GetAsync(StudentId)`, `PUT api/students/{id}/details`, national ID used by another Student refused | ✅ own commit |
| 2 | [task-02-deactivate-reactivate-endpoints.md](task-02-deactivate-reactivate-endpoints.md) | `POST api/students/{id}/deactivate|reactivate`, filter tests, `errors.studentAlready*` wording, API smoke of tasks 1 and 2 | ✅ own commit |
| 3 | [task-03-student-form-inactive-notice.md](task-03-student-form-inactive-notice.md) | Identify tells an Inactive Student apart (409 `submissionStudentMustBeActive`); student-form `inactive` notice | ✅ own commit |
| 4 | [task-04-client-students-store.md](task-04-client-students-store.md) | Refusal rename, `form-values`, details mapping, four API methods, store commands with stale handling | ✅ own commit |
| 5 | [task-05-client-row-actions-deactivate-reactivate.md](task-05-client-row-actions-deactivate-reactivate.md) | Row-actions menu, who card, Deactivate dialog, one-click Reactivate | ✅ own commit |
| 6 | [task-06-client-edit-details-dialog.md](task-06-client-edit-details-dialog.md) | Edit details dialog with inline refusals, spec, wiring | ✅ own commit |
| 7 | [task-07-verify-and-pr.md](task-07-verify-and-pr.md) | Full suites, browser verification (Hebrew RTL, English, 768px, student form 375px), design comparison, PR | ✅ PR |

The PR targets `82-users-and-roles`, says `Closes #94` (it won't auto-close on a non-default base; close it when the epic merges), and references #95 and the parent spec #82.
