# #90: Teacher-role Users See and Change Only Their Own Teacher's Data - Task Index

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

Per-task breakdown of issue [#90](https://github.com/silagy/DrivingLessonsBooking/issues/90), the second half of slice (2) "Roles, policies and Teacher scoping" of spec [#82](https://github.com/silagy/DrivingLessonsBooking/issues/82). Each task has its own file and is self-contained. Execute the tasks **in order**, one commit each. Every commit references #90.

**Goal:** A Teacher-role User sees and changes only their own Teacher's Week Schedules and Publications (Submissions, share link, Excel download, History), and marks only their own Slots Open or Unavailable. Any attempt to reach another Teacher's data, even by a crafted URL, is refused as not-found. An Administrator linked to a Teacher defaults to their own Teacher's view and can still switch to any Teacher.

**Architecture:**
- Backend: the existing `ICurrentUser` port (Application) grows `Role` and `TeacherId?`; `HttpCurrentUser` (Presentation.Web) reads them from the token. One rule, `CurrentUserExtension.MayReach(TeacherId)`, is called by the Teacher-reachable interactors before they load anything; a refusal throws the existing not-found exception (404). History filters in the query by an optional Teacher id. A new `GET api/me` returns the signed-in User with the linked Teacher's name, for the locked Teacher chip.
- Client: a `core\signed-in-user\` slice loads `api/me`. A shared `LockedFieldComponent` (extracted from the edit-user dialog) shows a Teacher's own Teacher on Weekly prep. Both stores default an Administrator linked to a Teacher to their own Teacher and mark it "(me)" in the picker. Publications shows a Teacher the "only your Publications" subtitle.

**Tech Stack:**
- Backend: .NET 10, ASP.NET Core Web API, EF Core 10 + Npgsql; tests with MSTest 4 + Shouldly + FakeItEasy.
- Client: Angular 21 (standalone, zoneless, signals) + PrimeNG 21 + Transloco; Vitest via `ng test`.
- No new package and no migration.

**Spec:** issue [#90](https://github.com/silagy/DrivingLessonsBooking/issues/90) (acceptance criteria) · parent spec [#82](https://github.com/silagy/DrivingLessonsBooking/issues/82) · predecessor [#89](https://github.com/silagy/DrivingLessonsBooking/issues/89) ([plan](../us-89-teacher-role-access-plan/README.md)) · [CONTEXT.md](../../../../CONTEXT.md) glossary · design handoff [users-and-roles-design.md](../users-and-roles-design.md) · `.claude\rules\*.md`

**Design:** Claude Design project `Users and Roles.html` (see [users-and-roles-design.md](../users-and-roles-design.md)), file `auth/screens2.jsx` (`AuPrep`, `AuPubs`, `AuLockChip`), copy in `auth/strings.jsx`. Frames for this slice:
- 10a: Weekly prep for a Teacher - the Teacher picker is replaced by a **locked chip**: label "מורה" / "Teacher" above, a 42px-high chip with a lock icon and the Teacher's name, grey surface and border. No "Create Week Schedule", no "Publish week" (already #89)
- 10b: Publications for a Teacher - subtitle "רק הפרסומים שלך: מה שהתלמידים ביקשו ממך בכל שבוע." / "Only your Publications: what Students requested from you each week."; share link copy and Excel download stay. The per-week table layout of 10b is **not** in this plan
- 10c: Weekly prep for an Administrator linked to a Teacher - the picker defaults to their own Teacher, shown as "{name} (אני)" / "{name} (me)", and the open list marks the same option "(me)"

Every frame's layout and copy is spelled out in the task that builds it, so the tasks can be built without the `claude_design` MCP. Task 8 compares against the frames when the MCP is connected.

**Branch:** `90-teacher-data-scoping`, from `89-teacher-role-navigation` at `8a967b8` (#89, PR #100 not merged yet). This plan is committed on the branch before task 1. The PR targets `82-users-and-roles`.

## Current State

| Piece | Today | File |
|-------|-------|------|
| Current-user port | `ICurrentUser { UserId Id }`; `HttpCurrentUser` reads `sub`, throws `InvalidOperationException` without one; registered scoped in `Program.cs` | `src\DrivingLessons.Application\Auth\ICurrentUser.cs`, `src\DrivingLessons.Presentation.Web\Auth\HttpCurrentUser.cs` |
| Token | `sub`, `email`, `role` (`administrator` / `teacher`), `teacher_id` (whenever the User is linked, any Role), `security_stamp`. A Role change rotates the stamp, so the claims are trusted | `src\DrivingLessons.Infrastructure\Auth\JwtTokenGenerator.cs`, `AuthClaims.cs` |
| Week Schedule reads | `GetWeekScheduleInteractor.ExecuteAsync(Guid teacherId, DateOnly weekStart)` trusts `teacherId` | `src\DrivingLessons.Application\Queries\GetWeekSchedule\` |
| Slot commands | `MarkSlotUnavailableInteractor` / `MarkSlotAvailableInteractor.ExecuteAsync(Guid id, Guid slotId)` load by id, no owner check | `src\DrivingLessons.Application\Commands\MarkSlot*\` |
| Dashboard (Submissions) | `GetPublicationDashboardInteractor.ExecuteAsync(Guid publicationId, Guid teacherId)` trusts `teacherId` | `src\DrivingLessons.Application\Queries\GetPublicationDashboard\` |
| Excel | `DownloadPublicationExcelInteractor.ExecuteAsync(Guid id, Guid teacherId)` trusts `teacherId` | `src\DrivingLessons.Application\Queries\DownloadPublicationExcel\` |
| History | `IPublicationQueries.FindHistoryAsync()` returns every Teacher's rows | `src\DrivingLessons.Infrastructure\EntityFramework\Queries\PublicationQueries.cs:99` |
| Me | `MeCommandController` (`PUT api/me/password`) only | `src\DrivingLessons.Presentation.Web\Controllers\Me\` |
| Client Weekly prep | Teacher: picker hidden, selection = token `teacher_id`, no name shown. Administrator: starts with no Teacher | `client\src\app\features\week-schedules\state\week-schedules.store.ts`, `ui\pages\weekly-prep\` |
| Client Publications | Teacher: picker hidden. Administrator: previous choice, else first Teacher by name; `?teacherId=` wins | `client\src\app\features\publications\state\publications.store.ts`, `ui\pages\publications-dashboard\` |
| Locked field | Markup + SCSS only inside the edit-user dialog | `client\src\app\features\users\ui\dialogs\edit-user\` |

## Decisions (made while planning, challenge on review)

| # | Decision |
|---|----------|
| 1 | **`ICurrentUser` grows to the port the AC asks for**: `UserId Id`, `Role Role`, `TeacherId? TeacherId`. `HttpCurrentUser` reads `role` and `teacher_id` through the `AuthClaims` constants. It throws `InvalidOperationException` for an unknown role claim, a Teacher-role token without `teacher_id`, and a malformed `teacher_id`, so a Teacher can never fall through to "no filter". |
| 2 | **One scoping rule, in Application**: `Application\Auth\CurrentUserExtension.cs`, extension `bool MayReach(this ICurrentUser user, TeacherId teacherId)`: true for an Administrator (linked or not); for a Teacher, true only when `user.TeacherId == teacherId`. Interactors call it; queries stay unaware of the current User, except History (decision 5). |
| 3 | **A refusal is not-found (404), never 403**: reuse `WeekScheduleNotFoundException` and `PublicationNotFoundException`, so another Teacher's data isn't revealed to exist (AC 3, 4). Where the Teacher id is an input, the check runs **before** any load. |
| 4 | **Scoped interactors**: `GetWeekScheduleInteractor` (checks `teacherId`); `MarkSlotUnavailableInteractor` / `MarkSlotAvailableInteractor` (load the Week Schedule, check `weekSchedule.TeacherId` before the Slot lookup, never commit on refusal); `GetPublicationDashboardInteractor` (checks `teacherId`; this covers Submissions, which are per-Teacher counts and stats); `DownloadPublicationExcelInteractor` (checks `teacherId` before loading the Publication). |
| 5 | **History filters in the query**: `IPublicationQueries.FindHistoryAsync(Guid? teacherId)`, null = all Teachers. `FindPublicationHistoryInteractor` passes the linked Teacher id for a Teacher and null for an Administrator, so a linked Administrator still sees every row. |
| 6 | **Unscoped on purpose**: `GetPublicationInteractor` (by week). A Publication is week-level and shared by every Teacher; its link token is the share link a Teacher copies (10b). Publication lifecycle commands, Week Schedule creation and every other endpoint are already Administrator-only (#89); the student Submission endpoints are anonymous by link. The scheduled close / Excel email path doesn't go through these interactors and stays untouched. |
| 7 | **`GET api/me`** (new `MeQueryController.GetAsync`, Teacher-or-Administrator) returns the signed-in User through `GetMeInteractor`, which reuses `IUserQueries.GetAsync` and `GetUserResponse` (Id, Name, SignInEmail, Role, TeacherId, TeacherName). The token has no Teacher name and a Teacher can't call `api/teachers/find`, so this is the only source for the 10a chip. The `ControllerAuthorizationTest` matrix gains `MeQueryController.GetAsync`. |
| 8 | **Client signed-in User slice lives in `core\`** so features can use it without cross-feature imports: `core\signed-in-user\signed-in-user-api.service.ts` (`getMe()`), `get-user.response.ts`, `signed-in-user.store.ts` (a `resource` keyed on `auth.token()`, readonly `teacherName`). It is unrelated to the admin-shell `MeApiService` (password), which stays put. |
| 9 | **The locked field (10a) moves to `shared\components\locked-field\`**: markup and SCSS extracted from the edit-user dialog, which then uses the component (inputs: `label`, `value`; optional projected note). Weekly prep shows it for a Teacher in place of the picker. |
| 10 | **An Administrator linked to a Teacher defaults to their own Teacher but can switch (AC 6)**. Weekly prep: `selectedTeacherId` defaults to `auth.teacherId()` for any Role (null for an unlinked Administrator). Publications: previous choice, then `auth.teacherId()`, then the first Teacher by name; `?teacherId=` still wins. `canChooseTeacher` stays Administrator-only. |
| 11 | **"(me)" in both pickers (10c)**: `TeacherOption` gains `isMe: boolean` (`id === auth.teacherId()`). The `p-select` item and selected-item templates append `weekSchedules.me` / `publications.me` ("(אני)" / "(me)"). The store never translates. |
| 12 | **Publications for a Teacher (10b)**: subtitle key `publications.dashboard.onlyYours`. The 10b per-week table layout stays out of scope (deferred by #89). |
| 13 | **Not in scope**: the user menu's name + "Linked Teacher: {t}" line (2c). `api/me` makes it possible; the PR lists it as a follow-up. |
| 14 | **Smoke tests and browser checks run against throwaway databases** (`drivinglessons_us90_smoke`, `drivinglessons_us90_verify`), like #89 decision 12. Colors come from the app theme. |

## Global Constraints

- `Domain` depends on nothing, and `Application` depends only on `Domain`. `HttpCurrentUser` lives in `Presentation.Web` (`Auth\`), which may read `Infrastructure.Auth.AuthClaims` as `Program.cs` already does.
- No comments in code. The only exception is the test section markers `//given //when //then //expected`.
- No long dashes or ellipsis characters in source, specs or translation files (`SourceTextTest`, `source-text.spec.ts`, `translations.spec.ts` enforce this). Use a plain `-` and three dots.
- C#: always `var`, braces on every block, multiline ternaries, no nested method calls outside tests, every parameter used, `is null` outside expression trees.
- Domain methods and the scoping rule take typed IDs (`TeacherId`, `WeekScheduleId`); raw `Guid` exists only at controller/response boundaries and in query interfaces.
- Controllers: `[ApiController]`, `[Route]`, `[Tags]`, interactors via `[FromServices]`, `EndpointSummary` and `ProducesResponseType` on every action, zero logic.
- Client rules:
  - Standalone components, `inject()`, OnPush.
  - Signals-only stores exposing readonly signals; components never subscribe. RxJS only in `data\` services, consumed with `firstValueFrom` / `resource()`.
  - No hardcoded user-visible strings; every new key in **both** `client\public\i18n\he.json` and `en.json`, Hebrew first. Logical CSS properties only.
  - `core\` and `shared\` never import from `features\`; no feature imports another.
- Terminology: User, Role, Administrator, Teacher Role, Teacher, Week Schedule, Publication, Slot, Submission. Never "account" for a User. Never "admin" in user-visible copy.

**Commands** (from the repository root unless a step says otherwise):

```bash
dotnet build
dotnet test tests\DrivingLessons.Domain.Test\DrivingLessons.Domain.Test.csproj
dotnet test tests\DrivingLessons.Application.Test\DrivingLessons.Application.Test.csproj
dotnet ef migrations has-pending-model-changes --project src\DrivingLessons.Infrastructure --startup-project src\DrivingLessons.Presentation.Web
```

To run one test class, append `--filter "FullyQualifiedName~GetWeekScheduleInteractorTest"` (any class name).

Client commands run from `client\` in PowerShell. The default `npm` can't install on this machine, but the local Angular CLI works:

```powershell
& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js test --watch=false
& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js build --project client
```

To run one spec file, append `--include src/app/core/auth.service.spec.ts` (any spec path) to the test command.

If `client\node_modules` is missing, install it from `client\` with `& "C:\Users\AdiSilagy\AppData\Roaming\nvm\v22.6.0\node.exe" "C:\Users\AdiSilagy\AppData\Roaming\nvm\v22.6.0\node_modules\npm\bin\npm-cli.js" ci`.

Before anything touches the database, switch to the compose Postgres instead of `dl-postgres`: run `docker stop dl-postgres`, then `docker compose up -d postgres`.

## Review Focus

1. **A crafted `teacherId` of another Teacher returns 404, not data**, on by-teacher-and-week, the dashboard and the Excel download. Covered in tasks 2 and 3 (`Other_Teachers_..._Is_Not_Found` tests assert the query / repository is never called) and the task 4 smoke.
2. **Marking a Slot in another Teacher's Week Schedule by id changes nothing.** Covered in task 2 (both commands: `WeekScheduleNotFoundException`, `CommitAsync` must not have happened).
3. **A Teacher-role token without `teacher_id` reaches nothing, not everything.** Covered in task 1 (`HttpCurrentUser` throws; `MayReach` is false for a Teacher with no linked Teacher).
4. **An Administrator linked to a Teacher is not narrowed to that Teacher.** Covered in task 1 (`Linked_Administrator_Reaches_Any_Teacher`) and task 3 (History passes null for a linked Administrator).
5. **An explicit choice beats the linked-Administrator default** (a History row link `?teacherId=other`, or a Teacher picked after load). Covered in task 6 (`still lets a linked Administrator choose another Teacher`) and task 7 (`keeps the query-param Teacher over the linked default`).

## File Structure

| File | Change | Task |
|------|--------|------|
| `src\DrivingLessons.Application\Auth\ICurrentUser.cs` | `Role`, `TeacherId?` | 1 |
| `src\DrivingLessons.Application\Auth\CurrentUserExtension.cs` | **New**: `MayReach` | 1 |
| `src\DrivingLessons.Presentation.Web\Auth\HttpCurrentUser.cs` | Reads `role`, `teacher_id` | 1 |
| `tests\DrivingLessons.Application.Test\Auth\HttpCurrentUserTest.cs`, `CurrentUserExtensionTest.cs` | Extended / **New** | 1 |
| `src\DrivingLessons.Application\Queries\GetWeekSchedule\GetWeekScheduleInteractor.cs` | Scoped | 2 |
| `src\DrivingLessons.Application\Commands\MarkSlotUnavailable\MarkSlotUnavailableInteractor.cs`, `MarkSlotAvailable\MarkSlotAvailableInteractor.cs` | Scoped | 2 |
| `tests\DrivingLessons.Application.Test\Queries\GetWeekScheduleInteractorTest.cs`, `Commands\MarkSlotUnavailableInteractorTest.cs`, `Commands\MarkSlotAvailableInteractorTest.cs` | **New** | 2 |
| `src\DrivingLessons.Application\Queries\GetPublicationDashboard\GetPublicationDashboardInteractor.cs` | Scoped | 3 |
| `src\DrivingLessons.Application\Queries\DownloadPublicationExcel\DownloadPublicationExcelInteractor.cs` | Scoped | 3 |
| `src\DrivingLessons.Application\Queries\FindPublicationHistory\FindPublicationHistoryInteractor.cs`, `Queries\IPublicationQueries.cs`, `src\DrivingLessons.Infrastructure\EntityFramework\Queries\PublicationQueries.cs` | `FindHistoryAsync(Guid? teacherId)` | 3 |
| `tests\DrivingLessons.Application.Test\Queries\GetPublicationDashboardInteractorTest.cs`, `DownloadPublicationExcelInteractorTest.cs`, `FindPublicationHistoryInteractorTest.cs` | **New** | 3 |
| `src\DrivingLessons.Application\Queries\GetMe\GetMeInteractor.cs`, `Application\DependencyInjection.cs` | **New** + registration | 4 |
| `src\DrivingLessons.Presentation.Web\Controllers\Me\MeQueryController.cs` | **New**: `GET api/me` | 4 |
| `tests\DrivingLessons.Application.Test\Queries\GetMeInteractorTest.cs`, `Auth\ControllerAuthorizationTest.cs` | **New** / matrix gains `MeQueryController.GetAsync` | 4 |
| `.claude\rules\api-guidelines.md` | Teacher scoping rule in the Auth section | 4 |
| `client\src\app\core\signed-in-user\signed-in-user-api.service.ts`, `get-user.response.ts`, `signed-in-user.store.ts`, `.spec.ts` | **New** | 5 |
| `client\src\app\shared\components\locked-field\locked-field.component.ts`, `.html`, `.scss`, `.spec.ts` | **New** (extracted) | 5 |
| `client\src\app\features\users\ui\dialogs\edit-user\edit-user.dialog.html`, `.scss`, `.ts` | Uses `LockedFieldComponent` | 5 |
| `client\src\app\features\week-schedules\domain\teacher-option.model.ts`, `state\week-schedules.store.ts`, `.spec.ts` | `isMe`, linked default, `ownTeacherName` | 6 |
| `client\src\app\features\week-schedules\ui\pages\weekly-prep\weekly-prep.page.html`, `.ts` | Locked chip, "(me)" templates | 6 |
| `client\src\app\features\publications\domain\teacher-option.model.ts`, `state\publications.store.ts`, `.spec.ts` | `isMe`, linked default | 7 |
| `client\src\app\features\publications\ui\pages\publications-dashboard\publications-dashboard.page.html`, `.ts`, `.spec.ts` | "(me)" templates, Teacher subtitle | 7 |
| `client\public\i18n\he.json`, `en.json` | `weekSchedules.me` (6); `publications.me`, `publications.dashboard.onlyYours` (7) | 6-7 |
| `docs\modules\auth\users-and-roles-design.md` | Slice (2) links this plan | plan commit |

## Execution Order

| # | File | Task | Commit point |
|---|------|------|--------------|
| 1 | [task-01-current-user-port.md](task-01-current-user-port.md) | `ICurrentUser` gains Role and linked Teacher, `HttpCurrentUser` reads them, `CurrentUserExtension.MayReach` | ✅ own commit |
| 2 | [task-02-scope-week-schedules.md](task-02-scope-week-schedules.md) | Get Week Schedule and both Slot commands scoped to the linked Teacher | ✅ own commit |
| 3 | [task-03-scope-publications.md](task-03-scope-publications.md) | Dashboard (Submissions), Excel download and History scoped | ✅ own commit |
| 4 | [task-04-get-me-endpoint.md](task-04-get-me-endpoint.md) | `GET api/me`, policy matrix, api-guidelines note, API smoke as a Teacher | ✅ own commit |
| 5 | [task-05-client-signed-in-user-and-locked-field.md](task-05-client-signed-in-user-and-locked-field.md) | Core signed-in User store, shared `LockedFieldComponent`, edit-user dialog reuses it | ✅ own commit |
| 6 | [task-06-client-weekly-prep-own-teacher.md](task-06-client-weekly-prep-own-teacher.md) | Weekly prep: locked chip for a Teacher, linked-Administrator default, "(me)" | ✅ own commit |
| 7 | [task-07-client-publications-own-teacher.md](task-07-client-publications-own-teacher.md) | Publications: linked-Administrator default, "(me)", Teacher subtitle | ✅ own commit |
| 8 | [task-08-verify-and-pr.md](task-08-verify-and-pr.md) | Full suites, browser verification (three personas, Hebrew RTL, English), design comparison, PR | ✅ PR |

The PR targets `82-users-and-roles`, closes #90 (`Closes #90`), and references #89 and the parent spec #82.
