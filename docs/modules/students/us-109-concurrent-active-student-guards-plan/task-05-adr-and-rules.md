# Task 5 of 6: ADR 0007, the architecture rules, README and requirements §10

> Part of [#109: Concurrent admin writes can't break the active-Student guards](README.md). Work on branch `109-concurrent-active-student-guards`. Requires tasks 1 to 4 committed (the ADR describes them).

**Files:**
- Create: `docs\decisions\0007-locking-for-the-active-student-guards.md`
- Modify: `.claude\rules\ddd-architecture.md` (the "Deliberately Omitted" list, line ~716)
- Modify: `.claude\rules\client-architecture.md` (line ~203)
- Modify: `README.md` (the ADR row of the Documentation table)
- Modify: `docs\requirements.md` (header, §10)

**Interfaces:**
- Consumes: the names from tasks 1 to 4: `ITeacherAndCarLocks.LockAsync` and `LockAllAsync`, `ConcurrentChangeException`, code `concurrentChange`, the Student `xmin` row version, and the migration `AddStudentConcurrencyToken`.
- Produces: the ADR path `docs/decisions/0007-locking-for-the-active-student-guards.md`, which task 6's PR description links.

**Why:** #109 AC 1, "The chosen approach is recorded as an ADR". The two rules files are auto-loaded for every backend and client task, and both say the backend has no locking, which stops being true with this branch.

- [ ] **Step 1: Write the ADR**

Create `docs\decisions\0007-locking-for-the-active-student-guards.md`:

```markdown
# ADR 0007: Row Locks for the Active-Student Guards, and a Token on Student

**Status:** Accepted
**Date:** <today's date, e.g. 12 October 2026>

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

**An optimistic token on Student.** Postgres's `xmin` system column is mapped as Student's row version (a shadow property, so the domain class doesn't change; no DDL). An `UPDATE students` saves only if the row hasn't changed since it was read. Otherwise `CommitAsync` throws `ConcurrentChangeException`, answered as 409 `concurrentChange`: "Someone else changed this at the same moment. Refresh the page and try again." This covers two requests on the same Student, which the Teacher and Car locks can't order because they may lock different rows.

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
- Verified by interactor tests that pin "lock before read" and by a Postgres smoke that queues real requests behind a held lock (#109 task 6). There is no automated concurrency test, because there is no database test project.
- Still open: two Add Student requests with the same national ID at once fail on the unique index rather than with `studentNationalIdAlreadyInUse`. That's a different invariant, outside #109.
```

Use plain `-` throughout; no long dashes or ellipses.

- [ ] **Step 2: `ddd-architecture.md`**

In the "Deliberately Omitted (keep it simple)" list, replace the bullet

```markdown
- ETag / If-Match optimistic locking — add per-aggregate concurrency tokens later if concurrent admin edits become real (concurrent student submissions are protected by the DB transaction)
```

with

```markdown
- ETag / If-Match on the API: concurrent admin writes are settled on the server instead, by row locks on Teacher and Car and an `xmin` token on Student ([ADR 0007](../../docs/decisions/0007-locking-for-the-active-student-guards.md)); concurrent student submissions are protected by the DB transaction
```

Right after that list ends (before the next heading), add:

```markdown
**Cross-aggregate guards lock first.** A command that allows or refuses a change based on another aggregate's rows (a Teacher's or Car's active Students, a Car's Teacher assignments) calls `ITeacherAndCarLocks.LockAsync(teacherIds, carIds)` once, before it reads them, or `LockAllAsync()` when it can only resolve them by reading (the Roster import). The interactor test pins the order with FakeItEasy's `.Then(...)`. See [ADR 0007](../../docs/decisions/0007-locking-for-the-active-student-guards.md).
```

- [ ] **Step 3: `client-architecture.md`**

Replace

```markdown
409 means the user's view was stale (e.g., publishing an already-published Publication) — surface a translated message and refresh the affected state. There is **no ETag/If-Match handling** — the backend deliberately omits optimistic locking.
```

with

```markdown
409 means the user's view was stale (e.g., publishing an already-published Publication) — surface a translated message and refresh the affected state. There is **no ETag/If-Match handling**: the server settles concurrent admin writes itself ([ADR 0007](../../docs/decisions/0007-locking-for-the-active-student-guards.md)), and a request that lost a race gets the rule it now breaks or `concurrentChange`, shown like any other 409.
```

(The first sentence, with its existing long dash, is unchanged.)

- [ ] **Step 4: README and requirements**

In `README.md`, in the Documentation table's ADR row, after the `[0006 User aggregate with two Roles](docs/decisions/0006-user-aggregate-and-roles.md)` link, add `, [0007 locking for the active-Student guards](docs/decisions/0007-locking-for-the-active-student-guards.md)`.

In `docs\requirements.md`:
- Set `**Version:** 1.6` and `**Date:**` to today's date.
- Right after `**Changelog:** `, insert `v1.6 - concurrent Administrator changes can no longer break decision #23: the later of two conflicting requests is refused as if it came a second later (§10, [ADR 0007](decisions/0007-locking-for-the-active-student-guards.md), issue #109). `, with the trailing space before `v1.5`.
- In §10, after the bullet `- The system must tolerate concurrent submissions without losing or duplicating requests`, add:

```markdown
- Two Administrators acting at the same moment never leave an active student on a deleted teacher or car, or on a car that isn't their teacher's: the later request is refused as if it came a second later, and two changes to the same student at once refuse the later one with a "changed at the same moment" message (see [ADR 0007](decisions/0007-locking-for-the-active-student-guards.md))
```

- [ ] **Step 5: Check and commit**

```bash
git grep -n "deliberately omits optimistic locking\|add per-aggregate concurrency tokens later" -- .claude/rules
for f in .claude/rules/ddd-architecture.md .claude/rules/client-architecture.md; do test -e "$(dirname $f)/../../docs/decisions/0007-locking-for-the-active-student-guards.md" && echo "ok $f"; done
test -e docs/decisions/0007-locking-for-the-active-student-guards.md && echo ok
```

Expected: the grep prints nothing; two `ok` lines for the rules files; `ok`.

```bash
git add docs/decisions/0007-locking-for-the-active-student-guards.md .claude/rules/ddd-architecture.md .claude/rules/client-architecture.md README.md docs/requirements.md
git commit -m "docs(students): ADR 0007 - row locks and a Student token for the active-Student guards (#109)"
```

End the commit message with the attribution trailer from the session's instructions.
