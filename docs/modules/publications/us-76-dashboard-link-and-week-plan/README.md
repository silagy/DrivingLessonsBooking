# #76: Publications Dashboard - Share Link While Open, Week Picker, Empty State - Task Index

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

Per-task breakdown of issue [#76](https://github.com/silagy/DrivingLessonsBooking/issues/76). Each task has its own GitHub sub-issue and its own file. Execute the tasks **in order**, one commit each. Each file is self-contained.

**Goal:** The publications dashboard always shows the admin the week they are looking at, with its link at hand. The share link stays available while the week is Open, the week can be picked in the header, a week with no publication says so, and a teacher is selected from the start.

**Architecture:** Client-only changes in the `publications` feature: the dashboard page template, a pure week-options helper, and `PublicationsStore`. No backend or API change. The existing `GET api/publications/by-week` 404 already tells the client that a week has no publication.

**Tech Stack:** Angular 21 (standalone, signals, zoneless), PrimeNG 21 `p-select` / `pButton`, Transloco, Vitest via `@angular/build:unit-test`. No new package.

**Spec:** issue [#76](https://github.com/silagy/DrivingLessonsBooking/issues/76) · [requirements §6.2–6.3](../../../requirements.md) (one link for all teachers, shared outside the system; the dashboard during the window) · US-10 [#11](https://github.com/silagy/DrivingLessonsBooking/issues/11) (copy the link with one click) · `.claude\rules\client-*.md`

**Branch:** `76-dashboard-link-and-week`, from `main` at `590bdc7`. The plan is committed on it before task 1.

**Design:** [designer-prompt.md](designer-prompt.md) is a prompt for Claude Designer that mocks the three new dashboard states. Run it before task 2 if you want to check the layout. The tasks below don't depend on it.

## Problems

| # | Symptom | Cause | Task |
|---|---------|-------|------|
| P1 | No share link while the week is Open | [publications-dashboard.page.html](../../../../client/src/app/features/publications/ui/pages/publications-dashboard/publications-dashboard.page.html) renders `<app-share-link-box>` only in `@case (PublicationState.published)`. The Open case shows stats and the grid only. | 1 |
| P2 | Opening Publications and choosing a teacher shows nothing | `PublicationsStore.selectedWeekStartState` defaults to the **current** week. When that week has no publication, `getByWeek` returns 404, `loadByWeek` resolves `undefined`, `state()` is `undefined`, and the `@switch` renders nothing. The page has no week picker (`weekOptions` is built but unused), so the only way to another week is a History link carrying `?week=`. | 2, 3 |
| P3 | An Open week shows zero counts before a teacher is chosen | `selectedTeacherIdState` starts `null`, so `dashboardResource` never loads and the stats read `0`. | 4 |

## Decisions (made while planning, challenge on review)

| # | Decision |
|---|----------|
| 1 | **The link is shown in Open, not in Closed.** Students get a read-only message on a closed link, so there is nothing to share. A reopened week is Open again and shows the link again. |
| 2 | **The week picker replaces the week range in the subtitle**, as a borderless select styled like the teacher name in the title. That's the header's existing pattern, and it adds no new row to the design. |
| 3 | **A week outside the picker's five upcoming weeks is added to the options** (`withWeekOption`), so a History link to a past week still shows as selected instead of a blank select. |
| 4 | **The default week stays the current calendar week.** With the picker and the empty state, that week is no longer a dead end. Opening on the currently Open week instead is a product decision, left out of this slice (see #76 "Out of scope"). |
| 5 | **The empty state reuses `publications.noPublication`**, the existing and unused key ("השבוע הזה עדיין לא הוכן."), plus one new key for the button. The button opens weekly prep without pre-selecting the week, because weekly prep takes no route inputs today (YAGNI). |
| 6 | **The first teacher is a `linkedSignal` over the sorted teacher list.** It defaults to the first teacher, and an explicit choice (`selectTeacher`, including `?teacherId=` from History) is kept when the list reloads. |

## Global Constraints

- No hardcoded user-visible strings. Every string is a Transloco key, and `he.json` and `en.json` change together in the same commit.
- Signals only: stores expose readonly signals, components never subscribe, and derived state is `computed()` / `linkedSignal()`.
- Every component stays `ChangeDetectionStrategy.OnPush`, with `input()` / `output()` functions only.
- Logical CSS properties only (`margin-block-start`, `padding-inline-start`). No `!important`. PrimeNG internals are styled only through `:host ::ng-deep` scoped to a `styleClass`.
- No comments in code. Test section markers `//given //when //then //expected` are the only exception.
- No long dashes or ellipsis characters in source (`SourceTextTest` guards this). Use a plain `-`.
- Tests are Vitest specs next to the code: `describe` / `it` with the section markers.

**Running client tests** (PowerShell, from `client\`; the default `npm` on this machine can't install, but `ng` via node works):

```powershell
& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js test --watch=false --include "src/app/features/publications/**/*.spec.ts"
```

## Review Focus

1. **A `?week=` URL for a week outside the five picker options** (a History link to a past week) must still show that week as selected, not a blank select. Covered in task 2.
2. **While the publication is loading**, the page must show the spinner, not flash the "not prepared" empty state. Covered in task 3.
3. **An Open week before any teacher is chosen** must not show zero counts as if they were real. The first teacher is selected automatically. Covered in task 4.
4. **An explicit teacher choice**, including `?teacherId=` from History, must survive the teacher list finishing loading. Covered in task 4.
5. **A Closed week** must not offer the share link. Covered in task 1.

## File Structure

| File | Change | Task |
|------|--------|------|
| `client\src\app\features\publications\ui\pages\publications-dashboard\publications-dashboard.page.html` | Share link in Open, week picker in subtitle, empty state | 1, 2, 3 |
| `client\src\app\features\publications\ui\pages\publications-dashboard\publications-dashboard.page.scss` | Share-box spacing, borderless week picker, empty-state layout | 1, 2, 3 |
| `client\src\app\features\publications\ui\pages\publications-dashboard\publications-dashboard.page.ts` | `RouterLink` and `appRoutes` for the empty-state link | 3 |
| `client\src\app\features\publications\ui\pages\publications-dashboard\publications-dashboard.page.spec.ts` | **New**: page rendering tests against a fake store | 1, 2, 3 |
| `client\src\app\features\publications\domain\week-options.ts` (+ `.spec.ts`) | `withWeekOption()` pure helper | 2 |
| `client\src\app\features\publications\state\publications.store.ts` | `weekChoices`; default-first-teacher `linkedSignal` | 2, 4 |
| `client\src\app\features\publications\state\publications.store.spec.ts` | **New**: store tests for the teacher default | 4 |
| `client\public\i18n\he.json`, `client\public\i18n\en.json` | `publications.dashboard.prepareWeek` | 3 |

## Execution Order

| # | File | Sub-issue | Task | Commit point |
|---|------|-----------|------|--------------|
| 1 | [task-01-share-link-while-open.md](task-01-share-link-while-open.md) | [#77](https://github.com/silagy/DrivingLessonsBooking/issues/77) | Share link while the week is Open (P1), TDD | ✅ own commit |
| 2 | [task-02-week-picker.md](task-02-week-picker.md) | [#78](https://github.com/silagy/DrivingLessonsBooking/issues/78) | `withWeekOption`, `weekChoices`, week picker in the subtitle (P2), TDD | ✅ own commit |
| 3 | [task-03-empty-state.md](task-03-empty-state.md) | [#79](https://github.com/silagy/DrivingLessonsBooking/issues/79) | "Not prepared" empty state with a weekly-prep button (P2), TDD | ✅ own commit |
| 4 | [task-04-default-teacher.md](task-04-default-teacher.md) | [#80](https://github.com/silagy/DrivingLessonsBooking/issues/80) | First teacher selected by default (P3), TDD | ✅ own commit |
| 5 | [task-05-verify-and-pr.md](task-05-verify-and-pr.md) | [#81](https://github.com/silagy/DrivingLessonsBooking/issues/81) | Full suite, build, Hebrew browser walk-through, PR | ✅ PR |

The PR body closes #76 and every sub-issue (`Closes #76`, `Closes #77` ... `Closes #81`).
