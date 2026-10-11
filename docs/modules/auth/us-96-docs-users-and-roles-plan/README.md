# #96: Docs for the User aggregate, the Roster amendment, requirements and glossary - Task Index

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

Per-task breakdown of issue [#96](https://github.com/silagy/DrivingLessonsBooking/issues/96), the docs slice of spec [#82](https://github.com/silagy/DrivingLessonsBooking/issues/82). Each task has its own file and is self-contained. Execute the tasks **in order**, one commit each. Every commit references #96.

**Goal:** The documentation describes the system as #84 to #95 and #108 built it: a new ADR records the User aggregate and its two Roles, `requirements.md` v1.5 reverses decision #1 and adds Students managed by hand, and the agent-facing docs (CLAUDE.md, `CONTEXT.md`, `api-guidelines.md`) use the new language. Future work then starts from documents that match the code.

**Architecture:** Docs only. No code, no migration, no test project change.
- New `docs\decisions\0006-user-aggregate-and-roles.md`. It supersedes ADR 0004, whose status line is pointed at it. Its body stays as written, because an ADR is a historical record.
- `docs\requirements.md` goes to v1.5 in two passes. Task 2 covers access: personas, scope, a new §5.8 User, the flows, decision #24 superseding #1, and the deferred list. Task 3 covers Students: §5.1, §5.2, §5.5, the student flow, decision #25 amending #19, and the out-of-scope list.
- ADR 0003 already carries the #92 amendment (no longer the single source of truth, never deactivates, Car-not-of-Teacher rows rejected, national-ID corrections made at the source). Task 3 adds one paragraph: Students still can't enter themselves.
- `CONTEXT.md` is already committed (`aefc9ee`). Task 4 fixes the one entry that breaks its own rules: Change Teacher is defined as "Moving", a word it lists under _Avoid_.

**Tech Stack:** Markdown. Checks use `git grep` and a short bash link check.

**Spec:** issue [#96](https://github.com/silagy/DrivingLessonsBooking/issues/96) (acceptance criteria) · parent spec [#82](https://github.com/silagy/DrivingLessonsBooking/issues/82) (Implementation Decisions, Out of Scope) · [ADR 0004](../../../decisions/0004-no-admin-domain-aggregate.md) (its "Revisit if" list) · [ADR 0003](../../../decisions/0003-roster-csv-and-weekly-link-model.md) (the #92 amendment) · [CONTEXT.md](../../../../CONTEXT.md) · the plans of #84 to #95 and #108 under `docs\modules\auth\`, `roster\`, `students\`, `teachers\`, for what was actually built.

**Branch:** `96-docs-users-and-roles`, from `82-users-and-roles` at `a90015b` (contains every #82 story through #108). This plan is committed on the branch before task 1. The PR targets `82-users-and-roles` (memory: every #82 story PRs into the epic branch).

## Current State

| Document | Today | Gap against #96 |
|----------|-------|-----------------|
| `docs\decisions\0004-no-admin-domain-aggregate.md` | Status: "Superseded by issue #84 ... The replacement ADR comes with the docs slice of spec #82" | Point the status at ADR 0006 |
| `docs\decisions\0006-*` | Doesn't exist | Write it (task 1) |
| `docs\decisions\0003-roster-csv-and-weekly-link-model.md` | Amendment of 5 October 2026 (#92) covers every point in the AC | The "ad-hoc students" alternative still argues "the roster is authoritative". Add one paragraph (task 3) |
| `docs\requirements.md` v1.4 | Decision #1 "single login, no teacher logins". Out of scope: "Logins for teachers". Deferred: "Teacher logins". Personas: Teacher "No login in v1". §5.5: students "pre-loaded from the admin-uploaded roster". §7: "New students are added by the admin re-uploading the roster". No User section, no Inactive Student in the student flow | Tasks 2 and 3 |
| `CLAUDE.md` | Domain Snapshot: "**Student** - identified by email". User bullet ends "replaces the infrastructure admin record (#84)". Critical rule 8 lists only five terms | Task 4 |
| `CONTEXT.md` | Committed, all ten AC terms present. Change Teacher: "Moving a Student to another Teacher..." with _Avoid_: "... Move" | Task 4 |
| `.claude\rules\api-guidelines.md` "Auth Slice Exception (US-01)" | Says sign-in is "pre-aggregate infrastructure - no domain aggregate" and uses `Handle(...)` | False since #84 (`LoginInteractor.ExecuteAsync`, `User` aggregate). Auto-loaded for every controller task. Task 4 |
| `README.md` | ADR list names only 0001 and 0002. Status: "Requirements approved. Implementation not started." | Task 1 (ADR list, glossary row, status) |

## Decisions (made while planning, challenge on review)

| # | Decision |
|---|----------|
| 1 | **ADR number and name: `0006-user-aggregate-and-roles.md`.** Next free number. ADR 0004's body is not edited; only its status line changes, as #84 did. |
| 2 | **The ADR records what was built, checked against the code**, not the spec's intent where the two differ. Two differences: the code refuses *any* change of your own Role (`UserMustNotChangeOwnRoleException`), not only demoting yourself. A Teacher-role User asking for another Teacher's data gets **404** (`MayReach` → `*NotFoundException`), not 403. Task 1 step 2 re-checks each fact before the ADR is written. |
| 3 | **The ADR says why User is not in a separate Identity bounded context.** ADR 0004 predicted one. It didn't happen: `User` sits in `DrivingLessons.Domain` and refers to Teacher only by `TeacherId`. The ADR records that choice and when to revisit it. |
| 4 | **The ADR lists Alternatives Considered** (keep the infrastructure record, ASP.NET Core Identity's user store, short tokens instead of a per-request check, a separate bounded context). They are written after the fact from the spec and the #84 to #90 plans. Read them on review as the reasons for the design, not as minutes of a meeting. |
| 5 | **requirements.md is one version bump, v1.5, written in two commits.** Task 2 creates the v1.5 changelog entry and task 3 extends it. Two reviewable halves (access, Students) that a reviewer can accept or reject separately. |
| 6 | **Superseded decisions keep their row**, struck through and pointing at the new one: `~~...~~ **superseded by #24**`, as #3, #6, #10 and #20 already do. #19 is amended in place the same way. No decision row is deleted. |
| 7 | **New decisions: #24 (Users with exactly one of two Roles, supersedes #1) and #25 (Administrators manage Students by hand, amends #18 and #19).** The Teacher / Car removal guards (#91) and the reactivation guard (#108) go into #25 and §5.1, §5.2, §5.5. They enforce decision #23 and don't need a decision of their own. |
| 8 | **Requirements wording follows the document's own style**: lower-case teacher / student / car in running prose, as v1.4 does, with the glossary terms that have no plain-English twin capitalised and bold where they are defined (**User**, **Role**, **Administrator**, **Teacher Role**, **Temporary Password**, **Deleted User**, **Change Teacher**, **Change Car**, **Inactive Student**). The ADR and CONTEXT.md use the capitalised glossary form throughout, as ADR 0003's amendment does. |
| 9 | **A few fixes outside the AC** because they are wrong and agents read them: `api-guidelines.md`'s "Auth Slice Exception" (auto-loaded for every controller), README's ADR list and status, and CLAUDE.md's Publication bullet ("per week schedule", wrong since ADR 0003) and Teacher bullet (no removal guard). All are small. Drop them on review if you'd rather keep #96 to its AC. |
| 10 | **README status becomes durable text** ("v1 is implemented, including Users and Roles and Student management from spec #82"), not a branch-specific note that goes stale when the epic merges. |
| 11 | **No `tech-stack.md` or `running-the-project.md` change.** `tech-stack.md` says nothing about auth. #84 already updated `running-the-project.md` for first-Administrator seeding. Task 5 re-greps both to confirm. |

## Global Constraints

- Docs only: no file under `src\`, `tests\` or `client\` changes. If a step finds the code contradicts a doc, record the difference in the doc as the code behaves and tell the user. Never change code in this slice.
- New prose uses a plain `-`, never a long dash or an ellipsis character, matching the v1.4 changelog and ADR 0003's amendment. Exception: CLAUDE.md Domain Snapshot bullets keep the existing `—` after the bold name, like their neighbours.
- Terminology comes from `CONTEXT.md`. Never write the _Avoid_ words in new prose: "account", "admin user", "login" as a noun for a User (the verb "sign in" is fine), "permission"/"group" for Role, "instructor", "pupil", "assign"/"transfer"/"reassign"/"move" for Change Teacher, "assign car" for Change Car, "deleted student"/"archived", "disabled"/"suspended"/"removed" for a Deleted User. Existing text that uses "admin" for the Administrator persona is left alone unless the step rewrites that sentence.
- History is kept: a superseded decision or ADR keeps its text and gains a pointer (decision 6 above).
- Each commit message is `docs(<scope>): <what> (#96)` and ends with the attribution trailer from the session's instructions.

**Commands** (from the repository root, bash):

```bash
git grep -n -i "<phrase>" -- docs README.md CLAUDE.md CONTEXT.md .claude/rules
git diff --stat 82-users-and-roles...HEAD
```

No build or test run is needed: `SourceTextTest` scans `*.cs` only, and `translations.spec.ts` scans `client\public\i18n` only.

## Review Focus

1. **A stale sentence survives in a section no task rewrote** (for example "no teacher logins", "identified by email", "the roster is authoritative", "re-uploading the roster"). Expected: no document still says Teachers can't sign in or that Students only come from the roster. Covered by task 5 step 1 (stale-phrase sweep across every doc).
2. **The ADR describes behaviour the code doesn't have** (403 where the code answers 404; "can't demote yourself" where the code refuses any change of your own Role; claims about the token that `AuthClaims` doesn't carry). Expected: every factual claim in ADR 0006 matches a named file. Covered by task 1 step 2 (fact check against the code before writing) and step 4 (each claim's file exists).
3. **A superseded decision is deleted or rewritten instead of struck through.** Expected: decisions #1, #18 and #19 keep their original words, and the decisions log keeps rows 1 to 23 in order. Covered by task 2 step 4 and task 3 step 5 (row count and `~~` checks).
4. **New prose uses a glossary _Avoid_ word** ("account", "transfer", "reassign", "move a Student"). Expected: none in the added lines. Covered by task 5 step 2 (grep over the diff's added lines).
5. **A relative link points nowhere** (README → ADR 0006, ADR 0004 → 0006, requirements → `decisions/0006-...`, CLAUDE.md → `CONTEXT.md`). Expected: every relative link in a changed file resolves. Covered by task 5 step 3 (link check script).

## File Structure

| File | Change | Task |
|------|--------|------|
| `docs\decisions\0006-user-aggregate-and-roles.md` | **New** ADR | 1 |
| `docs\decisions\0004-no-admin-domain-aggregate.md` | Status line → ADR 0006 | 1 |
| `README.md` | ADR list, glossary row, status | 1 |
| `docs\requirements.md` | v1.5: access (header, scope, personas, §5.8, §6, §8.2, #1 / #24, §12) | 2 |
| `docs\requirements.md` | v1.5: Students (changelog, scope, Student persona, §5.1, §5.2, §5.5, §6.1, §7, §8.2, #18, #19, #25) | 3 |
| `docs\decisions\0003-roster-csv-and-weekly-link-model.md` | One paragraph in the amendment | 3 |
| `CLAUDE.md` | Domain Snapshot (User, Student), critical rule 8, reference table row | 4 |
| `CONTEXT.md` | Change Teacher wording, Roster entry | 4 |
| `.claude\rules\api-guidelines.md` | "Auth Slice Exception" section | 4 |

## Execution Order

| # | File | Task | Commit point |
|---|------|------|--------------|
| 1 | [task-01-user-aggregate-adr.md](task-01-user-aggregate-adr.md) | Fact check, ADR 0006, ADR 0004 status, README | ✅ own commit |
| 2 | [task-02-requirements-access.md](task-02-requirements-access.md) | requirements v1.5: Users and Roles, decision #24 | ✅ own commit |
| 3 | [task-03-requirements-students.md](task-03-requirements-students.md) | requirements v1.5: Students by hand, decision #25, ADR 0003 paragraph | ✅ own commit |
| 4 | [task-04-agent-docs-and-glossary.md](task-04-agent-docs-and-glossary.md) | CLAUDE.md, CONTEXT.md, api-guidelines.md | ✅ own commit |
| 5 | [task-05-verify-and-pr.md](task-05-verify-and-pr.md) | Stale-phrase sweep, _Avoid_-word check, link check, PR | ✅ PR |

The PR targets `82-users-and-roles` and says `Closes #96`. It won't auto-close on a non-default base, so close #96 by hand after the merge, as #91 to #95 were. It references the parent spec #82.
