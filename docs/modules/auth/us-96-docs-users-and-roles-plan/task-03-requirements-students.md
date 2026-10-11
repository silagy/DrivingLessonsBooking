# Task 3 of 5: requirements v1.5, Students managed by hand, and the ADR 0003 paragraph

> Part of [#96: Docs for the User aggregate, the Roster amendment, requirements and glossary](README.md). Work on branch `96-docs-users-and-roles`. Requires task 2 committed (this task extends its changelog entry and its §6.1 step 5).

**Files:**
- Modify: `docs\requirements.md`: changelog, §3 scope, §4 Student persona, §5.1, §5.2, §5.5, §6.1 step 5, §7 steps 3 and 4 and the validation rules, §8.2 first bullet, §11 decisions #18 and #19 and a new #25.
- Modify: `docs\decisions\0003-roster-csv-and-weekly-link-model.md`: one paragraph at the end of the Amendment.

**Interfaces:**
- Consumes, from task 2: the v1.5 changelog sentence ending `(decision #24, see [ADR 0006](decisions/0006-user-aggregate-and-roles.md)).`; §6.1 step 5 `5. Upload the student roster (CSV). Re-upload at any time to add students or update assignments; upsert is by national ID (see 5.5.1).`; decision #24 as the last §11 row.
- Produces: decision #25 as the last §11 row; the requirements text task 5 sweeps.

**Why:** #96 AC 3, the Students half (§5.5.1 and the affected flows), and AC 2 (ADR 0003 amended). The ADR 0003 amendment from #92 already covers the four points in AC 2; this task closes the one argument in ADR 0003 that still rests on "the roster is authoritative".

- [ ] **Step 1: Confirm the behaviour being documented**

```bash
git grep -n "public void \(Deactivate\|Reactivate\|ChangeTeacher\|ChangeCar\|ChangeDetails\)" -A8 -- src/DrivingLessons.Domain/Entities/Student.cs | grep "Must"
git grep -n "MustNotHaveActiveStudents\|MustNotHaveActiveUser" -- src/DrivingLessons.Domain/Entities src/DrivingLessons.Application/Commands/DeleteTeacher
git grep -n -i "inactive" -- client/public/i18n/en.json | head -5
```

Expected:
- `Reactivate` guards: must be inactive, Teacher not deleted, Car not deleted, Car of Teacher. `ChangeTeacher` / `ChangeCar` guard the same Teacher / same Car and Car of Teacher.
- `Teacher.Delete` and `Car.Delete` / `Car.UnassignTeacher` check active Students; `DeleteTeacherInteractor` checks the active User.
- The student form has an "inactive" message.

If any of these differs, write the text below the way the code behaves and tell the user.

- [ ] **Step 2: Changelog, scope and the Student persona**

On the `**Changelog:**` line, right after `(decision #24, see [ADR 0006](decisions/0006-user-aggregate-and-roles.md)). ` insert:

```markdown
Administrators also manage students by hand: add, edit, **Change Teacher**, **Change Car**, deactivate and reactivate; a teacher or car can't be removed while active students depend on it (decision #25). 
```

In **In scope (v1)**, after the bullet `- Admin upload of the student roster as a CSV (national-ID-identified; binds each student to a teacher and car)`, add:

```markdown
- Administrator management of students by hand: add, edit details, Change Teacher, Change Car, deactivate and reactivate
```

In **Out of scope (v1)**, replace

```markdown
- Student self-registration: students are known only through the uploaded roster; there is no self-service onboarding
```

with

```markdown
- Student self-registration: students are known only through the roster or an Administrator adding them by hand; there is no self-service onboarding
- Bulk Change Teacher / Change Car, a student history or audit screen, and moving a student's existing submissions on Change Teacher
```

In §4, replace the **Student** row

```markdown
| **Student** | Receives the single weekly link (typically via WhatsApp). Submits weekly session preferences. Must already exist in the uploaded roster. | National ID identification, validated against the roster, no password (see 8.2) |
```

with

```markdown
| **Student** | Receives the single weekly link (typically via WhatsApp). Submits weekly session preferences. Must already be known to the school, through the roster or added by an Administrator, and be active. | National ID identification, validated against the school's students, no password (see 8.2) |
```

- [ ] **Step 3: §5.1 Teacher, §5.2 Car, §5.5 Student**

In §5.1, after `- May be assigned **zero or more Cars** from the shared pool (see decision #21)`, add:

```markdown
- **Soft-deleted.** A teacher can't be deleted while they have an active linked User (see 5.8) or active students; the refusal names the students to change first
```

In §5.2, replace

```markdown
- The roster assigns each student a specific car; that car's transmission is the student's transmission. The student is never asked (see 5.5, 7).
```

with

```markdown
- Each student is assigned a specific car, by the roster or an Administrator; that car's transmission is the student's transmission. The student is never asked (see 5.5, 7).
- A car can't be deleted while active students learn on it, and can't be unassigned from a teacher while that teacher's active students learn on it (see decision #23)
```

In §5.5, replace

```markdown
- Students are **pre-loaded from the admin-uploaded roster (CSV)** — there is no self-registration. An ID not in the roster cannot submit (see §7).
- Profile fields, all sourced from the roster and read-only to the student: full name (single field), phone, assigned teacher, assigned car, address, license type, start date
```

with

```markdown
- Students enter the system through the **roster (CSV)** or are **added by hand by an Administrator**; there is no self-registration. An unknown ID cannot submit (see §7).
- Profile fields, set by the roster or an Administrator and read-only to the student: full name (single field), phone, assigned teacher, assigned car, address, license type, start date
```

Replace

```markdown
- **Teacher association** comes directly from the roster record, never stamped from a link
```

with

```markdown
- **Teacher association** comes from the student's record (roster or Administrator), never stamped from a link
```

After the bullet that starts `- The roster is **not** the single source of truth:`, add:

```markdown
- An Administrator can edit a student's details (name, phone, address, start date, license type, national ID), **Change Teacher** (choosing one of the new teacher's cars in the same step; the current car is preselected when the new teacher also teaches on it), **Change Car** (among the current teacher's cars), and deactivate or reactivate. The national ID stays unique. Change Teacher to the current teacher, Change Car to the current car, deactivating an Inactive Student and reactivating an active one are rejected
- Change Teacher leaves the student's existing submissions with the Publication they were made to; their next identification on the student form uses the new teacher
- An **Inactive Student** can't identify on the student form. Reactivation is refused while the student's teacher or car is deleted, or while the car is not one of the teacher's cars
- A student whose car is not one of their teacher's cars (data from before decision #23) is flagged on the Students screen so an Administrator can Change Car
```

- [ ] **Step 4: §6.1 step 5, §7 and §8.2**

In §6.1, replace step 5 (written by task 2)

```markdown
5. Upload the student roster (CSV). Re-upload at any time to add students or update assignments; upsert is by national ID (see 5.5.1).
```

with

```markdown
5. Upload the student roster (CSV). Re-upload at any time to add students or update assignments; upsert is by national ID (see 5.5.1). Students can also be added and changed by hand on the Students screen (see 5.5).
```

In §7, replace step 3's two sub-bullets

```markdown
   - **Known ID (in the roster):** the system resolves their teacher, car, and transmission from the roster; any existing submission for this publication is loaded for editing.
   - **Unknown ID (not in the roster):** a clear "we don't have you on file — please contact your school" message. No form, no submission. (New students are added by the admin re-uploading the roster.)
```

with

```markdown
   - **Known ID (an active student):** the system resolves their teacher, car, and transmission from their record; any existing submission for this publication is loaded for editing.
   - **Inactive Student:** a clear message that they can't submit and should contact the school. No form, no submission.
   - **Unknown ID:** a clear "we don't have you on file, please contact your school" message. No form, no submission. (New students are added through the roster or by an Administrator.)
```

Replace step 4

```markdown
4. **Confirmation (display only):** the form shows the student their resolved details — "You are submitting availability for **Teacher X**" — sourced from the roster. There is no teacher selection, no mismatch warning, no "this is not my teacher" action, and no transmission question; the roster is authoritative.
```

with

```markdown
4. **Confirmation (display only):** the form shows the student their resolved details ("You are submitting availability for **Teacher X**") from their record. There is no teacher selection, no mismatch warning, no "this is not my teacher" action, and no transmission question; the student's record is authoritative.
```

In **Validation rules (student form)**, replace

```markdown
- National ID present in the roster (it is both the identity mechanism and the access gate; no verification step in v1)
```

with

```markdown
- National ID belongs to an active student (it is both the identity mechanism and the access gate; no verification step in v1)
```

In §8.2, in the first bullet, replace `Submission requires the ID to exist in the roster, so random walk-ins cannot submit.` with `Submission requires the ID to belong to an active student, so random walk-ins cannot submit.`, and replace `who knows a roster member's national ID` with `who knows a student's national ID`. Leave the rest of the bullet as it is.

- [ ] **Step 5: Decisions #18, #19 and new #25**

Replace

```markdown
| 18 | **Transmission and teacher come from the roster**, not the form | The CSV already assigns each student a teacher and car; asking would be redundant (see ADR 0003) |
| 19 | **Unknown ID is rejected** ("contact your school"); new students added by re-uploading the roster | Roster is authoritative; removes self-service onboarding (see ADR 0003) |
```

with

```markdown
| 18 | **Transmission and teacher come from the roster**, not the form (amended by #25: or from an Administrator's change) | The CSV already assigns each student a teacher and car; asking would be redundant (see ADR 0003) |
| 19 | **Unknown ID is rejected** ("contact your school"); ~~new students added by re-uploading the roster~~ new students are added through the roster or by an Administrator (amended by #25) | Roster is authoritative; removes self-service onboarding (see ADR 0003) |
```

After the row that starts `| 24 |`, add:

```markdown
| 25 | **Administrators manage students by hand**: add, edit details (national ID included), Change Teacher with one of the new teacher's cars, Change Car among the current teacher's cars, deactivate and reactivate. A teacher or car can't be deleted, and a car can't be unassigned from a teacher, while active students depend on it; a student can't be reactivated onto a deleted teacher or car or a car that isn't the teacher's | Students change teacher or car mid-course and the school must record it in the app; since the roster no longer overwrites it (#23), the rule that a student's car is one of their teacher's cars is enforced at every entry point (spec #82) |
```

Check:

```bash
grep -c "^| [0-9]* |" docs/requirements.md
grep -n "^| 18 |\|^| 19 |\|^| 25 |" docs/requirements.md
```

Expected: `25`; rows 18 and 19 keep their original bold words, row 25 printed last.

- [ ] **Step 6: ADR 0003, one paragraph**

In `docs\decisions\0003-roster-csv-and-weekly-link-model.md`, at the very end of the file (after the paragraph starting `The "Delete students missing from a re-upload" alternative above stays rejected;`), add a blank line and:

```markdown
The "Allow ad-hoc (unlisted) students to self-enter" alternative also stays rejected, but no longer because the Roster is authoritative: Students still can't add themselves, and enter only through the Roster or an Administrator adding them by hand (requirements decision #25). An unknown national ID is still refused on the student form.
```

Change nothing else in ADR 0003.

- [ ] **Step 7: Check and commit**

```bash
git grep -n -i "pre-loaded from\|exist in the uploaded roster\|re-uploading the roster\|in the roster)\|present in the roster\|roster member" -- docs/requirements.md
```

Expected: only decision row #19 (the struck-through words). Anything else is a sentence this task missed: rewrite it the way step 3 or 4 rewrote its neighbours.

```bash
git add docs/requirements.md docs/decisions/0003-roster-csv-and-weekly-link-model.md
git commit -m "docs(students): requirements v1.5 - Administrators manage Students by hand, and self-entry stays rejected (#96)"
```

End the commit message with the attribution trailer from the session's instructions.
