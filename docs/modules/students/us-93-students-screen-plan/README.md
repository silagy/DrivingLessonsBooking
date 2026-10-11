# #93: Students Screen - List, Filter and Add a Student by Hand - Task Index

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

Per-task breakdown of issue [#93](https://github.com/silagy/DrivingLessonsBooking/issues/93), slice (5) "Students screen" of spec [#82](https://github.com/silagy/DrivingLessonsBooking/issues/82). Each task has its own file and is self-contained. Execute the tasks **in order**, one commit each. Every commit references #93.

**Goal:** Administrators get a Students screen listing every Student (national ID, name, phone, Teacher, Car, active state), filterable by Teacher and active state and searchable by name or national ID, and can add a Student by hand, choosing the Car only from the chosen Teacher's Cars, so they can enrol someone without a Roster file. The "תלמידים" navigation item opens this screen; the Roster upload moves behind its "ייבוא רשימת תלמידים" action.

**Architecture:**
- Backend: a Get-Student query (`GET api/students/{id}`) and a Create Student command (`POST api/students`). `CreateStudentInteractor` builds the value objects, refuses a national ID already used by any Student through a new `IStudentRepository.GetByNationalIdAsync` (new 409 `StudentNationalIdAlreadyInUseException`, the existing Student's name in the ProblemDetails `params`), resolves the Teacher and Car (404s), and lets `Student.Create` enforce the #92 invariant "the Car is one of the Teacher's Cars". Find-students gains the Car's transmission.
- Client: a new lazy `students` feature (domain · data · state · ui). A page-scoped signals store loads Students, Teacher options and Car options; filtering, search, ordering and the "new" highlight are `computed()` over the loaded list through pure `domain` functions. The Add Student dialog is a dumb DynamicDialog fed with store signals; the Teacher → Car pick is a pure `domain` function. The admin shell routes `/students` to the feature and `/students/import` to the existing Roster feature.

**Tech Stack:**
- Backend: .NET 10, ASP.NET Core Web API, EF Core 10 + Npgsql; tests with MSTest 4 + Shouldly + FakeItEasy.
- Client: Angular 21 (standalone, zoneless, signals) + PrimeNG 21 + Transloco; Vitest via `ng test`.
- No new package and no migration (the `students.national_id` unique index already exists).

**Spec:** issue [#93](https://github.com/silagy/DrivingLessonsBooking/issues/93) (acceptance criteria) · follow-ups [#94](https://github.com/silagy/DrivingLessonsBooking/issues/94) (edit, deactivate, reactivate) and [#95](https://github.com/silagy/DrivingLessonsBooking/issues/95) (Change Teacher, Change Car, Car-not-of-Teacher flag) · predecessor [#92](https://github.com/silagy/DrivingLessonsBooking/issues/92) ([plan](../../roster/us-92-roster-car-of-teacher-plan/README.md)) · parent spec [#82](https://github.com/silagy/DrivingLessonsBooking/issues/82) · design brief [claude-design-prompt.md](../claude-design-prompt.md) · [CONTEXT.md](../../../../CONTEXT.md) glossary · `.claude\rules\*.md`

**Design:** Claude Design project `6a0ab892-caa4-49f7-baff-bba7ca38c862`, files `students/app.jsx` (frame list), `students/data.jsx` (copy deck, `ST_GROUPS`), `students/kit.jsx` (table, filters, fields, `StTx`), `students/dialogs.jsx` (`StAddDlg`), `students/more.jsx` (`StRoster`, `StInventory`), on top of `auth/kit.jsx` (`AuShell`, `AuTag`, `AuMsg`). Frames built by this story:

| Frame | What it shows |
|-------|---------------|
| 1a | Roster page reached from Students: back link "חזרה לתלמידים", title "ייבוא רשימת תלמידים", subtitle "קובץ CSV מוסיף תלמידים חדשים ומעדכן תלמידים קיימים לפי תעודת זהות. הוא אף פעם לא משבית תלמיד.", the "תלמידים" nav item highlighted. (1b / 1c, the tabs alternative, are not built.) |
| 2a | Populated, default filter Active: header "תלמידים" + subtitle, secondary "ייבוא רשימת תלמידים" (upload icon) and primary "הוספת תלמיד"; filters Teacher (220px) · status pill SelectButton (300px) · search at the inline end (300px); table Name (avatar + name) · National ID (LTR) · Phone (LTR) · Teacher · Car (name + transmission tag) · Status tag; footer "{n} תלמידים" |
| 2b | Filter All: Inactive rows muted (page-background tint, muted text and avatar, muted tag) and sorted last |
| 2g | Teacher filter open: "כל המורים" first, then every Teacher, Teachers without Cars included |
| 2h | Search "2093" matching a national ID |
| 2i / 2j | Loading (spinner + "טוען תלמידים...") / error (alert icon + "לא הצלחנו לטעון את התלמידים." + "ניסיון חוזר"), filters still shown |
| 2k | No Students at all: no filters; icon, "עדיין אין תלמידים", "הוסיפו תלמיד ידנית, או ייבאו את רשימת התלמידים מקובץ.", two actions |
| 2l | Empty after filter: table header, then "אין תלמידים שמתאימים לסינון", "נסו מורה אחר או מצב אחר.", "ניקוי הסינון" |
| 2m / 2n | English (LTR) / 768px: columns Student (name, national ID and phone below) · Teacher and Car · Status, filters on two rows |
| 3a to 3d | Add Student: Car locked until a Teacher is chosen; only that Teacher's Cars with transmission tags and the hint "מוצגים רק הרכבים של {t}."; a Teacher's only Car preselected ("זה הרכב היחיד של {t}, ולכן הוא נבחר."); a Teacher with no Cars: info message + link "מעבר לרכבים ומורים", Save disabled |
| 3e to 3g | 409s inline, first in the dialog and under the field: national ID exists (with the name), national ID not valid, Car not of this Teacher (stale) with a "רענון" link |
| 3h / 3i | Success toast "התלמיד נוסף" / "{name} נוסף/ה לרשימה של {t}." and the new row first with a "חדש" tag / the dialog in English |

Frames 2c to 2f (row actions, Car-not-of-Teacher marker), 4 to 7 (Edit, Deactivate / Reactivate, Change Teacher, Change Car) and 8 (student form) belong to #94 / #95. The copy deck strings are used verbatim (README decision 23). Every frame's layout, sizes, colors and copy are spelled out in the task that builds it (task 4: section 1 and 2; task 5: section 3), so the tasks can be built without the `claude_design` MCP. Task 6 compares against the frames when the MCP is connected.

**Branch:** `93-students-screen`, from `82-users-and-roles` at `1a72308` (contains #92). This plan is committed on the branch before task 1. The PR targets `82-users-and-roles` (memory: every #82 story PRs into the epic branch).

## Current State

| Piece | Today | File |
|-------|-------|------|
| Student aggregate | `Student.Create(NationalId, StudentName, PhoneNumber, Teacher, Car, Address?, LessonsStartDate?, LicenseType?)` throws `StudentCarMustBeAssignedToTeacherException` when `!car.IsAssignedTo(teacher)` (#92); `Deactivate` / `Reactivate` exist with no endpoint | `src\DrivingLessons.Domain\Entities\Student.cs` |
| Find students | `GET api/students/find?teacherId=` → `ItemForFindStudentsResponse` (id, national ID, name, phone, Teacher id + name, Car id + name, `IsActive`), ordered by Teacher name then Student name; no transmission | `src\DrivingLessons.Application\Queries\FindStudents\`, `src\DrivingLessons.Infrastructure\EntityFramework\Queries\StudentQueries.cs` |
| Get / create Student | None. Students enter only through the Roster import | - |
| Student repository | `FindAllAsync`, `GetActiveByNationalIdAsync` (active only), `Add`; `students.national_id` has a unique index | `src\DrivingLessons.Domain\Repositories\IStudentRepository.cs`, `Infrastructure\EntityFramework\Repositories\StudentRepository.cs`, `EntityConfigurations\StudentConfiguration.cs` |
| Not found | `StudentNotFoundException()` only, message about the student form's national ID lookup | `src\DrivingLessons.Application\Common\Exceptions\StudentNotFoundException.cs` |
| Exception filter | `code` from the type name; `params` only for the Roster columns and the constraint length | `src\DrivingLessons.Presentation.Web\Filters\ApiExceptionFilter.cs` |
| Uniqueness pattern | `CreateUserInteractor` checks before creating and throws `UserSignInEmailAlreadyInUseException` (Domain\Exceptions) | `src\DrivingLessons.Application\Commands\CreateUser\` |
| Policy matrix | Student endpoints have no attribute (Administrator fallback); `ControllerAuthorizationTest` checks everything not listed is Administrator-only | `tests\DrivingLessons.Application.Test\Auth\ControllerAuthorizationTest.cs` |
| Client navigation | `shell.nav.roster` ("תלמידים" / "Roster") → `/roster`, the Roster upload page with its own Student table | `client\src\app\features\admin-shell\domain\navigation.ts`, `admin.routes.ts`, `features\roster\` |
| Client Students | None | - |
| Translations | `errors.studentCarMustBeAssignedToTeacher` is missing (the import never surfaced it); `errors.nationalIdMust*` exist with student-form wording | `client\public\i18n\he.json`, `en.json` |

## Decisions (made while planning, challenge on review)

| # | Decision |
|---|----------|
| 1 | **Get Student** is `GET api/students/{id}` → `GetStudentInteractor` → `IStudentQueries.GetAsync(Guid)` → `GetStudentResponse` (id, national ID, name, phone, Teacher id + name, Car id + name + transmission, address, start date, license type, `IsActive`). A missing Student throws the existing `StudentNotFoundException` through a new `(StudentId)` overload (same code `studentNotFound`). The #93 client doesn't call it; it is the AC's endpoint and #94's Edit dialog will. The existing `errors.studentNotFound` text speaks of the student form and stays as is. |
| 2 | **Find-students gains `CarTransmission`** (design 2a's Car tag). The Roster feature's client DTO is left as it is: the extra JSON property is ignored, so the Roster screen needs no change. The `teacherId` filter stays (unused by the Students screen). |
| 3 | **The duplicate check matches any Student, active or Inactive**, through a new `IStudentRepository.GetByNationalIdAsync(NationalId)`. Reusing `GetActiveByNationalIdAsync` would miss an Inactive Student and the unique index would turn the insert into a 500. The lookup takes the value object, so "18" and "000000018" collide. |
| 4 | **`StudentNationalIdAlreadyInUseException(StudentName existingStudentName)`** in `Domain\Exceptions`, named and placed like `UserSignInEmailAlreadyInUseException`; code `studentNationalIdAlreadyInUse`. The filter maps `ExistingStudentName` into `params.name` for "כבר קיים תלמיד עם תעודת הזהות הזו: {name}." The developer `detail` carries no name (PII). #94's Edit can reuse it with its own wording. |
| 5 | **Interactor order**: value objects (invalid national ID, name, phone, address, license type → 409) → duplicate check → Teacher (404) → Car (404) → `Student.Create` (Car not of Teacher → 409) → add → one commit. An invalid ID never costs a lookup. |
| 6 | **Optional fields are `null` when absent.** The client trims and sends `null` for blank address / license type / start date; the server never coerces `""` (that would hit `AddressMustNotBeEmpty`). |
| 7 | **`POST api/students` answers 201 with `CreateStudentResponse(Guid Id)`** via `CreatedAtAction(null, result)`, like Create User. The client uses the id to highlight the new row. |
| 8 | **Authorization**: the new `StudentCommandController` has no attribute, like `StudentQueryController` (Administrator fallback). A new `Students_Stay_Administrator_Only` `[DataRow]` test pins `FindAsync`, `GetAsync` and `CreateAsync`. The client route uses `administratorGuard`. |
| 9 | **Two `errors.*` keys** land with the backend (task 2): `studentNationalIdAlreadyInUse` and the missing `studentCarMustBeAssignedToTeacher` (design `err.carStale`). |
| 10 | **Client feature `students`** with its own `TeacherOptionsApiService` (`api/teachers/find`) and `CarOptionsApiService` (`api/cars/find`, `assignedTeachers` → `teacherIds`); no import from `users`, `teachers` or `roster`. `StudentsStore` is `@Injectable()` and provided by the page, so filters and the "new" highlight reset per visit, like `UsersStore`. |
| 11 | **Filtering and search are client-side** on the loaded list (the AC's "reuse the find-students query" is met: one call, no Teacher parameter). No debounce: the list is small and filtering is instant. Default status Active; the Teacher filter lists every Teacher by name (Teachers without Cars too); search matches part of the national ID or of the name, case-insensitive, trimmed. "ניקוי הסינון" restores the defaults (All Teachers, Active, empty search). |
| 12 | **Order**: Students added on this visit first (tinted, "חדש" tag) while they match the filters, then active, then Inactive; inside each group the server's order (Teacher name, Student name). A reload of the page ends the highlight. |
| 13 | **Footer singular**: `students.footerOne` ("תלמיד אחד" / "1 Student") for one row, per the client-i18n plural rule; the copy deck only has the plural. |
| 14 | **Routes**: Students at `/students`; the Roster feature moves to `/students/import` (declared before `students`, both `canMatch: [administratorGuard]`), so `routerLinkActive` (`exact: false`) keeps "תלמידים" highlighted on the Roster page; `/roster` redirects to `/students/import`. Nav key `shell.nav.roster` becomes `shell.nav.students` ("תלמידים" / "Students") in the same position. The Roster page keeps its table and upload, gains the back link, and takes the design's `roster.title` / `roster.sub`. |
| 15 | **Not in #93**: the row-actions column and menu (#94 / #95), the Car-not-of-Teacher marker (#95), Edit / Deactivate / Change dialogs, the student-form Inactive notice (#94), the tabs alternative (1b / 1c). |
| 16 | **768px layout** at the project's single `992px` breakpoint: columns Student (name, national ID and phone below) · Teacher and Car · Status; filters on two rows. Implemented with `students__wide-only` / `students__compact-only` cells, no second table. |
| 17 | **Add Student is a DynamicDialog fed with store signals** (the `UserDialogData` pattern): it stays open on a refusal and closes on success. `refusalKindOf(code)` sorts refusals: `nationalIdInUse`; `nationalIdInvalid` (all three `NationalId*` codes → the design's single message `students.add.nationalIdInvalid`); `staleCar` (`studentCarMustBeAssignedToTeacher` and `carNotFound`) with a "רענון" link that reloads the Car options and clears the refusal; `other` (any other code, translated by `ToastService.messageOf`). The refused field gets a `refused` error, so Save stays disabled until it changes. |
| 18 | **Teacher → Car pick** is the pure `pickCarFor(teacherId, cars)`: no Teacher → locked; otherwise only that Teacher's Cars by name, nothing selected, except a Teacher's only Car, preselected. The dialog holds the Car outside the reactive form, as a `linkedSignal` derived from the pick (never an `effect()`, per `client-state.md`), so it resets whenever the Teacher or the Car options change, even when the new Teacher shares the old Car (design: "reset on change"; #95's Change Teacher preselects the current Car instead). The Teacher list marks Teachers without Cars "אין רכבים" (design `StTeacherField`). |
| 19 | **`DialogRefusalComponent` projects content** after its message, for the Refresh link. Existing uses project nothing. |
| 20 | **English buttons in Title Case** ("Add Student", "Import Roster") like every existing English button ("Add User"); the copy deck's capitals are a mock button style. |
| 21 | **Colors from the theme**: transmission tag automatic = `--p-sky-50 / 700 / 100`, manual = `--app-bg-muted` / `--app-text-secondary` / `--app-border`, muted on Inactive rows; new-row tint `--p-sky-50` (the app's picked-slot tint; the design said #F2F7FF); "חדש" tag in the Administrator tag tones; status Tags `success` / `secondary` like Users; refusals and errors `--p-red-600` like the rest of the app. |
| 22 | **Start date**: PrimeNG `DatePicker`, `dateFormat="dd/mm/yy"` in both languages; the picked `Date` becomes `yyyy-MM-dd` from its local calendar parts (`toNewStudent`), never `toISOString()`. |
| 23 | **Copy deck verbatim**, except: the ellipsis character becomes three dots (`translations.spec.ts`), placeholders `{t}` / `{n}` / `{name}` become `{{teacher}}` / `{{count}}` / `{{name}}`, English buttons per decision 20, `footerOne` per decision 13. Generic strings reuse `general.cancel`, `general.save`, `general.refresh`, `general.refusedTitle`. |
| 24 | **Smoke and browser checks run on throwaway databases** (`drivinglessons_us93_smoke`, `drivinglessons_us93_verify`) on the compose Postgres, like #90 / #92. |

## Global Constraints

- `Domain` depends on nothing; `Application` depends only on `Domain`.
- No comments in code. The only exception is the test section markers `//given //when //then //expected`.
- No long dashes or ellipsis characters in source, specs or translation files (`SourceTextTest`, `source-text.spec.ts`, `translations.spec.ts` enforce this). Use a plain `-` and three dots.
- C#: always `var`, braces on every block, multiline ternaries, no nested method calls outside tests, every parameter used, `is null` outside expression trees.
- Domain methods take resolved entities, typed IDs and value objects; raw `Guid` exists only at controller/response boundaries and in query interfaces.
- Operations are not idempotent; every refusal is a domain-specific exception mapped by `ApiExceptionFilter`.
- Controllers: `[ApiController]`, `[Route]`, `[Tags]`, interactors via `[FromServices]`, `EndpointSummary` and `ProducesResponseType` on every action, zero logic.
- Client rules:
  - Standalone components, `inject()`, OnPush.
  - Signals-only stores exposing readonly signals; components never subscribe. RxJS only in `data\` services, consumed with `firstValueFrom` / `resource()`.
  - No hardcoded user-visible strings; every new key in **both** `client\public\i18n\he.json` and `en.json`, Hebrew first. Logical CSS properties only. National IDs and phones are LTR-isolated.
  - `core\` and `shared\` never import from `features\`; no feature imports another.
  - Business rules stay in the backend: the client narrows the Car list for convenience and shows the server's refusals.
- Terminology: Student, Inactive Student, Teacher, Car, Roster, national ID, transmission. Never "assign", "transfer" or "move" a Student in user-visible copy.

**Commands** (from the repository root unless a step says otherwise):

```bash
dotnet build
dotnet test tests\DrivingLessons.Domain.Test\DrivingLessons.Domain.Test.csproj
dotnet test tests\DrivingLessons.Application.Test\DrivingLessons.Application.Test.csproj
dotnet ef migrations has-pending-model-changes --project src\DrivingLessons.Infrastructure --startup-project src\DrivingLessons.Presentation.Web
```

To run one test class, append `--filter "FullyQualifiedName~CreateStudentInteractorTest"` (any class name).

Client commands run from `client\` in PowerShell. The default `npm` can't install on this machine, but the local Angular CLI works:

```powershell
& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js test --watch=false
& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js build --project client
```

To run one spec file, append `--include src/app/features/students/state/students.store.spec.ts` (any spec path) to the test command.

If `client\node_modules` is missing, install it from `client\` with `& "C:\Users\AdiSilagy\AppData\Roaming\nvm\v22.6.0\node.exe" "C:\Users\AdiSilagy\AppData\Roaming\nvm\v22.6.0\node_modules\npm\bin\npm-cli.js" ci`.

Before anything touches the database, switch to the compose Postgres instead of `dl-postgres`: run `docker stop dl-postgres`, then `docker compose up -d postgres`.

## Review Focus

1. **A national ID that belongs to an Inactive Student is refused with the translated 409, not a 500 from the unique index.** Covered in task 2 (`National_Id_In_Use_Is_Rejected_Naming_The_Existing_Student` with an `[DataRow]` for active and Inactive; smoke "inactive duplicate") and task 6 step 7 (3e with Dana's ID).
2. **The same national ID typed without its leading zeros ("18" vs "000000018") is still a duplicate.** Covered in task 2 (`National_Id_Is_Checked_In_Its_Normalized_Form`; smoke "short").
3. **The start date isn't saved one day early** (DatePicker gives local midnight; `toISOString()` would shift it to the previous day in Israel). Covered in task 5 (`keeps the picked start date's calendar day, late in the evening too`) and task 6 step 7 (3h request body and `GET api/students/{id}`).
4. **Changing the Teacher never keeps the previous Car, even one the new Teacher shares**, and a Teacher's only Car is preselected. Covered in task 5 (`starts empty again when the Teacher changes, even to a Teacher who shares the chosen Car`, `preselects a Teacher's only Car`) and task 6 step 7 (Yael → Ronit with the shared i20).
5. **Blank optional fields are sent as `null`, never `""`**, so a valid Student isn't refused with `addressMustNotBeEmpty` / `licenseTypeMustNotBeEmpty`. Covered in task 5 (`sends blank optional details as absent, never as empty text`), task 2 (`Creates_A_Student_Without_The_Optional_Details`) and task 6 step 7 (3h with a spaces-only address).

## File Structure

| File | Change | Task |
|------|--------|------|
| `src\DrivingLessons.Application\Queries\FindStudents\ItemForFindStudentsResponse.cs` | `CarTransmission` | 1 |
| `src\DrivingLessons.Application\Queries\GetStudent\GetStudentInteractor.cs`, `GetStudentResponse.cs` | **New** | 1 |
| `src\DrivingLessons.Application\Queries\IStudentQueries.cs`, `src\DrivingLessons.Infrastructure\EntityFramework\Queries\StudentQueries.cs` | `GetAsync(Guid)`; find projects the transmission | 1 |
| `src\DrivingLessons.Application\Common\Exceptions\StudentNotFoundException.cs` | `(StudentId)` overload | 1 |
| `src\DrivingLessons.Presentation.Web\Controllers\Student\StudentQueryController.cs` | `GetAsync` | 1 |
| `src\DrivingLessons.Application\DependencyInjection.cs` | `GetStudentInteractor` (1), `CreateStudentInteractor` (2) | 1-2 |
| `tests\DrivingLessons.Application.Test\Queries\GetStudentInteractorTest.cs` | **New** | 1 |
| `tests\DrivingLessons.Application.Test\Auth\ControllerAuthorizationTest.cs` | `Students_Stay_Administrator_Only` | 1-2 |
| `src\DrivingLessons.Domain\Repositories\IStudentRepository.cs`, `src\DrivingLessons.Infrastructure\EntityFramework\Repositories\StudentRepository.cs` | `GetByNationalIdAsync` | 2 |
| `src\DrivingLessons.Domain\Exceptions\StudentNationalIdAlreadyInUseException.cs` | **New** | 2 |
| `src\DrivingLessons.Application\Commands\CreateStudent\CreateStudentInteractor.cs`, `CreateStudentRequest.cs`, `CreateStudentResponse.cs` | **New** | 2 |
| `src\DrivingLessons.Presentation.Web\Controllers\Student\StudentCommandController.cs` | **New**: `POST api/students` | 2 |
| `src\DrivingLessons.Presentation.Web\Filters\ApiExceptionFilter.cs` | `params.name` | 2 |
| `tests\DrivingLessons.Application.Test\Commands\CreateStudentInteractorTest.cs` | **New** | 2 |
| `tests\DrivingLessons.Application.Test\Filters\ApiExceptionFilterTest.cs` | New exception: code, params, detail | 2 |
| `client\src\app\features\students\domain\*` | **New**: models, enums, `student-list.ts`, `add-student-refusal.ts` (3); `car-pick.ts`, `new-student.ts` + specs (5) | 3, 5 |
| `client\src\app\features\students\data\*` | **New**: `StudentsApiService`, `TeacherOptionsApiService`, `CarOptionsApiService`, DTOs | 3 |
| `client\src\app\features\students\state\students.store.ts`, `.spec.ts` | **New** | 3 |
| `client\src\app\features\students\students.routes.ts` | **New** | 4 |
| `client\src\app\features\students\ui\components\transmission-tag\*` | **New** | 4 |
| `client\src\app\features\students\ui\pages\students\*` | **New** (4); Add Student wiring (5) | 4-5 |
| `client\src\app\shared\config\app-routes.ts` | `students`, `rosterImport` | 4 |
| `client\src\app\features\admin-shell\admin.routes.ts`, `domain\navigation.ts`, `navigation.spec.ts` | `/students`, `/students/import`, `/roster` redirect; `shell.nav.students` | 4 |
| `client\src\app\features\roster\ui\pages\roster\roster.page.html`, `.ts`, `.scss` | Back link | 4 |
| `client\src\app\features\students\ui\dialogs\add-student\*` | **New** | 5 |
| `client\src\app\shared\components\dialog-refusal\dialog-refusal.component.html` | `<ng-content />` | 5 |
| `client\public\i18n\he.json`, `en.json` | `errors.*` (2); `shell.nav.students`, `roster.*`, `students.*` list (4); `students.add*` (5) | 2, 4, 5 |

## Execution Order

| # | File | Task | Commit point |
|---|------|------|--------------|
| 1 | [task-01-get-student-and-car-transmission.md](task-01-get-student-and-car-transmission.md) | `GET api/students/{id}`, transmission in find-students, Student endpoints pinned Administrator-only | ✅ own commit |
| 2 | [task-02-create-student.md](task-02-create-student.md) | `POST api/students`, duplicate national ID (any state) refused with the name, filter `params`, two `errors.*` keys, API smoke | ✅ own commit |
| 3 | [task-03-client-students-store.md](task-03-client-students-store.md) | `students` feature domain, data services and the signals store (filters, search, order, new rows, refusals) | ✅ own commit |
| 4 | [task-04-client-students-page-and-navigation.md](task-04-client-students-page-and-navigation.md) | Students page (all list states, 768px), navigation, Roster at `/students/import` with a back link | ✅ own commit |
| 5 | [task-05-client-add-student-dialog.md](task-05-client-add-student-dialog.md) | Add Student dialog, Teacher → Car pick and form mapping in `domain` with specs, inline refusals | ✅ own commit |
| 6 | [task-06-verify-and-pr.md](task-06-verify-and-pr.md) | Full suites, browser verification (Hebrew RTL, English, 768px), design comparison, PR | ✅ PR |

The PR targets `82-users-and-roles`, says `Closes #93` (it won't auto-close on a non-default base; close it when the epic merges), and references #94, #95 and the parent spec #82.
