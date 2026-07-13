# Task 14 of 14: End-to-end verification (both languages) + PR

> Part of [US-08–21: Publications Module](README.md) ([parent plan](../us-08-21-publications-plan.md)). Requires tasks 1–13 committed. Work on branch `9-us-08-21-publications-module`, commands from the repo root unless noted.

The acceptance pass. **No new source files** — this task only runs, observes, and audits, then opens the PR. Any defect found here is fixed in the offending task's files (and re-verified), not patched in a new file. Use `Email:Enabled=false` and short windows (minutes, not days) so the full lifecycle runs inside one sitting; the `LoggingEmailSender` writes recipient / subject / attachment-size lines you assert against instead of a real inbox.

**No new files.** Fixes land in existing task files if verification fails.

---

- [ ] **Step 1: Backend regression**

```bash
dotnet build
dotnet test tests\DrivingLessons.Domain.Test\DrivingLessons.Domain.Test.csproj
dotnet ef migrations has-pending-model-changes --project src\DrivingLessons.Infrastructure --startup-project src\DrivingLessons.Presentation.Web
```

Expected: build clean; all domain tests PASS (aggregate transitions, `__Must_Be_*` guards with `[DataRow]`, event assertions, the domain-event dispatcher); `has-pending-model-changes` reports **no pending changes** (the `AddPublications` migration from task 06 is complete).

- [ ] **Step 2: Client build + lint**

```bash
cd client && npm run build
```

Expected: production build succeeds — no template type errors, no unused imports, no missing-translation compile issues.

- [ ] **Step 3: Configure short windows + logging email**

In `src\DrivingLessons.Presentation.Web\appsettings.Development.json` confirm:

```json
"Email": { "Enabled": false }
```

Bring up Postgres, run the API, and tail the app log so `LoggingEmailSender` lines are visible. Start the client (Browser pane / launch config). Reference the `preview_start` / browser-MCP verification workflow — drive the real UI, and for PrimeNG overlays verify via element rects (hidden-pane rAF caveat in project memory), not screenshots alone.

- [ ] **Step 4: Full lifecycle E2E walk** (maps each step to its US / issue)

Sign in as admin. Prepare **two** teachers for the same week in weekly-prep first (each first WeekSchedule creates/reuses the school-wide Draft Publication via the `WeekScheduleCreated` handler — decision #2).

1. **Draft chip on prep** — weekly-prep shows the **Draft** state chip for the prepared week. → US-11 ([#12](https://github.com/silagy/DrivingLessonsBooking/issues/12))
2. **Publish** — click **Publish week…** → dashboard opens the publish dialog (`publish=1`). Set start **+2m**, end **+5m** (Asia/Jerusalem wall time) → **Publish** → 204. Verify the window is stored in **UTC** (`by-week` response `windowStartUtc`/`windowEndUtc`). → US-08 ([#9](https://github.com/silagy/DrivingLessonsBooking/issues/9))
3. **Link generated + copyable** — the dialog / **Published** dashboard shows one unguessable link; **Copy link** succeeds (toast). → US-09 ([#10](https://github.com/silagy/DrivingLessonsBooking/issues/10)) + US-10 ([#11](https://github.com/silagy/DrivingLessonsBooking/issues/11))
4. **Auto-open** — at start+2m the Quartz `OpenPublicationJob` fires → state flips to **Open** (refresh the dashboard to observe). → US-16 ([#17](https://github.com/silagy/DrivingLessonsBooking/issues/17))
5. **Dashboard grid + zeros + blocked** — **Open** dashboard shows the day×slot grid, per-teacher, with **zero** counts (submissions module not built — `ISubmissionQueries` stub), Unavailable slots rendered blocked; stat chips read 0 / 0 / —. → US-12 ([#13](https://github.com/silagy/DrivingLessonsBooking/issues/13))
6. **Refresh stamp** — click **Refresh** → the "data as of" stamp updates; confirm exactly one dashboard GET per click, no polling. → US-13 ([#14](https://github.com/silagy/DrivingLessonsBooking/issues/14))
7. **On-demand download (mid-flow)** — click **Download Excel** while Open → a `.xlsx` downloads (placeholder workbook: summary grid + detail headers). → US-14 ([#15](https://github.com/silagy/DrivingLessonsBooking/issues/15))
8. **Extend** — **Extend deadline** → new end **+2m** → 204; the close job reschedules to the new time only (the original does not fire). → US-15 ([#16](https://github.com/silagy/DrivingLessonsBooking/issues/16))
9. **Auto-close + one email per teacher, v1** — at the extended end the `ClosePublicationJob` fires → state **Closed**; the log shows **one** `LoggingEmailSender` line **per teacher** with subject `Week {n} Requests - {teacherName} - v1` and a nonzero attachment size. → US-16/17/18 ([#17](https://github.com/silagy/DrivingLessonsBooking/issues/17), [#18](https://github.com/silagy/DrivingLessonsBooking/issues/18), [#19](https://github.com/silagy/DrivingLessonsBooking/issues/19))
10. **Reopen** — **Reopen window…** → new end **+2m** → 204; state returns to **Open**, students could edit again. → US-19 ([#20](https://github.com/silagy/DrivingLessonsBooking/issues/20))
11. **Close again → v2** — at the new end it closes again; the log shows a fresh email per teacher with subject `… - v2` (per-teacher counter incremented). → US-18 ([#19](https://github.com/silagy/DrivingLessonsBooking/issues/19))
12. **History + re-download** — nav → **History**: per-teacher rows with state + **Last Excel** version (`v2` for the reopened week); **Re-download** on a closed row returns the file. **View dashboard** on an open/published row navigates with `teacherId`+`week`. → US-20 ([#21](https://github.com/silagy/DrivingLessonsBooking/issues/21)) + US-21 ([#22](https://github.com/silagy/DrivingLessonsBooking/issues/22))
13. **Restart reconciliation** — publish a week with a window whose **end is already in the past**, stop the app, restart it → the `PublicationReconciliationHostedService` closes the overdue window (and opens any due-to-open) on startup, emitting the close emails. → US-16/17 (downtime recovery, decision #4)

> Wrong-state transitions must 409 (surfaced as a toast, never retried): try to publish an already-published week, or extend a closed one. A stale Quartz job firing after an extend/reopen no-ops via the swallowed 409 (task 08).

- [ ] **Step 5: Hebrew/RTL audit**

**Static sweep** — every SCSS file added in tasks 12–13 must have **zero** physical-property hits (logical equivalents only):

```bash
grep -rnE "margin-(left|right)|padding-(left|right)|text-align:\s*(left|right)|[^-](left|right):" client/src/app/features/publications client/src/app/shared/components/publication-state-tag
```

Expected: **no output**. Fix any hit with `margin-inline-*` / `padding-inline-*` / `inset-inline-*` / `text-align: start|end`.

**Runtime pass** — toggle to עברית and walk every new surface; each must be mirror-correct:

- [ ] Dashboard **Draft** variant — chip + Publish button at the inline-end, notice text RTL.
- [ ] Dashboard **Published** variant — share-link box: title RTL, link LTR-isolated in `<bdi>` (does not reorder), copy button inline-end.
- [ ] Dashboard **Open** variant — stat chips flow RTL, numbers in `<bdi>`; grid **Sunday at the inline-start** edge, Friday PM/Saturday absent; "data as of" stamp inline-start with the time in `<bdi>`.
- [ ] Dashboard **Closed** variant — closed notice with version, Reopen/Download buttons inline-end.
- [ ] **publish-week** dialog — two datepickers, lifecycle stepper mirrors, timezone note RTL.
- [ ] **extend-window** + **reopen-window** dialogs — datepicker + actions at the inline-end.
- [ ] **History** table — headers RTL, window range and version pill in `<bdi>`, row action at the inline-end.
- [ ] **Weekly-prep** Draft chip + Publish button — chip/button placement mirrors.

**Missing-key check** — with the browser console open, walk every screen and dialog in **both** languages → zero Transloco missing-key warnings. Any missing key added to **both** `en.json` and `he.json`.

- [ ] **Step 6: Fix + re-verify (if needed)**

If any step failed, fix it in the owning task's files, re-run the affected build/test, and re-walk the affected step. Commit fixes with a `fix(client)` / `fix(...)` message. Do not open the PR until Steps 1–5 are green.

- [ ] **Step 7: Push + open the PR (closing #9–#22)**

```bash
git push -u origin 9-us-08-21-publications-module
gh pr create --title "US-08..21: Publications module" --body "Closes #9, closes #10, closes #11, closes #12, closes #13, closes #14, closes #15, closes #16, closes #17, closes #18, closes #19, closes #20, closes #21, closes #22

## Summary
- Publication aggregate (Draft → Published → Open → Closed, reopenable) with unguessable link token, submission window, and per-teacher versioned Excel counters — through domain / application / infrastructure / API
- Cross-cutting mechanisms the prior slices deferred: domain-event dispatch, Quartz scheduling (auto open/close + startup reconciliation), outbound email (LoggingEmailSender, Email:Enabled gate)
- Seams for not-yet-built work: IExcelGenerator (ClosedXML placeholder), ISubmissionQueries (zero-count stub)
- Angular publications feature: dashboard (4 state variants), publish/extend/reopen dialogs, history page, weekly-prep publish entry point (EN + HE RTL)

## Verification
- dotnet build + domain tests green; no pending EF model changes
- Full lifecycle E2E with Email:Enabled=false and short windows (publish → auto-open → extend → auto-close v1 → reopen → close v2 → history re-download → restart reconciliation)
- Hebrew/RTL audit across all new screens; logical CSS only

🤖 Generated with [Claude Code](https://claude.com/claude-code)"
```

---

## Completion checklist

- [ ] `dotnet build` clean, `dotnet test` green, no pending EF migrations
- [ ] `npm run build` (client) succeeds
- [ ] Lifecycle walk covers US-08…US-21, each step observed (state transitions, link, dashboard, download, extend, auto-close, versioned emails, reopen → v2, history, re-download)
- [ ] Restart reconciliation closes/opens overdue windows on startup
- [ ] Wrong-state transitions return 409 (toast, no retry); stale jobs no-op
- [ ] Hebrew/RTL audit passed on all 4 dashboard variants, 3 dialogs, history, and the prep chip; grep for physical CSS returns nothing; zero missing-key warnings in both languages
- [ ] `en.json` / `he.json` fully mirrored
- [ ] PR opened closing #9–#22
- [ ] Rules-doc deviations (domain-event dispatch now implemented, Quartz added) recorded in `us-08-21-publications-changes.md`
