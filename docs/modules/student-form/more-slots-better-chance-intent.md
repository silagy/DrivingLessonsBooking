# More Slots, Better Chance - Intent

**Status:** Agreed intent, input to planning ([plan](more-slots-better-chance-plan/README.md))
**Date:** 26 October 2026
**Module:** `module:student-form`

This document says **what** we want to achieve and **why**. It is not a plan: tasks and file lists come later, in a plan folder next to this file (`docs\modules\student-form\more-slots-better-chance-plan\`, the same format as the other slices).

## 1. Outcome

A student filling in the weekly form understands that the number of slots they pick is **not** tied to the number of lessons they ask for, and that every extra slot they can make gives the school more room to schedule them. Students give more slots, and the school can actually build the week.

## 2. Why

Customer feedback (October 2026):

> צריך לרשום להם בעמוד שבו הם רושמים את המועדים האפשריים, שאין קשר בין כמות השיעורים שהם מבקשים, לבין כמות האופציות הפתוחות שעליהם לתת. לחשוב על ניסוח שמוכר.
>
> אני לא רוצה שהתלמידים יגבילו אותי, שתהיה לי גמישות כמה שיותר גדולה באפשרויות השיבוץ של כל תלמיד.
>
> לדוגמה: ״ככל שתיתנו יותר מועדים אפשריים, אתם מגדילים את הסיכויים שתוכלו לקבל את מספר השיעורים שביקשתם״.
>
> היה לי תלמיד שביקש שיעור אחד בודד, ונתן מועד אחד פנוי. זה גרוע מאוד. היתה תלמידה שביקשה שני שיעורים, ונתנה שני מועדים. זה קשה מאוד לשבץ אותם ככה כי אין גמישות.

The form today leads students to exactly that result. The rule is "picks ≥ target count" (requirements §7), and every line of copy around it anchors on the target:

| Step | Key | Today (he) | What it tells the student |
|------|-----|------------|---------------------------|
| Target | `studentForm.target.body` | אחר כך תבחרו לפחות אותו מספר של שעות, לפי סדר העדפה. | The target is the number to aim for |
| Slots | `studentForm.slots.backups` | בחירות נוספות = גיבוי | Extra picks are a spare tyre, rarely used |
| Review | `studentForm.review.preferredMany` | דירוגים 1-{{target}} הם השעות המועדפות שלכם; השאר הן גיבוי. | Same: only the first *target* picks matter |
| Review | `studentForm.review.unlockHint` | השליחה תיפתח כשיהיו לכם {{target}} בחירות. | Reaching the target is "done" |

A student who reads this sensibly picks exactly the target and submits. Nothing in the form says why more would help them.

## 3. Scope

| # | Capability |
|---|------------|
| 1 | **Copy rewrite** on the target, slots and review steps: the number of slots is independent of the number of lessons, and more slots mean a better chance of getting all the lessons asked for. "Backup" framing goes away |
| 2 | **Soft nudge on review**: when the list covers the target but leaves the school little room (§4.2), a non-blocking message repeats the "more slots, better chance" line with a button back to the slots step. Submit stays enabled |
| 3 | **Requirements record it**: §7 step 8 and a new decision in §11 |

### Non-goals

- **No rule change.** "Picks ≥ target count" stays the only minimum, in the domain and in the client. A student who truly has one free hour can still send one slot for one lesson. Raising the minimum was considered and rejected: it blocks students instead of persuading them, and the backend would have to know the Week Schedule's Open slot count to cap it fairly.
- No change to ranking, Single / Double, constraints, or how the Excel shows picks.
- No new API field or response change.

### Later (not in this slice)

- The WhatsApp reminder template ([student notifications intent](../notifications/student-notifications-intent.md)) could carry the same line. Templates need Meta approval, so it is a wording choice for that slice, not this one.
- Showing the school how much room each Submission gives (picks per target) on the Publication dashboard, to measure whether this worked.

## 4. Product behavior

### 4.1 Copy

Student-facing copy speaks in the plural, like the rest of the form.

**Hebrew words (decision 2, §7):** a slot is a **שיעור אפשרי** (plural **שיעורים אפשריים**) everywhere on the student form. **שיעורים** alone means only the lessons the student asks for, the target count. Every Hebrew student-form string that today calls a slot **שעה / שעות** changes, for example `target.pickSlots` (לבחירת שעות), `slots.title`, `slots.body`, `review.addSlots` (הוספת שעות), `review.unlockHint` and `pick.constraintLabel` (אילוץ לשעה הזו). **שעה** stays only where it means a clock time (`pick.constraintHint`: "רק אחרי 16:00"). Keys stay; only Hebrew values change. Planning lists every string.

Proposed wording for the lines this slice rewrites, to be settled in planning:

| Step | Key | he | en |
|------|-----|----|----|
| Target | `studentForm.target.body` (replaced) | בשלב הבא תבחרו שיעורים אפשריים לפי סדר העדפה. מספר השיעורים האפשריים לא צריך להיות שווה למספר השיעורים שביקשתם: ככל שתיתנו יותר שיעורים אפשריים, אתם מגדילים את הסיכוי לקבל את מספר השיעורים שביקשתם. | Next you'll pick the slots you can make, ranked by preference. They don't have to match the number of lessons: the more slots you give, the better your chance of getting all the lessons you asked for. |
| Slots | `studentForm.slots.backups` (replaced) | יותר שיעורים אפשריים = יותר סיכוי לקבל את השיעורים | more slots = better chance of getting your lessons |
| Review | `studentForm.review.preferredOne` / `preferredMany` (replaced) | דירוג 1 הוא השיעור האפשרי המועדף עליכם. כל שיעור אפשרי נוסף ברשימה מגדיל את הסיכוי שתקבלו את השיעורים שביקשתם. | Rank 1 is your preferred slot. Every extra slot on the list improves your chance of getting the lessons you asked for. |

`preferredOne` and `preferredMany` may collapse into one key once "ranks 1 to target" is no longer named. `unlockHint` keeps its meaning (it explains why Submit is locked): השליחה תיפתח כשיהיו לכם {{target}} שיעורים אפשריים.

### 4.2 Soft nudge on review

- Shown on the review step when **all** hold:
  - the target is met (`missingPicks() === 0`), so the existing not-enough message isn't showing;
  - the list gives the school little room: **picks < target + 2** (decision 1, §7);
  - the Week Schedule still has Open slots the student hasn't picked. A student who already picked every Open slot is never nudged.
- Content: an info `p-message`, not a warning. he: **רוצים להגדיל את הסיכוי?** הרשימה שלכם משאירה לנו מעט גמישות בשיבוץ. ככל שתיתנו יותר שיעורים אפשריים, אתם מגדילים את הסיכוי לקבל את מספר השיעורים שביקשתם. en: **Want a better chance?** Your list leaves us little room to schedule you. The more slots you give, the better your chance of getting all the lessons you asked for. No numbers in the sentence: the summary line right above it already shows the target and the pick count, and numbers would need one/many variants for both.
- One action, **הוספת שיעורים אפשריים** / **Add more slots**, the same navigation as the existing `addSlots` button.
- Submit stays enabled. The nudge is shown once per visit to the review step, never as a dialog that has to be dismissed.
- A returning student editing a saved Submission sees it under the same conditions.

It is display logic over values the store already holds (target, pick count, the week's Open slots), in the same spirit as `missingPicks`. It decides nothing: the backend still accepts any list with picks ≥ target (CLAUDE.md rule 12).

## 5. Documentation this will change

- `docs/requirements.md` §7 step 8: "The form encourages picking more slots than the target, because more slots give the school more room to schedule; the minimum stays the target." §11: a new decision recording this feedback and that the minimum was deliberately not raised.
- `docs/modules/student-form/README.md`: slice 8 row.
- `CONTEXT.md`: a note under Slot that the student form calls it **שיעור אפשרי** in Hebrew, and that **שיעור** alone means a requested lesson, never a Slot.

## 6. Success criteria

1. A student reading only the target step knows they may pick more slots than lessons, and why it helps them.
2. No student-form string still calls extra picks a "backup", and no Hebrew student-form string calls a slot שעה or a bare שיעור.
3. A student with target 1 and one or two picks, or target 2 and two or three picks, sees the nudge on review, with an Open slot left to add. They can still submit without adding. Target 2 with four picks sees no nudge.
4. A student who picked every Open slot of the week never sees the nudge.
5. The not-enough message and the nudge never show together.
6. Hebrew at 360px wraps inside the card with no horizontal overflow; the English toggle shows English, never a key.

## 7. Decisions closed for planning

| # | Question | Decision |
|---|----------|----------|
| 1 | Nudge threshold: only when picks equal the target (the customer's two examples), or with a margin? | **Decided: picks < target + 2.** The nudge stays until the list holds at least two slots beyond the target. A client constant, cheap to change |
| 2 | Hebrew word for a slot on the student form? | **Decided: שיעור אפשרי** (plural שיעורים אפשריים) everywhere on the student form; **שיעורים** alone stays for the target count. Keys stay; only Hebrew values change (§4.1) |
| 3 | Should the target step say outright "there's no link between the two numbers" (the customer's words), or the softer "they don't have to match"? | **Decided: "don't have to match"**: "no link" contradicts the minimum the form still enforces |
