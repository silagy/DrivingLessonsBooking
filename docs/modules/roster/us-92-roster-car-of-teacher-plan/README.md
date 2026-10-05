# #92: Roster Import Stops Deactivating Students and Enforces That a Student's Car Is One of Their Teacher's Cars - Task Index

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

Per-task breakdown of issue [#92](https://github.com/silagy/DrivingLessonsBooking/issues/92), slice (7) "Roster import changes" of spec [#82](https://github.com/silagy/DrivingLessonsBooking/issues/82) (user stories 56-59). Each task has its own file and is self-contained. Execute the tasks **in order**, one commit each. Every commit references #92.

**Goal:** The Roster import still adds and updates Students by national ID, but never deactivates anyone, and a row whose Car is not assigned to its Teacher is skipped and listed in the import result while the rest of the file imports. The invariant "a Student's Car is always one of their Teacher's Cars" lives in the `Student` aggregate.

**Architecture:**
- Domain: `Car.IsAssignedTo(Teacher)` answers the question; `Student.Create` and `Student.UpdateFromRoster` guard it with `StudentCarMustBeAssignedToTeacherException` (409 through the existing `DomainException` mapping).
- Application: `ImportRosterInteractor` drops the "deactivate absentees" step and checks the invariant per row **before** calling the domain, recording `RosterRowFailureReason.CarNotAssignedToTeacher` so one bad row never aborts the import.
- The "deactivated" count leaves the whole contract: aggregate, event, EF column (migration), both API responses, the client and the translations.
- Client: the Roster screen loses the "Deactivated" stat tile (three tiles remain) and translates the new failure reason, Hebrew first.

**Tech Stack:**
- Backend: .NET 10, ASP.NET Core Web API, EF Core 10 + Npgsql; tests with MSTest 4 + Shouldly + FakeItEasy.
- Client: Angular 21 (standalone, zoneless, signals) + PrimeNG 21 + Transloco; Vitest via `ng test`.
- One migration (`RemoveRosterDeactivation`); no new package.

**Spec:** issue [#92](https://github.com/silagy/DrivingLessonsBooking/issues/92) (acceptance criteria) · parent spec [#82](https://github.com/silagy/DrivingLessonsBooking/issues/82) ("Roster import (modified)", stories 56-59, Further Notes on existing violators) · original Roster plan [us-49-roster-plan](../us-49-roster-plan/README.md) · [CONTEXT.md](../../../../CONTEXT.md) glossary · `.claude\rules\*.md`

**Design:** Claude Design project `6a0ab892-caa4-49f7-baff-bba7ca38c862` (see [users-and-roles-design.md](../../auth/users-and-roles-design.md)). The Users and Roles frames (`auth/*.jsx`) have **no** Roster frame. The Roster screen's design is `mock/admin.jsx` → `AdminRoster` (also committed at `Driving Lesson Mockup\mock\admin.jsx`):
- Stat row: tiles `added` (green), `updated` (blue), `deactivated (absent from file)` (grey), `failed rows` (plum). This story removes the grey tile; the three remaining tiles keep their tones and share the row equally.
- Failed-rows panel: "Row {n} · {name}" then the reason in the danger color, e.g. "Unknown car "Mazda 2" - not in the school car list". The new reason follows that voice: Hebrew "הרכב לא משויך למורה הזה", English "Car is not assigned to this Teacher".
- The `MkNote` "The roster is the single source of truth" is now outdated; it's an annotation, not UI, so nothing to build.

Every visible change is spelled out in the task that builds it, so the tasks can be built without the `claude_design` MCP. Task 4 compares against `AdminRoster` when the MCP is connected.

**Branch:** `92-roster-car-of-teacher`, from `82-users-and-roles` at `f2a4b8f`. This plan is committed on the branch before task 1. The PR targets `82-users-and-roles` (memory: every #82 story PRs into the epic branch).

## Current State

| Piece | Today | File |
|-------|-------|------|
| Student invariant | None. `Student.Create(..., Teacher teacher, Car car, ...)` and `UpdateFromRoster(...)` take any Teacher and Car | `src\DrivingLessons.Domain\Entities\Student.cs` |
| Car ↔ Teacher | `Car.TeacherAssignments` (owned, `car_teachers`, auto-loaded); `AssignTeacher` / `UnassignTeacher`; private `MustNotBeAssigned` | `src\DrivingLessons.Domain\Entities\Car.cs` |
| Import | parse → resolve Teacher / Car by name → upsert by national ID (reactivating a reappearing Inactive Student) → `DeactivateAbsentees` → persist `RosterImport` → one commit | `src\DrivingLessons.Application\Commands\ImportRoster\ImportRosterInteractor.cs` |
| Import result | `ImportRosterResponse(RosterImportId, Added, Updated, Deactivated, Failed)`; `RosterImport.DeactivatedCount`; `RosterImportCreated.DeactivatedCount`; `RosterEntryOutcome.Deactivated = 30`; column `roster_imports.deactivated_count`; `GetLatestRosterImportResponse.Deactivated` | Domain `Entities\RosterImport.cs`, `Events\RosterImportCreated.cs`, `Values\RosterEntryOutcome.cs`; Infrastructure `EntityConfigurations\RosterImportConfiguration.cs`; Application `Queries\GetLatestRosterImport\` |
| Failure reasons | `RosterRowFailureReason` 10..90 (`InvalidStartDate = 90`), serialized camelCase by `JsonStringEnumConverter` | `src\DrivingLessons.Domain\Values\RosterRowFailureReason.cs` |
| Client | Four stat tiles incl. `deactivated` (tone `neutral`); `processedRows` sums four counts; `badgeByNationalId` skips `deactivated` entries; failure reasons translated via `roster.failureReasons.{reason}` | `client\src\app\features\roster\` |

## Decisions (made while planning, challenge on review)

| # | Decision |
|---|----------|
| 1 | **Drop the deactivated count, don't keep it at zero** (AC 1 asks to pick one). It leaves `RosterImport`, `RosterImportCreated`, `RosterEntryOutcome`, the EF mapping, `ImportRosterResponse`, `GetLatestRosterImportResponse`, the client models, the stat tile and the `roster.stats.deactivated` key. Removing a field from `RosterImportCreated` modifies an existing event (CLAUDE.md rule 9 prefers new events); nothing subscribes to it (task 2 greps to prove it), and keeping a field that is always 0 is the unclean contract the AC warns against. |
| 2 | **Migration `RemoveRosterDeactivation`** drops `roster_imports.deactivated_count` and first deletes historical `roster_import_entries` rows with `outcome = 30` (the removed enum value), so no stored entry deserializes to an undefined enum. Those rows only fed badges, and the client already hid deactivated badges. `Down` re-adds the column with default 0; deleted entries aren't restored. |
| 3 | **`Car.IsAssignedTo(Teacher teacher)`** is a public query on `Car`. `Student` and the import both use it, so there's one definition of "one of their Teacher's Cars". `MustNotBeAssigned` reuses it. |
| 4 | **Guard name and exception**: `Student` private static `MustBeCarOfTeacher(Car car, Teacher teacher)` throws `StudentCarMustBeAssignedToTeacherException(CarId carId, TeacherId teacherId)` (code `studentCarMustBeAssignedToTeacher`, 409). It runs first in `Create` and `UpdateFromRoster`, so a refusal changes no state and raises no event. Tests: `Create__Must_Be_Car_Of_Teacher`, `Update_From_Roster__Must_Be_Car_Of_Teacher` with `[DataRow]` (Car assigned to nobody / to another Teacher). |
| 5 | **The import checks before the domain**: right after the Car resolves, `!car.IsAssignedTo(teacher)` → `RosterRowFailureReason.CarNotAssignedToTeacher = 100`. The domain guard is the backstop; the import never lets it throw, so one row never aborts the file. |
| 6 | **A reappearing Inactive Student is still reactivated** by the import (unchanged). Spec story 59: the most recent change (the file) wins. Manual deactivation arrives in slice (5); this keeps the same rule. |
| 7 | **Existing violators** (Students already on a Car not of their Teacher) are never scanned: absent from the file, they stay as they are; present with a consistent row, they're updated; present with a still-mismatched row, the row is rejected and the Student is left unchanged. No migration touches `students`. Flagging them in a Students list is slice (5)/(6). |
| 8 | **National ID vs. Roster** (issue decision): accepted as-is, no guard. Upsert stays by national ID. |
| 9 | **Docs are slice (8)**: `requirements.md` §5.5 / §5.5.1, ADR 0003 and CONTEXT.md still say "single source of truth / deactivated". This PR doesn't edit them; the PR body lists them for slice (8). |
| 10 | **Stat row becomes three equal tiles at every width** (`repeat(3, 1fr)`); the narrow-screen 2-column override is removed so no tile sits alone on a row. The unused `neutral` tile tone goes too. |
| 11 | **Browser checks run against a throwaway database** (`drivinglessons_us92_verify`) on the compose Postgres, like #89 / #90. |

## Global Constraints

- `Domain` depends on nothing; `Application` depends only on `Domain`.
- No comments in code. The only exception is the test section markers `//given //when //then`. (Generated migration files keep EF's `/// <inheritdoc />` lines, like every existing migration.)
- No long dashes or ellipsis characters in source, specs or translation files (`SourceTextTest`, `source-text.spec.ts`, `translations.spec.ts` enforce this). Use a plain `-` and three dots.
- C#: always `var`, braces on every block, multiline ternaries, no nested method calls outside tests, every parameter used, `is null` outside expression trees.
- Domain methods take resolved entities, typed IDs and value objects; raw `Guid` exists only at controller/response boundaries.
- Operations are not idempotent; every state-changing domain method guards with a domain-specific exception.
- Test through aggregate roots only; `TeacherAssignment` is never built or asserted directly.
- Client rules:
  - Standalone components, `inject()`, OnPush.
  - Signals-only stores exposing readonly signals; components never subscribe. RxJS only in `data\` services, consumed with `firstValueFrom` / `resource()`.
  - No hardcoded user-visible strings; every new key in **both** `client\public\i18n\he.json` and `en.json`, Hebrew first. Logical CSS properties only.
  - Business rules stay in the backend: the client only translates the failure reason it receives.
- Terminology: Student, Inactive Student, Teacher, Car, Roster, national ID. Never "deactivated" in new user-visible copy.

**Commands** (from the repository root unless a step says otherwise):

```bash
dotnet build
dotnet test tests\DrivingLessons.Domain.Test\DrivingLessons.Domain.Test.csproj
dotnet test tests\DrivingLessons.Application.Test\DrivingLessons.Application.Test.csproj
dotnet ef migrations has-pending-model-changes --project src\DrivingLessons.Infrastructure --startup-project src\DrivingLessons.Presentation.Web
```

To run one test class, append `--filter "FullyQualifiedName~StudentTest"` (any class name).

Client commands run from `client\` in PowerShell. The default `npm` can't install on this machine, but the local Angular CLI works:

```powershell
& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js test --watch=false
& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js build --project client
```

To run one spec file, append `--include src/app/features/roster/state/roster.store.spec.ts` (any spec path) to the test command.

If `client\node_modules` is missing, install it from `client\` with `& "C:\Users\AdiSilagy\AppData\Roaming\nvm\v22.6.0\node.exe" "C:\Users\AdiSilagy\AppData\Roaming\nvm\v22.6.0\node_modules\npm\bin\npm-cli.js" ci`.

Before anything touches the database, switch to the compose Postgres instead of `dl-postgres`: run `docker stop dl-postgres`, then `docker compose up -d postgres`.

## Review Focus

1. **A Car assigned to the row's Teacher *and* another Teacher (shared Car) is accepted.** A naive "car's only Teacher" check would reject it. Covered in task 1 (`Create_With_A_Shared_Car`) and task 3 (`Shared_Car_Row_Is_Imported`).
2. **A mismatched row for an existing Student leaves that Student exactly as it was** (no partial update of name/phone, no `StudentUpdatedFromRoster` event, not counted as Updated). Covered in task 1 (`Update_From_Roster__Rejected_Car_Leaves_Student_Unchanged`) and task 3 (`Car_Not_Of_Teacher_Row_Leaves_Existing_Student_Unchanged`).
3. **A Student who already violates the invariant doesn't break the import**, whether absent from the file or present with a consistent row. Covered in task 3 (`Existing_Violator_Absent_From_File_Does_Not_Break_Import`, `Existing_Violator_Is_Updated_By_A_Consistent_Row`) and by the task 2 migration touching no Student rows.
4. **The latest-import screen still loads after the migration when the last import had deactivations** (historical `outcome = 30` rows). Covered in task 2 (migration deletes them) and task 4 (seed a pre-migration import, then load the screen).
5. **A mismatched row still occupies its national ID for duplicate detection**: a later row with the same national ID in the same file is `DuplicateNationalId`, not imported. Covered in task 3 (`Mismatched_Row_Still_Claims_Its_National_Id`).

## File Structure

| File | Change | Task |
|------|--------|------|
| `src\DrivingLessons.Domain\Entities\Car.cs` | `IsAssignedTo(Teacher)`; `MustNotBeAssigned` reuses it | 1 |
| `src\DrivingLessons.Domain\Entities\Student.cs` | `MustBeCarOfTeacher` guard in `Create` and `UpdateFromRoster` | 1 |
| `src\DrivingLessons.Domain\Exceptions\StudentCarMustBeAssignedToTeacherException.cs` | **New** | 1 |
| `tests\DrivingLessons.Domain.Test\Entities\CarTest.cs`, `StudentTest.cs`, `Fake\StudentFakeBuilder.cs` | Extended | 1 |
| `tests\DrivingLessons.Application.Test\Commands\CreateSubmissionInteractorTest.cs`, `ReviseSubmissionInteractorTest.cs`, `ImportRosterInteractorTest.cs` | Setup assigns the Car to the Teacher | 1 |
| `tests\DrivingLessons.Application.Test\Filters\ApiExceptionFilterTest.cs` | New exception → 409 + code | 1 |
| `src\DrivingLessons.Application\Commands\ImportRoster\ImportRosterInteractor.cs`, `ImportRosterResponse.cs` | No deactivation, no `Deactivated` | 2 |
| `src\DrivingLessons.Domain\Entities\RosterImport.cs`, `Events\RosterImportCreated.cs`, `Values\RosterEntryOutcome.cs` | No `Deactivated` | 2 |
| `src\DrivingLessons.Infrastructure\EntityFramework\EntityConfigurations\RosterImportConfiguration.cs` | No `deactivated_count` | 2 |
| `src\DrivingLessons.Infrastructure\EntityFramework\Migrations\*_RemoveRosterDeactivation.cs` (+ Designer, snapshot) | **New** migration | 2 |
| `src\DrivingLessons.Application\Queries\GetLatestRosterImport\GetLatestRosterImportResponse.cs` | No `Deactivated` | 2 |
| `tests\DrivingLessons.Domain.Test\Entities\RosterImportTest.cs`, `tests\DrivingLessons.Application.Test\Commands\ImportRosterInteractorTest.cs` | Absentees stay | 2 |
| `client\src\app\features\roster\data\import-roster.response.ts`, `get-latest-roster-import.response.ts`, `domain\roster-entry-outcome.enum.ts`, `state\roster.store.ts`, `.spec.ts`, `ui\pages\roster\roster.page.html`, `.ts`, `.scss`, `ui\components\roster-stat-tile\roster-stat-tile.component.ts`, `.scss` | Three tiles, no `deactivated` | 2 |
| `client\public\i18n\he.json`, `en.json` | − `roster.stats.deactivated` (2); + `roster.failureReasons.carNotAssignedToTeacher` (3) | 2-3 |
| `src\DrivingLessons.Domain\Values\RosterRowFailureReason.cs` | `CarNotAssignedToTeacher = 100` | 3 |
| `src\DrivingLessons.Application\Commands\ImportRoster\ImportRosterInteractor.cs` | Per-row invariant check | 3 |
| `tests\DrivingLessons.Application.Test\Commands\ImportRosterInteractorTest.cs` | Mismatch, shared Car, violators | 3 |
| `client\src\app\features\roster\domain\roster-row-failure-reason.enum.ts` | `carNotAssignedToTeacher` | 3 |
| `client\src\app\features\roster\ui\components\failed-rows-panel\failed-rows-panel.component.spec.ts` | **New** | 3 |
| `docs\modules\auth\users-and-roles-design.md` | Slices (5)-(8) row links this plan and names `AdminRoster` | plan commit |

## Execution Order

| # | File | Task | Commit point |
|---|------|------|--------------|
| 1 | [task-01-student-car-of-teacher-invariant.md](task-01-student-car-of-teacher-invariant.md) | `Car.IsAssignedTo`, `Student` guard + exception, builders and test setups made consistent | ✅ own commit |
| 2 | [task-02-import-never-deactivates.md](task-02-import-never-deactivates.md) | Import drops deactivation; deactivated count leaves domain, DB (migration), API and client | ✅ own commit |
| 3 | [task-03-reject-car-not-of-teacher-rows.md](task-03-reject-car-not-of-teacher-rows.md) | Per-row `CarNotAssignedToTeacher` failure; translated on the Roster screen | ✅ own commit |
| 4 | [task-04-verify-and-pr.md](task-04-verify-and-pr.md) | Full suites, migration on a pre-#92 database, browser check (Hebrew RTL, English), design comparison, PR | ✅ PR |

The PR targets `82-users-and-roles`, says `Closes #92` (it won't auto-close on a non-default base; close it when the epic merges), and references the parent spec #82.
