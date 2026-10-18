# Requirements Document: Weekly Demand Collection System for Driving Lessons
 
**Version:** 1.6
**Date:** 11 October 2026
**Status:** Approved scope for v1
**Changelog:** v1.6 - concurrent Administrator changes can no longer break decision #23: the later of two conflicting requests is refused as if it came a second later (§10, [ADR 0007](decisions/0007-locking-for-the-active-student-guards.md), issue #109). v1.5 - Users and Roles (spec #82): people sign in as **Users**, each with exactly one **Role**, Administrator or Teacher; a Teacher-role User sees only their own teacher's Week Schedules and Publications and marks that teacher's slots, and Administrators manage Users (decision #24, see [ADR 0006](decisions/0006-user-aggregate-and-roles.md)). Administrators also manage students by hand: add, edit, **Change Teacher**, **Change Car**, deactivate and reactivate; a teacher or car can't be deleted while active students depend on it (decision #25). v1.4 - the roster is no longer the single source of truth: an import adds and updates students but never deactivates anyone, and a row whose car is not assigned to its teacher is rejected and reported while the rest imports; a student's car is always one of their teacher's cars (see decision #23, the ADR 0003 amendment and issue #92). v1.3 — Car remodeled as its own aggregate: a **shared school pool** of cars, each assignable to any number of teachers (many-to-many); a teacher may have zero cars (the last-car rule is dropped); cars are soft-deleted like teachers (see decision #21). v1.2 — teacher car limit removed: a teacher owns **one or more** cars (was 1–2); the minimum-one rule binds as "the last car can never be removed" (see decision #20). v1.1 — student identity moved from email to national ID via an admin-uploaded roster (CSV); per-teacher links replaced by a single school-wide weekly link with ID-based routing; transmission question, teacher-confirmation/mismatch flow, and self-service onboarding removed. See [ADR 0003](decisions/0003-roster-csv-and-weekly-link-model.md).
**Scope discipline:** This document is technology-agnostic. A separate technology document (latest .NET, latest Angular, PrimeNG, signal-based patterns) will govern implementation.
 
---
 
## 1. Problem Statement
 
A driving school owner-teacher spends approximately three hours every week collecting students' scheduling preferences over WhatsApp and manually assembling them into an Excel file. The Excel file is the input to his actual booking work. The collection and assembly step is pure waste.
 
This system replaces the collection and assembly step. It does not replace the booking step.
 
## 2. Success Metric
 
One number: **minutes from submission-window close to a usable Excel file.** Target: under one minute. Current baseline: ~180 minutes.
 
## 3. Scope
 
### In scope (v1)
- Admin upload of the student roster as a CSV (national-ID-identified; binds each student to a teacher and car)
- Administrator management of students by hand: add, edit details, Change Teacher, Change Car, deactivate and reactivate
- Admin preparation of weekly availability grids, per teacher, published once per week as a single school-wide link
- Sign-in for **Users** holding one of two **Roles**: Administrator (everything) or Teacher (their own teacher's Week Schedules and Publications); Administrators add, edit, delete and restore Users, and every User changes their own password
- Student-facing submission flow via the shared weekly link, identified by national ID
- Live admin dashboard during the submission window
- Excel generation (on-demand download and automatic email at window close)
- Hebrew + English UI with full RTL support
### Out of scope (v1)
- Actual booking / assignment of students to slots. The Excel file is the end of this system's responsibility.
- Notifying students of their final schedule
- Self-service "forgot password", password emails and invitation links; forcing a password change after a Temporary Password
- More than two Roles, per-permission configuration, or a User holding several Roles
- Teachers creating Week Schedules or running the Publication lifecycle (publish, open, close, reopen)
- Sign-in for students: students identify by national ID only
- An audit log of User administration beyond domain events
- Payments, billing, multi-school tenancy
- Configurable slot windows (hardcoded in v1)
- Student self-registration: students are known only through the roster or an Administrator adding them by hand; there is no self-service onboarding
- Bulk Change Teacher / Change Car, a student history or audit screen, and moving a student's existing submissions on Change Teacher
- Native mobile apps (the student form must be mobile-browser friendly, since links are distributed via WhatsApp)
## 4. Personas
 
| Persona | Description | Authentication |
|---|---|---|
| **Administrator** | A User with the Administrator Role. The school owner, who also teaches, is the first; more can be added. Performs all planning for all teachers, manages Users, students and the roster. May be linked to a teacher to work with that teacher's own schedule. | Email + password sign-in (see 5.8) |
| **Teacher** | A teacher record: may be assigned cars from the shared pool and receives the resulting Excel. A teacher may have one User with the **Teacher Role**, which sees only that teacher's Week Schedules and Publications and marks that teacher's slots Open or Unavailable. The single weekly link covers all teachers; the student's record routes them to the right teacher's grid. | Email + password sign-in for the teacher's User, if they have one; none otherwise |
| **Student** | Receives the single weekly link (typically via WhatsApp). Submits weekly session preferences. Must already be known to the school, through the roster or added by an Administrator, and be active. | National ID identification, validated against the school's students, no password (see 8.2) |
 
## 5. Domain Model
 
### 5.1 Teacher
- Name, contact email (for receiving the Excel)
- May be assigned **zero or more Cars** from the shared pool (see decision #21)
- **Soft-deleted.** A teacher can't be deleted while they have an active linked User (see 5.8) or active students; the refusal names the students to change first
### 5.2 Car
- **Name:** display label used in the admin UI and Excel (e.g., "Corolla White")
- **Type:** make/model of the vehicle
- Belongs to the school's **shared pool**; may be assigned to **any number of Teachers (many-to-many)** — see decision #21
- Transmission: `Automatic` or `Manual`
- Cars are **soft-deleted** (like teachers)
- **Scheduling is per teacher, not per car.**
- Each student is assigned a specific car, by the roster or an Administrator; that car's transmission is the student's transmission. The student is never asked (see 5.5, 7).
- A car can't be deleted while active students learn on it, and can't be unassigned from a teacher while that teacher's active students learn on it (see decision #23)
### 5.3 Week Schedule (per teacher, per calendar week)
- Grid of **Slots**:
| Day | Slots |
|---|---|
| Sunday - Thursday | 07:00-12:00 (Morning), 12:00-15:00 (Noon), 15:00-18:00 (Afternoon), 18:00-22:00 (Evening) |
| Friday | 07:00-12:00 (Morning), 12:00-15:00 (Noon) |
| Saturday | Does not exist in the grid |
 
- Slot windows are **hardcoded** in v1.
- Each slot has a state: `Open` (default) or `Unavailable` (marked by an Administrator, or by the teacher's Teacher-role User).
- A new week **always starts fully open**. No copying from prior weeks in v1.
### 5.4 Publication
- Belongs to one **calendar week** and covers **all teachers** for that week. It aggregates the per-teacher Week Schedules (5.3) prepared for the week.
- Generates a **single, unguessable link** scoped to the week (no longer per teacher). A student opening the link identifies by national ID; their student record resolves which teacher's grid they see.
- Carries one **submission window**: start datetime and end datetime (Asia/Jerusalem timezone), applying to all teachers at once.
- States: `Draft` → `Published (window not yet open)` → `Open` → `Closed`
- Admin may **extend** the end time or **reopen** a closed window at any time; these act on the whole week.
- Excel is still generated **per teacher** (one file per teacher per publication; see §9), versioned per teacher on each close.
### 5.5 Student
- Identified uniquely by **national ID (ת.ז)** (per school)
- Students enter the system through the **roster (CSV)** or are **added by hand by an Administrator**; there is no self-registration. An unknown ID cannot submit (see §7).
- Profile fields, set by the roster or an Administrator and read-only to the student: full name (single field), phone, assigned teacher, assigned car, address, license type, start date
- **Transmission** is derived from the assigned car's transmission (via the car / license type), never asked
- **Teacher association** comes from the student's record (roster or Administrator), never stamped from a link
- A student's **car is always one of their teacher's cars** (see decision #23)
- The roster is **not** the single source of truth: an import updates the profile fields it carries, and the most recent change (a roster import or an Administrator's manual edit, spec #82) is what the student shows
- An Administrator can edit a student's details (name, phone, address, start date, license type, national ID), **Change Teacher** (choosing one of the new teacher's cars in the same step; the current car is preselected when the new teacher also teaches on it), **Change Car** (among the current teacher's cars), and deactivate or reactivate. The national ID stays unique. Change Teacher to the current teacher, Change Car to the current car, deactivating an Inactive Student and reactivating an active one are rejected
- Change Teacher leaves the student's existing submissions with the Publication they were made to; their next identification on the student form uses the new teacher
- An **Inactive Student** can't identify on the student form. Reactivation is refused while the student's teacher or car is deleted, or while the car is not one of the teacher's cars
- A student whose car is not one of their teacher's cars (data from before decision #23) is flagged on the Students screen so an Administrator can Change Car

### 5.5.1 Student Roster (CSV import)
- The admin uploads a CSV; each row is one student. Columns map to: full name, national ID (identifier), phone, assigned car, assigned teacher, address, notes, start date, license type (transmission), and admin-only bookkeeping fields.
- The Berosh export is accepted as it comes: its header names (`שם התלמיד`, `ת.ז`, `סוג רשיון`) are read as full name, national ID and license type, and its file, a CSV named `.xlsx`, can be picked on an iPad. A genuine Excel workbook is refused with "save it as CSV".
- Re-uploading **upserts by national ID**: existing students are updated and new rows are added. Students absent from the new file are **left unchanged**: an import never deactivates a student, so students added or edited by hand survive the next import. A student in the file who is an **Inactive Student** is reactivated.
- The assigned teacher and car strings in the CSV must resolve to existing Teacher/Car records; unresolved rows are reported as errors and skipped.
- A row whose car is **not assigned to the row's teacher** is rejected and reported ("car is not assigned to this teacher"); the rest of the file still imports. Students who already break this rule are left as they are unless a consistent row updates them.
- A national ID corrected by hand must also be corrected in the roster at its source: a later file still carrying the old ID creates a second student under the old ID (accepted, no guard).
### 5.6 Submission (per student, per publication)
- Target session count (integer ≥ 1, no upper limit)
- An **ordered list of Slot Requests**, ranked by selection sequence
- Editable by the student until the window closes (see 8.2)
### 5.7 Slot Request
- References one open Slot
- Session type: `Single` or `Double`
- Optional free-text constraint (per slot request, not per submission)
- Rank: position in the student's ordered list
**Ranking model decision:** There are no distinct "primary" and "alternative" request types. The model is one ranked preference list plus a target count. A student who wants 2 sessions and picks 5 slots has submitted ranks 1-5 against a target of 2. The teacher interprets rank against target during booking.
 
### 5.8 User
- A person who can sign in: name, sign-in email (unique, case-insensitive), password, and exactly one **Role**
- **Administrator**: can do everything. May be linked to one teacher (the owner who also teaches), and then also works with that teacher's schedule like any teacher
- **Teacher Role**: always linked to exactly one teacher. Sees only that teacher's Week Schedules and Publications (with their submissions, share link and Excel download) and marks that teacher's slots Open or Unavailable under the same rules as the Administrator. Cannot create Week Schedules or publish, open, close or reopen a Publication
- The teacher link is set when the User is created and never changes. A teacher has at most one User
- Administrators add a User with a **Temporary Password**, edit name and sign-in email, change Role, set a new Temporary Password, delete and restore. A Teacher-role User can be promoted to Administrator and keeps their teacher link; an Administrator can be demoted to the Teacher Role only when linked to a teacher
- No one can delete themselves or change their own Role, and the last active Administrator can never be deleted or demoted
- A **Deleted User** can't sign in and is signed out on their next request. Deleting a User leaves their teacher, Week Schedules and Publications untouched, and a Deleted User can be restored unless their teacher has since been deleted
- A Role change, a new Temporary Password or a password change also takes effect on the User's next request
- Every User can change their own password by entering their current one
- An operation already in its target state is rejected (deleting a Deleted User, restoring an active one, giving a User the Role they already have)
- On a fresh installation, the configured email and password create the first Administrator when no Users exist; later restarts leave Users untouched

## 6. Administrator Flows
 
### 6.1 Setup (one-time / rare)
1. Sign in. On a fresh installation, the first Administrator comes from the configured email and password (see 5.8).
2. Create teachers.
3. Create cars in the shared pool (setting transmission per car) and assign each car to its teachers.
4. Add Users: a Teacher-role User for each teacher who reports their own availability, and further Administrators if needed (see 5.8).
5. Upload the student roster (CSV). Re-upload at any time to add students or update assignments; upsert is by national ID (see 5.5.1). Students can also be added and changed by hand on the Students screen (see 5.5).
### 6.2 Weekly preparation
1. For each teacher, select the target week and view the fully open grid.
2. Toggle individual slots to `Unavailable` per teacher. A teacher with a Teacher-role User may do this for their own grid (see 6.5); creating the Week Schedules and publishing stay with the Administrator.
3. Publish the week **once**: set the submission window start and end datetimes. The system generates **one** shareable link covering all teachers.
4. Distribute the single link (outside the system, e.g., WhatsApp). Every teacher's students use the same link; each student's record routes them to the right grid.
### 6.3 During the window
- **Dashboard:** the summary grid (day × slot) shows the current request count per slot at any point during the window. Data reflects the state at page load; the admin refreshes the page to see new submissions. No push/auto-refresh requirement. Selecting a count with one or more requests lists the Students who requested that Slot, in submission order, each with which of their picks it was ("choice 2 of 3"); counts of 0 and Unavailable Slots open nothing. The list is read when it opens.
### 6.4 Window close and reopen
- At end time, the link becomes read-only for students and the Excel is **automatically emailed** to the teacher's contact email.
- The admin may extend the deadline before close, or reopen after close.
- **Versioning rule:** every window close event fires the email. The email subject carries an incrementing version number (e.g., "בקשות לשבוע 25 - משה כהן - גרסה 2"; the email is Hebrew, see decision #22) so the teacher always knows whether a previously received file is stale.
- The Excel is also available for **on-demand download** in the app (Administrators, and the teacher's Teacher-role User) at any time, including mid-window.
### 6.5 Teacher-role User
1. Sign in and land on the teacher's own Week Schedules.
2. Mark the teacher's own slots Open or Unavailable for a week the Administrator created.
3. See the teacher's own Publications with their state, submissions, share link and Excel download.
4. Teachers & Cars, Students, Roster and Users are not shown, and reaching another teacher's data, even by typing a URL, is refused.
## 7. Student Flow
 
1. Student opens the single weekly link on a mobile or desktop browser.
2. **If the window is not yet open or already closed:** a clear read-only message with the relevant dates. No form.
3. Student enters their **national ID**.
   - **Known ID (an active student):** the system resolves their teacher, car, and transmission from their record; any existing submission for this publication is loaded for editing.
   - **Inactive Student:** a clear message that they can't submit and should contact the school. No form, no submission.
   - **Unknown ID:** a clear "we don't have you on file, please contact your school" message. No form, no submission. (New students are added through the roster or by an Administrator.)
4. **Confirmation (display only):** the form shows the student their resolved details ("You are submitting availability for **Teacher X**") from their record. There is no teacher selection, no mismatch warning, no "this is not my teacher" action, and no transmission question; the student's record is authoritative.
5. Student declares the **target session count** for the week.
6. Student sees the week grid for their assigned teacher. `Unavailable` slots are visibly blocked and unselectable.
7. For each slot picked, the student specifies:
   - `Single` or `Double` session
   - Optional free-text constraint for that slot
8. Picks accumulate as a **ranked list in selection order**. The student may reorder before submitting. Minimum picks = target count; additional picks are permitted and serve as lower-ranked preferences.
9. Submit. A confirmation screen states that the submission can be edited via the same link and national ID until the window closes.
### Validation rules (student form)
- National ID belongs to an active student (it is both the identity mechanism and the access gate; no verification step in v1)
- Target count ≥ 1
- Picks ≥ target count
- A slot can be picked at most once per submission
- Submission rejected with a clear message if the window closed between page load and submit
## 8. Cross-Cutting Decisions
 
### 8.1 Demand counting
A `Double` session counts as **one request** in summary counts, by explicit decision. The detail sheet carries the single/double flag, which is what makes this acceptable. **Standing flag:** if the detail sheet is ever removed, this decision must be revisited, because three doubles and three singles would become indistinguishable.
 
### 8.2 Identity and abuse posture
- National-ID identification, no passwords, no magic links in v1. Submission requires the ID to belong to an active student, so random walk-ins cannot submit. Accepted risk: anyone holding the link who knows a student's national ID can impersonate that student. National IDs are semi-guessable (9 digits with a check digit), and because the link is now **school-wide**, the blast radius is the **whole school's** weekly preference lists rather than one teacher's. The mitigation remains that the teacher knows his students and reviews the Excel; this mitigation is weaker at school scale and is flagged for revisit if abuse appears.
- Re-entering the same national ID **loads the existing submission for editing**. This is mandatory: without self-service editing, correction requests return to WhatsApp and recreate the manual workload this system exists to eliminate.
- Users (Administrators and Teacher-role Users) sign in with email and password; this is separate from student identification. Revoking access (deleting a User, changing their Role, setting a new password) takes effect on the User's next request, not when their session expires.
### 8.3 Timezone
- **Instants** (submission window open/close, submission timestamps, audit fields) are stored in **UTC** and presented in Asia/Jerusalem. This is the future-proofing for eventual multi-school generalization.
- **Wall-clock definitions** (slot windows such as Morning 07:00-12:00) are stored as **local time plus a named timezone (Asia/Jerusalem)**, not UTC. A slot is a recurring local-time concept; converting it to UTC would shift it by an hour across DST transitions, which is a bug, not a simplification.
- No timezone selection anywhere in the UI in v1; Asia/Jerusalem is the single configured zone.
### 8.4 Localization
- Hebrew and English, user-toggleable on both admin and student surfaces.
- Hebrew is RTL-first; layouts must be mirror-correct, not merely translated.
## 9. Excel Output Specification
 
One file per teacher per publication. Two sheets.
 
### Sheet 1: Summary Grid
- Rows: slots (Morning / Noon / Afternoon / Evening)
- Columns: days (Sunday through Friday)
- Cell value: count of slot requests
- `Unavailable` slots visually marked as blocked
- Friday Afternoon/Evening cells do not exist
### Sheet 2: Request Detail
One row per slot request, sorted by day, then slot, then student rank:
 
| Column | Content |
|---|---|
| Day | Sunday-Friday |
| Slot | Morning / Noon / Afternoon / Evening |
| Student name | Full name (from the student's record) |
| Car | The name of the student's roster car |
| Session type | Single / Double |
| Rank | Position in the student's preference order |
| Requested lessons | The student's weekly target out of the slots they picked, for example "1 out of 3" (`1 מתוך 3`) |
| Constraints | Free text for this slot request |

National ID, phone and transmission were removed at the customer's request (October 2026).
 
### Delivery
- **Automatic:** emailed to the teacher's contact email at every window close (versioned per 6.4)
- **On-demand:** download button in the app, available at any time
## 10. Non-Functional Requirements
 
- Student form must be fully usable on mobile browsers (primary distribution channel is WhatsApp)
- Links must be unguessable (no sequential identifiers)
- The system must tolerate concurrent submissions without losing or duplicating requests
- Two Administrators acting at the same moment never leave an active student on a deleted teacher or car, or on a car that isn't their teacher's: the later request is refused as if it came a second later, and two changes to the same student at once refuse the later one with a "changed at the same moment" message (see [ADR 0007](decisions/0007-locking-for-the-active-student-guards.md))
- Data retention: past weeks remain queryable by the admin (history view is minimal in v1: list of past publications with Excel re-download)
## 11. Decisions Log
 
| # | Decision | Rationale |
|---|---|---|
| 1 | ~~Admin = owner-teacher, single login, no teacher logins~~ **superseded by #24** | v1 reality: one person plans for everyone |
| 2 | Single school, architecture should not block later generalization | May generalize later |
| 3 | Scheduling scoped per **teacher**, not per car (still holds). ~~Link scoped per teacher~~ **superseded by #16** | Teacher may operate multiple cars; the link is now school-wide (see #16) |
| 4 | One ranked list + target count, no primary/alternative types | Simpler model, rank carries the same information |
| 5 | Double = 1 request in summary counts | Detail sheet carries the weight; flagged in 8.1 |
| 6 | ~~Same email reloads submission~~ **superseded by #17**: same national ID reloads submission for editing | Kills the WhatsApp correction loop |
| 7 | Sunday-Friday grid, short Friday (morning + noon) | Israeli work week |
| 8 | Admin can extend/reopen windows | Operational reality; mitigated by versioned emails |
| 9 | Every week starts fully open | Explicit choice over copying prior week |
| 10 | ~~Transmission question rendered only when teacher has both types~~ **superseded by #18**: transmission derived from the roster | Eliminates the question entirely; the roster assigns the car |
| 11 | Constraints free text scoped per slot request | As originally envisioned |
| 12 | Dashboard during window, manual page refresh, no push updates | Admin checks demand on demand; real-time push is unjustified complexity |
| 13 | Booking and student notification out of scope | Excel is the end of this system |
| 14 | Hebrew + English toggle, RTL-first | Bilingual user base |
| 15 | Slot windows hardcoded in v1 | Configurability is a v2 concern |
| 16 | **Single school-wide weekly link** (one Publication per week), replacing per-teacher links | The roster routes each student to their teacher; one link is simpler to publish and distribute (see ADR 0003) |
| 17 | **National ID is the student identifier**, validated against an admin-uploaded roster (CSV); no self-registration (amended by #25: or added by an Administrator) | Customer supplies the student list as data; ID is the school's natural key (see ADR 0003) |
| 18 | **Transmission and teacher come from the roster**, not the form (amended by #25: or from an Administrator's change) | The CSV already assigns each student a teacher and car; asking would be redundant (see ADR 0003) |
| 19 | **Unknown ID is rejected** ("contact your school"); ~~new students added by re-uploading the roster~~ new students are added through the roster or by an Administrator (amended by #25) | ~~Roster is authoritative;~~ removes self-service onboarding; students still can't add themselves (see ADR 0003 and its amendment) |
| 20 | ~~No maximum on cars per teacher (was 1–2); minimum one binds as "the last car can never be removed" and a teacher may exist carless until their first car is added~~ **superseded by #21** | Fleet size is the school's business, not a system rule; the minimum only matters once car removal exists |
| 21 | **Car is its own aggregate — a shared school pool**: one car may be assigned to any number of teachers (many-to-many), a teacher may have zero cars (the last-car rule is dropped), and cars soft-delete like teachers | One physical car is shared between teachers in practice; modeling Car as owned by a single teacher was a modeling error |
| 22 | **The teacher's Excel email is Hebrew**: subject "בקשות לשבוע N - {teacher} - גרסה K", a right-to-left body, and the attachment named after the subject | Teachers read Hebrew and the workbook is already Hebrew (Excel roadmap decision 1); the version in the attachment's name keeps a saved file identifiable after it leaves the inbox |
| 23 | **The roster adds and updates but never deactivates, and a student's car must be one of their teacher's cars**; a roster row that breaks the rule is rejected and reported while the rest of the file imports | Students added or edited by hand must survive the next import; the car fixes the student's transmission, so it must be a car their teacher actually teaches on (see the ADR 0003 amendment, issue #92) |
| 24 | **Users with exactly one of two Roles**: Administrator (everything) or Teacher (only the linked teacher's Week Schedules and Publications, marking that teacher's slots). A Teacher-role User is linked to exactly one teacher, the link never changes, a teacher has at most one User, and the last active Administrator can never be deleted or demoted | Teachers report their own availability instead of the owner doing it for everyone; two fixed Roles cover one school without a permissions model (see [ADR 0006](decisions/0006-user-aggregate-and-roles.md), spec #82) |
| 25 | **Administrators manage students by hand**: add, edit details (national ID included), Change Teacher with one of the new teacher's cars, Change Car among the current teacher's cars, deactivate and reactivate. A teacher or car can't be deleted, and a car can't be unassigned from a teacher, while active students depend on it; a student can't be reactivated onto a deleted teacher or car or a car that isn't the teacher's | Students change teacher or car mid-course and the school must record it in the app; since the roster no longer overwrites it (#23), the rule that a student's car is one of their teacher's cars is enforced at every entry point (spec #82) |
 
## 12. Explicitly Deferred (v2 candidates)
 
- Copy previous week's unavailability as a starting point
- Configurable slot windows and session lengths
- Self-service password reset (forgot password, password emails, invitation links) and forcing a new password after a Temporary Password
- Magic-link email verification
- Booking/assignment module consuming the ranked requests
- Student notification of final schedule
- Multi-school tenancy