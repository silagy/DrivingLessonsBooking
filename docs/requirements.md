# Requirements Document: Weekly Demand Collection System for Driving Lessons
 
**Version:** 1.0
**Date:** 12 June 2026
**Status:** Approved scope for v1
**Scope discipline:** This document is technology-agnostic. A separate technology document (latest .NET, latest Angular, PrimeNG, signal-based patterns) will govern implementation.
 
---
 
## 1. Problem Statement
 
A driving school owner-teacher spends approximately three hours every week collecting students' scheduling preferences over WhatsApp and manually assembling them into an Excel file. The Excel file is the input to his actual booking work. The collection and assembly step is pure waste.
 
This system replaces the collection and assembly step. It does not replace the booking step.
 
## 2. Success Metric
 
One number: **minutes from submission-window close to a usable Excel file.** Target: under one minute. Current baseline: ~180 minutes.
 
## 3. Scope
 
### In scope (v1)
- Admin preparation and publication of weekly availability grids, per teacher
- Student-facing submission flow via shareable link
- Live admin dashboard during the submission window
- Excel generation (on-demand download and automatic email at window close)
- Hebrew + English UI with full RTL support
### Out of scope (v1)
- Actual booking / assignment of students to slots. The Excel file is the end of this system's responsibility.
- Notifying students of their final schedule
- Logins for teachers (teachers are data entities only)
- Payments, billing, multi-school tenancy
- Configurable slot windows (hardcoded in v1)
- Native mobile apps (the student form must be mobile-browser friendly, since links are distributed via WhatsApp)
## 4. Personas
 
| Persona | Description | Authentication |
|---|---|---|
| **Administrator** | The school owner. Also a teacher. Performs all planning for all teachers. | Email + password login |
| **Teacher** | A data entity. Owns one or two cars. Receives a weekly published link and the resulting Excel. No login in v1. | None |
| **Student** | Receives a teacher-specific link (typically via WhatsApp). Submits weekly session preferences. | Email-based identification, no password (see 8.2) |
 
## 5. Domain Model
 
### 5.1 Teacher
- Name, contact email (for receiving the Excel)
- Owns 1-2 **Cars**
### 5.2 Car
- **Name:** display label used in the admin UI and Excel (e.g., "Corolla White")
- **Type:** make/model of the vehicle
- Belongs to exactly one Teacher
- Transmission: `Automatic` or `Manual`
- Cars are attributes of a teacher's capability. **Scheduling is per teacher, not per car.**
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
- Belongs to one Week Schedule
- Generates a unique, unguessable **link** scoped to the teacher and week
- Carries a **submission window**: start datetime and end datetime (Asia/Jerusalem timezone)
- States: `Draft` → `Published (window not yet open)` → `Open` → `Closed`
- Admin may **extend** the end time or **reopen** a closed window at any time
### 5.5 Student
- Identified uniquely by **email** (per school)
- First name, last name
- Transmission preference: `Automatic` or `Manual`
- **Default teacher association:** the student's regular teacher. Stamped automatically from the teacher-scoped link on the student's first submission and updated to the most recent teacher used. Never asked as a form question, since the link already identifies the teacher.
- Profile fields are remembered across weeks and prefilled on return visits
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
### 6.2 Weekly preparation (per teacher)
1. Select a teacher and a target week.
2. View the fully open grid.
3. Toggle individual slots to `Unavailable`.
4. Publish: set the submission window start and end datetimes. The system generates the shareable link.
5. Distribute the link (outside the system, e.g., WhatsApp).
### 6.3 During the window
- **Dashboard:** the summary grid (day × slot) shows the current request count per slot at any point during the window. Data reflects the state at page load; the admin refreshes the page to see new submissions. No push/auto-refresh requirement.
### 6.4 Window close and reopen
- At end time, the link becomes read-only for students and the Excel is **automatically emailed** to the teacher's contact email.
- The admin may extend the deadline before close, or reopen after close.
- **Versioning rule:** every window close event fires the email. The email subject carries an incrementing version number (e.g., "Week 25 Requests - Teacher Cohen - v2") so the teacher always knows whether a previously received file is stale.
- The Excel is also available for **on-demand download** from the admin UI at any time, including mid-window.
## 7. Student Flow
 
1. Student opens the teacher-specific link on a mobile or desktop browser.
2. **If the window is not yet open or already closed:** a clear read-only message with the relevant dates. No form.
3. Student enters their **email**.
   - Known email: profile is prefilled; any existing submission for this publication is loaded for editing.
   - New email: student provides first name and last name.
4. **Teacher and transmission confirmation:**
   - The form prominently displays the teacher this link belongs to (e.g., "You are submitting availability for Teacher Cohen"). If the teacher's fleet has a single transmission type, that transmission is displayed alongside (e.g., "Automatic").
   - This gives the student an explicit opt-out point if he received the wrong link for the wrong teacher. A visible "This is not my teacher" action aborts the flow without creating a submission.
   - For a returning student whose stored default teacher differs from the link's teacher, the form shows a mismatch warning before proceeding.
   - **Transmission question (conditional):** rendered **only if the teacher owns both an automatic and a manual car.**
   - If the teacher owns cars of a single transmission type, the field is not shown and the value is stamped silently from the teacher's fleet.
5. Student declares the **target session count** for the week.
6. Student sees the week grid. `Unavailable` slots are visibly blocked and unselectable.
7. For each slot picked, the student specifies:
   - `Single` or `Double` session
   - Optional free-text constraint for that slot
8. Picks accumulate as a **ranked list in selection order**. The student may reorder before submitting. Minimum picks = target count; additional picks are permitted and serve as lower-ranked preferences.
9. Submit. A confirmation screen states that the submission can be edited via the same link and email until the window closes.
### Validation rules (student form)
- Email format validity (uniqueness is the identity mechanism; no verification email in v1)
- Target count ≥ 1
- Picks ≥ target count
- A slot can be picked at most once per submission
- Submission rejected with a clear message if the window closed between page load and submit
## 8. Cross-Cutting Decisions
 
### 8.1 Demand counting
A `Double` session counts as **one request** in summary counts, by explicit decision. The detail sheet carries the single/double flag, which is what makes this acceptable. **Standing flag:** if the detail sheet is ever removed, this decision must be revisited, because three doubles and three singles would become indistinguishable.
 
### 8.2 Identity and abuse posture
- Email-only identification, no passwords, no magic links in v1. Accepted risk: anyone holding the link can impersonate a student by entering their email. The blast radius is one teacher's weekly preference list; the mitigation is the teacher knows his students.
- Re-entering the same email **loads the existing submission for editing**. This is mandatory: without self-service editing, correction requests return to WhatsApp and recreate the manual workload this system exists to eliminate.
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
| Student name | First + last |
| Email | Identifier |
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
| 3 | Link and schedule scoped per **teacher**, not per car | Teacher may operate two cars; original per-car framing was an error |
| 4 | One ranked list + target count, no primary/alternative types | Simpler model, rank carries the same information |
| 5 | Double = 1 request in summary counts | Detail sheet carries the weight; flagged in 8.1 |
| 6 | Same email reloads submission for editing | Kills the WhatsApp correction loop |
| 7 | Sunday-Friday grid, short Friday (morning + noon) | Israeli work week |
| 8 | Admin can extend/reopen windows | Operational reality; mitigated by versioned emails |
| 9 | Every week starts fully open | Explicit choice over copying prior week |
| 10 | Transmission question rendered only when teacher has both types | Eliminates a redundant or impossible question |
| 11 | Constraints free text scoped per slot request | As originally envisioned |
| 12 | Dashboard during window, manual page refresh, no push updates | Admin checks demand on demand; real-time push is unjustified complexity |
| 13 | Booking and student notification out of scope | Excel is the end of this system |
| 14 | Hebrew + English toggle, RTL-first | Bilingual user base |
| 15 | Slot windows hardcoded in v1 | Configurability is a v2 concern |
 
## 12. Explicitly Deferred (v2 candidates)
 
- Copy previous week's unavailability as a starting point
- Configurable slot windows and session lengths
- Teacher logins and per-teacher self-service
- Magic-link email verification
- Booking/assignment module consuming the ranked requests
- Student notification of final schedule
- Multi-school tenancy