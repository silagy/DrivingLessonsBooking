# #108: Refuse Reactivating a Student Whose Teacher or Car Was Removed - Task Index

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

Per-task breakdown of issue [#108](https://github.com/silagy/DrivingLessonsBooking/issues/108), a slice of spec [#82](https://github.com/silagy/DrivingLessonsBooking/issues/82). Each task has its own file and is self-contained. Execute the tasks **in order**, one commit each. Every commit references #108.

**Goal:** Reactivating a Student is refused (409) while their Teacher is deleted, their Car is deleted, or their Car is not assigned to their Teacher, so decision #23 ("a student's car is always one of their teacher's cars") holds at all times. The Students screen shows each refusal in its existing Reactivate error toast. Students whose Teacher or Car was deleted stay on the Students screen, marked "Teacher deleted" / "Car deleted", instead of disappearing.

**Architecture:**
- Domain: `Student.Reactivate()` becomes `Student.Reactivate(Teacher teacher, Car car)`. Its guards run in this order: `MustBeInactive()` (existing), `TeacherMustNotBeDeleted(teacher)` → new `StudentTeacherMustNotBeDeletedException`, `CarMustNotBeDeleted(car)` → new `StudentCarMustNotBeDeletedException`, then the existing `MustBeCarOfTeacher(car, teacher)` → existing `StudentCarMustBeAssignedToTeacherException`. The event is still `StudentReactivated`.
- Application: `ReactivateStudentInteractor` loads the Student, then the Student's Teacher and Car **including deleted ones** through the new `ITeacherRepository.GetIncludingDeletedAsync(TeacherId)` / `ICarRepository.GetIncludingDeletedAsync(CarId)`. This is needed because Teacher and Car have a `!IsDeleted` query filter and `GetAsync` (`FindAsync`) would return null for a deleted one. It then calls `Reactivate(teacher, car)` and commits. `ImportRosterInteractor` passes the Teacher and Car it already resolved for the row.
- Presentation: no code change. `ApiExceptionFilter` maps any `DomainException` to 409 with the code derived from its class name (`studentTeacherMustNotBeDeleted`, `studentCarMustNotBeDeleted`, `studentCarMustBeAssignedToTeacher`). Tests pin the two new codes.
- Students list (task 2): `StudentQueries.FindAsync` / `GetAsync` join Teachers and Cars with `IgnoreQueryFilters()` and project the new `IsTeacherDeleted` / `IsCarDeleted`. The table shows a neutral `<app-deleted-flag>` next to a deleted Teacher or Car, which takes precedence over #95's "Not this Teacher's Car" flag. `students.flag.tipInactive` is reworded, because it tells the Administrator to reactivate first, which task 1 now refuses.
- Client messages (task 3): `StudentsStore.reactivate` already shows `toast.apiError(error)`, which renders `errors.{code}`, and reloads only for stale-toggle codes. This task adds the two `errors.*` keys, rewords the shared `errors.studentCarMustBeAssignedToTeacher`, and adds a store spec pinning that the three refusals toast without reloading.

**Tech Stack:** .NET 10, EF Core 10 + Npgsql, MSTest 4 + Shouldly + FakeItEasy; Angular 21 + Transloco, Vitest via `ng test`. No new package and no migration.

**Spec:** issue [#108](https://github.com/silagy/DrivingLessonsBooking/issues/108) (acceptance criteria) · predecessors [#91](https://github.com/silagy/DrivingLessonsBooking/issues/91) ([PR #107](https://github.com/silagy/DrivingLessonsBooking/pull/107), [plan](../../teachers/us-91-block-removal-with-active-students-plan/README.md)), [#94](https://github.com/silagy/DrivingLessonsBooking/issues/94) ([plan](../us-94-edit-deactivate-reactivate-plan/README.md)), [#95](https://github.com/silagy/DrivingLessonsBooking/issues/95) ([PR #105](https://github.com/silagy/DrivingLessonsBooking/pull/105)) · parent spec [#82](https://github.com/silagy/DrivingLessonsBooking/issues/82) · [docs/requirements.md](../../../requirements.md) decision #23 · `.claude\rules\*.md`

**Branch:** `108-reactivate-student-guards`, from `82-users-and-roles` **after #107 (#91) and #105 (#95) have merged into it**. Both were open on 2026-10-10. Task 1's code uses #91's signatures `Teacher.Delete(IReadOnlyCollection<Student>)`, `Car.Delete(IReadOnlyCollection<Student>)` and `Car.UnassignTeacher(Teacher, IReadOnlyCollection<Student>)`, and #95 edits the same `Student.cs` / `StudentTest.cs`. Check with `gh pr view 107 --json state` and `gh pr view 105 --json state`. Commit this plan on the branch before task 1. The PR targets `82-users-and-roles` (memory: every #82 story PRs into the epic branch).

## Current State (82-users-and-roles + #91 + #95)

| Piece | Today | File |
|-------|-------|------|
| `Student.Reactivate()` | Only `MustBeInactive()`, then `IsActive = true`, `StudentReactivated` | `src\DrivingLessons.Domain\Entities\Student.cs` |
| Car-of-Teacher guard | `private static void MustBeCarOfTeacher(Car car, Teacher teacher)` → `StudentCarMustBeAssignedToTeacherException(CarId, TeacherId)`; used by `Create`, `UpdateFromRoster`, `ChangeTeacher` | same |
| Soft delete | `Teacher.IsDeleted`, `Car.IsDeleted`; both configurations have `HasQueryFilter(x => !x.IsDeleted)`. Deleting a Teacher does **not** remove its Car assignments, so `car.IsAssignedTo(deletedTeacher)` can still be true. That is why the deleted checks are separate guards. | `Domain\Entities\Teacher.cs`, `Car.cs`, `Infrastructure\EntityFramework\EntityConfigurations\` |
| Restore | Teachers and Cars cannot be restored (only Users have Restore) | - |
| Teacher / Car repositories | `GetAsync` (`FindAsync`, filtered), `FindActiveAsync`, `Add` | `Domain\Repositories\ITeacherRepository.cs`, `ICarRepository.cs`, `Infrastructure\EntityFramework\Repositories\` |
| `ReactivateStudentInteractor` | Loads the Student (404 `studentNotFound`), `Reactivate()`, commit | `Application\Commands\ReactivateStudent\` |
| Roster import | `UpdateFromRoster(...)` then `if (!student.IsActive) student.Reactivate();` with the row's resolved (non-deleted, Car-of-Teacher-checked) Teacher and Car | `Application\Commands\ImportRoster\ImportRosterInteractor.cs` ~line 186-191 |
| Client Reactivate | One click from the row menu; `StudentsStore.reactivate` → success toast, or `toast.apiError(error)` and reload only for `STALE_TOGGLE_CODES` | `client\src\app\features\students\state\students.store.ts` |
| Students list / GET | `StudentQueries` inner-joins the filtered `Teachers` / `Cars`, so a Student of a deleted Teacher or Car is missing from the list (even "All") and `GET api/students/{id}` answers 404 (found reviewing #105) | `Infrastructure\EntityFramework\Queries\StudentQueries.cs` |
| Car flag (#95) | `<app-car-flag>` when `!isCarOfTeacher`; Inactive tip `students.flag.tipInactive` says "mark the Student as active first" | `client\...\students\ui\components\car-flag\`, `students.page.html` |
| Translations | `errors.studentCarMustBeAssignedToTeacher` = "הרכב הזה לא משויך למורה הזה. יש לרענן ולבחור שוב." / "This Car is not assigned to this Teacher. Refresh and choose again." | `client\public\i18n\he.json`, `en.json` |

## Decisions (made with the spec owner on 2026-10-10 or while planning; challenge on review)

| # | Decision |
|---|----------|
| 1 | **Block, don't flag.** Reactivate is refused. Relying on #95's "Not this Teacher's Car" flag was rejected (spec owner, 2026-10-10). |
| 2 | **Fixing a blocked Student is out of scope** (spec owner, 2026-10-10). Car not assigned → assign it back on Cars & teachers. Teacher or Car deleted → re-import the Student in the Roster with a current Teacher and Car. The import updates and reactivates them. Change Teacher / Change Car stay active-only. |
| 3 | **Guard order**: already active → Teacher deleted → Car deleted → Car not of Teacher. "Already active" stays first so a stale double-click still gets the stale-toggle reload. A deleted Teacher whose Car is still assigned reports the deletion, which is the more useful message. |
| 4 | **Two new exceptions**, `StudentTeacherMustNotBeDeletedException(StudentId, TeacherId)` and `StudentCarMustNotBeDeletedException(StudentId, CarId)`, named `{Entity}{Rule}Exception` with the Student as the entity whose rule it is. The Car-not-of-Teacher case **reuses** `StudentCarMustBeAssignedToTeacherException` through the existing `MustBeCarOfTeacher` (same rule, same code). |
| 5 | **No params on the new refusals.** The toast follows a one-click action on a named row, so the Administrator already knows which Student it is. |
| 6 | **`GetIncludingDeletedAsync`** on both repositories (`IgnoreQueryFilters()` + `FirstOrDefaultAsync(x => x.Id == id)`). Repositories return null; the interactor throws `TeacherNotFoundException` / `CarNotFoundException` (critical rule 6). Rows are never hard-deleted, so in practice those throws can't happen. |
| 7 | **`Reactivate` does not check that `teacher.Id == TeacherId` / `car.Id == CarId`.** Domain methods take resolved entities (critical rule 2) and the interactor test pins that the Student's own IDs are looked up. A wrong argument is a programming error, not a business rule. |
| 8 | **Reworded shared message** (spec owner, 2026-10-10) so it fits Add Student, Change Teacher / Change Car and Reactivate: he "הרכב לא משויך למורה של התלמיד. אפשר לשייך אותו למורה במסך רכבים ומורים, או לבחור רכב אחר." / en "The Car is not assigned to the Student's Teacher. Assign it to the Teacher on Cars & teachers, or choose another Car." |
| 9 | **Refusals don't reload the list.** They aren't stale toggles: the row is still accurately Inactive. This is the store's existing behaviour; task 3 adds a spec to pin it. |
| 10 | **Smoke runs on a throwaway database** (`drivinglessons_us108_smoke`) on the compose Postgres (memory: use `drivinglessonsbooking-postgres-1`). |
| 11 | **Students of a deleted Teacher or Car stay listed** (spec owner, 2026-10-10: added to #108's scope). Both queries ignore the soft-delete filter on the joins and expose `isTeacherDeleted` / `isCarDeleted`. Edit details opens for them again, because GET no longer 404s. The Teacher filter dropdown still lists only current Teachers, so these Students show under "All Teachers". |
| 12 | **Deleted marker**: a neutral (surface-gray) dotted-underline flag with a trash icon, "המורה נמחק" / "הרכב נמחק", keyboard-focusable with a tooltip that gives the fix (Roster re-import). It has the same shape as #95's car flag but isn't red: the Student is Inactive and nothing is wrong yet. There's no Claude Design frame for it; the PR asks the spec owner to confirm. A deleted marker replaces the "Not this Teacher's Car" flag on the same row. |
| 13 | **`students.flag.tipInactive` is reworded** to "assign it to {{teacher}} on Cars & teachers, then mark the Student as active", the order task 1 enforces. |

## Global Constraints

- No comments in C# (test markers `//given //when //then` only). Typed IDs and value objects into the domain; raw `Guid` only at the interactor/controller boundary.
- Operations are not idempotent; every refusal is a domain exception → 409 with `code`.
- Requirements terminology: Student, Teacher, Car, Roster; "Inactive Student".
- Every user-visible string is a translation key; Hebrew first; plain hyphen and three dots only (`translations.spec.ts`).
- Client build/test: memory "Client build / npm workaround" (node v26.4.0 + local `ng`).

## Review Focus

- **A deleted Teacher whose Car is still assigned to them**: `Teacher.Delete` keeps the Car's assignment, so without the separate deleted guard Reactivate would pass. Task 1 pins this with `Reactivate__Teacher_Must_Not_Be_Deleted`, which deletes the Teacher without unassigning the Car.
- **Loading a deleted Teacher or Car**: `GetAsync` would return null and turn the refusal into a 404 `teacherNotFound` / `carNotFound`. Task 1's interactor tests fake only `GetIncludingDeletedAsync`. Task 4's Postgres smoke proves the query filter is really bypassed (unit tests can't).
- **Roster import reactivating a Student**: still has to work with the new signature. The existing `ImportRosterInteractorTest` reactivation test must stay green (task 1, step 7).
- **Stale double-click on Reactivate**: an already active Student must still answer `studentAlreadyActive` and trigger the reload, even if their Teacher is now deleted. Task 1 pins this with `Reactivate__Must_Not_Be_Active_Before_Teacher_Check`.
- **The reworded Car message on the Add Student and Change Car dialogs**: those dialogs show `errors.studentCarMustBeAssignedToTeacher` as a refusal inside the dialog. Task 4's browser check reads it there in both languages.
- **A Student of a deleted Teacher or Car on the Students screen**: they must appear under "All" with the deleted marker, open in Edit details, and get the task 1 refusal from Reactivate. There are no query unit tests, so task 4's smoke and browser check cover this.

## Tasks

| # | Task | Commit |
|---|------|--------|
| 1 | [Reactivate guards: domain, repositories, interactors](task-01-reactivate-guards.md) | `feat(students): refuse reactivating a Student whose Teacher or Car was removed (#108)` |
| 2 | [Keep Students of a deleted Teacher or Car on the Students screen](task-02-list-students-of-removed-teacher-or-car.md) | `feat(students): keep Students of a deleted Teacher or Car on the Students screen (#108)` |
| 3 | [Problem codes and Students screen messages](task-03-problem-codes-and-messages.md) | `feat(students): explain why a Student can't be reactivated (#108)` |
| 4 | [Verify and open the PR](task-04-verify-and-pr.md) | none (PR) |
