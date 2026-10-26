# Chronological Picks on the Detail Sheet - Intent

**Status:** Agreed intent, input to planning
**Date:** 26 October 2026
**Module:** `module:excel`

This document says **what** we want to achieve and **why**. It is not a plan: tasks and file lists come later, in a plan folder next to this file (`docs\modules\excel\chronological-picks-plan\`, the same format as the other slices).

## 1. Outcome

A teacher reading sheet 2 (פירוט בקשות) sees each student's slots numbered in calendar order, Sunday morning to Friday evening: "1 מתוך 8" is the student's earliest slot of the week, "8 מתוך 8" the latest. The student's preference rank is no longer on the sheet. The target count stays, so the teacher still knows how many lessons to book.

## 2. Why

Customer feedback (26 October 2026) on the file for the week of 25 October:

> דירוג לא מעניין. השיעור המבוקש צריך להיות סדר יורד מיום ראשון בבוקר ועד יום שישי.
>
> כמה מתוך כמה… לפי סדר עולה מיום ראשון בבוקר ועד שישי בערב.

The teacher books the week day by day, not by what the student liked best. Today the sheet tells them the wrong thing on two columns:

| Column | Today | What the teacher reads |
|--------|-------|------------------------|
| דירוג | The student's preference order (1 = most preferred) | A number they don't use when booking |
| שיעורים מבוקשים | `{target} מתוך {picks}`, the same on every row of a student (מתן אלטמן: "3 מתוך 8" on all eight rows) | No way to tell which of the student's eight slots a row is, or how far through the student's week it falls |

The rows themselves already run from Sunday morning to Friday evening (day, then slot); rank is only the tie-breaker inside a slot. So the change is to the columns and to that tie-breaker, not to the day order.

## 3. Scope

| # | Capability |
|---|------------|
| 1 | **Rank leaves sheet 2**: the דירוג column goes, and rank stops being a sort key |
| 2 | **Calendar position per student**: a column reading `{position} מתוך {picks}`, where position counts the student's Slot Requests from Sunday morning to Friday evening |
| 3 | **Target in its own column**: the student's target count as a number, so "how many lessons to book" survives the change |
| 4 | **Docs record it**: requirements §9 Sheet 2, a locked decision and a slice row in the [Excel roadmap](README.md) |

### Non-goals

- **Rank stays everywhere else** (decision 2, §7). Students still order their picks on the form, the domain still stores `Rank`, and the admin slot-requests dialog on the Publication dashboard still shows it.
- No change to sheet 1 (the summary grid), the email, the download endpoint or the file name.
- No client change, no API change, no migration.

## 4. Product behavior

### 4.1 Columns

Proposed, to be settled in planning:

| # | Header | Content | Example |
|---|--------|---------|---------|
| 1 | יום | unchanged | ראשון |
| 2 | משבצת | unchanged | בוקר |
| 3 | שם התלמיד/ה | unchanged | מתן אלטמן |
| 4 | רכב | unchanged | ג'ילי - 25443704 |
| 5 | סוג שיעור | unchanged | כפול |
| 6 | שיעור אפשרי | `{position} מתוך {picks}` (new) | 2 מתוך 8 |
| 7 | שיעורים מבוקשים | the target count, a number (was `{target} מתוך {picks}`) | 3 |
| 8 | אילוצים | unchanged | פנוי אחרי 13:30 |

Still eight columns. **שיעור אפשרי** is the student form's word for a slot (requirements §11 decision 26), so the teacher and the student name the same thing the same way; **שיעורים מבוקשים** keeps meaning the lessons asked for.

### 4.2 Position

- A student's Slot Requests in the Submission are numbered 1 to *picks* by day (Sunday to Friday), then slot (Morning, Noon, Afternoon, Evening). A Submission never holds the same slot twice, so the order is total.
- *picks* is every Slot Request in the Submission, the same rows the sheet lists (Excel roadmap decision 4). A slot the admin marked Unavailable after the student picked it keeps its number, and the others don't shift.
- A Double is one Slot Request, so it takes one number.
- Numbering is per Submission, not per name: two students with the same name are numbered separately.
- Rank plays no part. A student whose favourite slot is Thursday evening still sees it as their last number.

### 4.3 Row order

Day, then slot, then student name, then car name (ordinal). Rank drops out as the tie-breaker, so inside one slot students read alphabetically.

## 5. Documentation this will change

- `docs/requirements.md` §9 Sheet 2: "sorted by day, then slot, then student name"; the Rank row goes; Requested lessons becomes the target count; a new row for the slot's position among the student's picks.
- `docs/modules/excel/README.md`: slice 5 row and locked decision 8.

## 6. Success criteria

1. Sheet 2 has no דירוג column, and no cell carries a student's rank.
2. A student with eight picks shows `1 מתוך 8` on their Sunday-morning-most row and `8 מתוך 8` on their Friday-evening-most row, each number once, whatever order they ranked them in.
3. The target count shows as a number on every row of the student.
4. A slot marked Unavailable after the student picked it is still listed, still numbered, and still counted in *picks*.
5. Two students with the same name in the same file each count from 1.
6. Rows within one slot are ordered by student name, then car name.
7. The student form and the admin slot-requests dialog still show rank exactly as before.

## 7. Decisions closed for planning

| # | Question | Decision |
|---|----------|----------|
| 1 | What does "כמה מתוך כמה" mean: target over picks (today), or the row's place in the student's week? | **Decided: the row's place**, `{position} מתוך {picks}`, counted Sunday morning to Friday evening, **and the target stays** in its own column so the teacher still knows how many lessons to book |
| 2 | How far does "דירוג לא מעניין" reach? | **Decided: sheet 2 only.** The form, the domain and the admin slot-requests dialog keep rank |
