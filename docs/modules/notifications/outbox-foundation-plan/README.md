# Outbox Foundation — Task Index (notifications slice 1)

> **For agentic workers:** Execute this plan task by task, in order. Steps use checkbox (`- [ ]`) syntax for tracking.

Per-task breakdown of slice 1 of the [Notifications roadmap](../README.md). Execute the tasks **in order**, one per session. Each file is self-contained.

**Goal:** The app gets one durable way to publish **integration events**: they are written to a PostgreSQL **outbox** in the same transaction as the change that caused them, and a **Quartz** job hands each one, after commit, to **every consumer registered for it**, tracking and retrying each consumer on its own. The teacher's Excel email is the first consumer: at every window close each Teacher still receives exactly one email per Excel version, but a failed send is now retried, and a restart during the close neither loses nor duplicates it. Nothing changes for Administrators or Students.

**Architecture:** Application owns the contracts. `IIntegrationEvent` and the event records live in `Application\IntegrationEvents\`; consumers implement `IIntegrationEventConsumer<TEvent>` and live in `Application\Consumers\`; `IOutbox.Add(IIntegrationEvent)` queues an event in the current unit of work. An `IntegrationEventCatalog` (singleton, filled by `AddIntegrationEvent<T>(name)` and `AddIntegrationEventConsumer<TEvent, TConsumer>()` in `AddApplication`) is the only place that knows which consumers listen to which event, so producers and consumers never reference each other. Infrastructure stores an `outbox_messages` row per event and an `outbox_deliveries` row per (event, consumer), both added in the same `CommitAsync` transaction as the aggregate change. `ProcessOutboxJob` (Quartz, every 5 s, no overlap) leases due deliveries with `UPDATE … FOR UPDATE SKIP LOCKED … RETURNING`, runs each consumer in its own DI scope, and marks the delivery succeeded, or schedules a retry with exponential backoff, or marks it dead after the last attempt. `PublicationClosedHandler` stops sending email: it maps the domain `PublicationClosed` to a `PublicationClosedIntegrationEvent` carrying each Teacher's Excel version. `SendTeacherExcelEmailsConsumer` sends the emails, skipping any (Publication, Teacher, version) already recorded in `sent_teacher_excel_emails`.

**Tech Stack:** .NET 10 / ASP.NET Core / EF Core 10 / Npgsql 10.0.2 / PostgreSQL / Quartz 3.18.2; System.Text.Json (in the framework); MSTest + Shouldly + FakeItEasy; Mailpit for the smoke. **No new package. Two migrations. No client change.**

**Spec:** [Student Notifications - Intent](../student-notifications-intent.md) §2, §3 items 1 and 8, §5.1–5.4, §5.6 (close flow), §7 criteria 5 and 8 · [Notifications roadmap](../README.md) decisions 2, 3, 4, 7 · [ADR 0005](../../../decisions/0005-email-over-ses-smtp.md) · [US-44 plan](../../excel/us-44-versioned-excel-email-plan/README.md) (the email this slice moves, unchanged) · requirements §6.4, §9 Delivery, decisions #8 and #22

**Branch:** `notifications-outbox-foundation`, created from `origin/main` after the intent branch `docs/notifications-intent` merges.

## Context

Today `DrivingLessonsDbContext.CommitAsync` saves, then dispatches each aggregate's domain events in-process (`SaveAndDispatchAsync`, up to 10 save/dispatch cycles), then commits the transaction if one is open. Only `TeacherAndCarLocks` opens one; every other command saves under Postgres auto-commit. `PublicationClosedHandler` is called during that dispatch and sends one email per Teacher inline (US-44): it catches and logs a failure per Teacher, and nothing records that an email is still owed. So:

- If the process stops after the close is saved but before the emails go out, those emails are lost: the close is never retried (a second close throws `PublicationMustBeOpenException`).
- A failed send is only a log line (ADR 0005, US-44 open item 1). The Administrator's Download button is the only fallback.
- The close's Quartz job and the startup reconciliation wait on SMTP (30-second timeout per Teacher).

This slice fixes all three without changing what a Teacher receives.

## Decisions (made while planning — challenge on review)

| # | Decision |
|---|----------|
| 1 | **Contracts in Application, storage and transport in Infrastructure.** `IIntegrationEvent`, `IIntegrationEventConsumer<T>`, `IOutbox` and `IntegrationEventCatalog` are in Application (pure C#, no package). The outbox tables, the serializer, the processor and the Quartz job are in Infrastructure. The Domain is untouched: integration events are not domain events and never live in `Domain\Events\`. |
| 2 | **Folders keep events and consumers apart:** `Application\IntegrationEvents\{Context}\{Entity}{Action}IntegrationEvent.cs` for contracts, `Application\Consumers\{Job}\{Job}Consumer.cs` for consumers. The suffix `IntegrationEvent` keeps the name distinct from the domain event it maps (`PublicationClosed` → `PublicationClosedIntegrationEvent`). |
| 3 | **Every event has an explicit, versioned wire name**, given at registration: `AddIntegrationEvent<PublicationClosedIntegrationEvent>("publication-closed.v1")`. The outbox stores that name, never a CLR type name, so renaming or moving a class never strands stored rows. A breaking payload change is a new record and a new name (`.v2`); both stay registered until old rows drain. |
| 4 | **Consumers are registered against an event, never called by the producer.** `AddIntegrationEventConsumer<TEvent, TConsumer>()` registers `TConsumer` as scoped and adds it to the catalog under the consumer's class name. An event can have any number of consumers. The consumer's class name is stored on each delivery, so **renaming a consumer class needs a data migration**; the ADR says so. |
| 5 | **One `outbox_deliveries` row per (event, consumer), written with the event.** `Outbox.Add` asks the catalog for the event's consumers and adds the message and its deliveries to the `DbContext`; they are saved by the same `CommitAsync`. A consumer registered later does not receive events written before it existed. |
| 6 | **`CommitAsync` always runs in a transaction.** It opens one when none is open (`TeacherAndCarLocks` may already have), so the aggregate's rows, the domain-event handlers' rows and the outbox rows commit together or not at all. Domain-event handlers keep running in-process inside it; after this slice none of them calls anything outside the database. |
| 7 | **Leases, not long transactions.** The processor claims up to `BatchSize` (20) due deliveries in one statement: `UPDATE outbox_deliveries SET locked_until_utc = now + Lease WHERE id IN (SELECT … WHERE state = 'Pending' AND next_attempt_at_utc <= now AND (locked_until_utc IS NULL OR locked_until_utc < now) ORDER BY next_attempt_at_utc LIMIT n FOR UPDATE SKIP LOCKED) RETURNING id`. The lock is released at once; the lease (5 minutes) keeps a second instance away while the consumer runs. A process that dies mid-delivery leaves the lease to expire, and the delivery runs again. |
| 8 | **Retries live on the delivery row**: `attempts`, `next_attempt_at_utc`, `last_error`. After a failure the next attempt is `2^(attempts-1)` minutes later, capped at 60 (1, 2, 4, 8, 16, 32, 60 …). After `MaxAttempts` (8, about 2 hours in total) the delivery is `Dead`: logged at Error and left in the table. There are no extra Quartz triggers for retries, so a pending retry survives a restart. |
| 9 | **Delivery is at least once; consumers are safe to run twice.** The teacher email consumer's guard is a `sent_teacher_excel_emails` row per (Publication, Teacher, Excel version), recorded and committed right after each successful send. A retry skips Teachers already sent; a duplicate is possible only if the process dies between the SMTP send and that commit. |
| 10 | **One failed Teacher never blocks another, and still gets retried.** The consumer tries every Teacher, logs each failure at Error (GUIDs only, as US-44), then throws `TeacherExcelEmailFailedException` if any failed, so the delivery is retried; the retry sends only to the Teachers still missing. A Teacher deleted since the close is skipped, as today. |
| 11 | **The event carries the versions**: `PublicationClosedIntegrationEvent(Guid PublicationId, IReadOnlyList<ClosedTeacherVersion> Teachers)`, with `ClosedTeacherVersion(Guid TeacherId, int Version)`, copied from `Publication.TeacherVersions` at close. The email's subject uses the version from the event, so a retry after a later reopen still sends the version that close produced. The Excel content is generated at send time from current data, exactly as today. |
| 12 | **The email itself is unchanged.** Subject, three-line body, attachment name and `IEmailSender` move verbatim from `PublicationClosedHandler` to `SendTeacherExcelEmailsConsumer` (decision #22, US-44 decisions 2–4). The US-44 handler tests move with it. |
| 13 | **Quartz schedules the job from configuration at startup**: `AddQuartz(q => q.ScheduleJob<ProcessOutboxJob>(…RepeatForever()))` with `[DisallowConcurrentExecution]`. It needs nothing from the database, so the startup reconciliation is not involved. A second job, `PurgeOutboxJob`, runs every hour and deletes up to 500 `Succeeded` deliveries older than `RetentionDays` (30), then the messages left with no delivery. `Dead` deliveries are never purged. |
| 14 | **No PII in logs or in `last_error`.** Log lines carry delivery, message and entity GUIDs, the event name and the consumer name. `last_error` stores the exception's type name and message, cut to 1000 characters; the SMTP sender's exceptions don't carry addresses (US-44 decision 4). |
| 15 | **JSON payloads** via `System.Text.Json` with web defaults (camelCase), stored in a `jsonb` column. `OutboxSerializer` is the only place that serializes, so the format is one decision. |

## Conventions that OVERRIDE the rules docs (follow the code, per prior modules)

- DI registrations go in each project's `DependencyInjection.cs` (`AddInfrastructure`, `AddApplication`).
- Application tests build real domain objects (`Publication.Create(WeekStart.Of(…))`, `Teacher.Create(TeacherName.Of(…), Email.Of(…))`); `DrivingLessons.Application.Test` does not reference the domain fake builders.
- FakeItEasy: an unconfigured `Task<T?>` call returns a **dummy object, not null**. Configure `null` explicitly where a test needs it.
- Infrastructure classes are tested from `DrivingLessons.Application.Test` (it references Infrastructure). There are no database tests: what needs Postgres (the lease SQL, the transaction, the migration) is proven by the smoke in task 6.
- Outbox rows are **infrastructure records, not domain entities**: plain classes in `Infrastructure\Outbox\` with EF configurations, no `AggregateRoot`, no typed IDs, no domain events. Task 5 records this in the rules.
- No comments anywhere except `//given //when //then` test markers.

## Global Constraints

- Every window close still emails every Teacher with a Week Schedule for that Publication, once per Excel version, with today's subject, body and attachment (§6.4, §9 Delivery, decisions #8 and #22).
- No outbound call (SMTP, HTTP) happens inside a request's or job's unit of work.
- An event is stored if and only if the change that raised it commits.
- The Domain project gets no change and no new dependency. Layer dependencies hold: Application → Domain; Infrastructure → Application, Domain.
- No new package. No client change. No new configuration is required to run: `Outbox` options have defaults.
- No PII in new log lines or in `last_error` (code-style.md Logging).

## Review Focus

Inputs the spec implies but a happy-path test would not exercise. Each is pinned by a step in the owning task:

1. **The mail server is down when the window closes** → the close stands, the delivery is retried with growing gaps, and when the server comes back each Teacher gets exactly one email → task 4 `Throws_After_Trying_Every_Teacher_When_One_Fails`, task 6 Step 3.
2. **The app stops after the close commits, before the email is sent** → after the restart the email goes out once → task 6 Step 4.
3. **A retry runs after one of two Teachers was already sent** → only the other Teacher is sent → task 4 `Skips_A_Teacher_Already_Sent_For_This_Version`, task 6 Step 3.
4. **The close's transaction fails after the domain handler queued the event** → no outbox row exists → task 2 Step 6 (the transaction), task 6 Step 2 (one message per close, written with it).
5. **Reopen and close again before the first email went out** → two events, versions 1 and 2, each sent once with its own version → task 4 `Uses_The_Version_From_The_Event`, task 6 Step 5.
6. **Two app instances during a deploy** → a delivery is leased by one of them only → decision 7, task 3 Step 2 (the SQL), task 6 Step 6.
7. **A consumer keeps failing** → after 8 attempts the delivery is `Dead`, logged once at Error, kept in the table, never purged → task 2 `Becomes_Dead_After_The_Last_Attempt`, task 3 Step 2 (`LogFailure`), task 6 Step 7.

## Execution Order

| # | File | Task | Commit point |
|---|------|------|--------------|
| 1 | [task-01-integration-event-contracts.md](task-01-integration-event-contracts.md) | Application — `IIntegrationEvent`, `IIntegrationEventConsumer<T>`, `IOutbox`, `IntegrationEventCatalog` and its registration extension (TDD) | ✅ own commit |
| 2 | [task-02-outbox-storage.md](task-02-outbox-storage.md) | Infrastructure — `OutboxMessage`, `OutboxDelivery` (attempt rules, TDD), `OutboxSerializer` (TDD), `Outbox : IOutbox`, EF configurations, `CommitAsync` in one transaction, migration `AddOutbox` | ✅ own commit |
| 3 | [task-03-outbox-processor.md](task-03-outbox-processor.md) | Infrastructure — `OutboxOptions`, `OutboxProcessor` (lease, run, retry, dead, purge), `ProcessOutboxJob` on Quartz, DI, `appsettings` | ✅ own commit |
| 4 | [task-04-teacher-email-consumer.md](task-04-teacher-email-consumer.md) | Application + Infrastructure — `PublicationClosedIntegrationEvent`, `PublicationClosedHandler` queues it, `SendTeacherExcelEmailsConsumer` (moved US-44 tests + new guard tests), `ITeacherExcelEmailLog`, migration `AddSentTeacherExcelEmails` | ✅ own commit |
| 5 | [task-05-adr-and-rules.md](task-05-adr-and-rules.md) | Docs — ADR 0010, ADR 0005 amendment, `ddd-architecture.md`, `CLAUDE.md` file table, `tech-stack.md`, requirements §9 Delivery, runbook | ✅ own commit |
| 6 | [task-06-smoke-verify-and-pr.md](task-06-smoke-verify-and-pr.md) | Smoke on a throwaway database with Mailpit — normal close, mail server down, restart mid-send, reopen and close, two instances, dead delivery; full suites; PR | — (no planned source change) |

## How to Run a Task

1. Confirm you are on branch `notifications-outbox-foundation` and all earlier tasks are committed.
2. Open the task file and follow the steps exactly. Each step has the file contents or an anchored edit, and exact commands.
3. Run the verification step(s) before committing.
4. Check off the `- [ ]` boxes in the task file as you go.
5. `git add` only the paths the task lists.

Backend commands run from the repo root, in bash:

```bash
dotnet build
dotnet test tests/DrivingLessons.Domain.Test/DrivingLessons.Domain.Test.csproj
dotnet test tests/DrivingLessons.Application.Test/DrivingLessons.Application.Test.csproj
dotnet ef migrations add <Name> --project src/DrivingLessons.Infrastructure --startup-project src/DrivingLessons.Presentation.Web --output-dir EntityFramework/Migrations
dotnet ef migrations has-pending-model-changes --project src/DrivingLessons.Infrastructure --startup-project src/DrivingLessons.Presentation.Web
```

`dotnet ef migrations add` runs `Program.cs` at design time: keep `appsettings.Development.json` populated so the connection string parses.

## Open Items (non-blocking)

1. **No screen for dead deliveries.** A dead delivery is an Error log line and a row with `state = 'Dead'`. The runbook (task 5) shows the `psql` query and how to re-arm one (`UPDATE … SET state = 'Pending', attempts = 0, next_attempt_at_utc = now()`). Slice 4 shows the teacher email status on the dashboard.
2. **A duplicate email is still possible** if the process dies between the SMTP send and the commit of the `sent_teacher_excel_emails` row (decision 9). SMTP has no idempotency key, so this window can be narrowed, not closed.
3. **`Teacher` soft delete:** a Teacher deleted after the close is skipped and the delivery succeeds; nobody is told. Same as today.
4. **Consumer class renames** need a data migration of `outbox_deliveries.consumer` (decision 4). The ADR records it; no tooling is built for it.

## Target Layout (new/changed this slice)

```
src\DrivingLessons.Application\
├── IntegrationEvents\IIntegrationEvent.cs                                   new
├── IntegrationEvents\IIntegrationEventConsumer.cs                           new
├── IntegrationEvents\IntegrationEventCatalog.cs                             new
├── IntegrationEvents\IntegrationEventServiceCollectionExtension.cs          new
├── IntegrationEvents\Publications\PublicationClosedIntegrationEvent.cs     new (task 4)
├── Abstractions\IOutbox.cs                                                  new
├── Abstractions\ITeacherExcelEmailLog.cs                                    new (task 4)
├── Consumers\TeacherExcelEmail\SendTeacherExcelEmailsConsumer.cs            new (task 4, the email moved here)
├── Consumers\TeacherExcelEmail\TeacherExcelEmailFailedException.cs          new (task 4)
├── EventHandlers\PublicationClosedHandler.cs                                maps to the integration event
└── DependencyInjection.cs                                                   event + consumer registrations
src\DrivingLessons.Infrastructure\
├── Outbox\OutboxMessage.cs · OutboxDelivery.cs · OutboxDeliveryState.cs     new
├── Outbox\OutboxSerializer.cs · Outbox.cs · OutboxProcessor.cs              new
├── Email\SentTeacherExcelEmail.cs · TeacherExcelEmailLog.cs                 new (task 4)
├── Options\OutboxOptions.cs                                                 new
├── Scheduling\ProcessOutboxJob.cs                                           new
├── EntityFramework\EntityConfigurations\OutboxMessageConfiguration.cs       new
├── EntityFramework\EntityConfigurations\OutboxDeliveryConfiguration.cs      new
├── EntityFramework\EntityConfigurations\SentTeacherExcelEmailConfiguration.cs new (task 4)
├── EntityFramework\DrivingLessonsDbContext.cs                               DbSets, CommitAsync in one transaction
├── EntityFramework\Migrations\…_AddOutbox.cs · …_AddSentTeacherExcelEmails.cs new
└── DependencyInjection.cs                                                   outbox, options, Quartz job
src\DrivingLessons.Presentation.Web\appsettings.json                         Outbox section
tests\DrivingLessons.Application.Test\
├── IntegrationEvents\IntegrationEventCatalogTest.cs                         new
├── Outbox\OutboxDeliveryTest.cs · OutboxSerializerTest.cs                   new
├── Consumers\SendTeacherExcelEmailsConsumerTest.cs                          new (US-44 tests moved here)
└── EventHandlers\PublicationClosedHandlerTest.cs                            rewritten
docs\decisions\0010-integration-events-and-outbox.md                         new
docs\decisions\0005-email-over-ses-smtp.md                                   amended
.claude\rules\ddd-architecture.md · CLAUDE.md · docs\tech-stack.md           integration events, outbox
docs\requirements.md · docs\operations\runbook.md                            §9 Delivery; dead deliveries
docs\modules\notifications\README.md                                         slice 1 row links this plan (done at planning)
```
