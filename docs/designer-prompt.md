# Claude Designer Prompt — Driving Lessons Booking Mockups

Copy everything below the line into Claude Designer.

---

Create high-fidelity clickable mockups for a **weekly demand collection system for a driving school**. The mockups will be shown to the client (the school owner) to validate requirements assumptions before implementation, so favor realistic content over lorem ipsum and make every screen tell a story.

## Product in one paragraph

The school owner (the "admin", who is also a teacher) currently spends ~3 hours a week collecting students' lesson preferences over WhatsApp and assembling them into an Excel file. This system replaces that: the admin publishes a weekly availability grid per teacher, students submit ranked slot preferences through a shareable link (distributed via WhatsApp), and the system produces the Excel file automatically. Booking itself stays manual and out of scope — the Excel file is the end of the system.

## Two surfaces, two form factors

1. **Admin app — design desktop-first.** Used by one person: the school owner. Email + password login.
2. **Student form — design mobile-first (~375px).** Links arrive via WhatsApp, so assume a phone browser. No login, no password — students identify by typing their email.

Use a clean, friendly, slightly utilitarian style — this is a tool for a small business, not a startup landing page. Use realistic Israeli names (Teacher: "Cohen", "Levi"; students: "Noa Mizrahi", "Daniel Peretz", etc.) and realistic car names ("Corolla White", "i20 Silver").

## Core domain rules (bake these into every screen)

- **Week grid:** Sunday–Thursday have 4 slots: Morning 07:00–12:00, Noon 12:00–15:00, Afternoon 15:00–18:00, Evening 18:00–22:00. **Friday has only Morning and Noon. Saturday does not exist in the grid at all.** Never render Friday Afternoon/Evening or any Saturday cells.
- **Scheduling is per teacher, not per car.** A teacher owns 1–2 cars, each Automatic or Manual transmission. Cars only matter for the transmission question and admin setup.
- **Slots are Open by default; the admin can mark them Unavailable.** Unavailable slots appear blocked in both admin and student views.
- **Submission model:** a student declares a **target session count** (e.g., "I want 2 lessons this week"), then picks **at least that many slots**, building one **ranked preference list** in selection order (rank 1, 2, 3…). There are NO separate "primary" and "alternative" picks — extra picks are simply lower-ranked. Each pick has a session type (**Single** or **Double**) and an **optional free-text constraint** for that specific slot (e.g., "only after 16:00").
- **Submission window:** each published week has a start and end datetime (Asia/Jerusalem). Before open / after close, the student link shows a read-only message with dates — no form. The admin can extend a deadline or reopen a closed window.
- **Editing:** re-entering the same email on the same link loads the existing submission for editing, until the window closes.

## Screens to mock

### Admin (desktop)

1. **Login** — email + password, nothing else.
2. **Teachers & cars setup** — list of teachers, each with name, contact email, and 1–2 cars (car name, vehicle type, Automatic/Manual). Show one teacher with two cars of different transmissions.
3. **Weekly preparation** — pick a teacher and a target week, see the full Sun–Fri grid (all slots open by default), click slots to toggle them Unavailable. Show a few slots toggled off.
4. **Publish dialog** — set submission window start and end datetimes; on publish the system generates a unique shareable link with a copy button (admin distributes it via WhatsApp himself). Show publication states somewhere: Draft → Published (not yet open) → Open → Closed.
5. **Live dashboard (window open)** — the same day × slot summary grid showing the **current request count per slot**. Unavailable slots marked blocked. Include a manual "Refresh" affordance and a "data as of HH:MM" stamp — there is deliberately **no auto-refresh**. Include a "Download Excel" button available even mid-window, plus "Extend deadline" and (on a closed window) "Reopen" actions.
6. **Publications history** — list of past weeks per teacher with status, version number of the last emailed Excel (e.g., "v2"), and a re-download action.

### Student form (mobile)

A single linear flow — show it as a sequence of mobile screens:

1. **Window closed / not yet open** — friendly read-only message with the relevant open/close dates. No form.
2. **Email entry** — just an email field. Two branches: known email → everything prefilled and existing submission loaded for editing (show an "editing your submission" state); new email → ask first name + last name.
3. **Teacher confirmation** — prominently: "You are submitting availability for **Teacher Cohen**" with the transmission shown if his fleet is single-type (e.g., "Automatic"). A clearly visible **"This is not my teacher"** action that aborts the flow. Also mock the **mismatch warning** variant: a returning student whose regular teacher differs from this link's teacher.
4. **Transmission question — conditional.** Only shown when the teacher owns BOTH an automatic and a manual car. Mock both: one teacher where the question appears, one where it's silently skipped.
5. **Target count** — "How many lessons do you want this week?" integer ≥ 1.
6. **Slot picking** — the Sun–Fri grid, mobile-friendly. Unavailable slots visibly blocked and unselectable. Tapping a slot opens a small panel: Single/Double choice + optional free-text constraint. Picked slots show their rank number badge (1, 2, 3…).
7. **Ranked review** — the ordered list of picks with drag-to-reorder, target count shown against pick count (e.g., "Target: 2 · Picked: 5 — your top 2 are your preferred slots, the rest are backups"). Validation states: picks < target blocks submit.
8. **Confirmation** — "Submitted. You can edit anytime before the window closes by reopening this link and entering your email." Also mock the error state where the window closed between page load and submit.

### Excel preview (one screen)

A mockup of the generated Excel, two sheets, to validate the output format with the client:
- **Sheet 1 "Summary":** rows = slots (Morning/Noon/Afternoon/Evening), columns = Sunday–Friday, cells = request counts, Unavailable slots greyed/blocked, Friday Afternoon/Evening cells absent.
- **Sheet 2 "Detail":** one row per slot request, columns: Day, Slot, Student name, Email, Transmission, Session type (Single/Double), Rank, Target count, Constraints. Sorted by day, then slot, then rank.
- Show the email it arrives in, with the versioned subject: "Week 25 Requests – Teacher Cohen – v2".

### Localization

The UI is **Hebrew + English with a visible language toggle** on both surfaces. Hebrew is **RTL-first**: produce at least the student slot-picking screen and the admin dashboard in Hebrew with fully mirrored layout (not just translated text) to validate RTL with the client.

## Assumptions these mockups must surface for client validation

Design so the client can react to each of these explicitly (it's fine to annotate screens):

1. One ranked list + target count — no separate "primary/alternative" request types.
2. A Double session counts as **1** in the summary grid counts; the detail sheet carries the Single/Double flag.
3. Sun–Fri grid with short Friday and no Saturday; the four hardcoded slot windows.
4. Dashboard refreshes manually — no live push updates.
5. Email-only identity: anyone with the link who types a student's email can view/edit that student's submission. Self-service editing via the same link is intentional.
6. The teacher confirmation step and "This is not my teacher" escape hatch.
7. The transmission question appears only for dual-fleet teachers.
8. Every window close (including reopens) emails a new versioned Excel.
9. Booking/assignment is out of scope — the system ends at the Excel file.

Where a screen embodies one of these assumptions, add a small annotation callout so the client review can walk through them one by one.
