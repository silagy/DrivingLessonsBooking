# US-08–21: Publications Module — Changes from the Plan

Companion to the [task index](us-08-21-publications-plan/README.md) and the
[parent plan](us-08-21-publications-plan.md). Records how the implementation on branch
`9-us-08-21-publications-module` diverged from the rules docs and the plan, plus the one defect
found and fixed during end-to-end verification (task 14).

## Summary

The plan was followed faithfully across all four onion layers and the Angular feature. The
meaningful, reviewer-relevant points are:

1. **Two cross-cutting mechanisms the rules docs assumed but earlier slices had deferred are now
   built**: in-process **domain-event dispatch** and **Quartz.NET scheduling** (auto open/close +
   startup reconciliation).
2. **Seams for not-yet-built work** return placeholders: `IExcelGenerator` (ClosedXML placeholder),
   `ISubmissionQueries` (zero counts), `IEmailSender` (`LoggingEmailSender`, gated by `Email:Enabled`).
3. **One defect found and fixed in verification**: the dashboard refresh issued two dashboard GETs
   per click; fixed to one (US-13).

## Rules-doc deviations

| Area | Rules doc said | Actual | Why |
|------|----------------|--------|-----|
| Domain-event dispatch | `ddd-architecture.md` describes events "dispatched after `SaveChanges`" as if the mechanism exists | This slice **builds** it: `IDomainEvent`/`IDomainEventHandler`/`IDomainEventDispatcher` (Application), `DomainEventDispatcher` (Infrastructure), invoked from `DrivingLessonsDbContext.CommitAsync` after `SaveChangesAsync`, then `CommitEvents()` clears them | The teachers slice deferred it; the `WeekScheduleCreated`→Draft handler and `PublicationClosed`→email handler need it |
| Quartz.NET | Not part of the documented stack (`CLAUDE.md`/`tech-stack.md`) | New `Quartz` dependency + `Infrastructure/Scheduling/` (`OpenPublicationJob`, `ClosePublicationJob`, `PublicationScheduler`, `PublicationReconciliationHostedService`), RAMJobStore (non-persistent) | Time-based auto open/close and downtime reconciliation (decision #4) |
| Open/Close endpoints | API guidelines imply a controller per operation | **No HTTP endpoints** for Open/Close — Quartz jobs and the reconciliation service call the interactors directly (decision #11) | Open/Close are time-driven, never admin-driven |
| Email | Not in the documented stack | `IEmailSender` + `LoggingEmailSender` behind `Email:Enabled` (`false` in Development) — logs recipient/subject/attachment-size instead of sending (decision #8) | Real SMTP/SES is a later slice |

The conventions that intentionally override the rules docs (no `UnitOfWork` property style differences,
`[EndpointSummary]` over `[SwaggerOperation]`, `FirstOrDefaultAsync`, per-project `DependencyInjection.cs`,
Transloco lowercase namespaces) are listed in the plan README and were followed as prior slices established.

## Defects found and fixed in verification (task 14)

| Symptom | Cause | Fix | Commit |
|---------|-------|-----|--------|
| A single dashboard **Refresh** click issued **two** `GET .../dashboard` requests (US-13 requires exactly one) | `PublicationsStore.refresh()` reloaded both `publicationResource` and `dashboardResource`; the dashboard resource already reloads via its params dependency on the publication value, so the explicit reload was redundant | Dropped the explicit `dashboardResource.reload()`; the publication reload cascades exactly one dashboard fetch and still surfaces state changes | `fix(client): avoid duplicate dashboard fetch on publications refresh` |
| **DatePicker calendar rendered in English** (day names `Su…Sa`, month `July`) inside the Hebrew UI — every publish/extend/reopen dialog | No PrimeNG `translation` was configured, so the DatePicker used its built-in English locale | Added Hebrew + English PrimeNG locales in `LanguageService` (`PRIMENG_HE`/`PRIMENG_EN`), applied via `PrimeNG.setTranslation()` on language change, and set the initial Hebrew locale in `providePrimeNG({ translation })` | `fix(client): localize datepicker + float its overlay in dialogs` |
| **DatePicker overlay overflowed the dialog** — the inline calendar/time panel expanded the dialog into horizontal + vertical scrollbars and clipped the time spinner | The `p-datepicker`s had no `appendTo`, so the overlay rendered inline in the DynamicDialog DOM | Added `appendTo="body"` to all four datepickers so the overlay floats above the dialog instead of growing it | `fix(client): localize datepicker + float its overlay in dialogs` |

Re-verified after the fixes: one Refresh → exactly one dashboard GET, stamp updates, no polling (16 s idle
→ zero background GETs); the datepicker calendar shows Hebrew day/month names (Sunday-first), the overlay is
appended to `body` and the dialog has **no** scroll overflow in either axis.

> These datepicker issues were **missed in the first UI pass** because browser-pane screenshots time out
> (hidden-pane rAF throttling, see project memory) and the DOM/rect checks used instead confirmed
> `dir=rtl`, input/button presence, and the timezone note — but not the calendar's locale or the overlay's
> overflow. They surfaced from user-provided screenshots; the audit method has been noted for next time.

## Verification method notes

- The full lifecycle (publish → auto-open → extend → auto-close v1 → reopen → close v2 → history
  re-download → **restart reconciliation**) and the 409 wrong-state guards were driven through the API
  with short (seconds-scale) windows and asserted against the `LoggingEmailSender` log lines
  (`Week {n} Requests - {teacher} - v{n}`, non-zero attachment) — the authoritative check for the
  time- and email-sensitive behaviour. The admin UI (draft chip, publish/extend/reopen dialogs,
  dashboard variants, history, refresh, weekly-prep entry point) and the Hebrew/RTL audit were driven
  through the real client.
- **Restart reconciliation** was exercised deterministically: publish a future window, stop the app
  (RAMJobStore is non-persistent, so its jobs vanish), backdate the window into the past to simulate
  boundaries crossed during downtime, restart → `PublicationReconciliationHostedService` opened then
  closed the overdue publication and emitted the close e-mails.

## Notable / worth review

- **Copy-failure toast severity.** `publish-week.dialog.ts` routes both outcomes of the copy through
  `toast.success(...)` (`copied ? 'shareLink.copied' : 'shareLink.copyFailed'`), so a copy *failure*
  is shown with success severity. Cosmetic only; the message text is correct. (The store's own
  `copyLink()` uses `toast.apiError` on failure — the two copy paths differ.) Worth aligning.
- **Copy success path unverified in the automated browser.** The browser pane denies Clipboard
  write/read permission, so only the failure toast could be exercised at runtime; the success branch
  was confirmed by code (`ClipboardService.copy` returns `true` → `shareLink.copied`).
- **Environment (not a code change).** The shared dev Postgres volume carried a parallel branch's
  `PromoteCarToSharedPool` schema (cars via a `car_teachers` join, no `cars.teacher_id`), which this
  branch's `TeacherQueries.FindAsync` — unchanged from `main` — cannot query. Verification ran against a
  fresh `drivinglessons_e2e` database so this branch's five migrations built the expected schema. The
  `teachers/find` mismatch is a pre-existing concern on `main`, out of scope for this PR.
