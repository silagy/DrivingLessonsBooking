# More Slots, Better Chance: Task Index (student-form slice 8)

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

Customer feedback (October 2026), applied to the student form.

**Goal:** A student understands that the number of slots they pick is not tied to the number of lessons they ask for, and is nudged to give more when their list leaves the school little room to schedule them.

**Architecture:** Client only. The rule "picks ≥ target count" (requirements §7) is unchanged in the domain, the API and the client. Task 1 rewrites the Hebrew and English copy: a slot becomes **שיעור אפשרי** on the Hebrew student form, and "backup" framing goes. Task 2 adds a non-blocking info message on the review step, driven by a pure domain function (`leavesLittleRoom`) that the store exposes as one computed signal. It is a UX hint over values the store already holds, never a rule (CLAUDE.md rule 12).

**Tech Stack:** Angular (standalone, signals), Transloco, PrimeNG `p-message` and `p-button`, Vitest via `ng test`. No backend change.

**Spec:** [More Slots, Better Chance - Intent](../more-slots-better-chance-intent.md) · [requirements §7](../../../requirements.md) · [first-submission plan](../us-33-first-submission-plan/README.md) (target, slots and review steps as built) · [Double Counts Once plan](../double-counts-once-plan/README.md) (the last copy change to the same steps)

**Branch:** `more-slots-better-chance` (off `main`; the intent and this plan are its first commit).

## Decisions (made while planning; challenge on review)

| # | Decision |
|---|----------|
| 1 | **Nudge threshold** (intent decision 1): shown while `pickCount < targetCount + 2`, only once the target is met, and only while the Week Schedule has an Open slot the student hasn't picked. Constant `ROOM_TO_SCHEDULE_MARGIN = 2` in `domain\target-count.ts` |
| 2 | **Hebrew word for a slot** (intent decision 2): **שיעור אפשרי** / **שיעורים אפשריים** in every Hebrew student-form string that today says **שעה / שעות** for a slot. **שיעורים** alone means the requested lessons only. **שעה** stays in `pick.constraintHint` ("רק אחרי 16:00"), where it is a clock time. **בחירה / בחירות** stays: it names a pick on the ranked list, a different thing from a slot |
| 3 | **"Don't have to match"**, not the customer's "no link" (intent decision 3): "no link" contradicts the minimum the form still enforces |
| 4 | **The nudge carries no numbers.** The summary line right above it already shows `יעד: 2 · נבחרו: 2`, and numbers in the sentence would need one/many variants for both the pick count and the target ("שיעור אפשרי אחד לשיעור אחד"). One title key and one body key; the intent's §4.2 carries the same wording |
| 5 | **`review.preferredOne` / `preferredMany` collapse into `review.preferred`**, with no parameter: the new line no longer names "ranks 1 to target". `SINGLE_PREFERRED_TARGET` and `preferredKey` go. The `--preferred` row styling stays (it is how the ranked list shows the top picks) |
| 6 | **`slots.backups` is renamed `slots.moreSlotsHint`** (class `.slots__backups` → `.slots__more-slots`): key names describe meaning (client-i18n), and the meaning changed |
| 7 | **The nudge's button reuses `studentForm.review.addSlots`** and the existing `addSlots` output (back to the slots step, picks kept). Its own class, `.review__more-room-add`, so specs tell it from the not-enough button |
| 8 | **Requirements and glossary record it**: §7 step 8, a new decision in §11 (number 26, or the next free one if another branch took 26 first; the notifications intent also plans one), and a **Slot** entry in `CONTEXT.md` |

## Global Constraints

- Every user-visible string is a translation key in both `he.json` and `en.json`, in the same commit (client-i18n rule 4).
- Plain hyphen and three dots only: `translations.spec.ts` must stay green.
- Hebrew addresses the student in the plural, like the rest of the form.
- Logical CSS properties only, inside `@layer app`, tokens only (no hex). Mobile-first at 375px; checked at 360px.
- The backend decides: no client change to `missingPicks`, `canSubmit` or any submit path. The nudge never disables Submit.
- No comments in code; `//given //when //then` in specs.

## Review Focus

1. **A student who picked every Open slot of the week** must never be nudged: there is nothing left to add. Pinned by task 2 `never suggests more slots when every open slot is picked`.
2. **The not-enough message and the nudge together** would contradict each other. Pinned by task 2 `never shows the suggestion together with the not-enough message`.
3. **A returning student** whose saved list is target 2 with three picks sees the nudge on the review step without changing anything. Pinned by task 2 `suggests more slots to a returning student whose saved list leaves little room`.
4. **The long Hebrew target body and nudge at 360px** wrap inside the card with no horizontal overflow. Checked in task 3 Step 2.
5. **A Hebrew string still saying שעה for a slot, or "גיבוי"** after the sweep, would undo decision 2 on one screen. Pinned by task 1 `call a slot שיעור אפשרי on the Hebrew student form, never שעה` and `never call extra picks a backup on the student form`.

## Execution Order

| # | File | Task | Commit point |
|---|------|------|--------------|
| 1 | [task-01-slot-wording.md](task-01-slot-wording.md) | Hebrew word sweep, target / slots / review copy, `review.preferred`, `slots.moreSlotsHint`, `CONTEXT.md` | own commit |
| 2 | [task-02-more-room-nudge.md](task-02-more-room-nudge.md) | `leavesLittleRoom`, store signal, review-step message and button, requirements | own commit |
| 3 | [task-03-verification-and-pr.md](task-03-verification-and-pr.md) | Phone check in Hebrew and English against a smoke API, full checks, PR | none (no source change) |

Client commands (project memory: the default npm is broken for installs, `npm run` is fine). From `client\`: `& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js test --watch=false`, with `--include <spec path>` to run one file.
