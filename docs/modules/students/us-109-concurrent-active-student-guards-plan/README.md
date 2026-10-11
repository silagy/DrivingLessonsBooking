# #109: Concurrent admin writes can't break the active-Student guards - Task Index

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

Per-task breakdown of issue [#109](https://github.com/silagy/DrivingLessonsBooking/issues/109), a follow-up of spec [#82](https://github.com/silagy/DrivingLessonsBooking/issues/82). Each task has its own file and is self-contained. Execute the tasks **in order**, one commit each. Every commit references #109.

**Goal:** Two Administrators acting at the same moment can never leave an active Student on a deleted Teacher or Car, or on a Car that isn't their Teacher's (decision #23). The request that loses the race gets the same translated refusal it would get had it come a second later. When two requests change the same Student at once, the later one gets a new 409, `concurrentChange`.

**Architecture:**
- **Row locks on Teacher and Car (pessimistic).** A new application port, `ITeacherAndCarLocks`, has `LockAsync(teacherIds, carIds)` and `LockAllAsync()`. It opens a database transaction and takes `SELECT ... FOR UPDATE` row locks, Teachers first and then Cars, each in id order. Every command that removes a Teacher, a Car or an assignment, and every command that puts an active Student on a Teacher and Car, calls it **once, before it reads the Teacher, the Car or their Students**:
  - Delete Teacher, Delete Car, Unassign a Car.
  - Add Student, Change Teacher, Change Car, Reactivate, Roster import (which locks every Teacher and Car).

  The second of two conflicting requests waits on the lock. It then reads the committed state, and the existing domain rule refuses it (#91, #95, #108). Nothing new needs translating for these cases.
- **Optimistic token on Student.** Postgres's `xmin`, mapped as a row version, guards two requests that change the *same* Student: for example, Change Car checks the Car against a Teacher that a parallel Change Teacher just replaced. `DrivingLessonsDbContext.CommitAsync` turns EF's `DbUpdateConcurrencyException` into the new `ConcurrentChangeException`. `ApiExceptionFilter` maps that to a 409 with code `concurrentChange`, which the client already shows through `errors.{code}`.
- `CommitAsync` commits the transaction the locks opened. A refused command never commits; the scoped `DbContext` is disposed at the end of the request, which rolls the transaction back and releases the locks.
- New ADR 0007 records the choice. `ddd-architecture.md` and `client-architecture.md` stop saying the backend has no locking.

**Tech Stack:**
- Backend: .NET 10, ASP.NET Core Web API, EF Core 10 + Npgsql 10.0.2, PostgreSQL. Tests use MSTest 4, Shouldly and FakeItEasy 8.
- Client: Angular 21 with Transloco; translation keys only, no code change.
- No new package. One migration (`AddStudentConcurrencyToken`), which must produce **no DDL**: `xmin` is a system column.

**Spec:** issue [#109](https://github.com/silagy/DrivingLessonsBooking/issues/109) (acceptance criteria) · decision #23 in [docs/requirements.md](../../../requirements.md) · the guards it protects: [#91 plan](../../teachers/us-91-block-removal-with-active-students-plan/README.md), [#95 plan](../us-95-change-teacher-and-car-plan/README.md), [#108 plan](../us-108-reactivate-student-guards-plan/README.md) · [ADR 0006](../../../decisions/0006-user-aggregate-and-roles.md) (ADR format) · `.claude\rules\ddd-architecture.md` line 716 (optimistic locking deferred "until concurrent admin edits become real").

**Branch:** `109-concurrent-active-student-guards`, from `82-users-and-roles` at `869e7be` (contains #91, #95, #96 and #108). This plan is committed on the branch before task 1. The PR targets `82-users-and-roles` (memory: every #82 story PRs into the epic branch).

## Current State

| Piece | Today | File |
|-------|-------|------|
| Unit of work | `DrivingLessonsDbContext : IUnitOfWork`. `CommitAsync()` loops `SaveChangesAsync` and in-process event dispatch. No explicit transaction, and every read before it is autocommit | `src\DrivingLessons.Infrastructure\EntityFramework\DrivingLessonsDbContext.cs`, `src\DrivingLessons.Application\Common\IUnitOfWork.cs` |
| Removal guards | `DeleteTeacherInteractor` loads the Teacher, checks the active User, loads `FindActiveByTeacherAsync`, then `teacher.Delete(students)`. `DeleteCarInteractor` and `UnassignCarFromTeacherInteractor` do the same with `FindActiveByCarAsync` | `src\DrivingLessons.Application\Commands\DeleteTeacher\`, `DeleteCar\`, `UnassignCarFromTeacher\` |
| Student writers | `CreateStudent`, `ChangeStudentTeacher`, `ChangeStudentCar` and `ReactivateStudent` load the Teacher and Car and let `Student` check that the Car is the Teacher's. `ImportRosterInteractor` loads `FindActiveAsync()` Teachers and Cars, then every Student | `src\DrivingLessons.Application\Commands\{CreateStudent,ChangeStudentTeacher,ChangeStudentCar,ReactivateStudent,ImportRoster}\` |
| Concurrency | None: no `IsConcurrencyToken`, `xmin`, `FOR UPDATE` or isolation level anywhere. `ddd-architecture.md:716` defers it; `client-architecture.md:203` says "the backend deliberately omits optimistic locking" | |
| Retry strategy | `UseNpgsql(connectionString)` with no `EnableRetryOnFailure`, so a user-opened transaction is allowed | `src\DrivingLessons.Infrastructure\DependencyInjection.cs:29-30` |
| Event handlers | Only `PublicationClosed` and `WeekScheduleCreated`. None of the events these commands raise has a handler, so dispatch inside the new transaction does no I/O | `src\DrivingLessons.Application\EventHandlers\` |
| Problem details | `ApiExceptionFilter`: `NotFoundException` → 404, `DomainException` → 409, `code` = exception name in camelCase without `Exception` | `src\DrivingLessons.Presentation.Web\Filters\ApiExceptionFilter.cs` |
| Client | Every affected store calls `toast.apiError(error)` on failure, which shows `errors.{code}` and falls back to `errors.conflict` | `client\src\app\features\{students,teachers,roster}\state\*.store.ts` |

## Decisions (made while planning, challenge on review)

| # | Decision |
|---|----------|
| 1 | **Row locks, not serializable transactions or tokens on Teacher/Car.** With `FOR UPDATE` on the parent rows the loser *waits*, then re-reads and gets the existing rule's message, such as "Can't delete this Teacher: Noa still learns with them". Serializable isolation would abort it with a generic retry error. It also only protects transactions that are all serializable, and can abort unrelated work through predicate-lock false positives. `xmin` tokens on Teacher and Car would make every Student write update its Teacher and Car rows just to bump the version. Unassign writes `car_teachers`, not `cars`, so it would need the same artificial write. The issue lists all three options; ADR 0007 records why this one was chosen. |
| 2 | **Accepting the risk was not chosen**, although #109 offers it, because the AC's second half is cheap with locks: one call per command, no retry loop and no new UI. If you'd rather only document the risk, drop tasks 1 to 4 and 6, and keep task 5's ADR with the accept-the-risk decision. |
| 3 | **One lock call per command, before any read it depends on.** Under READ COMMITTED, a read after the lock sees everything committed by the transaction that held it. Reading first and locking afterwards would check stale data, so each interactor test pins the order with FakeItEasy's `.Then(...)`. Change Car and Reactivate read the Student first to learn its Teacher and Car; a parallel change to that Student is caught by the Student's `xmin` (decision 5). |
| 4 | **Lock order is fixed inside the port**: Teachers by id, then Cars by id, in one call. Since no command locks twice, two lock calls can't deadlock. The Student row is never locked explicitly, only written last by `UPDATE`, so it can't close a cycle either. No deadlock handling is added. |
| 5 | **Same-Student races use `xmin` on Student.** Example: Change Car (Noa → Corolla, checked against Ronit) and Change Teacher (Noa → Yael + Polo) run together. The Teacher and Car locks don't overlap, so without a token Noa could end up with Yael and the Corolla. EF's `UPDATE ... WHERE xmin = @original` makes the later commit fail. `CommitAsync` maps the failure to `ConcurrentChangeException` → 409 `concurrentChange`: "Someone else changed this at the same moment. Refresh the page and try again." The token also covers Change Details, Deactivate and Roster updates of the same Student, which is correct: each was decided on a version that no longer exists. |
| 6 | **The Roster import locks every Teacher and Car (`LockAllAsync`)** rather than the ones its rows name. It resolves Teachers and Cars by name from `FindActiveAsync()`, so it can only know the ids after reading. Locking all of them before that read keeps decision 3, and a school has a handful of each. An import therefore briefly blocks Teacher and Car removals and Student changes, which is the point. |
| 7 | **A losing Add Student or Change Teacher whose Teacher was deleted gets 404 `teacherNotFound`**, the answer it would get a second later; the same applies to Cars. The AC says the loser gets "a 409 with a translated message". Every loser does get a translated message: 409 when a rule refuses it (`teacherMustNotHaveActiveStudents`, `teacherAssignmentMustNotHaveActiveStudents`, `studentCarMustBeAssignedToTeacher`, `concurrentChange`), and 404 when the thing it points at is gone. Reporting a deleted Teacher as a conflict would contradict the rest of the API. |
| 8 | **`ConcurrentChangeException` is an Application exception** (`Application\Common\Exceptions\`), not a `DomainException`: no domain rule is broken, the data moved underneath. `ApiExceptionFilter` maps it before the `DomainException` arm. Its code follows the existing derivation: `concurrentChange`. |
| 9 | **Verification is a Postgres smoke, not an integration test project.** There is no Infrastructure test project, and Testcontainers would be a new package. The smoke makes the race deterministic: a `psql` session holds the row lock (`BEGIN; SELECT ... FOR UPDATE; SELECT pg_sleep(4); COMMIT`) while two API requests queue behind it in a known order. It covers delete vs add, add vs delete, change vs unassign, unassign vs change, and change vs change on the same Student, then checks the invariant with one SQL query. Throwaway database `drivinglessons_us109_smoke` on the compose Postgres, like #91 to #108. |
| 10 | **National-ID uniqueness races (two Add Student with one ID) are out of scope.** That's a different invariant, held by the unique index, and it isn't in #109's AC. |

## Global Constraints

- `Domain` depends on nothing; `Application` depends only on `Domain`; `Infrastructure` → Application, Domain, EF Core, Npgsql.
- No comments in code. The only exception is the test section markers `//given //when //then //expected`.
- No long dashes or ellipsis characters in source, specs, translation files or new docs prose (`SourceTextTest`, `source-text.spec.ts` and `translations.spec.ts` enforce this for code and i18n). Use a plain `-` and three dots.
- C#: always `var`, braces on every block, multiline ternaries, no nested method calls outside tests, every parameter used, `is null` outside expression trees.
- Domain methods take resolved entities, typed IDs and value objects; raw `Guid` exists only at controller and response boundaries.
- Interactors keep their existing shape: constructor injection into `private readonly` fields, `ExecuteAsync`. The new `ITeacherAndCarLocks locks` parameter goes **immediately before the `IUnitOfWork unitOfWork` parameter**, in the constructor and in every `new ...Interactor(` call in its test file.
- Every new `errors.{code}` key goes in **both** `client\public\i18n\he.json` and `en.json`, Hebrew first. The Hebrew voice is the existing plural imperative ("רעננו את הדף ונסו שוב").
- Terminology: Teacher, Car, Student, Inactive Student, Change Teacher, Change Car, Roster. Never "move", "transfer" or "reassign" a Student.

**Commands** (from the repository root unless a step says otherwise):

```bash
dotnet build
dotnet test tests\DrivingLessons.Domain.Test\DrivingLessons.Domain.Test.csproj
dotnet test tests\DrivingLessons.Application.Test\DrivingLessons.Application.Test.csproj
dotnet ef migrations has-pending-model-changes --project src\DrivingLessons.Infrastructure --startup-project src\DrivingLessons.Presentation.Web
```

To run one test class, append `--filter "FullyQualifiedName~DeleteCarInteractorTest"` (any class name).

Client commands run from `client\` in PowerShell, using the local Angular CLI (the default `npm` can't install on this machine):

```powershell
& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js test --watch=false
```

If `client\node_modules` is missing, install it from `client\` with `& "C:\Users\AdiSilagy\AppData\Roaming\nvm\v22.6.0\node.exe" "C:\Users\AdiSilagy\AppData\Roaming\nvm\v22.6.0\node_modules\npm\bin\npm-cli.js" ci`.

Before anything touches the database, use the compose Postgres, not `dl-postgres`: run `docker stop dl-postgres`, then `docker compose up -d postgres`.

National IDs in backend tests and smokes must pass the check digit: use `205374184`, `312456783`, `000000018`, `123456782` (memory: the design's sample IDs fail it).

## Review Focus

1. **A command reads the Teacher, Car or Students before it locks**, so the check runs on data from before the other request committed. Expected: every lock call comes before the dependent reads. Covered by an ordering test per interactor in tasks 2 and 3 (`Locks_..._Before_Reading_...`), and by smoke scenarios A to D in task 6.
2. **A refused command keeps its locks.** A domain exception after `LockAsync` must not leave the transaction open past the request. Expected: the next request on that Teacher doesn't hang. Covered in task 6 step 4: scenario A's refused delete is followed at once by scenario E's requests on the same Teacher, which must answer within the hold time.
3. **The same Student changed twice at once** (Change Car vs Change Teacher). Expected: the later commit gets 409 `concurrentChange` with a translated message, and the Student isn't left on a Car outside their Teacher's. Covered by task 4 (token mapping), the task 1 filter test, and smoke scenario E in task 6.
4. **The `xmin` migration alters the table.** Expected: `AddStudentConcurrencyToken` produces no `ALTER TABLE`. Covered by task 4 step 4 (`migrations script` check).
5. **A Roster import running alongside Delete Teacher or Delete Car.** Expected: the import locks every Teacher and Car before it reads them, so the delete either goes first (the import then fails that row as an unknown Teacher or Car) or waits and is refused. Covered by task 3's `Locks_Every_Teacher_And_Car_Before_Reading_Them`. It isn't smoked, because a Hebrew CSV upload through Windows curl is fragile (memory note).

## File Structure

| File | Change | Task |
|------|--------|------|
| `src\DrivingLessons.Application\Common\ITeacherAndCarLocks.cs` | **New** port | 1 |
| `src\DrivingLessons.Infrastructure\EntityFramework\TeacherAndCarLocks.cs` | **New**: transaction + `FOR UPDATE` | 1 |
| `src\DrivingLessons.Infrastructure\DependencyInjection.cs` | Register the port | 1 |
| `src\DrivingLessons.Infrastructure\EntityFramework\DrivingLessonsDbContext.cs` | `CommitAsync` commits the open transaction and maps `DbUpdateConcurrencyException` | 1 |
| `src\DrivingLessons.Application\Common\Exceptions\ConcurrentChangeException.cs` | **New** | 1 |
| `src\DrivingLessons.Presentation.Web\Filters\ApiExceptionFilter.cs`, `tests\...\Filters\ApiExceptionFilterTest.cs` | 409 `concurrentChange` | 1 |
| `client\public\i18n\he.json`, `en.json` | `errors.concurrentChange` | 1 |
| `src\DrivingLessons.Application\Commands\{DeleteTeacher,DeleteCar,UnassignCarFromTeacher}\*Interactor.cs` + their tests | Lock before reading | 2 |
| `src\DrivingLessons.Application\Commands\{CreateStudent,ChangeStudentTeacher,ChangeStudentCar,ReactivateStudent,ImportRoster}\*Interactor.cs` + their tests | Lock before reading | 3 |
| `src\DrivingLessons.Infrastructure\EntityFramework\EntityConfigurations\StudentConfiguration.cs`, new migration `AddStudentConcurrencyToken` | `xmin` row version | 4 |
| `docs\decisions\0007-locking-for-the-active-student-guards.md` | **New** ADR | 5 |
| `.claude\rules\ddd-architecture.md`, `.claude\rules\client-architecture.md`, `README.md`, `docs\requirements.md` | Locking is no longer deferred; ADR list; §10 bullet | 5 |

## Execution Order

| # | File | Task | Commit point |
|---|------|------|--------------|
| 1 | [task-01-locks-port-and-concurrent-change.md](task-01-locks-port-and-concurrent-change.md) | Port, Infrastructure locks, transaction-aware commit, `ConcurrentChangeException`, filter, translations | ✅ own commit |
| 2 | [task-02-lock-teacher-and-car-removals.md](task-02-lock-teacher-and-car-removals.md) | Delete Teacher, Delete Car, Unassign lock first | ✅ own commit |
| 3 | [task-03-lock-student-writers.md](task-03-lock-student-writers.md) | Add Student, Change Teacher, Change Car, Reactivate, Roster import lock first | ✅ own commit |
| 4 | [task-04-student-concurrency-token.md](task-04-student-concurrency-token.md) | `xmin` on Student, migration without DDL | ✅ own commit |
| 5 | [task-05-adr-and-rules.md](task-05-adr-and-rules.md) | ADR 0007, rules, README, requirements §10 | ✅ own commit |
| 6 | [task-06-smoke-verify-and-pr.md](task-06-smoke-verify-and-pr.md) | Full suites, deterministic Postgres race smoke, PR | ✅ PR |

The PR targets `82-users-and-roles` and says `Closes #109`. It won't auto-close on a non-default base, so close #109 by hand after the merge. It references #82.
