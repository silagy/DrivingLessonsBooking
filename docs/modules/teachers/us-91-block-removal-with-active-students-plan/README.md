# #91: Block deleting or unassigning Teachers and Cars while active Students depend on them - Task Index

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

Per-task breakdown of issue [#91](https://github.com/silagy/DrivingLessonsBooking/issues/91), a slice of spec [#82](https://github.com/silagy/DrivingLessonsBooking/issues/82). Each task has its own file and is self-contained. Execute the tasks **in order**, one commit each. Every commit references #91.

**Goal:** Unassigning a Car from a Teacher, deleting a Car and deleting a Teacher are refused while active Students depend on them, and the Cars & Teachers screen tells the Administrator which Students to change first.

**Architecture:**
- Backend: the rule lives in the aggregates. `Teacher.Delete(IReadOnlyCollection<Student>)`, `Car.Delete(IReadOnlyCollection<Student>)` and `Car.UnassignTeacher(Teacher, IReadOnlyCollection<Student>)` take the resolved Students and refuse while any of them is active and depends on the Teacher / Car / assignment, with the new `TeacherMustNotHaveActiveStudentsException`, `CarMustNotHaveActiveStudentsException` and `TeacherAssignmentMustNotHaveActiveStudentsException`. Each carries the active Students' names, sorted. The interactors load the Students with the new `IStudentRepository.FindByTeacherAsync(TeacherId)` / `FindByCarAsync(CarId)`. `ApiExceptionFilter` maps the three to 409 with `code` and `params` (`count`, `names` = the first three names, and `teacher` for the assignment).
- Client: no new UI. The Cars & Teachers stores already show every refusal as an error toast through `ToastService.apiError` → `errors.{code}`. `apiError` gains the `isolatedParams` argument `messageOf` already has, so Student and Teacher names stay direction-isolated inside the translated sentence; the stores pass `['names']` / `['teacher', 'names']`.

**Tech Stack:**
- Backend: .NET 10, ASP.NET Core Web API, EF Core 10 + Npgsql; tests with MSTest 4 + Shouldly + FakeItEasy.
- Client: Angular 21 (standalone, zoneless, signals) + PrimeNG 21 + Transloco; Vitest via `ng test`.
- No new package and no migration.

**Spec:** issue [#91](https://github.com/silagy/DrivingLessonsBooking/issues/91) (acceptance criteria) · parent spec [#82](https://github.com/silagy/DrivingLessonsBooking/issues/82) · precedent [#85 task 4](../../auth/us-85-add-users-plan/task-04-teacher-delete-guard.md) (`TeacherMustNotHaveActiveUserException`, the first "can't delete while something depends on it" refusal on this screen) · [docs/requirements.md](../../../requirements.md) §5.5, decision #23 ("a student's car is always one of their teacher's cars") · [CONTEXT.md](../../../../CONTEXT.md) glossary (Inactive Student, Change Teacher, Change Car) · `.claude\rules\*.md`

**Design:** there is no Claude Design frame for these refusals. The Students design (project `6a0ab892-caa4-49f7-baff-bba7ca38c862`, `Students.html`) covers the Students screen, Change Teacher and Change Car only; its copy deck sets the voice used here ("יש להחליף... במסך התלמידים", Change Teacher / Change Car, never "move"). The refusals follow the Cars & Teachers screen's existing pattern: an error toast with the translated `errors.{code}` message, exactly how `teacherMustNotHaveActiveUser` shows today.

**Branch:** `91-block-removal-with-active-students`, from `82-users-and-roles` at `86d1a4d` (contains #93 and #94; **not** #95, whose PR #105 is still open). This plan is committed on the branch before task 1. The PR targets `82-users-and-roles` (memory: every #82 story PRs into the epic branch).

## Current State

| Piece | Today | File |
|-------|-------|------|
| Teacher delete | `Teacher.Delete()` guards `MustNotBeDeleted`. `DeleteTeacherInteractor` refuses first while an active User is linked (`IUserQueries.ActiveExistsLinkedToTeacherAsync` → `TeacherMustNotHaveActiveUserException`, #85) | `src\DrivingLessons.Domain\Entities\Teacher.cs`, `Application\Commands\DeleteTeacher\DeleteTeacherInteractor.cs` |
| Car delete | `Car.Delete()` guards `MustNotBeDeleted`; `DeleteCarInteractor` loads the Car and deletes | `Domain\Entities\Car.cs`, `Application\Commands\DeleteCar\DeleteCarInteractor.cs` |
| Unassign | `Car.UnassignTeacher(Teacher)` throws `TeacherNotAssignedToCarException` when not assigned; `UnassignCarFromTeacherInteractor` loads Car then Teacher. Allowed today while Students learn on it, which is how a Student ends up on a Car that is not their Teacher's | `Domain\Entities\Car.cs`, `Application\Commands\UnassignCarFromTeacher\` |
| Student | `Name` (`StudentName`), `TeacherId`, `CarId`, `IsActive`; `Deactivate()` / `Reactivate()` | `Domain\Entities\Student.cs` |
| Student repository | `FindAllAsync`, `GetAsync(StudentId)`, `GetActiveByNationalIdAsync`, `GetByNationalIdAsync`, `Add` | `Domain\Repositories\IStudentRepository.cs`, `Infrastructure\EntityFramework\Repositories\StudentRepository.cs` |
| Problem details | `code` = exception name in camelCase minus `Exception`; `params` from `ApiExceptionFilter.ParamsOf` (`columns`, `maxLength`, `name`) | `Presentation.Web\Filters\ApiExceptionFilter.cs` |
| Client refusals | `TeachersStore.delete`, `CarsStore.delete` and `CarsStore.applyAssignments` catch and call `toast.apiError(error)`; `applyAssignments` reloads the Cars in `finally`. `ToastService.messageOf(error, isolatedParams)` already isolates named params; `apiError(error)` does not pass any | `client\src\app\features\teachers\state\*.store.ts`, `client\src\app\core\services\toast.service.ts` |
| Translations | Transloco interpolation is `{{param}}`. `errors.teacherMustNotHaveActiveUser` sits in `errors` | `client\public\i18n\he.json`, `en.json` |

## Decisions (made while planning, challenge on review)

| # | Decision |
|---|----------|
| 1 | **The rule is in the aggregates, not the interactors.** The domain methods take the resolved Students (critical rule 2: resolved entities), so "active" and "depends on" are decided by `Student.IsActive`, `TeacherId` and `CarId` inside `Teacher` / `Car`, and AC "Inactive Students do not block" is a domain test. This differs from #85's active-User guard (interactor + query): a User is not something the Teacher aggregate can reason about, Students are. The #85 guard stays where it is. |
| 2 | **No overloads.** `Teacher.Delete()`, `Car.Delete()` and `Car.UnassignTeacher(Teacher)` are replaced, not kept, so no caller can skip the rule. Existing tests pass `[]`. |
| 3 | **Repositories return every Student of the Teacher / Car, active or not**: `IStudentRepository.FindByTeacherAsync(TeacherId)` and `FindByCarAsync(CarId)`. The domain filters. Unassign loads by Car; the domain narrows to the Teacher. |
| 4 | **Guard order**: `Teacher.Delete`: already deleted → active Students. `Car.Delete`: already deleted → active Students. `Car.UnassignTeacher`: not assigned → active Students of that Teacher on this Car. So a Student flagged by #95 (active, on a Car not assigned to their Teacher) never turns "not assigned" into "has active Students". `DeleteTeacherInteractor`: not found → active User (#85) → load Students → `teacher.Delete(students)`. |
| 5 | **New exceptions**, each a `DomainException` with `IReadOnlyList<StudentName> ActiveStudentNames` sorted ordinally: `TeacherMustNotHaveActiveStudentsException(TeacherId, IReadOnlyList<StudentName>)` → `teacherMustNotHaveActiveStudents`; `CarMustNotHaveActiveStudentsException(CarId, IReadOnlyList<StudentName>)` → `carMustNotHaveActiveStudents`; `TeacherAssignmentMustNotHaveActiveStudentsException(CarId, TeacherId, TeacherName, IReadOnlyList<StudentName>)` → `teacherAssignmentMustNotHaveActiveStudents` (also exposes `TeacherName`). No domain event: a refusal changes nothing. |
| 6 | **"Problem type" = the `code` extension**, as #85 decision 4 settled: `type` stays null; only the student form branches on `type`. `ApiExceptionFilterTest` gains one test per exception asserting 409, `code` and `params`. |
| 7 | **`params`**: `count` (all active Students, invariant culture), `names` (the first three names joined by `, `, nothing appended when there are more), and `teacher` (the Teacher's name) for the assignment refusal. Three names keep the toast short; `count` tells the Administrator how many there are; the Students screen's Teacher filter lists them all. **Challenge on review** if all names are wanted. |
| 8 | **Copy** (Hebrew first; `{{names}}` and `{{teacher}}` direction-isolated): see task 2 step 6. It names the fix ("Change their Teacher / Car on the Students screen") in the glossary's words; never "move", "transfer" or "reassign". |
| 9 | **The client renders the server's refusal**: no client-side pre-check, no disabled Delete button (critical rule 12). `ToastService.apiError(error, isolatedParams = [])` forwards to `messageOf`. |
| 10 | **The assign popover's Apply sends the unassigns before the assigns** (today it assigns first). A refused unassign therefore stops Apply before any assign is sent, so a refusal never leaves a half-applied edit made of new assignments. Several unassigns still go one by one: the first refused one stops the rest, the toast names that Teacher, and the Cars reload in `finally` (existing behavior), so the popover reopens on the true state. An all-or-nothing Apply would need a batch endpoint; out of scope. |
| 11 | **Smoke and browser checks run on throwaway databases** (`drivinglessons_us91_smoke`, `drivinglessons_us91_verify`) on the compose Postgres, like #93 to #95. |
| 12 | **#95 overlap**: #95 (PR #105, open) calls `car.UnassignTeacher(teacher)` twice in `StudentTest.cs` to build flagged Students. Whichever PR merges second updates those calls to `car.UnassignTeacher(teacher, [])` while merging `82-users-and-roles` (task 4 step 6). Both branches append to `errors` in the i18n files and to `ApiExceptionFilterTest.cs`; resolve by keeping both sides. |

## Global Constraints

- `Domain` depends on nothing; `Application` depends only on `Domain`.
- No comments in code. The only exception is the test section markers `//given //when //then //expected`.
- No long dashes or ellipsis characters in source, specs or translation files (`SourceTextTest`, `source-text.spec.ts`, `translations.spec.ts` enforce this). Use a plain `-` and three dots.
- C#: always `var`, braces on every block, multiline ternaries, no nested method calls outside tests, every parameter used, `is null` outside expression trees.
- Domain methods take resolved entities, typed IDs and value objects; raw `Guid` exists only at controller/response boundaries.
- Operations are not idempotent; every refusal is a domain-specific exception mapped by `ApiExceptionFilter`.
- Client rules:
  - Standalone components, `inject()`, OnPush.
  - Signals-only stores exposing readonly signals; components never subscribe. RxJS only in `data\` services, consumed with `firstValueFrom` / `resource()`.
  - No hardcoded user-visible strings; every new key in **both** `client\public\i18n\he.json` and `en.json`, Hebrew first. Names in translated sentences go through `isolateDirection`.
  - `core\` and `shared\` never import from `features\`; no feature imports another.
  - Business rules stay in the backend.
- Terminology: Teacher, Car, Student, Inactive Student, Change Teacher, Change Car. In Hebrew UI copy "שיוך" / "הסרת השיוך" for assign / unassign (the screen's existing words).

**Commands** (from the repository root unless a step says otherwise):

```bash
dotnet build
dotnet test tests\DrivingLessons.Domain.Test\DrivingLessons.Domain.Test.csproj
dotnet test tests\DrivingLessons.Application.Test\DrivingLessons.Application.Test.csproj
dotnet ef migrations has-pending-model-changes --project src\DrivingLessons.Infrastructure --startup-project src\DrivingLessons.Presentation.Web
```

To run one test class, append `--filter "FullyQualifiedName~CarTest"` (any class name).

Client commands run from `client\` in PowerShell. The default `npm` can't install on this machine, but the local Angular CLI works:

```powershell
& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js test --watch=false
& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js build --project client
```

To run one spec file, append `--include src/app/core/services/toast.service.spec.ts` (any spec path) to the test command.

If `client\node_modules` is missing, install it from `client\` with `& "C:\Users\AdiSilagy\AppData\Roaming\nvm\v22.6.0\node.exe" "C:\Users\AdiSilagy\AppData\Roaming\nvm\v22.6.0\node_modules\npm\bin\npm-cli.js" ci`.

Before anything touches the database, switch to the compose Postgres instead of `dl-postgres`: run `docker stop dl-postgres`, then `docker compose up -d postgres`.

National IDs in backend tests and smokes must pass the check digit: use `205374184`, `312456783`, `000000018` (memory: the design's sample IDs fail it).

## Review Focus

1. **A shared Car: unassigning one Teacher is refused only for that Teacher's active Students; another Teacher's Students on the same Car don't block it.** Covered in task 1 (`Unassign_Teacher_With_Active_Students_Of_Another_Teacher`) and task 2 step 8 (smoke: Yael's Student on the shared i20 doesn't block unassigning Ronit once Ronit's Student is inactive).
2. **A Student flagged by #95 (active, on a Car no longer assigned to their Teacher) doesn't make the unassign refusal lie: unassigning a Teacher who isn't assigned still says "not assigned".** Covered in task 1 (`Unassign_Teacher__Teacher_Must_Be_Assigned_Before_Active_Students_Are_Checked`).
3. **Hebrew names inside an English sentence (and the reverse) don't reorder the sentence; the Teacher's name in the unassign refusal stays readable.** Covered in task 3 (`isolates the names in a refusal toast` in `toast.service.spec.ts`, the stores' `apiError` argument specs) and task 4 step 3 (English UI with Hebrew names).
4. **A Teacher or Car with many active Students gives a short, honest message: three names and the full count.** Covered in task 2 (`Active_Students_Beyond_Three_Are_Counted_Not_Named`).
5. **A Teacher with an active User and active Students is refused for the User first (#85 behavior unchanged), and once the User is deleted, for the Students.** Covered in task 1 (`Teacher_With_An_Active_User_Is_Refused_Before_Students_Are_Loaded`) and task 4 step 3.

## File Structure

| File | Change | Task |
|------|--------|------|
| `src\DrivingLessons.Domain\Entities\Teacher.cs` | `Delete(IReadOnlyCollection<Student>)`, `MustNotHaveActiveStudents` | 1 |
| `src\DrivingLessons.Domain\Entities\Car.cs` | `Delete(IReadOnlyCollection<Student>)`, `UnassignTeacher(Teacher, IReadOnlyCollection<Student>)`, guards | 1 |
| `src\DrivingLessons.Domain\Exceptions\TeacherMustNotHaveActiveStudentsException.cs`, `CarMustNotHaveActiveStudentsException.cs`, `TeacherAssignmentMustNotHaveActiveStudentsException.cs` | **New** | 1 |
| `src\DrivingLessons.Domain\Repositories\IStudentRepository.cs`, `src\DrivingLessons.Infrastructure\EntityFramework\Repositories\StudentRepository.cs` | `FindByTeacherAsync`, `FindByCarAsync` | 1 |
| `src\DrivingLessons.Application\Commands\DeleteTeacher\DeleteTeacherInteractor.cs`, `DeleteCar\DeleteCarInteractor.cs`, `UnassignCarFromTeacher\UnassignCarFromTeacherInteractor.cs` | Load Students, pass them | 1 |
| `tests\DrivingLessons.Domain.Test\Entities\TeacherTest.cs`, `CarTest.cs` | New guard tests; existing calls pass `[]` | 1 |
| `tests\DrivingLessons.Application.Test\Commands\DeleteTeacherInteractorTest.cs` | Student repository fake, guard tests | 1 |
| `tests\DrivingLessons.Application.Test\Commands\DeleteCarInteractorTest.cs`, `UnassignCarFromTeacherInteractorTest.cs` | **New** | 1 |
| `tests\DrivingLessons.Application.Test\Commands\ImportRosterInteractorTest.cs` | Line ~563 passes `[]` | 1 |
| `src\DrivingLessons.Presentation.Web\Filters\ApiExceptionFilter.cs` | `params` for the three exceptions | 2 |
| `tests\DrivingLessons.Application.Test\Filters\ApiExceptionFilterTest.cs` | Four tests | 2 |
| `client\public\i18n\he.json`, `en.json` | Three `errors.*` keys | 2 |
| `client\src\app\core\services\toast.service.ts`, `.spec.ts` | `apiError(error, isolatedParams)` | 3 |
| `client\src\app\features\teachers\state\teachers.store.ts`, `cars.store.ts`, their specs | Pass the isolated params | 3 |

## Execution Order

| # | File | Task | Commit point |
|---|------|------|--------------|
| 1 | [task-01-active-students-guards.md](task-01-active-students-guards.md) | Domain guards, exceptions, repository methods, interactors, domain + interactor tests | ✅ own commit |
| 2 | [task-02-refusal-problem-details.md](task-02-refusal-problem-details.md) | `params` in `ApiExceptionFilter`, filter tests, translations, Postgres smoke | ✅ own commit |
| 3 | [task-03-client-refusal-toasts.md](task-03-client-refusal-toasts.md) | `apiError` isolated params, store calls, specs | ✅ own commit |
| 4 | [task-04-verify-and-pr.md](task-04-verify-and-pr.md) | Full suites, browser verification (Hebrew RTL, English), #95 merge note, PR | ✅ PR |

The PR targets `82-users-and-roles`, says `Closes #91` (it won't auto-close on a non-default base; close it when the epic merges), and references the parent spec #82.
