# Task 2 of 5: requirements v1.5, Users and Roles

> Part of [#96: Docs for the User aggregate, the Roster amendment, requirements and glossary](README.md). Work on branch `96-docs-users-and-roles`. Requires task 1 committed (this task links to ADR 0006).

**Files:**
- Modify: `docs\requirements.md`: header (lines 3 to 6), §3 scope (lines 23 to 37), §4 personas (lines 42 and 43), new §5.8 after §5.7 (after line 101), §6.1 and §6.2 (lines 105 to 114), new §6.5 after §6.4 (after line 121), §8.2 (after line 150), §11 decision #1 (line 197) and a new #24 (after line 219), §12 (line 225). Line numbers are for v1.4 and shift as you go: find each block by its quoted text.

**Interfaces:**
- Consumes: `decisions/0006-user-aggregate-and-roles.md` (task 1).
- Produces, for task 3:
  - The v1.5 changelog entry begins `**Changelog:** v1.5 - Users and Roles (spec #82):` and its first sentence ends with `(decision #24, see [ADR 0006](decisions/0006-user-aggregate-and-roles.md)).` Task 3 adds a second sentence right after it.
  - §6.1 has five steps. Step 5 is the roster step, exactly `5. Upload the student roster (CSV). Re-upload at any time to add students or update assignments; upsert is by national ID (see 5.5.1).`
  - Decision #24 is the last row of §11.
  - §5.8 is titled `### 5.8 User`.

**Why:** #96 AC 3, the access half: personas, decision #1 reversed, the out-of-scope and deferred lists, and the affected flows.

- [ ] **Step 1: Header**

Replace

```markdown
**Version:** 1.4
**Date:** 13 July 2026
```

with

```markdown
**Version:** 1.5
**Date:** 11 October 2026
```

On the `**Changelog:**` line, insert this text right after `**Changelog:** ` and before `v1.4 - the roster`:

```markdown
v1.5 - Users and Roles (spec #82): people sign in as **Users**, each with exactly one **Role**, Administrator or Teacher; a Teacher-role User sees only their own teacher's Week Schedules and Publications and marks that teacher's slots, and Administrators manage Users (decision #24, see [ADR 0006](decisions/0006-user-aggregate-and-roles.md)). 
```

(The trailing space separates it from `v1.4 - ...`.)

- [ ] **Step 2: Scope (§3)**

In **In scope (v1)**, after the bullet `- Admin preparation of weekly availability grids, per teacher, published once per week as a single school-wide link`, add:

```markdown
- Sign-in for **Users** holding one of two **Roles**: Administrator (everything) or Teacher (their own teacher's Week Schedules and Publications); Administrators add, edit, delete and restore Users, and every User changes their own password
```

In **Out of scope (v1)**, replace the bullet

```markdown
- Logins for teachers (teachers are data entities only)
```

with

```markdown
- Self-service "forgot password", password emails and invitation links; forcing a password change after a Temporary Password
- More than two Roles, per-permission configuration, or a User holding several Roles
- Teachers creating Week Schedules or running the Publication lifecycle (publish, open, close, reopen)
- Student logins: students identify by national ID only
- An audit log of User administration beyond domain events
```

- [ ] **Step 3: Personas (§4)**

Replace the **Administrator** and **Teacher** rows:

```markdown
| **Administrator** | The school owner. Also a teacher. Performs all planning for all teachers. Uploads and maintains the student roster. | Email + password login |
| **Teacher** | A data entity. May be assigned cars from the shared pool. Receives the resulting Excel. No login in v1. The single weekly link covers all teachers; the student's roster record routes them to the right teacher's grid. | None |
```

with

```markdown
| **Administrator** | A User with the Administrator Role. The school owner, who also teaches, is the first; more can be added. Performs all planning for all teachers, manages Users, students and the roster. May be linked to a teacher to work with that teacher's own schedule. | Email + password sign-in (see 5.8) |
| **Teacher** | A teacher record: may be assigned cars from the shared pool and receives the resulting Excel. A teacher may have one User with the **Teacher Role**, which sees only that teacher's Week Schedules and Publications and marks that teacher's slots Open or Unavailable. The single weekly link covers all teachers; the student's record routes them to the right teacher's grid. | Email + password sign-in for the teacher's User, if they have one; none otherwise |
```

Leave the **Student** row to task 3.

- [ ] **Step 4: Decisions log (§11), decision #1 and new #24**

Replace

```markdown
| 1 | Admin = owner-teacher, single login, no teacher logins | v1 reality: one person plans for everyone |
```

with

```markdown
| 1 | ~~Admin = owner-teacher, single login, no teacher logins~~ **superseded by #24** | v1 reality: one person plans for everyone |
```

After the row that starts `| 23 |`, add:

```markdown
| 24 | **Users with exactly one of two Roles**: Administrator (everything) or Teacher (only the linked teacher's Week Schedules and Publications, marking that teacher's slots). A Teacher-role User is linked to exactly one teacher, the link never changes, a teacher has at most one User, and the last active Administrator can never be deleted or demoted | Teachers report their own availability instead of the owner doing it for everyone; two fixed Roles cover one school without a permissions model (see [ADR 0006](decisions/0006-user-aggregate-and-roles.md), spec #82) |
```

Check that no row was lost:

```bash
grep -c "^| [0-9]* |" docs/requirements.md
grep -n "^| 1 |\|^| 24 |" docs/requirements.md
```

Expected: `24`, and rows 1 and 24 both printed, row 1 containing `~~Admin = owner-teacher, single login, no teacher logins~~`.

- [ ] **Step 5: New §5.8 User**

After the §5.7 Slot Request block, which ends with the paragraph starting `**Ranking model decision:**`, and before `## 6. Administrator Flows`, add:

```markdown
### 5.8 User
- A person who can sign in: name, sign-in email (unique, case-insensitive), password, and exactly one **Role**
- **Administrator**: can do everything. May be linked to one teacher (the owner who also teaches), and then also works with that teacher's schedule like any teacher
- **Teacher Role**: always linked to exactly one teacher. Sees only that teacher's Week Schedules and Publications (with their submissions, share link and Excel download) and marks that teacher's slots Open or Unavailable under the same rules as the Administrator. Cannot create Week Schedules or publish, open, close or reopen a Publication
- The teacher link is set when the User is created and never changes. A teacher has at most one User
- Administrators add a User with a **Temporary Password**, edit name and sign-in email, change Role, set a new Temporary Password, delete and restore. A Teacher-role User can be promoted to Administrator and keeps their teacher link; an Administrator can be demoted to the Teacher Role only when linked to a teacher
- No one can delete themselves or change their own Role, and the last active Administrator can never be deleted or demoted
- A **Deleted User** can't sign in and is signed out on their next request. Deleting a User leaves their teacher, Week Schedules and Publications untouched, and a Deleted User can be restored
- A Role change, a new Temporary Password or a password change also takes effect on the User's next request
- Every User can change their own password by entering their current one
- An operation already in its target state is rejected (deleting a Deleted User, restoring an active one, giving a User the Role they already have)
- On a fresh installation, the configured email and password create the first Administrator when no Users exist; later restarts leave Users untouched
```

- [ ] **Step 6: Flows (§6.1, §6.2, new §6.5)**

In §6.1 Setup, replace the four steps

```markdown
1. Log in.
2. Create teachers.
3. Create cars in the shared pool (setting transmission per car) and assign each car to its teachers.
4. Upload the student roster (CSV). Re-upload at any time to add students or update assignments; upsert is by national ID (see 5.5.1).
```

with

```markdown
1. Sign in. On a fresh installation, the first Administrator comes from the configured email and password (see 5.8).
2. Create teachers.
3. Create cars in the shared pool (setting transmission per car) and assign each car to its teachers.
4. Add Users: a Teacher-role User for each teacher who reports their own availability, and further Administrators if needed (see 5.8).
5. Upload the student roster (CSV). Re-upload at any time to add students or update assignments; upsert is by national ID (see 5.5.1).
```

In §6.2 Weekly preparation, replace

```markdown
2. Toggle individual slots to `Unavailable` per teacher.
```

with

```markdown
2. Toggle individual slots to `Unavailable` per teacher. A teacher with a Teacher-role User may do this for their own grid (see 6.5); creating the Week Schedules and publishing stay with the Administrator.
```

After §6.4 (its last bullet starts `- The Excel is also available for **on-demand download**`) and before `## 7. Student Flow`, add:

```markdown
### 6.5 Teacher-role User
1. Sign in and land on the teacher's own Week Schedules.
2. Mark the teacher's own slots Open or Unavailable for a week the Administrator created.
3. See the teacher's own Publications with their state, submissions, share link and Excel download.
4. Teachers & Cars, Students, Roster and Users are not shown, and reaching another teacher's data, even by typing a URL, is refused.
```

- [ ] **Step 7: §8.2 Identity and abuse posture**

After the bullet that starts `- Re-entering the same national ID **loads the existing submission for editing**.`, add:

```markdown
- Users (Administrators and Teacher-role Users) sign in with email and password; this is separate from student identification. Revoking access (deleting a User, changing their Role, setting a new password) takes effect on the User's next request, not when their session expires.
```

- [ ] **Step 8: Deferred (§12)**

Replace

```markdown
- Teacher logins and per-teacher self-service
```

with

```markdown
- Self-service password reset (forgot password, password emails, invitation links) and forcing a new password after a Temporary Password
```

- [ ] **Step 9: Check and commit**

```bash
git grep -n -i "no teacher logins\|logins for teachers\|no login in v1\|teacher logins and" -- docs/requirements.md
grep -n "^### 5.8 User\|^### 6.5 Teacher-role User\|^\*\*Version:\*\* 1.5" docs/requirements.md
```

Expected: the first command prints only decision row #1 (the struck-through text). The second prints three lines.

```bash
git add docs/requirements.md
git commit -m "docs(auth): requirements v1.5 - Users with two Roles replace the single admin login (#96)"
```

End the commit message with the attribution trailer from the session's instructions.
