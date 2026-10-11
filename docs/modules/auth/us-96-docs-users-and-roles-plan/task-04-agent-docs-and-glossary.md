# Task 4 of 5: CLAUDE.md, the CONTEXT.md glossary and api-guidelines.md

> Part of [#96: Docs for the User aggregate, the Roster amendment, requirements and glossary](README.md). Work on branch `96-docs-users-and-roles`. Requires task 1 committed (CLAUDE.md links to ADR 0006).

**Files:**
- Modify: `CLAUDE.md` (Reference Documentation table, critical rule 8, Domain Snapshot)
- Modify: `CONTEXT.md` (Change Teacher, Roster)
- Modify: `.claude\rules\api-guidelines.md` (section "Auth Slice Exception (US-01)")

**Interfaces:**
- Consumes: `docs/decisions/0006-user-aggregate-and-roles.md` (task 1).
- Produces: nothing later tasks build on. Task 5 sweeps these files.

**Why:** #96 AC 4 ("CLAUDE.md domain snapshot: Student is identified by national ID (not email); the User aggregate is added") and AC 5 (the glossary is committed; it is, at `aefc9ee`, and this task makes it consistent with its own _Avoid_ list). The `api-guidelines.md` fix is plan decision 9: the section is auto-loaded for every controller task and says sign-in has no domain aggregate.

- [ ] **Step 1: Confirm CONTEXT.md is tracked**

```bash
git ls-files --error-unmatch CONTEXT.md && git log --oneline -1 -- CONTEXT.md
```

Expected: `CONTEXT.md`, then a commit line (`aefc9ee docs(auth): plan for #84 ...`). If it isn't tracked, `git add CONTEXT.md` in step 5's commit; AC 5 requires it.

- [ ] **Step 2: CLAUDE.md**

In the **Reference Documentation** table, after the row `| Product behavior questions | \`docs/requirements.md\` |`, add:

```markdown
| Domain terms and words to avoid | `CONTEXT.md` |
```

In **Critical Rules**, replace rule 8:

```markdown
8. **Use requirements terminology** — entity, state, and flow names come from `docs/requirements.md` (Publication, Week Schedule, Slot, Submission, Slot Request). Never invent alternative names
```

with

```markdown
8. **Use requirements terminology** — entity, state, and flow names come from `docs/requirements.md` and the `CONTEXT.md` glossary (Publication, Week Schedule, Slot, Submission, Slot Request, User, Role, Change Teacher, Change Car, Inactive Student). Never invent alternative names
```

In **Domain Snapshot**, replace the **User** bullet:

```markdown
- **User** — a person who can sign in; exactly one Role (`Administrator` | `Teacher`); a Teacher-Role User is linked to exactly one Teacher; sign-in email unique case-insensitively; soft-deleted; replaces the infrastructure admin record (#84)
```

with

```markdown
- **User** — a person who can sign in; exactly one Role (`Administrator` | `Teacher`); a Teacher-Role User is linked to exactly one Teacher, fixed at creation, and reaches only that Teacher's Week Schedules and Publications; sign-in email unique case-insensitively; soft-deleted (Deleted User) and restorable; a security stamp signs the User out on their next request after delete, Role or password change ([ADR 0006](docs/decisions/0006-user-aggregate-and-roles.md))
```

Replace the **Teacher** bullet:

```markdown
- **Teacher** — no longer owns cars (decision #21); scheduling is per teacher, not per car
```

with

```markdown
- **Teacher** — no longer owns cars (decision #21); scheduling is per teacher, not per car; soft-deleted, refused while an active User or active Students depend on it
```

Replace the **Publication** bullet's start `per week schedule;` with `per week, school-wide (aggregates that week's Week Schedules, one link for every teacher);`, so the bullet reads:

```markdown
- **Publication** — per week, school-wide (aggregates that week's Week Schedules, one link for every teacher); state machine `Draft → Published → Open → Closed`, reopenable; carries the unguessable link token and submission window; every close increments the Excel version
```

Replace the **Student** bullet:

```markdown
- **Student** — identified by email; remembers profile and default teacher
```

with

```markdown
- **Student** — identified by national ID; added by the Roster or by an Administrator; belongs to one Teacher and learns on one of that Teacher's Cars (decision #23); Change Teacher / Change Car; deactivated (Inactive Student), never deleted
```

(Snapshot bullets keep the `—` after the bold name, like their neighbours.)

- [ ] **Step 3: CONTEXT.md**

Replace

```markdown
**Change Teacher**:
Moving a Student to another Teacher together with one of that Teacher's Cars.
```

with

```markdown
**Change Teacher**:
Giving a Student another Teacher together with one of that Teacher's Cars. The Student's existing Submissions stay with the Publication they were made to.
```

Replace

```markdown
**Roster**:
A bulk file of Students that adds new Students and updates existing ones; it never deactivates a Student.
```

with

```markdown
**Roster**:
A bulk file of Students that adds new Students and updates existing ones by national ID; it never deactivates a Student, and a row whose Car isn't one of its Teacher's Cars is rejected.
```

- [ ] **Step 4: api-guidelines.md**

First confirm the claim the new text makes:

```bash
git grep -n "class AuthController" -- src/DrivingLessons.Presentation.Web/Controllers/AuthController.cs
git grep -c "FromServices" -- src/DrivingLessons.Presentation.Web/Controllers/User src/DrivingLessons.Presentation.Web/Controllers/Me
```

Expected: `AuthController(LoginInteractor login)` (constructor injection), and every Users / Me controller file with a count of at least 1.

In `.claude\rules\api-guidelines.md`, replace the whole section

```markdown
## Auth Slice Exception (US-01)

The admin sign-in slice (`docs/modules/auth/us-01-admin-sign-in-plan.md`) is deliberately **pre-aggregate infrastructure** — no domain aggregate, and its code style deviates from these rules in places (constructor injection, `Handle(...)` naming). That deviation is accepted **for that slice only**. Every aggregate endpoint (Teacher, WeekSchedule, Publication, Student, Submission) follows these rules: `ExecuteAsync`, `[FromServices]` injection, CQRS controller split, no comments.
```

with

```markdown
## Auth Controller Exception

`AuthController` (`POST /api/auth/login`) predates these rules: it takes `LoginInteractor` by constructor injection and is not split into command and query controllers. That deviation is accepted **for that controller only**. Sign-in itself goes through the `User` aggregate and `LoginInteractor.ExecuteAsync` ([ADR 0006](../../docs/decisions/0006-user-aggregate-and-roles.md)). Every other endpoint, Users and Me included, follows these rules: `ExecuteAsync`, `[FromServices]` injection, CQRS controller split, no comments.
```

- [ ] **Step 5: Check and commit**

```bash
git grep -n -i "identified by email\|pre-aggregate\|infrastructure admin record\|^Moving a Student" -- CLAUDE.md CONTEXT.md .claude/rules
test -e docs/decisions/0006-user-aggregate-and-roles.md && echo ok
```

Expected: the grep prints nothing; `ok`. (The link in `api-guidelines.md` is relative to `.claude\rules\`, so `../../docs/...` resolves to the repository root's `docs`. Task 5 checks it.)

```bash
git add CLAUDE.md CONTEXT.md .claude/rules/api-guidelines.md
git commit -m "docs: CLAUDE.md snapshot, glossary and auth controller note match Users and national-ID Students (#96)"
```

End the commit message with the attribution trailer from the session's instructions.
