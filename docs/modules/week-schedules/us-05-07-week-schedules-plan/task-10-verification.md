# Task 10 of 10: End-to-end verification (both languages) + PR

> Part of [US-05–07: Week Schedules Module](README.md). Requires tasks 1–9 complete. Work on branch `6-us-05-07-week-schedules-module`, commands from the repo root unless noted.

- [ ] **Step 1: Full backend check**

```bash
dotnet build
dotnet test tests\DrivingLessons.Domain.Test\DrivingLessons.Domain.Test.csproj
```
Expected: build clean, all tests PASS.

- [ ] **Step 2: Run the app and verify in the browser** (use the Browser pane / launch config, PrimeNG overlay rAF caveat noted in project memory)

With Postgres up and the app running, sign in as admin and verify:

1. **US-05**: Nav shows "Weekly prep" → open it → select a teacher → grid appears for the default (next) week: Sun–Thu × Morning/Noon/Afternoon/Evening, Friday Morning+Noon only, Fri afternoon/evening cells void, no Saturday column, all cells "Open", counter "0 slots marked unavailable".
2. **US-06**: Click an Open slot → cell flips to Unavailable (striped), counter increments, **no success toast**.
3. **US-07**: Click that Unavailable slot → back to Open, counter decrements.
4. Switch week in the picker → fresh fully-open grid for that week; switch back → previous toggles persisted.
5. Refresh the page → state persists (server-backed).
6. **Hebrew/RTL**: toggle language → `dir="rtl"`, Sunday column at the inline-start (right) edge, day/window labels in Hebrew, time labels LTR-isolated (`<bdi>`), nav shows "הכנת שבוע". Layout mirror-correct — this is part of acceptance (client-i18n rule).
7. Network tab: initial selection issues GET → 404 → POST create → GET 200; each toggle issues exactly one POST + one GET reload.

- [ ] **Step 3: Push and open the PR**

```bash
git push -u origin 6-us-05-07-week-schedules-module
gh pr create --title "US-05..07: Week schedules module" --body "Closes #6, closes #7, closes #8

## Summary
- WeekSchedule aggregate (22 Slot children, Sunday WeekStart, Open/Unavailable transitions) through domain/application/infrastructure/API
- Client create-on-404 flow, shared WeekGridComponent, Weekly Prep page (EN + HE RTL)

🤖 Generated with [Claude Code](https://claude.com/claude-code)"
```

---

## Self-Review (done)

- **Spec coverage**: US-05 → Tasks 1–6, 9 (fully open grid, Friday short, no Saturday, hardcoded windows, no copy-from-prior-week — creation always builds fresh Open slots); US-06 → mark-unavailable path (Tasks 2, 3, 5, 8, 9); US-07 → mark-available path (same tasks). Decision 9 (no copying) holds — creation never reads prior weeks. Decision 15 (hardcoded windows) → `SlotWindowTimes`/`WeekGridDefinition`.
- **Type consistency**: `WeekStart`/`SlotId`/`SlotWindowType`/`SlotState` names consistent across all layers; client `SlotWindow`/`DayOfWeek` string enums match camelCase JSON of `SlotWindowType`/`System.DayOfWeek`; `by-teacher-and-week?teacherId&week` matches the API service.
- **Known judgment calls** (flagged inline with ">"): exact shapes of `TeacherId.cs`, `TeacherNotFoundException`, `resource()` API usage, and token names must be re-checked against the real files during execution — existing code always wins over this plan's snippets.
