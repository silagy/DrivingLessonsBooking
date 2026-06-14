# Claude Designer Prompt — Driving Lessons Booking Mockups

Copy everything below the line into Claude Designer.

---

Create high-fidelity clickable mockups for a **weekly demand collection system for a driving school**. The mockups will be shown to the client (the school owner) to validate requirements assumptions before implementation, so favor realistic content over lorem ipsum and make every screen tell a story.

## Product in one paragraph

The school owner (the "admin", who is also a teacher) currently spends ~3 hours a week collecting students' lesson preferences over WhatsApp and assembling them into an Excel file. This system replaces that: the admin uploads a student roster (CSV that already binds each student to a teacher and car), prepares a weekly availability grid per teacher, and publishes the week once as a single shareable link (distributed via WhatsApp). Students identify by national ID; their roster record routes them to the right teacher's grid, where they submit ranked slot preferences. The system produces the Excel file automatically. Booking itself stays manual and out of scope — the Excel file is the end of the system.

## Two surfaces, two form factors

1. **Admin app — design desktop-first.** Used by one person: the school owner. Email + password login.
2. **Student form — design mobile-first (~375px).** Links arrive via WhatsApp, so assume a phone browser. No login, no password — students identify by typing their **national ID**, which is matched against the uploaded roster. One link serves all teachers; the roster routes each student to their teacher's grid.

Use a clean, friendly, slightly utilitarian style — this is a tool for a small business, not a startup landing page. Use realistic Israeli names (Teacher: "Cohen", "Levi"; students: "Noa Mizrahi", "Daniel Peretz", etc.) and realistic car names ("Corolla White", "i20 Silver").

## Core domain rules (bake these into every screen)

- **Week grid:** Sunday–Thursday have 4 slots: Morning 07:00–12:00, Noon 12:00–15:00, Afternoon 15:00–18:00, Evening 18:00–22:00. **Friday has only Morning and Noon. Saturday does not exist in the grid at all.** Never render Friday Afternoon/Evening or any Saturday cells.
- **Scheduling is per teacher, not per car.** A teacher owns 1–2 cars, each Automatic or Manual transmission. The roster assigns each student a specific car, so transmission is known from the roster and never asked.
- **Slots are Open by default; the admin can mark them Unavailable.** Unavailable slots appear blocked in both admin and student views.
- **Submission model:** a student declares a **target session count** (e.g., "I want 2 lessons this week"), then picks **at least that many slots**, building one **ranked preference list** in selection order (rank 1, 2, 3…). There are NO separate "primary" and "alternative" picks — extra picks are simply lower-ranked. Each pick has a session type (**Single** or **Double**) and an **optional free-text constraint** for that specific slot (e.g., "only after 16:00").
- **Submission window:** each published week has a start and end datetime (Asia/Jerusalem). Before open / after close, the student link shows a read-only message with dates — no form. The admin can extend a deadline or reopen a closed window.
- **Editing:** re-entering the same national ID on the link loads the existing submission for editing, until the window closes.

## Screens to mock

### Admin (desktop)

1. **Login** — email + password, nothing else.
2. **Teachers & cars setup** — list of teachers, each with name, contact email, and 1–2 cars (car name, vehicle type, Automatic/Manual). Show one teacher with two cars of different transmissions.
3. **Student roster upload** — upload a CSV (one row per student: full name, national ID, phone, assigned car, assigned teacher, address, license type). Show the post-upload result: a table of imported students grouped by teacher, a count of added/updated/deactivated rows, and an error panel listing rows that failed (e.g., unknown teacher/car, invalid ID). Re-upload replaces the roster by upserting on national ID.
4. **Weekly preparation** — pick a teacher and a target week, see the full Sun–Fri grid (all slots open by default), click slots to toggle them Unavailable. Show a few slots toggled off. (Repeat per teacher before publishing.)
5. **Publish dialog** — set the submission window start and end datetimes for the **whole week**; on publish the system generates **one** shareable link with a copy button (admin distributes it via WhatsApp himself). The link covers all teachers. Show publication states somewhere: Draft → Published (not yet open) → Open → Closed.
6. **Live dashboard (window open)** — per teacher, the same day × slot summary grid showing the **current request count per slot** (a teacher selector switches between teachers within the one open week). Unavailable slots marked blocked. Include a manual "Refresh" affordance and a "data as of HH:MM" stamp — there is deliberately **no auto-refresh**. Include a "Download Excel" button (per teacher) available even mid-window, plus "Extend deadline" and (on a closed window) "Reopen" actions that apply to the whole week.
7. **Publications history** — list of past weeks, each expandable to per-teacher Excel files with status and the version number of the last emailed Excel (e.g., "v2"), and a re-download action.

### Student form (mobile)

A single linear flow — show it as a sequence of mobile screens:

1. **Window closed / not yet open** — friendly read-only message with the relevant open/close dates. No form.
2. **National ID entry** — just a national-ID field. Two branches: **known ID** (in the roster) → proceed, with any existing submission loaded for editing (show an "editing your submission" state); **unknown ID** (not in the roster) → a friendly dead-end: "We don't have you on file — please contact your school." No form. Mock both branches.
3. **Your details (display only)** — prominently: "You are submitting availability for **Teacher Cohen**", with the student's resolved name and transmission shown read-only (all from the roster). No teacher selection, no "this is not my teacher" action, no mismatch warning, no transmission question — the roster is authoritative. A single "Continue" advances.
4. **Target count** — "How many lessons do you want this week?" integer ≥ 1.
5. **Slot picking** — the Sun–Fri grid for the student's assigned teacher, mobile-friendly. Unavailable slots visibly blocked and unselectable. Tapping a slot opens a small panel: Single/Double choice + optional free-text constraint. Picked slots show their rank number badge (1, 2, 3…).
6. **Ranked review** — the ordered list of picks with drag-to-reorder, target count shown against pick count (e.g., "Target: 2 · Picked: 5 — your top 2 are your preferred slots, the rest are backups"). Validation states: picks < target blocks submit.
7. **Confirmation** — "Submitted. You can edit anytime before the window closes by reopening this link and entering your national ID." Also mock the error state where the window closed between page load and submit.

### Excel preview (one screen)

A mockup of the generated Excel, two sheets, to validate the output format with the client:
- **Sheet 1 "Summary":** rows = slots (Morning/Noon/Afternoon/Evening), columns = Sunday–Friday, cells = request counts, Unavailable slots greyed/blocked, Friday Afternoon/Evening cells absent.
- **Sheet 2 "Detail":** one row per slot request, columns: Day, Slot, Student name, National ID, Phone, Transmission, Session type (Single/Double), Rank, Target count, Constraints. Sorted by day, then slot, then rank.
- Show the email it arrives in, with the versioned subject: "Week 25 Requests – Teacher Cohen – v2".

### Localization

The UI is **Hebrew + English with a visible language toggle** on both surfaces. Hebrew is **RTL-first**: produce at least the student slot-picking screen and the admin dashboard in Hebrew with fully mirrored layout (not just translated text) to validate RTL with the client.

## Assumptions these mockups must surface for client validation

Design so the client can react to each of these explicitly (it's fine to annotate screens):

1. One ranked list + target count — no separate "primary/alternative" request types.
2. A Double session counts as **1** in the summary grid counts; the detail sheet carries the Single/Double flag.
3. Sun–Fri grid with short Friday and no Saturday; the four hardcoded slot windows.
4. Dashboard refreshes manually — no live push updates.
5. National-ID identity: anyone with the link who types a roster member's national ID can view/edit that student's submission. Self-service editing via the same link is intentional; an ID not in the roster cannot submit.
6. One school-wide weekly link: the roster routes each student to their teacher; there is no teacher selection, mismatch warning, or "this is not my teacher" escape hatch.
7. Transmission and teacher come from the uploaded roster — never asked on the form.
8. Every window close (including reopens) emails a new versioned Excel, per teacher.
9. Booking/assignment is out of scope — the system ends at the Excel file.

Where a screen embodies one of these assumptions, add a small annotation callout so the client review can walk through them one by one.
