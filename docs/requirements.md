# Requirements Document: Weekly Demand Collection System for Driving Lessons
 
**Version:** 1.2
**Date:** 11 July 2026
**Status:** Approved scope for v1
**Changelog:** v1.2 — teacher car limit removed: a teacher owns **one or more** cars (was 1–2); the minimum-one rule binds as "the last car can never be removed" (see decision #20). v1.1 — student identity moved from email to national ID via an admin-uploaded roster (CSV); per-teacher links replaced by a single school-wide weekly link with ID-based routing; transmission question, teacher-confirmation/mismatch flow, and self-service onboarding removed. See [ADR 0003](decisions/0003-roster-csv-and-weekly-link-model.md).
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
- Admin preparation of weekly availability grids, per teacher, published once per week as a single school-wide link
- Student-facing submission flow via the shared weekly link, identified by national ID
- Live admin dashboard during the submission window
- Excel generation (on-demand download and automatic email at window close)
- Hebrew + English UI with full RTL support
### Out of scope (v1)
- Actual booking / assignment of students to slots. The Excel file is the end of this system's responsibility.
- Notifying students of their final schedule
- Logins for teachers (teachers are data entities only)
- Payments, billing, multi-school tenancy
- Configurable slot windows (hardcoded in v1)
- Student self-registration: students are known only through the uploaded roster; there is no self-service onboarding
- Native mobile apps (the student form must be mobile-browser friendly, since links are distributed via WhatsApp)
## 4. Personas
 
| Persona | Description | Authentication |
|---|---|---|
| **Administrator** | The school owner. Also a teacher. Performs all planning for all teachers. Uploads and maintains the student roster. | Email + password login |
| **Teacher** | A data entity. Owns one or more cars. Receives the resulting Excel. No login in v1. The single weekly link covers all teachers; the student's roster record routes them to the right teacher's grid. | None |
| **Student** | Receives the single weekly link (typically via WhatsApp). Submits weekly session preferences. Must already exist in the uploaded roster. | National ID identification, validated against the roster, no password (see 8.2) |
 
## 5. Domain Model
 
### 5.1 Teacher
- Name, contact email (for receiving the Excel)
- Owns one or more **Cars** (at least one; the last car can never be removed — see decision #20)
### 5.2 Car
- **Name:** display label used in the admin UI and Excel (e.g., "Corolla White")
- **Type:** make/model of the vehicle
- Belongs to exactly one Teacher
- Transmission: `Automatic` or `Manual`
- Cars are attributes of a teacher's capability. **Scheduling is per teacher, not per car.**
- The roster assigns each student a specific car; that car's transmission is the student's transmission. The student is never asked (see 5.5, 7).
### 5.3 Week Schedule (per teacher, per calendar week)
- Grid of **Slots**:
| Day | Slots |
|---|---|
| Sunday - Thursday | 07:00-12:00 (Morning), 12:00-15:00 (Noon), 15:00-18:00 (Afternoon), 18:00-22:00 (Evening) |
| Friday | 07:00-12:00 (Morning), 12:00-15:00 (Noon) |
| Saturday | Does not exist in the grid |
 
- Slot windows are **hardcoded** in v1.
- Each slot has a state: `Open` (default) or `Unavailable` (admin-marked).
- A new week **always starts fully open**. No copying from prior weeks in v1.
### 5.4 Publication
- Belongs to one **calendar week** and covers **all teachers** for that week. It aggregates the per-teacher Week Schedules (5.3) prepared for the week.
- Generates a **single, unguessable link** scoped to the week (no longer per teacher). A student opening the link identifies by national ID; their roster record resolves which teacher's grid they see.
- Carries one **submission window**: start datetime and end datetime (Asia/Jerusalem timezone), applying to all teachers at once.
- States: `Draft` → `Published (window not yet open)` → `Open` → `Closed`
- Admin may **extend** the end time or **reopen** a closed window at any time; these act on the whole week.
- Excel is still generated **per teacher** (one file per teacher per publication; see §9), versioned per teacher on each close.
### 5.5 Student
- Identified uniquely by **national ID (ת.ז)** (per school)
- Students are **pre-loaded from the admin-uploaded roster (CSV)** — there is no self-registration. An ID not in the roster cannot submit (see §7).
- Profile fields, all sourced from the roster and read-only to the student: full name (single field), phone, assigned teacher, assigned car, address, license type, start date
- **Transmission** is derived from the assigned car's transmission (via the car / license type), never asked
- **Teacher association** comes directly from the roster record, never stamped from a link
- The roster is the single source of truth for every student profile field

### 5.5.1 Student Roster (CSV import)
- The admin uploads a CSV; each row is one student. Columns map to: full name, national ID (identifier), phone, assigned car, assigned teacher, address, notes, start date, license type (transmission), and admin-only bookkeeping fields.
- Re-uploading **upserts by national ID**: existing students are updated, new rows are added, and students absent from the new file are **deactivated, not deleted** (historical submissions are preserved).
- The assigned teacher and car strings in the CSV must resolve to existing Teacher/Car records; unresolved rows are reported as errors and skipped.
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
 
## 6. Administrator Flows
 
### 6.1 Setup (one-time / rare)
1. Log in.
2. Create teachers.
3. Create cars under each teacher, setting transmission per car.
4. Upload the student roster (CSV). Re-upload at any time to add students or update assignments; upsert is by national ID (see 5.5.1).
### 6.2 Weekly preparation
1. For each teacher, select the target week and view the fully open grid.
2. Toggle individual slots to `Unavailable` per teacher.
3. Publish the week **once**: set the submission window start and end datetimes. The system generates **one** shareable link covering all teachers.
4. Distribute the single link (outside the system, e.g., WhatsApp). Every teacher's students use the same link; the roster routes each student to the right grid.
### 6.3 During the window
- **Dashboard:** the summary grid (day × slot) shows the current request count per slot at any point during the window. Data reflects the state at page load; the admin refreshes the page to see new submissions. No push/auto-refresh requirement.
### 6.4 Window close and reopen
- At end time, the link becomes read-only for students and the Excel is **automatically emailed** to the teacher's contact email.
- The admin may extend the deadline before close, or reopen after close.
- **Versioning rule:** every window close event fires the email. The email subject carries an incrementing version number (e.g., "Week 25 Requests - Teacher Cohen - v2") so the teacher always knows whether a previously received file is stale.
- The Excel is also available for **on-demand download** from the admin UI at any time, including mid-window.
## 7. Student Flow
 
1. Student opens the single weekly link on a mobile or desktop browser.
2. **If the window is not yet open or already closed:** a clear read-only message with the relevant dates. No form.
3. Student enters their **national ID**.
   - **Known ID (in the roster):** the system resolves their teacher, car, and transmission from the roster; any existing submission for this publication is loaded for editing.
   - **Unknown ID (not in the roster):** a clear "we don't have you on file — please contact your school" message. No form, no submission. (New students are added by the admin re-uploading the roster.)
4. **Confirmation (display only):** the form shows the student their resolved details — "You are submitting availability for **Teacher X**" — sourced from the roster. There is no teacher selection, no mismatch warning, no "this is not my teacher" action, and no transmission question; the roster is authoritative.
5. Student declares the **target session count** for the week.
6. Student sees the week grid for their assigned teacher. `Unavailable` slots are visibly blocked and unselectable.
7. For each slot picked, the student specifies:
   - `Single` or `Double` session
   - Optional free-text constraint for that slot
8. Picks accumulate as a **ranked list in selection order**. The student may reorder before submitting. Minimum picks = target count; additional picks are permitted and serve as lower-ranked preferences.
9. Submit. A confirmation screen states that the submission can be edited via the same link and national ID until the window closes.
### Validation rules (student form)
- National ID present in the roster (it is both the identity mechanism and the access gate; no verification step in v1)
- Target count ≥ 1
- Picks ≥ target count
- A slot can be picked at most once per submission
- Submission rejected with a clear message if the window closed between page load and submit
## 8. Cross-Cutting Decisions
 
### 8.1 Demand counting
A `Double` session counts as **one request** in summary counts, by explicit decision. The detail sheet carries the single/double flag, which is what makes this acceptable. **Standing flag:** if the detail sheet is ever removed, this decision must be revisited, because three doubles and three singles would become indistinguishable.
 
### 8.2 Identity and abuse posture
- National-ID identification, no passwords, no magic links in v1. Submission requires the ID to exist in the roster, so random walk-ins cannot submit. Accepted risk: anyone holding the link who knows a roster member's national ID can impersonate that student. National IDs are semi-guessable (9 digits with a check digit), and because the link is now **school-wide**, the blast radius is the **whole school's** weekly preference lists rather than one teacher's. The mitigation remains that the teacher knows his students and reviews the Excel; this mitigation is weaker at school scale and is flagged for revisit if abuse appears.
- Re-entering the same national ID **loads the existing submission for editing**. This is mandatory: without self-service editing, correction requests return to WhatsApp and recreate the manual workload this system exists to eliminate.
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
| Student name | Full name (from roster) |
| National ID | Identifier |
| Phone | Contact number (from roster) |
| Transmission | Automatic / Manual |
| Session type | Single / Double |
| Rank | Position in the student's preference order |
| Target count | The student's declared weekly target |
| Constraints | Free text for this slot request |
 
### Delivery
- **Automatic:** emailed to the teacher's contact email at every window close (versioned per 6.4)
- **On-demand:** download button in the admin UI, available at any time
## 10. Non-Functional Requirements
 
- Student form must be fully usable on mobile browsers (primary distribution channel is WhatsApp)
- Links must be unguessable (no sequential identifiers)
- The system must tolerate concurrent submissions without losing or duplicating requests
- Data retention: past weeks remain queryable by the admin (history view is minimal in v1: list of past publications with Excel re-download)
## 11. Decisions Log
 
| # | Decision | Rationale |
|---|---|---|
| 1 | Admin = owner-teacher, single login, no teacher logins | v1 reality: one person plans for everyone |
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
| 17 | **National ID is the student identifier**, validated against an admin-uploaded roster (CSV); no self-registration | Customer supplies the student list as data; ID is the school's natural key (see ADR 0003) |
| 18 | **Transmission and teacher come from the roster**, not the form | The CSV already assigns each student a teacher and car; asking would be redundant (see ADR 0003) |
| 19 | **Unknown ID is rejected** ("contact your school"); new students added by re-uploading the roster | Roster is authoritative; removes self-service onboarding (see ADR 0003) |
| 20 | **No maximum on cars per teacher** (was 1–2); minimum one binds as "the last car can never be removed" and a teacher may exist carless until their first car is added | Fleet size is the school's business, not a system rule; the minimum only matters once car removal exists |
 
## 12. Explicitly Deferred (v2 candidates)
 
- Copy previous week's unavailability as a starting point
- Configurable slot windows and session lengths
- Teacher logins and per-teacher self-service
- Magic-link email verification
- Booking/assignment module consuming the ranked requests
- Student notification of final schedule
- Multi-school tenancy