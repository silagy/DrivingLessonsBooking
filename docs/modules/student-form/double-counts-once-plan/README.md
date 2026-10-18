# Double Counts Once: Task Index (student-form slice 7)

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

Customer feedback 2 (18 October 2026), applied to the student form. This is the second half of one branch: run it after the [Detail Sheet Customer Feedback plan](../../excel/detail-sheet-feedback-plan/README.md). Its last task verifies both plans and opens one PR.

**Goal:** A student who picks a Double understands, before and after picking, that it counts as one lesson toward the weekly target, so the "not enough picks" message in the customer's screenshot no longer surprises them.

**Architecture:** Copy only. The rule (each Slot Request counts once toward the target, Single or Double: requirements §7 validation and §8.1) is unchanged in the domain and in the client. The target step gains a note under its unit line. The review step's not-enough message gains one sentence, shown only when the list holds a Double. Both strings are new translation keys in `he.json` and `en.json`.

**Tech Stack:** Angular (standalone, signals), Transloco, PrimeNG `p-message`, Vitest via `ng test`. No backend change.

**Spec:** the customer's feedback 2 and screenshot (target 2, one Double pick, "אין מספיק בחירות") · [requirements §7 and §8.1](../../../requirements.md) · [first-submission plan](../us-33-first-submission-plan/README.md) (target and review steps as built)

> שיעור כפול נספר כשיעור יחיד לצורכי היעד. אולי כדאי להוסיף הבהרה בדף בחירת היעד כדי למנוע בלבול, או לשנות את צורת הספירה.

**Branch:** `customer-feedback-excel-and-target`.

## Decisions (made while planning; challenge on review)

| # | Decision |
|---|----------|
| 1 | **Keep the counting; clarify the copy** (chosen by your human partner over counting a Double as two). No domain, API or store change. |
| 2 | **Target step note**, key `studentForm.target.doubleCountsOnce`, rendered as `<p class="target__note">` right after `.target__unit`. he: `שיעור כפול נספר כשיעור אחד ביעד. רוצים שיעור כפול אחד? בחרו 1.` en: `A double lesson counts as one lesson toward your target. Want one double lesson? Choose 1.` |
| 3 | **Review reminder**, key `studentForm.review.doubleCountsOnce`, rendered as `<span class="review__double-note">` inside the existing `.review__not-enough` message after the body, only when `missingPicks() > 0` and some item is `SessionType.double`. he: `שיעור כפול נספר כבחירה אחת.` en: `A double lesson counts as one pick.` It is display logic over the items the step already receives (`hasDoublePick` computed), not a rule. |
| 4 | **`studentForm.pick.doubleHint` "(2×)" stays.** It describes the length of the lesson, and the two new lines settle how it counts. |
| 5 | **Requirements record the decision:** §7 step 5 says the target step states that a Double counts as one lesson toward the target. §8.1 gains: "Customer feedback (October 2026) asked whether a Double should count as two toward the target; it stays one, and the form says so." |

## Global Constraints

- Every user-visible string is a translation key in both `he.json` and `en.json`, in the same commit (client-i18n rule 4).
- Plain hyphen and three dots only: `translations.spec.ts` must stay green.
- Logical CSS properties only, inside `@layer app`, tokens only (no hex). Mobile-first at 375px.
- The backend decides: no client re-implementation of the target rule beyond the existing `missingPicks` UX hint.

## Review Focus

1. **A short list with only Singles** must not show the Double reminder, or it misleads. Pinned by task 1 `leaves the double reminder out when a short list holds only single lessons`.
2. **A Double on a list that already covers the target** must not show it either (there is no message). Pinned by task 1 `shows no not-enough message or double reminder when a double covers a target of one`.
3. **The long Hebrew note at 360px** wraps inside the card with no horizontal overflow. Pinned by task 2 Step 2.
4. **The English toggle** shows the English copy, not a key. Pinned by task 2 Step 2.
5. **A returning student whose saved list holds a Double** sees the reminder if they lower their picks below the target. Covered by the same `hasDoublePick` computed over `items()`, which the returning-student flow fills from the saved submission; checked in task 2 Step 2.

## Execution Order

| # | File | Task | Commit point |
|---|------|------|--------------|
| 1 | [task-01-double-counts-once-copy.md](task-01-double-counts-once-copy.md) | Target note and review reminder (TDD in `student-form.page.spec.ts`), requirements | own commit |
| 2 | [task-02-verification-and-pr.md](task-02-verification-and-pr.md) | Both plans: phone check in Hebrew and English, full backend and client checks, PR | none (no source change) |

Client commands (project memory: the default npm is broken for installs, `npm run` is fine): `& "C:\Program Files\nodejs\node.exe" client\node_modules\@angular\cli\bin\ng.js test --watch=false` from `client\`.
