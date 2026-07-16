# US-49: Roster Module — Task Index

Per-task breakdown of the approved implementation plan (the source of truth). Execute the tasks **in order**, one per session — each file is self-contained.

**Goal:** Implement the student roster module — the admin uploads the school's CSV (from "Berosh") to pre-load every student, bound to their teacher and car, with upsert-by-national-ID and deactivation of absentees — covering GitHub issue [#51](https://github.com/silagy/DrivingLessonsBooking/issues/51) (US-49). This module is the prerequisite for the entire student-form module (national-ID identification and roster-based routing, ADR 0003).

**Architecture:** New `Student` + `RosterImport` aggregates through all four onion layers; CSV parsing as an Application port (`IRosterCsvParser`) with a hand-rolled RFC-4180 Infrastructure adapter mapped by canonical Hebrew header names; a single-transaction import interactor (parse → resolve teachers/cars → upsert → deactivate absent → persist report); new Angular `roster` admin feature (signals store + `resource()`, PrimeNG, Transloco he/en) per the committed mockup.

**Tech Stack:** .NET 10 / ASP.NET Core / EF Core / PostgreSQL backend; Angular (signals, standalone, zoneless) + PrimeNG + Transloco client. No new NuGet packages.

**Branch:** `51-us-49-roster-module`

## User Stories

- **US-49** ([#51](https://github.com/silagy/DrivingLessonsBooking/issues/51)) — admin uploads a CSV roster of students (national ID, full name, phone, assigned car, assigned teacher, address, license type); rows upsert by national ID, teacher/car strings resolve to existing records, students absent from the file are deactivated (not deleted), and rows with an unresolved teacher/car or invalid ID are reported and skipped.

## Context

Per [ADR 0003](../../decisions/0003-roster-csv-and-weekly-link-model.md), the roster is the single source of truth for every student profile field: national ID is the student identifier, there is no self-registration, and an ID not in the roster cannot submit. The CSV is Hebrew (UTF-8, likely BOM; phone fields may carry RTL control marks). Re-upload upserts by national ID; absentees are **deactivated, not deleted**, so historical submissions survive (decision #19). The committed mockups (`Driving Lesson Mockup\mock\admin.jsx` → `AdminRoster`) are the UX source of truth for the admin page. The student-form module roadmap consuming this module lives at [docs/modules/student-form/README.md](../../student-form/README.md).

## Decisions (resolved via grilling)

| # | Decision |
|---|----------|
| 1 | **`NationalId`** validates format: digits only (after stripping bidi marks/NBSP), zero-pad to 9, Israeli check-digit. Roster membership remains the actual access gate. Exception messages carry no ID payload (PII). |
| 2 | **CSV parsing is header-name-based** (order-independent, unknown columns ignored) against a canonical Hebrew header set isolated in one swap file (`RosterCsvHeaders.cs`) — the real Berosh headers slot in later as a one-file change. |
| 3 | **Hand-rolled RFC-4180 parser, no new NuGet package** — ~60-row file, fully unit-tested state machine behind the `IRosterCsvParser` port; CsvHelper remains a drop-in swap if real exports surface pathological cases. |
| 4 | **`RosterImport` is persisted per upload** (filename, timestamp, counts, entries, failed rows) so the page's last-import summary survives refresh; counts are computed from entries+failures at creation (single source). |
| 5 | **Roster page = current roster table + last-import summary**: per-teacher filter, Added/Updated badges from the latest import only (badges reset each upload by design), inactive students dimmed. |
| 6 | **Error-report CSV download deferred** — failed rows are listed inline (row number, name, translated reason enum). |
| 7 | **The CSV notes column (`הערות`) is ignored** like other Berosh bookkeeping columns — no v1 consumer (not on the student form, not in Excel §9, not in the mockup). No `StudentNotes` on the aggregate. |
| 8 | **`Student.UpdateFromRoster(...)` is one full-overwrite method** (the roster is the source of truth; no granular field setters). `Deactivate`/`Reactivate` are separate guarded transitions per the non-idempotency rule; reactivation-on-reappearance is orchestrated by the interactor. |
| 9 | **Transmission is not stored on Student** — it derives from the assigned Car (decision #18). |
| 10 | **Duplicate ID in file: first occurrence wins**, later duplicates recorded as `DuplicateNationalId` failures; DB unique index on `national_id` as backstop. |
| 11 | **One transaction per upload** — a single `CommitAsync` after all upserts/deactivations and the import record; skipped rows are report data, not rollbacks. |
| 12 | **Failure reasons are an enum** (`RosterRowFailureReason`), never backend free text — the client translates them (i18n rule 11). |
| 13 | **No `StudentCommandController`** — students are mutated only through roster import. |

## Conventions that OVERRIDE the rules docs (follow the code, per prior modules)

- Repositories have **no `UnitOfWork` property** — interactors inject `IUnitOfWork` separately.
- Controllers use `[EndpointSummary]`, not `[SwaggerOperation]`; routes are prefixed `api/`.
- DI registrations go in each project's `DependencyInjection.cs` (`AddApplication` / `AddInfrastructure`).
- No comments anywhere except `//given //when //then` test markers.
- `FirstOrDefaultAsync` for predicate lookups, `FindAsync` for PK; never `SingleOrDefaultAsync`.

## Execution Order

| # | File | Task | Commit point |
|---|------|------|--------------|
| 1 | [task-01-national-id-value.md](task-01-national-id-value.md) | `NationalId` value object with check-digit validation (TDD) | ✅ own commit |
| 2 | [task-02-student-values.md](task-02-student-values.md) | Student profile value objects (TDD) | ✅ own commit |
| 3 | [task-03-student-aggregate.md](task-03-student-aggregate.md) | `Student` aggregate with roster-driven lifecycle (TDD) | ✅ own commit |
| 4 | [task-04-roster-import-aggregate.md](task-04-roster-import-aggregate.md) | `RosterImport` aggregate recording import outcomes (TDD) | ✅ own commit |
| 5 | [task-05-csv-parser.md](task-05-csv-parser.md) | Roster CSV parser — port + hand-rolled RFC-4180 adapter (TDD) | ✅ own commit |
| 6 | [task-06-application-layer.md](task-06-application-layer.md) | Application layer — repositories, import interactor, queries (TDD) | ✅ own commit |
| 7 | [task-07-infrastructure-persistence.md](task-07-infrastructure-persistence.md) | Infrastructure — persistence, query implementations, migration | ✅ own commit |
| 8 | [task-08-controllers.md](task-08-controllers.md) | Controllers + API smoke test | ✅ own commit |
| 9 | [task-09-client-feature.md](task-09-client-feature.md) | Client — routes, nav, i18n, data layer, signals store | ✅ own commits (3) |
| 10 | [task-10-client-ui-and-verification.md](task-10-client-ui-and-verification.md) | Client — roster page UI + end-to-end verification (both languages) + PR | ✅ own commit |

## How to Run a Task

1. Confirm you are on branch `51-us-49-roster-module` and all earlier tasks are committed.
2. Open the task file and follow the steps exactly — each step has full file contents and exact commands.
3. Run the verification step(s) before committing.
4. Check off the `- [ ]` boxes in the task file as you go.

## Open Items (non-blocking, carried from planning)

1. **Real Berosh headers unknown** — the canonical Hebrew set is our invention; when a real export arrives, only `RosterCsvHeaders.cs` changes.
2. **`LicenseType` semantics** (transmission words vs license class vs free text) — modeled as a free-text value object (safe superset); transmission truth remains the Car. Confirm with the customer.
3. **Check-digit strictness** — a legacy ID failing the checksum cannot submit; the failed-rows panel surfaces it. Accepted risk; restate to the customer.
4. **Start-date format** — `dd/MM/yyyy` + `d/M/yyyy`; unparseable non-empty values become failed rows (fail loud).
5. **Whole-file errors map to 409** (DomainException → filter); arguably 400, but not worth a new exception family now.

## Target Layout (new/changed this module)

```
src\DrivingLessons.Domain\
├── Values\        StudentId, NationalId, StudentName, PhoneNumber, Address, LessonsStartDate, LicenseType,
│                  RosterImportId, RosterFileName, RosterImportEntry, RosterImportFailure,
│                  RosterEntryOutcome, RosterRowFailureReason
├── Entities\      Student, RosterImport
├── Events\        StudentCreated, StudentUpdatedFromRoster, StudentDeactivated, StudentReactivated, RosterImportCreated
├── Exceptions\    NationalIdMustBeDigitsException, NationalIdMustBeAtMostNineDigitsException,
│                  NationalIdMustHaveValidCheckDigitException, StudentNameMustNotBeEmptyException,
│                  PhoneNumberMustBeValidException, AddressMustNotBeEmptyException, LicenseTypeMustNotBeEmptyException,
│                  StudentAlreadyDeactivatedException, StudentAlreadyActiveException,
│                  RosterFileNameMustNotBeEmptyException, RosterImportFailureRowNumberMustBePositiveException,
│                  RosterFileMustContainRequiredColumnsException, RosterFileMustNotBeEmptyException
└── Repositories\  IStudentRepository, IRosterImportRepository (+ FindActiveAsync on ITeacherRepository/ICarRepository)
src\DrivingLessons.Application\
├── Abstractions\        IRosterCsvParser, RosterCsvRow
├── Common\Exceptions\   RosterImportNotFoundException
├── Commands\            ImportRoster
└── Queries\             IStudentQueries, FindStudents, IRosterImportQueries, GetLatestRosterImport
src\DrivingLessons.Infrastructure\
├── Csv\                             RosterCsvParser, RosterCsvHeaders
└── EntityFramework\
    ├── EntityConfigurations\        StudentConfiguration, RosterImportConfiguration, Converters\ (9 new)
    ├── Migrations\                  new AddStudentsAndRosterImports
    ├── Queries\                     StudentQueries, RosterImportQueries
    └── Repositories\                StudentRepository, RosterImportRepository
src\DrivingLessons.Presentation.Web\Controllers\
├── RosterImport\    RosterImportCommandController, RosterImportQueryController
└── Student\         StudentQueryController
tests\DrivingLessons.Domain.Test\        Values\, Entities\ + Fake\, Common\Faker (FakeNationalId)
tests\DrivingLessons.Application.Test\   Csv\RosterCsvParserTest, Commands\ImportRosterInteractorTest
client\src\app\
├── shared\config\app-routes.ts         + roster
└── features\roster\{domain, data, state, ui}\
client\public\i18n\he.json + en.json     + shell.nav.roster, roster.*
```
