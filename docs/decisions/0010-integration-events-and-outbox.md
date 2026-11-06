# ADR 0010: Integration Events Through a Transactional Outbox, Delivered by Quartz

**Status:** Accepted
**Date:** 6 November 2026

## Context

Domain events are dispatched in-process by `DrivingLessonsDbContext.CommitAsync`, between saves. Until now the only handler with a side effect outside the database, `PublicationClosedHandler`, sent each Teacher's Excel email inline. Nothing recorded that an email was owed: if the process stopped after the close was saved, the email was lost, and a failed send was only logged ([ADR 0005](0005-email-over-ses-smtp.md), US-44). Student WhatsApp notifications ([intent](../modules/notifications/student-notifications-intent.md)) need the same path for dozens of messages per Publication, and one event will soon have several independent reactions.

## Decision

**Integration events**, distinct from domain events, written to a **transactional outbox** in PostgreSQL and delivered **after commit, at least once**, to **every consumer registered for them**, by a **Quartz** job inside the app.

- **Contracts in Application:** `IIntegrationEvent` records in `Application\IntegrationEvents\{Context}\` (suffix `IntegrationEvent`, primitives only, serialized as camelCase JSON); consumers implement `IIntegrationEventConsumer<TEvent>` (`ConsumeAsync`, returning a `DeliveryOutcome`) in `Application\Consumers\{Job}\`; producers call `IOutbox.Add`. Producers and consumers never reference each other.
- **Registration is the only link:** `AddIntegrationEvent<TEvent>("{name}.v{n}")` gives each event a stable, versioned wire name (the first is `publication-closed.v1`); `AddIntegrationEventConsumer<TEvent, TConsumer>()` adds a consumer. `IntegrationEventCatalog` holds both.
- **Storage:** `outbox_messages` (one row per event) and `outbox_deliveries` (one row per event and consumer), written by the same `CommitAsync` as the change, which now always runs in one transaction.
- **Delivery:** `ProcessOutboxJob` (Quartz, every 5 s, never overlapping) leases due deliveries with `FOR UPDATE SKIP LOCKED`, runs each consumer in its own DI scope, and marks it succeeded, schedules a retry (1, 2, 4 … 60 minutes) or, after 8 attempts, marks it dead. A consumer can also answer `DeliveryOutcome.HoldUntil(instant)`: the delivery waits until then without counting a failure. An unknown event or consumer name, or a payload that doesn't deserialize, is dead at once, without retries. `PurgeOutboxJob` (hourly) deletes succeeded deliveries after 30 days, and then the messages left with no delivery. Settings in the `Outbox` section (`PollSeconds`, `BatchSize`, `MaxAttempts`, `LeaseSeconds`, `RetentionDays`).
- **Consumers are safe to run twice**, each with its own guard (for the teacher email: `sent_teacher_excel_emails` per Publication, Teacher and version).

## Alternatives Considered

| Alternative | Verdict |
|---|---|
| Keep side effects in domain-event handlers | No record of what is owed, no retry, outbound calls inside the unit of work |
| RabbitMQ, SNS/SQS or another broker | Still needs the outbox (a broker can't join a Postgres transaction); adds a container or a cloud service to run for one deployable whose producers and consumers share a process. The processor sits behind one interface, so a broker can replace it if the app is ever split |
| A hand-written `BackgroundService` loop | Quartz already runs the Publication open and close jobs; one scheduler, one way to configure and observe jobs |
| A per-delivery Quartz trigger for retries | Retries would live in Quartz's in-memory store and vanish on restart; on the row they survive it |
| Persistent Quartz job store | Not needed: what must happen is in the outbox tables, and the outbox jobs are scheduled from configuration at every start |

## Consequences

- Every new side effect outside the database is a consumer of an integration event, never code in a domain-event handler.
- Delivery is at least once: a consumer must tolerate a second run. Where the outside system has no idempotency key (SMTP), a duplicate is possible only if the process dies between the send and recording it.
- A consumer's class name is stored on its deliveries: renaming a consumer needs a data migration of `outbox_deliveries.consumer`. A breaking payload change is a new event record and a new `.v{n}` name, with both registered until old rows drain.
- Dead deliveries stay in `outbox_deliveries` with `state = 'Dead'` and an Error log line; the [runbook](../operations/runbook.md) shows how to find and re-arm them. There is no screen for them yet.
- Outbox rows are infrastructure records, not domain entities.
