# Prompt for Claude Design — Users, Roles and Sign-in (module:auth)

Source: spec [#82](https://github.com/silagy/DrivingLessonsBooking/issues/82), sub-issues [#84](https://github.com/silagy/DrivingLessonsBooking/issues/84)–[#90](https://github.com/silagy/DrivingLessonsBooking/issues/90). Paste everything below the line into Claude Design. Attach screenshots of the current Login, Dashboard, Teachers & Cars, Weekly prep and Publications screens so the designs match what already exists.

---

## Context

You are designing the next set of screens for an existing web app: a weekly lesson-planning system for a small driving school in Israel. Each week the school publishes every Teacher's available Slots; Students submit ranked Slot Requests through a shared link. Today exactly one person can sign in: the school owner, as the only Administrator.

This release adds **Users**: people who can sign in, each with exactly one **Role**:

- **Administrator** can do everything, as today. May optionally be linked to a Teacher (the owner who also teaches).
- **Teacher** sees only their own Teacher's Week Schedules and Publications and marks their own Slots Open or Unavailable. Always linked to exactly one Teacher.

Administrators manage Users from a new **Users** screen. Every User can change their own password.

Design the screens, dialogs and states listed below. Extend the existing visual language. Do not invent a new one.

## Users and their goals

1. **Administrator (the school owner)**, desktop, Hebrew. Wants to give each Teacher their own sign-in, keep control of the weekly cycle, and be sure they can never lock the school out.
2. **Teacher-role User**, desktop or tablet, Hebrew. Wants to sign in, see their own week straight away, mark when they can't teach, and see what Students requested from them. Never sees other Teachers' data.

## Existing design system (keep it)

- **Framework:** Angular with PrimeNG (Aura preset, customised). Use PrimeNG components only: Table, Dialog (DynamicDialog), Button, Select, InputText, Password, SelectButton, Tag, ConfirmDialog, Toast, Menu, Popover, Message. Current names: `Select` not Dropdown, `Drawer` not Sidebar, `Popover` not OverlayPanel.
- **Brand:** primary "sky" `#0057FF`; secondary "ocean" `#00BBC7`; brand gradient `135deg #0057FF → #00BBC7`. Surfaces: page `#FBFBFF`, card `#FFFFFF`, muted `#F5F6FF`, border `#E6E8F6`, ink `#121418`, secondary text `#52555C`, muted text `#9FA9BB`. Light mode only.
- **Type:** Noto Sans Hebrew for display and text.
- **Shape:** card radius 14px, field radius 6px, soft card shadow, a "lift" shadow on hover.
- **Shell:** a top bar with the brand logo, horizontal text navigation (Dashboard · Cars & Teachers · Students · Weekly prep · Publications · History), and on the end side a language toggle (Hebrew/English), an initials avatar, the signed-in email, and a text "Log out" button.
- **Page pattern:** page title and subtitle, primary and secondary actions at the end of the header, then content in cards or grids. Every list has loading (spinner), error (inline text), and empty (title and one-line hint) states.
- **Dialogs:** a form in a modal. Labelled fields stacked vertically. Validation message under the field. Footer has text "Cancel" (secondary) and "Save" (primary, disabled while the form is invalid).

## Hard constraints

- **Hebrew first, RTL.** Design every frame in Hebrew, right-to-left, then show one or two key frames in English (LTR) to prove the mirroring works. Use logical layout: the "start" side is right in Hebrew. Icons with direction (arrows, chevrons) flip.
- **Emails and passwords are always LTR**, even inside Hebrew layouts, isolated so punctuation doesn't jump.
- **Use these terms.** User (משתמש), Role (תפקיד), Administrator (מנהל), Teacher (מורה), Temporary Password (סיסמה זמנית), Deleted User (משתמש שנמחק). Never use "account", "admin user", or "login" as a noun for a person.
- **The server enforces the business rules; the UI shows its refusals.** When the server refuses an action (HTTP 409), show a clear translated message in the dialog or as a toast. Design these error states explicitly. Don't design the UI as if it could predict every refusal in advance.
- **Desktop-first** for every screen in this release (minimum 1280px wide). Show the Users table at tablet width (768px) as a secondary frame.
- **Sample data must be fictional:** made-up Hebrew names, `@example.com` emails.

## Screens and states to design

### 1. Sign in (update the existing screen)

- Same email and password form. Replace the subtitle and footnote, which today say "single admin account". New copy says this is the sign-in for the school's staff (Administrators and Teachers).
- Error state: one generic message for a wrong email, a wrong password, or a Deleted User ("Email or password is incorrect"). Never reveal which one it was.
- "Forgot your password?" hint: "Ask your school's Administrator to set a Temporary Password." (There is no self-service reset.)

### 2. Shell, navigation by Role

- **Administrator:** today's navigation plus a new **Users** item (place it last, after History).
- **Teacher Role:** only Weekly prep (Week Schedules) and Publications (and History if it fits). Dashboard, Cars & Teachers, Students, Roster and Users are not shown at all, not even disabled.
- **User menu:** replace the bare avatar and email with an avatar button that opens a Menu or Popover. It shows the User's name, email, and a **Role tag** (Administrator / Teacher, plus the linked Teacher's name when there is one), then "Change my password" and "Log out". The language toggle stays in the bar.
- Frames: the Administrator bar, the Teacher bar, and the open user menu for each.

### 3. Users screen (Administrator only)

- Header: title "Users", subtitle explaining that Users are the people who can sign in, primary action **Add User**.
- **Table columns:** Name · Email (LTR) · Role (Tag) · Linked Teacher (name, or a muted "—") · Status (Active / Deleted Tag) · row actions.
- **Row actions** (a kebab Menu or icon buttons): Edit details · Change Role · Set Temporary Password · Delete. A Deleted User's row is visually muted and offers only **Restore**.
- Mark the signed-in User's own row ("You"), which matters for the self-protection rules below.
- Consider a "Show Deleted Users" toggle, or Deleted Users sorted last. Pick one and show it.
- States: loading, error, empty (only the first Administrator exists, so prompt to add a Teacher's sign-in), and a populated table with about 6 Users: 2 Administrators (one linked to a Teacher), 3 Teacher-role Users, 1 Deleted User.

### 4. Add User dialog

- Fields: Name · Email (LTR) · Role (SelectButton: Administrator / Teacher) · Linked Teacher (Select) · Temporary Password (Password field with a show/hide toggle).
- **Linked Teacher** is required when Role = Teacher and optional when Role = Administrator ("Link if this Administrator also teaches"). Show both variants.
- Teachers that already have a User appear disabled in the list with "Already has a User".
- Helper text under the Linked Teacher field: **the link can't be changed after creation**.
- Helper text under the Temporary Password field: the Administrator must pass it to the person themselves (no email is sent).
- Server-refusal states: "This email is already used by another User"; "This Teacher already has a User".

### 5. Edit details dialog

- Fields: Name and Email only.
- The linked Teacher (if any) appears as read-only text with a lock icon and the note "can't be changed".
- Server-refusal state: email already in use.

### 6. Change Role dialog

- Shows the current Role and lets the Administrator pick the other one.
- **Promote** Teacher → Administrator: the Teacher link is kept. Say so.
- **Demote** Administrator → Teacher: allowed only when the User is linked to a Teacher.
- Explain that the change takes effect on that person's next action (they may be signed out).
- Server-refusal states, one frame each:
  - Demoting a User with no linked Teacher
  - Demoting yourself
  - Demoting the last active Administrator
  - Picking the Role the User already has (stale screen)

### 7. Set Temporary Password dialog

- Shows the User's name and email for context, then the new Temporary Password field (show/hide).
- Note: their current sessions end immediately, and the Administrator must tell them the new password.

### 8. Delete and Restore

- **Delete confirmation** (ConfirmDialog, danger button). Copy explains three things: the person is signed out **immediately**; their Teacher record, Week Schedules and Publications are **not** affected; and the action can be undone with Restore.
- Server-refusal states: "You can't delete yourself"; "You can't delete the last active Administrator"; "This User is already deleted" (stale screen).
- **Restore:** a one-click row action with a success toast, plus the "already active" refusal.

### 9. Change my password dialog (every User)

- Opened from the user menu. Fields: Current password · New password · Confirm new password.
- States: mismatch (client-side), wrong current password (server refusal), success.
- **After success the User stays signed in, with a success toast.** Also design the alternative as a separate frame: a success message, then return to the sign-in screen asking them to sign in with the new password. This lets us choose between them.

### 10. Teacher-role experience on existing screens

- **Weekly prep / Week Schedules:** the Teacher lands here after signing in, on **their own** Teacher. There is no Teacher picker; show their Teacher's name as a static label or a locked chip. Marking Slots Open or Unavailable looks and works exactly as it does for the Administrator. "Create Week Schedule" is not shown.
- **Publications:** lists only their own Publications, with state, Submissions, the share link (copy) and Excel download. The lifecycle controls (Publish, Open, Close, Reopen, Extend) are **hidden**, not disabled.
- **Administrator linked to a Teacher:** sees everything, but Weekly prep defaults to their own Teacher. The Teacher picker stays available, with a "My schedule" shortcut or a highlighted "(me)" entry.
- **Refused URL:** if a Teacher types a URL for a screen they can't use, they are redirected to their Week Schedule. Optionally show an info toast.

### 11. Cars & Teachers (small change)

- Teacher card: an optional indicator that this Teacher has a sign-in (a small "Has a User" tag or a user icon).
- Delete Teacher refusal: "This Teacher still has an active User. Delete the User first." Show this as the 409 state of the existing delete confirmation.

## Out of scope (don't design)

- Self-service "forgot password", password emails, invitation links.
- Forcing a password change at the next sign-in.
- More than two Roles, per-permission settings, or Users with several Roles.
- Student sign-in (Students keep identifying by national ID on the public student form).
- Audit log or User history screens.
- The Students management screens (Change Teacher / Change Car). These are a separate module with their own brief.

## Deliverables

1. A frame for every screen, dialog and state above, in Hebrew RTL, at 1280px wide.
2. English (LTR) versions of: the Users screen, the Add User dialog, and the user menu.
3. The Users table at 768px wide.
4. A short component inventory: which PrimeNG component each element uses, and any new token or variant (for example, a "deleted row" style or Role tag colours), expressed with the existing palette.
5. A copy deck: every new user-visible string in Hebrew and English, grouped by screen, so it can go straight into the translation files.
