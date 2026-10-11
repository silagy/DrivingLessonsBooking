# ADR 0007: Row Locks for the Active-Student Guards, and a Token on Student

**Status:** Accepted
**Date:** 11 October 2026

## Context

Decision #23 says a Student's Car is always one of their Teacher's Cars, and #91, #95 and #108 refuse every change that would leave an active Student on a deleted Teacher or Car, or on a Car their Teacher doesn't teach on. Each of those commands reads the data the rule needs, checks it, then commits. Nothing stopped a second request from committing in between ([#109](https://github.com/silagy/DrivingLessonsBooking/issues/109)):

- Delete Teacher, Delete Car or Unassign finds no active Students, while Add Student, Change Teacher, Change Car, Reactivate or a Roster import puts one there.
- Add Student, Change Teacher, Change Car or Reactivate finds the Car assigned to the Teacher, while Unassign removes the assignment.
- Change Car checks the new Car against the Student's Teacher, while Change Teacher replaces that Teacher.

It takes two Administrators acting within the same moment, so it is rare, but the result breaks the invariant silently. `ddd-architecture.md` had deferred locking until concurrent admin edits became real.

## Decision

**Row locks on the Teacher and Car rows, taken before the check.** An application port, `ITeacherAndCarLocks`, opens a transaction and runs `SELECT ... FOR UPDATE` on the Teacher and Car rows a command depends on: Teachers first, then Cars, each ordered by id. Each of these commands calls it once, before it reads the Teacher, the Car or their Students:

| Command | Locks |
|---|---|
| Delete Teacher | the Teacher |
| Delete Car | the Car |
| Unassign a Car from a Teacher | the Teacher and the Car |
| Add Student, Change Teacher | the new Teacher and Car |
| Change Car, Reactivate | the Student's Teacher and the (new) Car |
| Roster import | every Teacher and Car (`LockAllAsync`), since it resolves them by name |

The second of two conflicting requests waits for the first to commit, then reads the committed state. The existing rule refuses it with its usual translated message, for example `teacherMustNotHaveActiveStudents`, `teacherAssignmentMustNotHaveActiveStudents` or `studentCarMustBeAssignedToTeacher`. A request whose Teacher or Car was deleted meanwhile gets the same 404 (`teacherNotFound`, `carNotFound`) it would get a second later. `CommitAsync` commits the transaction; a refused command never commits, and the transaction rolls back when the request's `DbContext` is disposed.

**An optimistic token on Student.** Postgres's `xmin` system column is mapped as Student's row version (a shadow property, so the domain class doesn't change; no DDL, and the migration `AddStudentConcurrencyToken` generates no SQL in either direction). An `UPDATE students` saves only if the row hasn't changed since it was read. Otherwise `CommitAsync` throws `ConcurrentChangeException`, answered as 409 `concurrentChange`: "Someone else changed this at the same moment. Refresh the page and try again." This covers two requests on the same Student, which the Teacher and Car locks can't order because they may lock different rows.

**No deadlocks by construction.** Each command takes its locks in one call, in one global order (Teachers, then Cars, by id). The Student row is only ever written last.

## Alternatives Considered

| Alternative | Verdict |
|---|---|
| `xmin` tokens on Teacher and Car, bumped by every dependent Student write | Rejected: every Student write would have to update its Teacher and Car rows just to change their version, and Unassign writes `car_teachers`, not `cars`. The loser would get a generic conflict instead of the rule's message |
| Serializable transactions for these commands | Rejected: it only protects transactions that are all serializable, it can abort unrelated work through predicate-lock false positives, and the loser gets a serialization failure that would need a retry loop or a generic message |
| Accept the risk and document it | Rejected: the locks cost one call per command and no new UI, and the invariant is one the whole Students and Cars & Teachers design relies on |
| Advisory locks keyed on the Teacher or Car id | Rejected: same effect as row locks, with keys derived from ids by hand; the rows already exist (soft delete) and can be locked directly |
| API-level ETag / If-Match | Rejected: it addresses stale screens, not two requests racing on the server; the existing non-idempotent operations already refuse stale actions |

## Consequences

- Every interactor that checks an active-Student rule against another aggregate's rows takes `ITeacherAndCarLocks` and calls it before reading. A new command of that kind must do the same; `ddd-architecture.md` says so.
- A Roster import briefly blocks Teacher and Car removals and Student changes for its duration. At one school's size this is a fraction of a second.
- Any change to a Student, including Change Details and Deactivate, now fails with 409 `concurrentChange` if another request changed that Student after it was read.
- The mapping of EF's concurrency failure to 409 `concurrentChange` lives in `CommitAsync` and applies to every command: an `UPDATE` or `DELETE` that finds its row changed or gone answers 409 instead of 500. Today only Student carries a token.
- Verified by interactor tests that pin "lock before read" and by a Postgres smoke that queues real requests behind a held lock (#109 task 6). There is no automated concurrency test, because there is no database test project.
- Still open: two Add Student requests with the same national ID at once fail on the unique index rather than with `studentNationalIdAlreadyInUse`. That's a different invariant, outside #109.
