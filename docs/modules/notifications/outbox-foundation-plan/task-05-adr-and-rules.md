# Task 5 of 6: ADR 0010, the rules, and the docs that describe the close

> Part of [Outbox Foundation](README.md). Work on branch `notifications-outbox-foundation`. Requires tasks 1 to 4 committed.

**Files:**
- Create: `docs\decisions\0010-integration-events-and-outbox.md`
- Modify: `docs\decisions\0005-email-over-ses-smtp.md` (amendment)
- Modify: `.claude\rules\ddd-architecture.md` (Where to Put Code, Domain Events, Deliberately Omitted, a new Integration Events section)
- Modify: `CLAUDE.md` (File Location Quick Reference)
- Modify: `docs\tech-stack.md` (Background Processing)
- Modify: `docs\requirements.md` (§9 Delivery)
- Modify: `docs\operations\runbook.md` (§4 Day to day: outbox)

**Interfaces:**
- Consumes: what tasks 1–4 built, as built. Before writing each fact, re-check it against the code on this branch (class names, table and column names, option defaults, the event name `publication-closed.v1`). Where the code differs from this task's text, the code wins and the text changes.
- Produces: no code change.

- [ ] **Step 1: Write ADR 0010**

Create `docs\decisions\0010-integration-events-and-outbox.md`:

```markdown
# ADR 0010: Integration Events Through a Transactional Outbox, Delivered by Quartz

**Status:** Accepted
**Date:** {the day this task runs}

## Context

Domain events are dispatched in-process by `DrivingLessonsDbContext.CommitAsync`, between saves. Until now the only handler with a side effect outside the database, `PublicationClosedHandler`, sent each Teacher's Excel email inline. Nothing recorded that an email was owed: if the process stopped after the close was saved, the email was lost, and a failed send was only logged (ADR 0005, US-44). Student WhatsApp notifications ([intent](../modules/notifications/student-notifications-intent.md)) need the same path for dozens of messages per Publication, and one event will soon have several independent reactions.

## Decision

**Integration events**, distinct from domain events, written to a **transactional outbox** in PostgreSQL and delivered **after commit, at least once**, to **every consumer registered for them**, by a **Quartz** job inside the app.

- **Contracts in Application:** `IIntegrationEvent` records in `Application\IntegrationEvents\{Context}\` (suffix `IntegrationEvent`, primitives only, serialized as camelCase JSON); consumers implement `IIntegrationEventConsumer<TEvent>` in `Application\Consumers\{Job}\`; producers call `IOutbox.Add`. Producers and consumers never reference each other.
- **Registration is the only link:** `AddIntegrationEvent<TEvent>("{name}.v{n}")` gives each event a stable, versioned wire name; `AddIntegrationEventConsumer<TEvent, TConsumer>()` adds a consumer. `IntegrationEventCatalog` holds both.
- **Storage:** `outbox_messages` (one row per event) and `outbox_deliveries` (one row per event and consumer), written by the same `CommitAsync` as the change, which now always runs in one transaction.
- **Delivery:** `ProcessOutboxJob` (Quartz, every 5 s, never overlapping) leases due deliveries with `FOR UPDATE SKIP LOCKED`, runs each consumer in its own DI scope, and marks it succeeded, schedules a retry (1, 2, 4 … 60 minutes) or, after 8 attempts, marks it dead. `PurgeOutboxJob` deletes succeeded deliveries after 30 days. Settings in the `Outbox` section.
- **Consumers are safe to run twice**, each with its own guard (for the teacher email: `sent_teacher_excel_emails` per Publication, Teacher and version).

## Alternatives Considered

| Alternative | Verdict |
|---|---|
| Keep side effects in domain-event handlers | No record of what is owed, no retry, outbound calls inside the unit of work |
| RabbitMQ, SNS/SQS or another broker | Still needs the outbox (a broker can't join a Postgres transaction); adds a container or a cloud service to run for one deployable whose producers and consumers share a process. The processor sits behind one interface, so a broker can replace it if the app is ever split |
| A hand-written `BackgroundService` loop | Quartz already runs the Publication open and close jobs; one scheduler, one way to configure and observe jobs |
| A per-delivery Quartz trigger for retries | Retries would live in Quartz's in-memory store and vanish on restart; on the row they survive it |
| Persistent Quartz job store | Not needed: what must happen is in the outbox tables, and the outbox job is scheduled from configuration at every start |

## Consequences

- Every new side effect outside the database is a consumer of an integration event, never code in a domain-event handler.
- Delivery is at least once: a consumer must tolerate a second run. Where the outside system has no idempotency key (SMTP), a duplicate is possible only if the process dies between the send and recording it.
- A consumer's class name is stored on its deliveries: renaming a consumer needs a data migration of `outbox_deliveries.consumer`. A breaking payload change is a new event record and a new `.v{n}` name, with both registered until old rows drain.
- Dead deliveries stay in `outbox_deliveries` with `state = 'Dead'` and an Error log line; the runbook shows how to find and re-arm them. There is no screen for them yet.
- Outbox rows are infrastructure records, not domain entities.
```

- [ ] **Step 2: Amend ADR 0005**

At the end of `docs\decisions\0005-email-over-ses-smtp.md` add:

```markdown
## Amendment ({the day this task runs}): the close email goes through the outbox

The Excel email is no longer sent by `PublicationClosedHandler` during the close. The handler queues a `publication-closed.v1` integration event, and `SendTeacherExcelEmailsConsumer` sends one email per Teacher from the outbox job ([ADR 0010](0010-integration-events-and-outbox.md)). A failed send is now **retried** (up to 8 attempts over about two hours) and each sent (Publication, Teacher, version) is recorded in `sent_teacher_excel_emails`, so a retry never sends a Teacher the same version twice. The SMTP transport, MailKit and every setting above are unchanged. The Consequences bullet "Delivery problems are visible only in the logs" now reads: visible in the logs and as `Dead` rows in `outbox_deliveries`.
```

- [ ] **Step 3: Update the architecture rules**

In `.claude\rules\ddd-architecture.md`:

1. **Where to Put Code?** table: after the `Domain event` row add:

```markdown
| Integration event | `Application\IntegrationEvents\{Context}\{Entity}{Action}IntegrationEvent.cs` | `{Entity}{Action}IntegrationEvent` |
| Integration event consumer | `Application\Consumers\{Job}\{Job}Consumer.cs` | `{Job}Consumer` |
```

2. **Domain Events**, replace the bullet "Events are dispatched after `SaveChanges` succeeds (in-process handlers, e.g. the Excel email on `PublicationClosed`), then `CommitEvents()` clears them" with:

```markdown
- Events are dispatched in-process inside `CommitAsync`'s transaction, after each save, then `CommitEvents()` clears them. Handlers change only the database: anything outside it (email, WhatsApp, HTTP) is an integration event queued with `IOutbox.Add` (see Integration Events)
```

3. Add a section **Integration Events** just before **Repository Interfaces** (or the next `###` after Domain Events):

```markdown
### Integration Events

Facts published for consumers outside the aggregate's transaction: notifications, emails, anything that calls out. They go through the transactional outbox ([ADR 0010](../../docs/decisions/0010-integration-events-and-outbox.md)).

- A record implementing `IIntegrationEvent`, primitives only (`Guid`, `int`, `string`, lists of records), in `Application\IntegrationEvents\{Context}\`, named `{Entity}{Action}IntegrationEvent` with a `const string Name = "{entity}-{action}.v{n}"`
- Raised by a domain-event handler that maps the domain event (`PublicationClosedHandler` → `PublicationClosedIntegrationEvent`), or by an interactor or job with no domain change behind it; queued with `IOutbox.Add`, written by `CommitAsync`
- Registered in `AddApplication` with `AddIntegrationEvent<T>(T.Name)`; never rename a published name, add `.v{n+1}` for a breaking change
- A consumer implements `IIntegrationEventConsumer<T>`, does one job, lives in `Application\Consumers\{Job}\`, and is registered with `AddIntegrationEventConsumer<TEvent, TConsumer>()`. An event can have many consumers; they never know about each other or about the producer
- Consumers turn the event's `Guid`s into typed IDs with `.Of()` before touching the domain, use their own `IUnitOfWork`, and **must be safe to run twice** (delivery is at least once): check a guard record before the side effect, record it right after
- A consumer signals "retry me" by throwing; the outbox retries with backoff and marks the delivery dead after the last attempt
```

4. **Deliberately Omitted**: replace "Outbox pattern + message broker integration events — domain events are handled in-process" with:

```markdown
- A message broker (RabbitMQ, SNS/SQS, Kafka): integration events go through the PostgreSQL outbox and a Quartz job in the app ([ADR 0010](../../docs/decisions/0010-integration-events-and-outbox.md))
```

5. **Infrastructure Layer**: add one line to the DbContext section: "`CommitAsync` opens a transaction when none is open, so the aggregate, the domain-event handlers' rows and the outbox rows commit together. Outbox rows (`Infrastructure\Outbox\`) are infrastructure records: plain classes with EF configurations, no `AggregateRoot`, no typed IDs."

- [ ] **Step 4: Update CLAUDE.md**

In the **File Location Quick Reference** table, after the `Domain event` row add the same two rows as Step 3.1. In **Implementation Checklist → During implementation** add: `- [ ] Side effects outside the database go through an integration event and a consumer (ADR 0010), never inline in a handler`.

- [ ] **Step 5: Update tech-stack, requirements and runbook**

`docs\tech-stack.md`, **Background Processing**: replace the paragraph with:

```markdown
Quartz runs everything that happens on a clock inside the app process: opening and closing each Publication at its window times (`OpenPublicationJob`, `ClosePublicationJob`, re-registered at startup by `PublicationReconciliationHostedService`), and delivering integration events from the PostgreSQL outbox every few seconds (`ProcessOutboxJob`, `PurgeOutboxJob`; [ADR 0010](decisions/0010-integration-events-and-outbox.md)). The teacher's Excel email at every close is the first outbox consumer. No external scheduler, no cron, no broker.
```

`docs\requirements.md` §9 **Delivery**, replace the "Automatic" bullet with:

```markdown
- **Automatic:** emailed to the teacher's contact email at every window close (versioned per 6.4). A failed send is retried for about two hours; a Teacher never receives the same version twice
```

`docs\operations\runbook.md` §4 **Day to day**: add a subsection:

````markdown
### Outbox: emails that didn't go out

Emails (and, later, WhatsApp messages) are sent by the outbox job. A delivery that failed 8 times is `Dead` and logged at Error ("Outbox delivery [...] is dead after [8] attempts."). To list them:

```bash
docker compose exec postgres psql -U drivinglessons -d drivinglessons -c "SELECT d.id, m.event_name, d.consumer, d.attempts, d.last_error, d.completed_at_utc FROM outbox_deliveries d JOIN outbox_messages m ON m.id = d.message_id WHERE d.state = 'Dead' ORDER BY d.completed_at_utc DESC;"
```

After fixing the cause (for example SES credentials), re-arm one delivery; the job picks it up within seconds, and Teachers already sent are skipped:

```bash
docker compose exec postgres psql -U drivinglessons -d drivinglessons -c "UPDATE outbox_deliveries SET state = 'Pending', attempts = 0, next_attempt_at_utc = now(), completed_at_utc = NULL WHERE id = '<delivery id>';"
```
````

Check the `psql` user and database names against the runbook's existing commands and use the same ones.

- [ ] **Step 6: Check links and commit**

```bash
grep -rn "0010-integration-events-and-outbox" docs .claude CLAUDE.md
```

Every link must resolve (relative paths from each file). Then:

```bash
git add docs/decisions/0010-integration-events-and-outbox.md docs/decisions/0005-email-over-ses-smtp.md .claude/rules/ddd-architecture.md CLAUDE.md docs/tech-stack.md docs/requirements.md docs/operations/runbook.md
git commit -m "docs(notifications): ADR 0010 integration events and outbox; rules, close email and runbook"
```
