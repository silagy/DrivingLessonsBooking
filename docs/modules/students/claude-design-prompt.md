# Prompt for Claude Design - Students (module:students)

Source: spec [#82](https://github.com/silagy/DrivingLessonsBooking/issues/82), sub-issues [#93](https://github.com/silagy/DrivingLessonsBooking/issues/93) (list, filter, add), [#94](https://github.com/silagy/DrivingLessonsBooking/issues/94) (edit, deactivate, reactivate) and [#95](https://github.com/silagy/DrivingLessonsBooking/issues/95) (Change Teacher, Change Car). The rule "a Student's Car is always one of their Teacher's Cars" landed in [#92](https://github.com/silagy/DrivingLessonsBooking/issues/92).

Design it as a new file, `Students.html`, in the existing Claude Design project `6a0ab892-caa4-49f7-baff-bba7ca38c862` (the Users and Roles project; see [users-and-roles-design.md](../auth/users-and-roles-design.md)), so it reuses the same kit (`auth/kit.jsx`), shell and tokens. Paste everything below the line into Claude Design. Attach screenshots of the current Roster screen ("רשימת תלמידים"), the Users screen with its Add User and Edit details dialogs, and Cars & Teachers, so the designs match what already exists.

---

## Context

You are designing the next screens for an existing web app: a weekly lesson-planning system for a small driving school in Israel. Each week the school publishes every Teacher's available Slots; Students identify on a shared link with their national ID and submit ranked Slot Requests for their own Teacher.

Today Students enter the system only through the **Roster**: a CSV file the Administrator uploads. The import adds new Students and updates existing ones by national ID; it never deactivates anyone, and it rejects a row whose Car is not one of its Teacher's Cars.

This release gives Administrators a **Students** screen to manage Students by hand:

- **List and filter** every Student: national ID, name, phone, Teacher, Car, active state.
- **Add a Student** by hand, without a Roster file.
- **Edit details** (name, phone, address, start date, license type, national ID).
- **Deactivate** a Student who finished or left; **reactivate** one who returns. An Inactive Student can't submit on the student form.
- **Change Teacher**: move the Student to another Teacher **together with one of that Teacher's Cars**, in one step.
- **Change Car**: switch to another of the current Teacher's Cars.

**The rule that shapes every dialog:** a Student's Car is always one of their Teacher's Cars. One Car can serve several Teachers (shared Cars). A Teacher may have no Cars at all.

Design the screens, dialogs and states listed below. Extend the existing visual language. Do not invent a new one.

## Users and their goals

1. **Administrator (the school owner)**, desktop, Hebrew. Wants to enrol a new Student the moment they sign up, fix a typo in a phone or national ID, record that a Student moved to another Teacher or Car mid-course, and stop a Student who left from submitting, without waiting for the next Roster file. Teacher-role Users never see this screen.

## Existing design system (keep it)

- **Framework:** Angular with PrimeNG (Aura preset, customised). Use PrimeNG components only: Table, Dialog (DynamicDialog), Button, Select, InputText, InputMask or InputText for phone/ID, DatePicker, SelectButton, Tag, ConfirmDialog, Toast, Menu, Popover, Message. Current names: `Select` not Dropdown, `DatePicker` not Calendar, `Popover` not OverlayPanel.
- **Brand:** primary "sky" `#0057FF`; secondary "ocean" `#00BBC7`; brand gradient `135deg #0057FF → #00BBC7`. Surfaces: page `#FBFBFF`, card `#FFFFFF`, muted `#F5F6FF`, border `#E6E8F6`, ink `#121418`, secondary text `#52555C`, muted text `#9FA9BB`. Light mode only.
- **Type:** Noto Sans Hebrew for display and text.
- **Shape:** card radius 14px, field radius 6px, soft card shadow, a "lift" shadow on hover.
- **Shell:** as in the Users and Roles frames: top bar, horizontal navigation, language toggle, user menu.
- **Patterns to reuse from the Users screen:** the table card, the row actions kebab Menu, the muted row for an inactive item, Status Tags, the stacked-field dialog with Cancel / Save, the locked read-only field (lock icon + "can't be changed" note), inline 409 refusal messages in dialogs, success toasts.
- **Page pattern:** page title and subtitle, primary and secondary actions at the end of the header, then content in cards. Every list has loading (spinner), error (inline text with "Try again"), and empty (title and one-line hint) states.

## Hard constraints

- **Hebrew first, RTL.** Design every frame in Hebrew, right-to-left, then show the key frames listed under Deliverables in English (LTR). Use logical layout: the "start" side is right in Hebrew. Icons with direction flip.
- **National IDs and phone numbers are always LTR**, isolated inside Hebrew text so digits and dashes don't reorder. Dates show day-first (`dd/MM/yyyy`) in both languages.
- **Use these terms.** Student (תלמיד), Inactive Student (תלמיד לא פעיל), Teacher (מורה), Car (רכב), national ID (תעודת זהות), Roster (רשימת תלמידים / קובץ), Change Teacher (החלפת מורה), Change Car (החלפת רכב), Deactivate (השבתה) / Reactivate (הפעלה מחדש). Never "assign", "transfer", "reassign" or "move" a Student; never "delete" or "archive" a Student.
- **The server enforces the business rules; the UI shows its refusals.** When the server refuses an action (HTTP 409), show a clear translated message in the dialog or as a toast. Design these states explicitly.
- **The Car picker is always scoped to the chosen Teacher.** The UI never offers a Car that isn't one of that Teacher's Cars; the server would refuse it anyway.
- **Desktop-first** (1280px wide). Show the Students table at tablet width (768px) as a secondary frame.
- **Sample data must be fictional:** made-up Hebrew names, valid-looking but invented 9-digit national IDs, `050-0000000`-style phones.

## Screens and states to design

### 1. Navigation and how Students relates to Roster

- Today the nav item **"תלמידים" (Students)** opens the Roster upload page. In this release the nav item opens the new **Students** screen.
- The Roster upload page stays, reached from a secondary header action on the Students screen, **"ייבוא רשימת תלמידים" / "Import Roster"**, and it links back to Students. Show the action on the Students header and the back link on the Roster page.
- If you think a different arrangement is clearly better (for example Students and Roster as two tabs of one page), show it as an alternative frame, not instead.

### 2. Students screen (Administrator only)

- Header: title "Students", subtitle explaining Students are the people who submit weekly requests, and that this list includes Students from the Roster and Students added by hand. Primary action **Add Student**; secondary action **Import Roster**.
- **Filters** above the table:
  - **Teacher** (Select: "All Teachers" + each Teacher).
  - **Active state** (SelectButton: Active / Inactive / All; default Active).
  - Optional free-text search by name or national ID, if it fits without crowding. Pick one and show it.
- **Table columns:** Name · National ID (LTR) · Phone (LTR) · Teacher · Car · Status (Active / Inactive Tag) · row actions.
- **Row actions** (kebab Menu): Edit details · Change Teacher · Change Car · Deactivate. An Inactive Student's row is visually muted and offers Edit details and **Reactivate** (Change Teacher / Change Car can stay available or be hidden; pick one and say why).
- **Car-not-of-Teacher flag (#95):** existing data may hold Students whose Car is not one of their Teacher's Cars (from before the rule existed). Their Car cell shows a warning marker (icon + short text such as "Not this Teacher's Car") with a tooltip explaining it, and the row action "Change Car" is the obvious fix. Show one such row.
- Footer: count of Students shown, e.g. "{n} Students".
- States: loading, error, empty (no Students yet: hint to add one or import the Roster, with both actions), empty-after-filter ("No Students match these filters" + clear filters), and a populated table with about 8 Students across 3 Teachers: 6 active, 2 inactive, 1 flagged Car.

### 3. Add Student dialog (#93)

- Fields, in this order:
  - **National ID** (required, LTR, 9 digits).
  - **Full name** (required).
  - **Phone** (required, LTR).
  - **Teacher** (required, Select).
  - **Car** (required, Select): lists **only the selected Teacher's Cars**, each with its transmission as a small tag (Automatic / Manual). Disabled with the placeholder "Choose a Teacher first" until a Teacher is chosen. **Resets when the Teacher changes.** If the Teacher has exactly one Car, it may be preselected; say whether you'd do that.
  - **Address**, **Start date** (DatePicker), **License type** (free text), grouped under an "Optional" sub-heading.
- **Teacher with no Cars:** the Car field shows an inline message "This Teacher has no Cars yet. Assign one in Cars & Teachers." with a link, and Save stays disabled.
- Server-refusal states, one frame each:
  - "A Student with this national ID already exists" (name the existing Student if it fits).
  - "This national ID isn't valid" (wrong length or check digit).
  - Stale data: "This Car is not assigned to this Teacher. Refresh and choose again."
- Success: toast "Student added", the new row appears in the list.

### 4. Edit details dialog (#94)

- Fields: National ID · Full name · Phone · Address · Start date · License type.
- Teacher and Car are **not** edited here: show them as read-only text with a hint pointing to "Change Teacher" / "Change Car".
- A short note under National ID: changing it here doesn't change the Roster file; a later Roster file with the old ID would add a second Student, so the file must be corrected too.
- Server-refusal states: national ID already used by another Student; invalid national ID.

### 5. Deactivate and Reactivate (#94)

- **Deactivate confirmation** (ConfirmDialog). Copy explains: the Student can't submit on the student form until reactivated; their past Submissions stay; a Roster file that still lists them will reactivate them.
- **Reactivate:** a one-click row action with a success toast.
- Server-refusal states: "This Student is already inactive" / "This Student is already active" (stale screen), shown as toasts.

### 6. Change Teacher dialog (#95)

- Header shows the Student's name and current Teacher and Car for context.
- **New Teacher** (Select; the current Teacher is excluded or disabled with "(current)").
- **Car** (Select): only the new Teacher's Cars. **If the new Teacher also teaches on the Student's current Car, it is preselected** and marked "(current Car)". Otherwise it starts empty with "Choose a Car". Resets when the Teacher changes.
- Teacher with no Cars: same inline message as in Add Student; Save disabled.
- Note: existing Submissions stay with the week they were made in; from the next identification the Student sees the new Teacher's week.
- Server-refusal states: same Teacher (stale), Car not of the new Teacher (stale).
- Frames: the preselected-Car case and the empty-Car case.

### 7. Change Car dialog (#95)

- Header shows the Student's name, Teacher and current Car.
- **Car** (Select or a list of radio cards): only the current Teacher's Cars, each with its transmission. The current Car is shown disabled with "(current)".
- If the Teacher has only the current Car: a message "This Teacher has no other Cars" and no Save.
- Flagged Student (Car not of their Teacher): the dialog explains the current Car isn't one of the Teacher's Cars and asks to choose one of theirs. Show this frame.
- Server-refusal states: same Car (stale), Car no longer assigned to the Teacher (stale).

### 8. Student form: an Inactive Student identifies (#94)

- On the public, mobile student form, an Inactive Student who enters their national ID sees a clear message that they can't submit this week and should contact the school. Today they see the "not on the roster" message; design a distinct one ("Your registration isn't active") at 375px wide, Hebrew, matching the existing student-form "not on the roster" card.

## Out of scope (don't design)

- Deleting Students (Students are only deactivated).
- Bulk actions on several Students at once.
- Student history, audit log, or a per-Student Submissions view.
- Editing Teachers or Cars from this screen (Cars & Teachers keeps that).
- Changes to the Roster import itself, except the back link in section 1.
- Teacher-role Users: they never see the Students screen.

## Deliverables

1. A frame for every screen, dialog and state above, in Hebrew RTL, at 1280px wide (section 8 at 375px).
2. English (LTR) versions of: the Students screen, the Add Student dialog and the Change Teacher dialog.
3. The Students table at 768px wide.
4. A short component inventory: which PrimeNG component each element uses, and any new token or variant (for example the Car-not-of-Teacher warning, the transmission tag in the Car picker), expressed with the existing palette.
5. A copy deck: every new user-visible string in Hebrew and English, grouped by screen, so it can go straight into the translation files (same format as `auth/strings.jsx`).
