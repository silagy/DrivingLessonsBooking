# Student Notifications (stage 1) — Task Index

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Status:** draft for review (19 October 2026). Every task names its files, contracts, tests and commands; the decisions below are open to challenge. Once they are confirmed, each task's steps get their full file contents, like the earlier plans.

Per-task breakdown of stage 1 of the [Student Notifications intent](../student-notifications-intent.md). Execute the tasks **in order**, one per session. Each file stands on its own.

**Goal:** When a Publication opens, every Student who can submit gets the weekly link on WhatsApp. When the window is extended or reopened, they get the new end time. While the window is open, Students who haven't submitted get reminders, sent by an Administrator or on a schedule, within anti-spam limits. Every side effect, the teacher's Excel email included, goes through a transactional outbox: written in the same transaction as the change, delivered after commit, at least once, to any number of consumers, with retries and a visible dead state.

**Architecture:** A domain event never leaves the process as is. An Application handler (`IDomainEventHandler<T>`) maps it to a versioned **integration event** (`Application\IntegrationEvents\`) and adds it to the **outbox** (`IIntegrationEventOutbox`) on the same `DbContext`. `DrivingLessonsDbContext.CommitAsync` now always runs in one transaction, so the aggregate change and its outbox rows commit or roll back together. When a row is written, the outbox also writes one **delivery** row per consumer registered for that event type. An in-process `OutboxProcessor` (a `BackgroundService`) claims due deliveries with `FOR UPDATE SKIP LOCKED`, runs each **consumer** (`IIntegrationEventConsumer<T>`) in its own DI scope, and retries with backoff until the delivery succeeds or goes dead. Consumers run at least once, so each one guards against a repeat with a record of what it already did. For the teacher's email that record is the `TeacherExcelVersion` the email was sent for. For Students it is a new **`StudentNotification`** aggregate, one row per Student per Publication per kind per trigger, which is also what the dashboard reads. WhatsApp goes through an `IWhatsAppSender` port, implemented over `HttpClient` against the Meta Cloud API. When it is disabled it logs instead of sending, the same switch as `Email:Enabled`.

**Tech Stack:** .NET 10 / ASP.NET Core / EF Core 10 + Npgsql / PostgreSQL / Quartz (in-memory, already in place); **Microsoft.Extensions.Http 10.0.9** (new, Infrastructure, for the typed `HttpClient`); System.Text.Json for payloads; MSTest + Shouldly + FakeItEasy; Angular + PrimeNG + Transloco on the client. No broker, no third-party WhatsApp SDK.

**Spec:** [intent](../student-notifications-intent.md) (§3 scope, §4 behavior, §5 architecture, §7 success criteria, §8 open questions) · [requirements](../../../requirements.md) §3, §5.4, §5.5, §6.2–6.4, §8.3, §11 · [ADR 0005](../../../decisions/0005-email-over-ses-smtp.md) · [ADR 0007](../../../decisions/0007-locking-for-the-active-student-guards.md) · `.claude/rules/ddd-architecture.md` ("Deliberately Omitted" lists the outbox; task 1 changes that).

**Branches (one PR per milestone):** `notifications-outbox` (tasks 1–4), `notifications-whatsapp` (tasks 5–14), `notifications-reminders` (tasks 15–18). Each one is created from `origin/main` once the milestone before it is merged. No GitHub issues exist yet; see Open Item 1.

## Context

What the code does today (read 19 October 2026):

- `DrivingLessonsDbContext.CommitAsync` → `SaveAndDispatchAsync` runs up to 10 cycles: `SaveChangesAsync()`, collect `UncommittedEvents`, `CommitEvents()`, `DomainEventDispatcher.DispatchAsync` for each, repeat. With no explicit transaction, **each cycle is its own implicit transaction**: the aggregate change is committed before its handlers even run. A transaction exists only when `TeacherAndCarLocks` opened one; then `CommitAsync` commits it at the end.
- `PublicationClosedHandler` (Application) runs inside that loop. It generates each teacher's Excel and calls `IEmailSender` (`SmtpEmailSender`, MailKit) inline, catching and logging each teacher's failure (US-44 decision 4: no retry, no record).
- `Publication` raises `PublicationOpened(Id)`, `PublicationWindowExtended(Id, NewEndUtc)`, `PublicationReopened(Id, NewEndUtc)` and `PublicationClosed(Id)`. Nobody handles the first three. `Close(teacherIds)` bumps one `TeacherExcelVersion` per teacher (stored in `publication_teacher_versions`).
- `OpenPublicationJob` / `ClosePublicationJob` (Quartz, one-shot triggers per publication) and `PublicationReconciliationHostedService` (startup) call `OpenPublicationInteractor` / `ClosePublicationInteractor`. Quartz has no persistent store: `RegisterFutureJobsAsync` re-registers the triggers at every boot.
- `Student.Phone` is a `PhoneNumber` that keeps the roster text as entered (bidi marks stripped, at least 7 digits). Nothing converts it to an international number. Nothing records WhatsApp consent.
- The backend has no Asia/Jerusalem helper. Instants are UTC `DateTimeOffset`; slot windows are local times by `SlotWindowType`. The client formats with `formatInstantInJerusalem`.
- The dashboard (`GET api/publications/{id}/dashboard?teacherId=`) is per Teacher and open to Teacher-role Users. Publication commands have no `[Authorize]` attribute, so they fall back to the Administrator policy.
- No test touches a real database. Infrastructure is tested from `DrivingLessons.Application.Test`. Transactions, `SKIP LOCKED` and restarts are proven in each milestone's smoke task, on a throwaway database, as in US-44 and US-95.

## Decisions (made while planning — challenge on review)

Intent §8 lists open questions. Where these decisions take an intent default, the question number is given.

| # | Decision |
|---|----------|
| 1 | **Three PRs, one plan.** Milestone A (tasks 1–4) puts the outbox in place and moves the teacher's Excel email onto it. It ships alone and fixes ADR 0005's "no retry" today, before any WhatsApp setup exists. Milestone B (tasks 5–14) adds consent, the WhatsApp channel, the opening and window change notifications and a read-only dashboard panel. Milestone C (tasks 15–18) adds manual and scheduled reminders. Each milestone ends in a smoke task and a PR. |
| 2 | **`CommitAsync` always runs in one transaction.** It begins one when none is open (`TeacherAndCarLocks` still opens its own first) and commits it after the last dispatch cycle. All cycles, and so every handler's writes and every outbox row, commit together or not at all. This also makes `WeekScheduleCreatedHandler`'s Publication atomic with its Week Schedule. The cost is a slightly longer transaction per command, and no outbound call ever runs inside one (milestone A removes the only one, the inline email). |
| 3 | **Integration events live in `Application\IntegrationEvents\`**, one record per file, suffixed `IntegrationEvent` (`PublicationOpenedIntegrationEvent`). The suffix keeps them apart from the domain events with the same stem; the namespace alone would leave two `PublicationOpened` types side by side in every handler. Each one implements `IIntegrationEvent` with `static abstract string EventType` (kebab-case, stable, e.g. `publication-opened`) and `static abstract int SchemaVersion`. Payloads hold primitives only (`Guid`, `DateTimeOffset`, `int`, `string`, lists of records of these) and are serialized with System.Text.Json (web defaults). A class rename never changes `EventType`, so stored rows keep deserializing. |
| 4 | **Two tables: `outbox_messages` and `outbox_deliveries`.** A message holds `id`, `event_type`, `schema_version`, `payload` (`jsonb`) and `occurred_at_utc`. A delivery holds `id`, `message_id`, `consumer`, `state` (`Pending=10`, `Processed=20`, `Dead=30`), `attempts`, `next_attempt_at_utc`, `last_error` (cut to 2,000 characters) and `processed_at_utc`, unique on `(message_id, consumer)`, indexed on `(state, next_attempt_at_utc)`. The outbox writes the deliveries **when it writes the message**, one per consumer registered for that event type at that moment. The processor claims deliveries, not messages, so one consumer's failure never re-runs another. A consumer added later doesn't get old messages. These are infrastructure records, not aggregates: they live in `Infrastructure\Outbox\` with their EF configurations. |
| 5 | **Consumers are registered with `services.AddIntegrationEventConsumer<TEvent, TConsumer>()`.** This registers the scoped consumer and a singleton `IntegrationEventConsumerRegistration` that the outbox reads at write time. A consumer is named after its class (`nameof`), which is what `outbox_deliveries.consumer` stores. A delivery whose consumer no longer exists goes dead with "consumer not registered". |
| 6 | **The consumer contract:** `Task<DeliveryOutcome> HandleAsync(TEvent integrationEvent, DeliveryAttempt attempt)`. `DeliveryOutcome.Done` marks the delivery processed. `DeliveryOutcome.HoldUntil(instantUtc)` reschedules it without counting an attempt (quiet hours, decision 16). An exception counts as a failed attempt. `DeliveryAttempt(Number, IsLast)` lets a consumer record a final failure on its own rows before the delivery goes dead. |
| 7 | **The processor is a `BackgroundService` (`OutboxProcessor`), not a Quartz job.** It polls every `Outbox:PollIntervalSeconds` (default 5) and claims up to `Outbox:BatchSize` (default 10) due deliveries in one transaction with `SELECT … ORDER BY next_attempt_at_utc LIMIT n FOR UPDATE SKIP LOCKED`. It runs each one in a fresh scope and writes its new state in that same claim transaction. A second app instance during a deploy skips the locked rows. Quartz stays for wall-clock schedules (open, close, reminder checks). |
| 8 | **Retries:** after attempts 1–7 the next attempt waits 30 s, 1 min, 5 min, 15 min, 30 min, 1 h and 2 h. A delivery that fails its **8th** attempt goes `Dead`, about 4 hours after the first attempt. A dead delivery logs at Error with the message, delivery and consumer IDs only (no names, numbers or addresses, per the logging rule). Retention (open question 9): once a day the processor deletes processed and dead deliveries older than `Outbox:RetentionDays` (default 30), and the messages left with no delivery. `student_notifications` rows are history and are never purged. |
| 9 | **Teacher email on the outbox, guarded by the version.** `PublicationClosedHandler` only maps `PublicationClosed` to `PublicationClosedIntegrationEvent(PublicationId, ClosedAtUtc, Teachers[{TeacherId, ExcelVersion}])`. A new `TeacherExcelEmailConsumer` loops over the Teachers. It skips a Teacher whose current version is no longer the event's (a later close superseded it) or is already emailed. Otherwise it sends, then calls `publication.RecordExcelEmailed(teacher, emailedAtUtc)` and commits, one Teacher at a time. If any send failed, it throws once the loop ends, and the retry re-sends only the Teachers not yet recorded. Subject, body, attachment name and version stay exactly as US-44 built them (decision #22). `TeacherExcelVersion` gains `EmailedAtUtc`, cleared by `Increment()`. Few Teachers per close, so the per-Teacher loop is enough; there is no per-Teacher event. |
| 10 | **Recipient (intent §4.1):** an active Student whose Teacher has a Week Schedule for the Publication's week. Every recipient gets a `StudentNotification` row. It is `Pending` if they have WhatsApp Consent and a phone number that converts to a WhatsApp number, and `Skipped` with the reason (`NoConsent`, `InvalidNumber`) otherwise. A disabled sender also records `Skipped` (`WhatsAppDisabled`), so a local run fills the dashboard exactly as production would. |
| 11 | **Phone to WhatsApp number:** `PhoneNumber.ToWhatsAppNumber() : WhatsAppNumber?`. It keeps digits only, accepts `05XXXXXXXX`, `5XXXXXXXX`, `9725XXXXXXXX`, `+9725…` and `009725…`, and returns `9725XXXXXXXX` (12 digits). Anything else, landlines included, returns `null`, a recorded skip, never an exception. `WhatsAppNumber.Of(string)` validates `^9725\d{8}$` for reads back from storage. `PhoneNumber` itself is unchanged. |
| 12 | **WhatsApp Consent (open question 4) is a Student field set by an Administrator.** It is a `WhatsAppConsentGivenAtUtc` on `Student`, with `GiveWhatsAppConsent(givenAtUtc)` / `WithdrawWhatsAppConsent()` and their own events, exceptions and endpoints. The add and edit dialogs get a checkbox, the Students list a marker. Existing Students start **without** consent: no consent, no message. Task 7 adds an **optional** Roster column (`הסכמה לוואטסאפ`: yes gives consent, no withdraws it, an absent column or a blank cell changes nothing). **Drop task 7 if the school would rather tick students by hand.** Rejected for now: a checkbox on the student form, the strongest opt-in, but it can't reach a Student before their first submission. |
| 13 | **`StudentNotification` is an aggregate** (`Domain\Entities\StudentNotification.cs`), because "sent once, superseded only while pending" are business rules. It holds `PublicationId`, `StudentId`, `Kind` (`Opening=10`, `WindowChange=20`, `Reminder=30`), `Trigger` (a `NotificationTrigger` value: `opening`, `window-change:{changeId}`, `reminder:{requestId}`, `scheduled-reminder:{checkUtc:O}`), `State` (`Pending=10`, `Sent=20`, `Failed=30`, `Skipped=40`, `Superseded=50`), `SkipReason?`, `FailureReason?` (provider code and title, no PII), `CreatedAtUtc`, `SentAtUtc?` and `ProviderMessageId?`. A unique index on `(publication_id, student_id, kind, trigger)` stops a repeat. Transitions are `MarkSent`, `MarkFailed`, `MarkSkipped` and `Supersede`, each guarded by `MustBePending`. |
| 14 | **One send loop for every kind: plan, then send.** Each Student consumer first **plans**: if this trigger has no rows yet, it resolves the recipients and writes their rows in one commit. Then it **sends** through `StudentNotificationSender`: for each `Pending` row of the trigger it sends, marks the row and commits that row. A permanent provider rejection marks the row `Failed`. A transient one leaves it `Pending` and makes the consumer throw once the loop ends, so the retry only re-sends rows still pending. On `attempt.IsLast` the rows still pending become `Failed` ("provider unavailable"). The message text is built **at send time** from the current Publication, so a retried message never shows a stale end time. This is intent §5.5 with per-recipient rows instead of per-Student events. |
| 15 | **Three Hebrew Utility templates**, as intent §9 says: `opening` (name, week, end time, link button), `window_change` (week, end time, link button) and `reminder` (name, end time, link button). The window change wording fits both an extension and a reopen ("ההרשמה לשבוע {week} פתוחה עד {end}"), so a reopen needs no fourth template. **Challenge this if the school wants the reopen to say "opened again" explicitly; that's a fourth template and a `wasReopened` switch the payload already carries.** The button is a URL button whose base `https://{DOMAIN}/s/` is fixed in the template, and whose dynamic suffix is the link token. |
| 16 | **Quiet hours (open questions 3 and 8):** nights 21:00–08:00 and Friday 15:00 to Saturday 21:00, Asia/Jerusalem, all configuration. A **scheduled** check inside them is skipped, not postponed. **Every other message** (opening, window change, manual reminder) is planned at once and held: the consumer returns `HoldUntil(end of quiet hours)`. The intent left the manual reminder and the opening open; holding them too means no Student is ever messaged at night. A held window change can still be superseded by a later one (decision 17). |
| 17 | **Window change (open question 5):** sent to every recipient, Submission or not. Its plan step first marks `Superseded` every `Pending` window change row of the same Publication created before this change. Two quick extensions, or one held overnight, therefore send one message, with the latest end time. |
| 18 | **Reminder limits (open question 2), a domain value `ReminderLimits`:** a reminder goes to a recipient without a Submission when at least `FirstReminderDelay` (4 h) has passed since their opening notification was sent, at least `MinimumGap` (4 h) since their last message of any kind, and they have fewer than `MaximumReminders` (3) reminders sent or pending, manual and scheduled together. A Student the limits exclude gets **no** row. One a later Submission excludes before the send is marked `Skipped` (`AlreadySubmitted`). |
| 19 | **Manual reminder (open question 1: Administrator only):** `POST api/publications/{id}/reminders` returns **202 Accepted** with no body. The interactor calls the new `Publication.RequestReminder()`, which guards `MustBeOpen` (`PublicationMustBeOpenException`, 409) and raises `PublicationReminderRequested`. Its handler writes `SubmissionReminderRequestedIntegrationEvent(PublicationId, RequestId, Source=Manual, RequestedAtUtc)`. Recipients are decided when the consumer runs, not at the click (intent §4.4). |
| 20 | **Scheduled reminders:** a Quartz `SubmissionReminderCheckJob` fires every 15 minutes (cron `0 0/15 * * * ?`, Asia/Jerusalem). A pure domain value, `ReminderSchedule`, decides from the fire time whether a check is due: a configured time (10:00, 14:00, 19:00) or the last call (3 h before the window ends) falls inside the 15-minute tick, and the tick is outside quiet hours. A due check writes one `SubmissionReminderRequestedIntegrationEvent(Source=Scheduled)` per Open Publication that has scheduled reminders on, with no domain change behind it (intent §5.1). The trigger key comes from Quartz's *scheduled* fire time, so two instances firing the same tick write the same rows: the unique index and the plan step turn the second into a no-op. |
| 21 | **Scheduled reminders switch (open question 7: on by default):** `Publication.ScheduledRemindersOn`, with `TurnOffScheduledReminders()` / `TurnOnScheduledReminders()`, each refusing the state it's already in, events `PublicationScheduledRemindersTurnedOff` / `…On`, and `POST api/publications/{id}/scheduled-reminders/turn-off|turn-on` (Administrator). |
| 22 | **WhatsApp channel:** `IWhatsAppSender.SendAsync(WhatsAppMessage) : Task<WhatsAppSendResult>` (`Sent(providerMessageId)` · `Rejected(reason)` · `Disabled`). It throws `WhatsAppUnavailableException` for a timeout, a 429, a 5xx or Meta's throttling codes (130429, 131000, 131016), so the outbox retries. Any other 4xx is `Rejected`, with Meta's code and title as the reason. The implementation, `WhatsAppCloudApiSender`, is a typed `HttpClient` (15 s timeout) posting to `{ApiBaseUrl}/{ApiVersion}/{PhoneNumberId}/messages`. `WhatsAppOptions` (`Enabled`, `ApiBaseUrl` default `https://graph.facebook.com`, `ApiVersion`, `PhoneNumberId`, `BusinessAccountId`, `AccessToken`, `LanguageCode` default `he`, `Templates:{Opening,WindowChange,Reminder}`) is validated at start-up when enabled. `ApiBaseUrl` is configuration, so the smoke points it at a local fake. Recorded as ADR 0011. |
| 23 | **Message text is Hebrew, built in Application** (`NotificationTemplateParameters`), like the teacher email (US-44 decision 2): week as `4.10–9.10`, end time as `יום ד׳ 21.10 בשעה 20:00` in Asia/Jerusalem. The backend gets a `SchoolTimeZone` (`Domain\Values`, `Asia/Jerusalem`). Requirements §8.3 already fixes the zone, and the aspnet image is Debian-based with tzdata. |
| 24 | **Dashboard visibility (intent §4.7), Administrator only:** `GET api/publications/{id}/notifications` returns, for the whole Publication, the recipients, the opening and window change counts (sent / pending / failed / skipped), reminders sent, recipients without a Submission, the scheduled reminders switch, each Teacher's Excel email (version, sent at or not sent), and the failed and skipped rows by Student with their reason. On the client it is a `PublicationNotificationsPanel` under the slot grid, shown to Administrators only. It is read-only in milestone B; milestone C adds "Send reminder" (a confirm dialog that shows the count) and the switch. |
| 25 | **Glossary terms:** Recipient, WhatsApp Consent, Opening Notification, Window Change Notification, Submission Reminder (manual or scheduled), Quiet Hours, Student Notification. Words to avoid: "message blast", "push", "SMS" (it isn't), "subscriber", "broadcast". Task 8 adds them to `CONTEXT.md`, with the requirements changes listed in intent §6. |

## Conventions that OVERRIDE the rules docs (follow the code, per prior modules)

- DI registrations go in each project's `DependencyInjection.cs` (`AddApplication`, `AddInfrastructure`).
- Application tests build real domain objects (`Publication.Create(WeekStart.Of(…))`, `Student.Create(…)`). `DrivingLessons.Application.Test` does not reference the domain fake builders.
- FakeItEasy: an unconfigured `Task<T?>` call returns a **dummy object, not null**, so configure `null` explicitly where a test needs it.
- Infrastructure classes (outbox writer, retry policy, WhatsApp sender, options) are tested from `DrivingLessons.Application.Test`, like `EmailMimeMessageTest`.
- Test folders avoid names that shadow a domain type. The US-44 lesson was `Emails`, not `Email`, so these use `IntegrationEvents\`, `Outbox\`, `Notifications\` and `WhatsAppChannel\`; never `WhatsApp\` beside a `WhatsAppNumber` value object.
- Migrations: `dotnet ef migrations add {Name} --project src/DrivingLessons.Infrastructure --startup-project src/DrivingLessons.Presentation.Web --output-dir EntityFramework/Migrations`.
- No comments anywhere except `//given //when //then` test markers.

## Global Constraints

- No outbound call (SMTP, WhatsApp) runs inside a request's or a job's unit of work. After milestone A, `IEmailSender` is called only from a consumer.
- A Publication transition never fails because of a notification: open, close, extend and reopen commit even with WhatsApp or SMTP down (intent §7.4).
- No PII in logs: GUIDs only, never names, phone numbers, emails or message text (code-style.md Logging).
- Credentials come only from configuration and environment variables. The WhatsApp token lives only in the production `.env`.
- New packages: exactly `Microsoft.Extensions.Http` 10.0.9 (Infrastructure). Nothing else.
- Layer dependencies hold. Integration events, the outbox port and consumers are in Application; tables, the processor and the HTTP sender are in Infrastructure; the Domain knows none of them.
- Every new user-visible string is a translation key in `client\public\i18n\he.json` and `en.json`, Hebrew first, RTL-correct.

## Review Focus

Inputs the spec implies but a happy-path test would not exercise. Each one is pinned by a step in the task that owns it:

1. **The command fails after its handlers ran** (a later cycle throws, or the commit itself fails) → the aggregate change and its outbox rows roll back together → task 4 Step 3 (a trigger refuses the outbox insert).
2. **The app stops between sending and recording** → at most one duplicate, never a loss: at-least-once can't do better without the provider's idempotency key (Open Item 3) → task 4 Step 5 (email), task 14 Step 5 (WhatsApp).
3. **Two app instances poll the same table during a deploy** → no delivery is claimed twice → task 4 Step 6.
4. **SMTP is down at the close and comes back an hour later** → every Teacher gets exactly one email, with the right version, and the close itself never failed → task 3 `Retries_Only_The_Teachers_Not_Yet_Emailed`, task 4 Step 4.
5. **Reopen and close again before the first close's email went out** → only the new version is sent → task 3 `Skips_A_Version_Superseded_By_A_Later_Close`.
6. **A Student's number is a landline, has dashes or `+972`, or is missing digits** → converted, or recorded `Skipped InvalidNumber`; the others are still sent → task 5 `To_WhatsApp_Number` rows, task 11 `Records_An_Invalid_Number_As_Skipped_And_Sends_The_Others`.
7. **Meta rejects one Student permanently and throttles another** → the first is `Failed` with the code, the second retried, everyone else sent once → task 11 `A_Rejected_Student_Fails_And_A_Throttled_One_Stays_Pending`.
8. **The window is extended twice in a minute, or overnight** → one window change message, with the last end time → task 12 `Supersedes_The_Pending_Rows_Of_An_Earlier_Change`, task 14 Step 6.
9. **A Student submits between "Send reminder" and the send** → not reminded → task 15 `Skips_A_Student_Who_Submitted_Before_The_Send`.
10. **Two instances fire the same 10:00 check** → one reminder per Student → task 16 `The_Same_Check_Twice_Writes_The_Same_Trigger`, task 18 Step 5.
11. **The last call coincides with a 19:00 check, or falls in quiet hours** → one check, or none → task 16 `Schedule` rows.
12. **"Send reminder" on a Closed Publication** → 409 `PublicationMustBeOpenException`, nothing queued → task 15 `Request_Reminder__Must_Be_Open`.

## Execution Order

| # | File | Task | Commit point |
|---|------|------|--------------|
| **A** | | **Outbox and the teacher's email — branch `notifications-outbox`** | |
| 1 | [task-01-integration-events-and-outbox-write.md](task-01-integration-events-and-outbox-write.md) | Contracts, `outbox_messages` / `outbox_deliveries`, the outbox writer, consumer registration, `CommitAsync` in one transaction, migration; ADR 0010 + `ddd-architecture.md` | ✅ own commit |
| 2 | [task-02-outbox-processor.md](task-02-outbox-processor.md) | `OutboxProcessor`: claim with `SKIP LOCKED`, run the consumer, retry and backoff, dead, daily purge, `OutboxOptions` | ✅ own commit |
| 3 | [task-03-teacher-excel-email-on-outbox.md](task-03-teacher-excel-email-on-outbox.md) | `Publication.RecordExcelEmailed`, `PublicationClosedIntegrationEvent`, `TeacherExcelEmailConsumer`; inline send removed; ADR 0005 amended, requirements §6.4 / §9 | ✅ own commit |
| 4 | [task-04-outbox-smoke-and-pr.md](task-04-outbox-smoke-and-pr.md) | Smoke on a throwaway DB: rollback, SMTP down then up, restart mid-send, two instances; PR A | — |
| **B** | | **Consent, WhatsApp, opening and window change — branch `notifications-whatsapp`** | |
| 5 | [task-05-whatsapp-number-and-consent.md](task-05-whatsapp-number-and-consent.md) | `WhatsAppNumber`, `PhoneNumber.ToWhatsAppNumber()`, Student WhatsApp Consent (domain, column, commands, endpoints, responses) | ✅ own commit |
| 6 | [task-06-client-student-consent.md](task-06-client-student-consent.md) | Consent checkbox in add / edit, marker on the list, store calls, i18n | ✅ own commit |
| 7 | [task-07-roster-consent-column.md](task-07-roster-consent-column.md) | **Optional.** Roster column `הסכמה לוואטסאפ` | ✅ own commit |
| 8 | [task-08-student-notification-aggregate.md](task-08-student-notification-aggregate.md) | `StudentNotification` aggregate, `NotificationTrigger`, `SchoolTimeZone`, `QuietHours`; table, repository, recipient query; requirements + `CONTEXT.md` | ✅ own commit |
| 9 | [task-09-whatsapp-channel.md](task-09-whatsapp-channel.md) | `IWhatsAppSender`, `WhatsAppCloudApiSender`, `WhatsAppOptions`, compose + `.env.example`, ADR 0011, local fake docs | ✅ own commit |
| 10 | [task-10-student-notification-sender.md](task-10-student-notification-sender.md) | The shared plan-and-send loop, `NotificationTemplateParameters` (Hebrew text), quiet-hours hold | ✅ own commit |
| 11 | [task-11-opening-notification.md](task-11-opening-notification.md) | `PublicationOpened` → integration event → `OpeningNotificationConsumer` | ✅ own commit |
| 12 | [task-12-window-change-notification.md](task-12-window-change-notification.md) | `PublicationWindowExtended` / `PublicationReopened` → `PublicationWindowChangedIntegrationEvent` → consumer with supersede | ✅ own commit |
| 13 | [task-13-notifications-summary.md](task-13-notifications-summary.md) | `GET api/publications/{id}/notifications` + client `PublicationNotificationsPanel` (read-only) | ✅ own commit |
| 14 | [task-14-whatsapp-smoke-and-pr.md](task-14-whatsapp-smoke-and-pr.md) | Smoke with a local fake Cloud API: open, skips, rejection, throttling, restart, supersede, quiet hours; PR B | — |
| **C** | | **Reminders — branch `notifications-reminders`** | |
| 15 | [task-15-manual-reminder.md](task-15-manual-reminder.md) | `Publication.RequestReminder`, `ReminderLimits`, `SubmissionReminderConsumer`, `POST …/reminders` (202) | ✅ own commit |
| 16 | [task-16-scheduled-reminders.md](task-16-scheduled-reminders.md) | `ReminderSchedule`, `SubmissionReminderCheckJob`, the scheduled reminders switch (domain + endpoints) | ✅ own commit |
| 17 | [task-17-client-reminders.md](task-17-client-reminders.md) | "Send reminder" confirm dialog, the switch, reminder counts in the panel | ✅ own commit |
| 18 | [task-18-reminders-smoke-docs-and-pr.md](task-18-reminders-smoke-docs-and-pr.md) | Smoke: manual, scheduled, limits, two instances; runbook (WhatsApp setup, dead outbox rows); requirements final pass; PR C | — |

## How to Run a Task

1. Confirm you are on the milestone's branch and every earlier task of it is committed.
2. Open the task file and follow the steps exactly. Each step names its files, its contracts and the exact commands.
3. Run the verification step(s) before committing.
4. Check off the `- [ ]` boxes in the task file as you go.
5. `git add` only the paths each task lists.

Backend commands run from the repo root, in bash. Environment notes (project memory, as in US-44 and US-95):
- From-source API runs use the compose Postgres container `drivinglessonsbooking-postgres-1` (`docker stop dl-postgres; docker compose up -d postgres`). `dl-postgres` has a stale migration history.
- JSON bodies with Hebrew go from UTF-8 files (`--data-binary @file`). Hebrew command-line arguments turn into `???`.
- Smoke files live under a scratch folder, never in the repo.

## Open Items (non-blocking)

1. **No GitHub issues yet.** Proposal: one issue per milestone (A: "teacher email retried through an outbox", B: "students get the weekly link on WhatsApp", C: "submission reminders"), each with the intent's success criteria that milestone covers. The branch names above would then take the issue numbers, as in the other modules.
2. **Meta onboarding is the school's, not code** (intent §9): the Business portfolio, WhatsApp Business Account, a dedicated number (open question 6), a payment method, the three templates approved in WhatsApp Manager, and a permanent system-user token. Milestone B can merge and deploy with `WHATSAPP_ENABLED=false`. The dashboard then shows every row as `Skipped (WhatsApp disabled)`, which is a useful dry run on real data.
3. **Exactly-once is out of reach.** A crash after the provider accepted a message but before the row was marked `Sent` resends it once on recovery. The Cloud API offers no idempotency key for template messages. The window is one HTTP round trip per Student, so this is accepted and stated in ADR 0010.
4. **Delivery and read statuses, and opt-outs sent in WhatsApp** (intent "later stages") need the webhook. `ProviderMessageId` is stored now so that stage can match statuses to rows.
5. **Teacher-role Users** see neither the notifications panel nor the reminder button (open question 1). Opening them up later means filtering the summary by Teacher and adding a `MayReach` check, the same pattern as the dashboard.
6. **Cost:** Utility templates are billed per delivered message. Three reminders, an opening and a window change come to about 5 messages per Student per week at most. ADR 0011 records the rate the school gets at onboarding.

## Target Layout (new/changed this stage)

```
src\DrivingLessons.Domain\
├── Entities\Publication.cs                         RecordExcelEmailed, RequestReminder, scheduled reminders switch
├── Entities\TeacherExcelVersion.cs                 EmailedAtUtc
├── Entities\Student.cs                             WhatsApp Consent
├── Entities\StudentNotification.cs                 new aggregate
├── Events\PublicationExcelEmailed.cs · PublicationReminderRequested.cs · PublicationScheduledRemindersTurnedOn/Off.cs
├── Events\StudentWhatsAppConsentGiven.cs · StudentWhatsAppConsentWithdrawn.cs
├── Events\StudentNotification{Created,Sent,Failed,Skipped,Superseded}.cs
├── Exceptions\…                                     one per new guard
├── Repositories\IStudentNotificationRepository.cs
└── Values\WhatsAppNumber.cs · NotificationKind.cs · NotificationState.cs · NotificationSkipReason.cs
          NotificationTrigger.cs · StudentNotificationId.cs · SchoolTimeZone.cs · QuietHours.cs
          ReminderLimits.cs · ReminderHistory.cs · ReminderSchedule.cs
src\DrivingLessons.Application\
├── IntegrationEvents\IIntegrationEvent.cs · IIntegrationEventOutbox.cs · IIntegrationEventConsumer.cs
│                     DeliveryOutcome.cs · DeliveryAttempt.cs · IntegrationEventConsumerRegistration.cs
│                     IntegrationEventServiceCollectionExtension.cs
│                     PublicationClosedIntegrationEvent.cs · PublicationOpenedIntegrationEvent.cs
│                     PublicationWindowChangedIntegrationEvent.cs · SubmissionReminderRequestedIntegrationEvent.cs (+ ReminderSource)
├── EventHandlers\PublicationClosedHandler.cs (maps only) · PublicationOpenedHandler.cs
│                 PublicationWindowExtendedHandler.cs · PublicationReopenedHandler.cs · PublicationReminderRequestedHandler.cs
├── Consumers\TeacherExcelEmailConsumer.cs · OpeningNotificationConsumer.cs
│             WindowChangeNotificationConsumer.cs · SubmissionReminderConsumer.cs
├── Notifications\StudentNotificationSender.cs · NotificationTemplateParameters.cs · TeacherExcelEmail.cs
├── Abstractions\IWhatsAppSender.cs
├── Commands\GiveStudentWhatsAppConsent\ · WithdrawStudentWhatsAppConsent\ · RequestSubmissionReminder\
│            RequestScheduledReminders\ · TurnOnScheduledReminders\ · TurnOffScheduledReminders\
└── Queries\IStudentNotificationQueries.cs · GetPublicationNotifications\
src\DrivingLessons.Infrastructure\
├── EntityFramework\DrivingLessonsDbContext.cs      CommitAsync in one transaction; new DbSets
├── EntityFramework\EntityConfigurations\OutboxMessageConfiguration.cs · OutboxDeliveryConfiguration.cs
│                                          StudentNotificationConfiguration.cs · Publication/StudentConfiguration.cs
├── EntityFramework\Repositories\StudentNotificationRepository.cs
├── EntityFramework\Queries\StudentNotificationQueries.cs
├── EntityFramework\Migrations\…AddOutbox · …AddStudentWhatsAppConsent · …AddStudentNotifications · …AddScheduledRemindersSwitch
├── Outbox\OutboxMessage.cs · OutboxDelivery.cs · IntegrationEventOutbox.cs · IntegrationEventTypes.cs
│          OutboxProcessor.cs · OutboxRetryPolicy.cs · OutboxOptions.cs
├── WhatsAppChannel\WhatsAppCloudApiSender.cs · WhatsAppOptions.cs · WhatsAppCloudApiRequest.cs
├── Scheduling\SubmissionReminderCheckJob.cs
└── Options\NotificationOptions.cs             binds Notifications:* and registers QuietHours, ReminderLimits, ReminderSchedule as singletons
src\DrivingLessons.Presentation.Web\Controllers\Publication\…   reminders, scheduled-reminders, notifications
src\DrivingLessons.Presentation.Web\Controllers\Student\…       give/withdraw-whatsapp-consent
client\src\app\features\publications\…               notifications panel, reminder dialog, store
client\src\app\features\students\…                   consent checkbox and marker
docs\decisions\0010-integration-events-and-outbox.md · 0011-whatsapp-over-cloud-api.md · 0005 amended
docs\requirements.md · CONTEXT.md · .claude\rules\ddd-architecture.md · docs\operations\runbook.md
docs\development\running-the-project.md · docker-compose.yml · .env.example
```
