# WhatsApp Reminders for Students Who Haven't Submitted - Intent

**Status:** Agreed intent, input to planning ([plan](whatsapp-reminders-plan/README.md))
**Date:** 3 November 2026
**Module:** `module:publications`

This document says **what** we want to achieve and **why**. It is not a plan: tasks and file lists come later, in a plan folder next to this file (`docs\modules\publications\whatsapp-reminders-plan\`, the same format as the other slices).

## 1. Outcome

While a Publication is `Open`, the Publication dashboard shows which Students haven't submitted yet: an Administrator sees them for **every teacher** in the Publication, in one list, and a Teacher-role User sees only their own teacher's. Next to each Student is a WhatsApp button. Pressing it opens WhatsApp on the sender's own phone or computer, in a chat with that Student, with a Hebrew reminder already written: the Student's name, the week, when the window closes and the weekly link. The sender presses send in WhatsApp, comes back, and moves on to the next Student.

DriveCal sends nothing itself. It works out who is missing and writes the message, and the sender's WhatsApp does the sending.

## 2. Why

- Some Students forget to submit while the window is open. Today nobody can see who they are until the window closes and the Excel arrives, and by then it is too late to ask.
- Chasing them is the part of the old WhatsApp workload that is still manual (requirements §1, §6.2 step 4). Even with the list, writing the same message by hand to every Student is slow and makes mistakes easy: the wrong link, the wrong end time, a forgotten Student.
- The automated route, WhatsApp messages from the school's number through the Meta Cloud API ([student notifications intent](https://github.com/silagy/DrivingLessonsBooking/blob/docs/notifications-intent/docs/modules/notifications/student-notifications-intent.md), branch `docs/notifications-intent`, not merged), needs the outbox foundation, Meta onboarding, Coexistence and approved templates before the first message goes out. A click-to-chat link (`https://wa.me/{number}?text={message}`) needs none of that, so it can ship now.

## 3. Scope

| # | Capability |
|---|------------|
| 1 | **Students without a Submission**: on the Publication dashboard, a list of active Students with no Submission in this Publication, with each Student's teacher and phone. School-wide for an Administrator, one teacher for a Teacher-role User (§4.6) |
| 2 | **WhatsApp button per Student**: opens a click-to-chat link to the Student's number, with the reminder text filled in |
| 3 | **Phone to WhatsApp number**: an Israeli phone as the Roster or an Administrator entered it (`05X-XXXXXXX`) becomes an international number (`9725XXXXXXXX`); a number that can't be converted shows the phone without a button |
| 4 | **Fixed Hebrew reminder text** (§4.4) |
| 5 | **Teacher-role Users see it too**, for their own teacher only, under the same scoping as the rest of the dashboard |
| 6 | **Docs record it**: requirements §3, §6.3, §6.5 and a new decision in §11 |

### Non-goals

- **No record of reminders** (decision 2, §7). DriveCal doesn't store who was reminded, by whom or when. A Student leaves the list when they submit, and only then.
- **No sending from DriveCal**: no WhatsApp Cloud API, no templates, no outbox, no background job, no "remind everyone" button. That stays with the notifications roadmap.
- No change to the Student form, the Submission rules, the Excel or the teacher's email.
- No editable message text and no message settings screen (decision 3, §7).
- No backend write, no domain event, no migration. This is a read (a query) and a client feature.

### Later (not in this slice)

- When the notifications module lands, its manual reminder (slice 4) sends to everyone at once from the school's number, and its delivery view can reuse this list and the same phone conversion. The per-Student button stays for one-off chasing.
- The same list, read-only, on a `Closed` Publication, to help decide whether to reopen.

## 4. Product behavior

### 4.1 Who is listed

A Student is listed for a Publication when all of these hold:

- the Student is active (an Inactive Student can't identify on the form, so reminding them is pointless);
- the Student's **current** Teacher has a Week Schedule in the Publication;
- the Student has **no Submission** in this Publication, under any Teacher.

Each row shows the Student's current Teacher. A Teacher-role User's list keeps only the rows of their own Teacher (§4.6).

So:

- A Student who submitted and then had **Change Teacher** applied is not listed under either Teacher, because they have a Submission.
- A Student who had Change Teacher applied before submitting is listed under the new Teacher, the one whose grid the form now shows them.
- A Student flagged because their car isn't one of their teacher's cars (requirements §5.5) is listed like anyone else. They can still submit.
- A Student whose Teacher has no Week Schedule in the Publication has no grid to submit to, so they aren't listed.

Order: by Teacher name, then Student name.

The count of Students listed stands on its own. It is **not** shown as "X of Y" against the dashboard's "students submitted" figure, because that figure counts Submissions made against one teacher's Week Schedule, including Students who have since moved to another teacher. The two numbers don't add up to the teacher's Students.

### 4.2 When it shows

- Only while the Publication is `Open`, including after a reopen.
- In `Draft` and `Published` the link doesn't accept submissions yet, and in `Closed` it no longer does, so the section isn't shown.
- Like the rest of the dashboard, the list reflects the page load (requirements decision #12). A Student who submits while the sender works down the list stays on it until the next load. This is useful: the list doesn't shift under the sender while they go through it. The section says the list is as of page load, and the sender can refresh before starting.

### 4.3 The button

- Opens `https://wa.me/{internationalNumber}?text={encodedMessage}` in a new tab or window. On a phone this opens the WhatsApp app; on a computer, WhatsApp Web or Desktop.
- The message goes out from **the sender's own WhatsApp**, whichever number that device uses. It may not be the school's number, and replies come back to the sender.
- The sender can still edit the text in WhatsApp before sending.
- Pressing the button changes nothing in DriveCal. Pressing it twice opens the chat twice.
- The dashboard is used on phones (the admin-mobile module), and a phone is where WhatsApp is most likely installed, so the list and the button must work at phone width, mirror-correct in Hebrew.

### 4.4 The message

Always Hebrew, whatever language the sender's UI is in: Students read Hebrew, and the Excel and the teacher's email are Hebrew too (requirements decision #22). It addresses the Student in the plural like the Student form, and calls a slot **שיעור אפשרי** (requirements decision #26).

Proposed wording, to be settled in planning:

> שלום {name}, עוד לא קיבלנו מכם את השיעורים האפשריים לשבוע {weekNumber} ({weekRange}) אצל {teacherName}.
> אפשר לשלוח אותם עד {closesAt} בקישור הזה:
> {link}
> ככל שתיתנו יותר שיעורים אפשריים, כך גדל הסיכוי שתקבלו את כל השיעורים שביקשתם.

- `{name}` is the Student's full name (a single field, requirements §5.5).
- `{weekNumber}`, `{weekRange}` and `{closesAt}` use the same formats as the Student form, in Asia/Jerusalem (requirements §8.3).
- `{link}` is the Publication's weekly link, the same one the dashboard's share box shows.

### 4.5 Phone numbers

- `PhoneNumber` keeps the text as entered (bidi marks removed, at least 7 digits). The conversion to a WhatsApp number is new, lives in the backend, and the response carries the converted number, or none.
- Proposed rules, to be settled in planning: strip spaces, dashes and brackets; `05XXXXXXXX` (10 digits) becomes `9725XXXXXXXX`; `+9725…` and `9725…` are kept as digits; anything else (landlines, foreign numbers, typos) can't be converted.
- A Student whose number can't be converted is still listed, with the phone as entered and no button, plus a short note that the number can't be used for WhatsApp. The sender can still call them or fix the number on the Students screen.
- The notifications intent needs the same conversion (its §5.7). This one is written so that module can reuse it.

### 4.6 Who sees it

- **Administrator**: one school-wide list, the Students of **every** Teacher in the Publication, whatever teacher the dashboard's teacher picker shows. The list is about the Publication, which is school-wide (requirements §5.4), not about the grid on screen. Proposed, to be settled in planning: a filter to narrow the list to one Teacher, with all Teachers as the default.
- An Administrator linked to a Teacher (the owner who also teaches) still sees every Teacher's Students, not only their own.
- **Teacher-role User**: their own teacher's Students only. Asking for the school-wide list, or for another teacher's Students, even by URL, is refused like the rest of the dashboard (`MayReach`).
- This is new for Teacher-role Users. Today they see Submissions but never the Students screen (requirements §6.5 step 4). They now see the names and phones of **their own** Students who haven't submitted. Their own Students only: still no Students screen, no other teacher's Students, no national IDs.

## 5. Documentation this will change

- `docs/requirements.md`:
  - §3 in scope: the dashboard also lists Students who haven't submitted, with a WhatsApp reminder button.
  - §6.3: the school-wide list, who is in it, the button and the fixed message.
  - §6.5 step 3: the Teacher-role User sees their own Students who haven't submitted, with their phones.
  - §11: a new decision #27 (click-to-chat reminders from the sender's own WhatsApp, no record kept).
  - Decision #13 is unaffected: it is about the final schedule, not a reminder to submit.
- `CONTEXT.md`: no new term. "Students who haven't submitted" is plain language, not a new concept.
- If the notifications branch merges later: its §4.7 "Students not yet submitted" and its phone conversion point to this work instead of redefining them. Its decision 1 (manual bulk reminder is Administrator only) still holds, because that action sends from the school's number. This button sends from the sender's own WhatsApp.

## 6. Success criteria

1. While a Publication is `Open`, an Administrator's list holds exactly the active Students, of every Teacher in the Publication, who have no Submission in it, each with their Teacher, ordered by Teacher and then Student name. It is the same list whichever teacher the dashboard's picker shows.
2. A Student who submits is gone from the list on the next page load. An Inactive Student is never listed. A Student who submitted and then had Change Teacher applied is listed under neither teacher.
3. Pressing a Student's button opens WhatsApp in a chat with that Student's number, with the Hebrew message filled in: their name, the week, the teacher, the end time in Asia/Jerusalem, and a link that opens this week's form.
4. The message is Hebrew even when the sender's UI is English.
5. A Student whose phone can't be converted is listed with their phone and no button.
6. A Teacher-role User sees only their own teacher's Students. Asking for the school-wide list or another teacher's Students is refused the same way the dashboard refuses it.
7. In `Draft`, `Published` and `Closed` the section isn't shown.
8. Nothing is written to the database when the list loads or a button is pressed.
9. The section works at phone width, in Hebrew (RTL) and English, light and dark.

## 7. Decisions closed for planning

| # | Question | Decision |
|---|----------|----------|
| 1 | How does this relate to the planned automated WhatsApp notifications (Cloud API, outbox)? | **Ship now, on its own.** Click-to-chat needs no Meta onboarding, templates or outbox. The notifications intent stays as it is; its bulk manual reminder comes later, and the per-Student button stays |
| 2 | Does DriveCal remember who was already reminded? | **No.** No record on the server or the device. A Student leaves the list only by submitting. A click-to-chat link can't tell whether the message was actually sent anyway |
| 3 | What is the message? | **Fixed Hebrew text**, built in (§4.4). The sender can edit it in WhatsApp before sending. No settings screen |
| 4 | Who sees the list and the button? | **The Administrator and the Teacher-role User** |
| 5 | Does an Administrator see one teacher at a time or the whole school? | **The whole school** (3 November 2026): every Teacher's Students in one list, each row showing the Teacher, whatever the dashboard's teacher picker shows. A Teacher-role User sees only their own teacher's Students |
