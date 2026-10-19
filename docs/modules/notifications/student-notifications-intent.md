# Student Notifications - Intent

**Status:** Draft intent, input to planning
**Date:** 19 October 2026
**Module:** `module:notifications` (new)

This document says **what** we want to achieve and **why**, and the architectural shape we commit to. It is not a plan: tasks, file lists and the slice order come later, in a plan folder next to this file (`docs\modules\notifications\...-plan\`, the same format as the other modules).

## 1. Outcome

When a Publication's submission window opens, every Student who can submit receives a WhatsApp message with the weekly link. When the window is extended or reopened, they are told the new end time. While the window is open, Students who have not submitted yet receive a reminder, sent either:

- **manually**, when a person in the product decides to send one, or
- **on a schedule**, a few times a day, while the window is open.

Behind this, the system gets a simple, durable way to publish **integration events** through a **transactional outbox**, and to hand each event to **any number of consumers**. WhatsApp is the first consumer. More channels and other side effects plug in later without touching the code that raises the event.

## 2. Why

- The weekly link is still sent by hand (requirements §6.2 step 4). Chasing students who haven't submitted is the part of the old WhatsApp workload that remains, and that workload is what this system exists to remove (requirements §1).
- Side effects run today inside the caller's unit of work. `DrivingLessonsDbContext.SaveAndDispatchAsync` dispatches domain events synchronously, between saves and, when an explicit transaction is open (`TeacherAndCarLocks`), before it commits. `PublicationClosedHandler` sends the teacher's email inline. Nothing durable records that a send is owed: if the process stops between the save and the send, the email is lost, and a failed send is only logged, with no retry (ADR 0005). Adding WhatsApp the same way would multiply these problems, and it would mean dozens of outbound HTTP calls inside one request or job. Stage 1 fixes this for the teacher's email too, not only for the new WhatsApp messages.
- We want a structure that can grow: more channels (SMS, email, push), more events, and maybe a real broker one day, without paying for SNS/SQS-level infrastructure now.

## 3. Scope

### Stage 1 (this intent)

| # | Capability |
|---|------------|
| 1 | Integration events, a transactional outbox in PostgreSQL, an outbox processor run by Quartz, and consumers registered independently, many per event |
| 2 | **Opening notification**: when a Publication becomes `Open`, every recipient (§4.1) gets a WhatsApp template message with the link |
| 3 | **Window change notification**: when an open window is extended, or a closed Publication is reopened, recipients get a WhatsApp message with the new end time and the link |
| 4 | **Manual reminder**: from the Publication dashboard, a person decides to send a reminder now, to every recipient without a Submission |
| 5 | **Scheduled reminders**: while the window is open, the system checks at configured times of day and reminds every recipient without a Submission, within the anti-spam limits in §4.5 |
| 6 | **WhatsApp channel** over the Meta WhatsApp Cloud API: approved Hebrew Utility templates, Israeli phone numbers converted to international format, and a sender that logs instead of sending when disabled (same pattern as `Email:Enabled`) |
| 7 | A record of every message attempt per Student per Publication (sent / failed / skipped, with reason), so nobody is messaged twice and the Administrator can see who wasn't reached |
| 8 | **The teacher's Excel email moves onto the outbox**: `PublicationClosedHandler` no longer sends inline; a consumer of the `PublicationClosed` integration event sends one email per Teacher, with retries and per-Teacher tracking. Subject, body, attachment and versioning stay exactly as today (decision #22) |

### Later stages (not in stage 1)

- Delivery and read statuses from the WhatsApp webhook, and handling opt-outs the student sends in WhatsApp.
- A per-Publication delivery report screen beyond the counts of stage 1.
- More channels (SMS, email to students, push) as new consumers of the same events.
- Replacing the in-process outbox processor with a broker, should the app ever run as more than one deployable.

### Non-goals

- No SNS, SQS, RabbitMQ, Kafka or any other external broker or queue. A broker would still need the outbox (it can't join a PostgreSQL transaction), so it adds a container to run, monitor and back up without removing any of this design. It becomes worth it only when the app is split into several deployables.
- No notification of the final booked schedule (requirements decision #13 still holds for that).
- No free-text WhatsApp conversations or replies handled by the system.
- No marketing messages. Only Utility templates tied to the Student's own weekly submission.

## 4. Product behavior

### 4.1 Recipients

A **recipient** of a Publication is an active Student whose Teacher has a Week Schedule in that Publication, and who has a phone number that converts to a valid WhatsApp number. Inactive Students and Students of Teachers absent from that week are never messaged. A Student whose number can't be converted is recorded as **skipped** with that reason.

### 4.2 Opening notification

- Triggered once, when the Publication transitions to `Open`, whether by the scheduled job or by reconciliation at startup.
- Content (Hebrew Utility template): the Student's name, the week, the window's end time (Asia/Jerusalem), and a button to the weekly link.
- **Reopen** and **extend window** don't send the opening notification again. They send the window change notification (§4.3).

### 4.3 Window change notification

- Triggered by the two ways a window changes today: **extend window** while `Open` (`PublicationWindowExtended`) and **reopen** after `Closed` (`PublicationReopened`). Every later way of changing the window raises its own domain event and joins this trigger.
- Content (Hebrew Utility template, separate from the opening one): the week, the **new** end time (Asia/Jerusalem), and a button to the weekly link. For a reopen, the wording says the window is open again.
- Sent to all recipients by default, including Students who already submitted, since a longer window also lets them revise. Whether to limit it to Students without a Submission is open question 4.
- Sent right away, because it follows a deliberate action by a person, even during quiet hours: quiet hours apply only to scheduled reminders (§4.5). It doesn't count toward the reminder cap, but it does count as the Student's latest message for the minimum gap before the next scheduled reminder (§4.5).
- Changing the window again before the previous messages went out sends only the latest end time: messages still waiting for an older change are dropped as superseded.

### 4.4 Manual reminder

- Available only while the Publication is `Open`. Asking for one on a Publication in any other state is refused with the existing `PublicationMustBeOpenException` (409), not silently ignored. That's what CLAUDE.md rule 1, "operations are not idempotent", asks for: a command that can't do what it says fails loudly. Sending a second reminder while the window is still open is not a repeat of the same transition, so it is allowed, and the limits in §4.5 decide who actually gets it.
- The dashboard shows how many recipients have no Submission yet. The person confirms, and the reminder is queued. The request returns immediately; sending happens in the background.
- Who may send it: the **Administrator**, for the whole Publication. Whether a Teacher-role User may remind only their own Teacher's Students is an open question (§8).
- Who is reminded is decided **when the reminder is processed**, not when it was requested: a Student who submitted in between is not reminded.

### 4.5 Scheduled reminders

- While a Publication is `Open`, a recurring check runs at configured local times (Asia/Jerusalem), e.g. `10:00, 14:00, 19:00`. The times are configuration, not code. Quartz runs the check (§5.3).
- Each check reminds recipients without a Submission, subject to these limits:
  - no reminder in the first hours after the opening notification, or within a minimum gap after the Student's last reminder, manual or scheduled;
  - at most **N** reminders per Student per Publication, counting manual and scheduled together;
  - quiet hours (night, and possibly Shabbat, see §8): checks inside them are skipped, not postponed;
  - a last-call reminder at a set time before the window ends, if not already covered by a check.
- Scheduled reminders can be switched off per Publication (default on), so the person deciding can rely on manual reminders only.

### 4.6 Consent

Not captured in DriveCal. Students reach DriveCal through the Roster, which the Administrator exports from the school's own system, and Students give their consent to receive messages there. Every Student in DriveCal is treated as having agreed: there is no consent field, no consent column in the Roster and no "no consent" skip. Keeping consent records stays with the school's own system. An opt-out a Student sends in WhatsApp is still a later-stage concern (§3).

### 4.7 What the Administrator sees

On the Publication dashboard, per Publication: recipients, opening and window change notifications sent / failed / skipped, reminders sent, Students not yet submitted, and whether each Teacher's Excel email was sent. Failures and skips are listed by Student with their reason (e.g. invalid number, provider error).

## 5. Architecture intent

### 5.1 Two kinds of events, kept apart

| | Domain event | Integration event |
|---|---|---|
| Purpose | A fact inside the domain, for the domain and application layers | A fact published for **consumers** (other modules, channels, the outside world) |
| Lives in | `Domain\Events\` (unchanged) | Application, as an explicit, versioned contract |
| Delivered | In-process, inside the unit of work (as today) | Through the outbox, **after commit**, at least once |
| Payload | Typed IDs, value objects | Primitives and IDs only: serializable, stable, versioned |

A domain event is never published as is. An application-level handler maps it to an integration event and writes it to the outbox in the **same transaction** as the aggregate change. A trigger with no domain change behind it (a scheduled check) writes its integration event directly. Naming follows the domain: past tense, glossary terms (e.g. `PublicationOpened`, `SubmissionReminderRequested`, distinct by namespace and suffix in planning).

### 5.2 Transactional outbox

- A PostgreSQL `outbox_messages` table: id, event type, schema version, JSON payload, occurred-at (UTC), and processing state.
- Written by the same `DbContext` and transaction as the business change. If the transaction rolls back, the event never existed. If it commits, the event will be delivered.
- No outbound call ever happens inside a request's unit of work.

### 5.3 Quartz runs everything that happens on time

Quartz is already the app's scheduler: `OpenPublicationJob` and `ClosePublicationJob` run from `PublicationScheduler`, and `PublicationReconciliationHostedService` catches up on startup. Everything new that runs on a clock uses it too, with no second scheduler and no hand-written `BackgroundService` loop.

- **Outbox processor:** one recurring Quartz job, every few seconds, marked `[DisallowConcurrentExecution]` so runs never overlap. Each run claims a batch of due rows with `SELECT ... FOR UPDATE SKIP LOCKED`, so a second app instance during a deploy never double-claims a row, and hands each row to its consumers.
- **Retries:** stored on the outbox row (attempts, next attempt time), not as extra Quartz triggers. The same recurring job picks a row up again once its next attempt time has passed, so a retry survives a restart.
- **Scheduled reminders:** one Quartz cron trigger per open Publication, in the Asia/Jerusalem time zone, built from the configured times. It is registered when the Publication opens or reopens, kept when the window is extended, and removed when it closes, through `PublicationScheduler`, next to the existing open and close triggers. A run only writes a "reminder requested (scheduled)" integration event to the outbox; it never sends anything itself.
- **After a restart:** Quartz keeps its triggers in memory today (`AddQuartz()` with the default RAM store), so `PublicationReconciliationHostedService` also re-registers the outbox job and the reminder triggers of every open Publication, the same way it re-registers open and close today. Nothing is lost, because what must happen is in the database (the outbox and the Publication's state), not in Quartz. A persistent Quartz store isn't needed.
- **Transport behind one interface:** the outbox job calls the consumers through it. Replacing it with a broker later changes only this piece, never producers or consumers.

### 5.4 Consumers

- A consumer is a class that handles **one** integration event type and does one job. An event may have **many** consumers, registered in DI independently of the producer and of each other. The producer never knows who consumes.
- Delivery is tracked **per consumer**: each (message, consumer) pair has its own state, attempts and last error. A failing WhatsApp consumer retries on its own, without re-running a consumer that already succeeded.
- Retries with backoff up to a maximum, then a **dead** state that is logged and visible, never silently dropped.
- Delivery is **at least once**, so every consumer must be safe to run twice. This is a different idea from CLAUDE.md rule 1, which is about domain commands (§4.4): here, the same event may simply be *delivered* again, after a crash or a retry, and the second delivery must change nothing. For notifications, the per-Student message record (§3 item 7) is the guard: a (Publication, Student, kind, trigger) that is already sent is not sent again. For the teacher's email, the guard is (Publication, Teacher, Excel version).

### 5.5 Fan-out per Student

One integration event concerns a whole Publication, but a message concerns one Student. The Publication-level consumer resolves recipients and writes one **per-Student** unit of work (e.g. a per-recipient message row or a per-Student integration event, decided in planning). The WhatsApp sender processes them one by one. One bad number, or one provider error, never blocks or repeats the others, and retries happen per Student.

### 5.6 Triggers, all through the same path

```
Publication opens (Quartz / reconciliation)
    → domain event PublicationOpened
    → mapped to integration event, outbox, same transaction
    → consumer: opening notification → per-Student messages → WhatsApp

Window extended (Open) or Publication reopened (Closed)
    → domain event PublicationWindowExtended / PublicationReopened
    → integration event "window changed", outbox, same transaction
    → consumer: window change notification → per-Student messages → WhatsApp

Person presses "Send reminder" (dashboard, 202 Accepted)
    → interactor, Publication must be Open
    → integration event "reminder requested (manual)", outbox
    → consumer: reminder → recipients without Submission, limits (§4.5) → per-Student messages → WhatsApp

Quartz cron trigger at configured times, while Open
    → integration event "reminder requested (scheduled)", outbox
    → same reminder consumer

Publication closes (Quartz / reconciliation)
    → domain event PublicationClosed (Excel versions already incremented)
    → integration event "publication closed", outbox, same transaction
    → consumer: teacher Excel email → one email per Teacher → SMTP (ADR 0005)
```

The close is the proof of "many consumers": later, a student-facing consumer (e.g. "the window is closed") can subscribe to the same integration event without touching the teacher email.

Manual and scheduled reminders differ only in their trigger. They share one consumer, one set of anti-spam limits and one message record.

### 5.7 WhatsApp channel

- Port in Application (an `IWhatsAppSender`-style abstraction, like `IEmailSender`). Implementation in Infrastructure over `HttpClient` to the Cloud API (`graph.facebook.com`), with no third-party SDK unless planning finds a reason.
- Configuration through environment variables, like `EMAIL_*` (`.env.example`): access token, Phone Number ID, WhatsApp Business Account ID, template names and language, an `Enabled` switch. Disabled, the sender logs and records the message as skipped. Locally, it stays disabled or points at a fake.
- Phone numbers: `PhoneNumber` keeps the roster text as entered. A conversion to international format (`05X-XXXXXXX` → `9725XXXXXXXX`) is added, and a number that can't convert is a recorded skip, never an exception that stops the batch.
- Templates are created and approved in the client's WhatsApp Manager (Utility, Hebrew). The template names in configuration must match.
- **The school's existing number sends the messages** (Meta's Coexistence): the same number stays active in the WhatsApp Business app on the manager's phone and is connected to the Cloud API. Messages DriveCal sends also appear in the app's chats, and a Student's reply lands there too, so the manager answers by hand as today and DriveCal never handles replies (§3 non-goals). The code is the same as with a dedicated number; only onboarding differs (§9).

## 6. Documentation and decisions this will change

- `docs/requirements.md`: §3 in scope, §6.2 step 4 (the link is sent by the system; manual distribution stays possible), §6.3 (reminders during the window), and a new decision #26 in §11. Decision #13 stays, narrowed to "final schedule notification".
- `CONTEXT.md`: new glossary terms (e.g. Opening Notification, Window Change Notification, Submission Reminder, Recipient), with words to avoid.
- New ADRs: **0010** integration events and transactional outbox (processor and reminder checks run by Quartz, per-consumer delivery, no broker); **0011** WhatsApp over the Cloud API (templates, configuration, consent held by the school's own system, cost).
- ADR 0005 amended: the teacher's email is sent by an outbox consumer, with retries, instead of inline with no retry.
- `.claude/rules/ddd-architecture.md`: where integration events, outbox writes and consumers live, so later code follows one pattern.
- `docs/operations/runbook.md`: WhatsApp credentials and template setup, reading dead outbox messages.

## 7. Success criteria

1. When a Publication opens, every recipient with a valid number gets one opening message. Nobody gets two, including after an app restart during the send.
2. A manual reminder reaches only recipients without a Submission at the time it is processed, and the dashboard request never waits on WhatsApp.
3. Scheduled reminders respect the configured times, quiet hours, minimum gap and per-Student cap, and stop when the window closes.
4. A WhatsApp outage or a bad number never fails a Publication transition, never blocks other Students, and ends as either a retried success or a visible failure.
5. Adding a second consumer to an existing integration event takes a new class and a DI registration, with no change to the producer or to the other consumers.
6. With WhatsApp disabled, the whole flow runs end to end and logs what would have been sent.
7. Extending or reopening a window sends one message with the latest end time, never one per intermediate change.
8. Every close still emails each Teacher exactly one file with the right version. An SMTP failure is retried, and a restart during the close neither loses nor duplicates an email.

## 8. Open questions for planning

| # | Question | Default if not decided |
|---|----------|------------------------|
| 1 | May a Teacher-role User send a manual reminder, limited to their own Teacher's Students? | Administrator only |
| 2 | Default scheduled times, minimum gap, per-Student cap, and the last-call offset before the window ends | 10:00 / 14:00 / 19:00, 4 hours, 3 reminders, 3 hours before end |
| 3 | Quiet hours: nights only, or also Friday evening through Saturday? | Nights 21:00-08:00 and Shabbat |
| 4 | Does the window change notification go to every recipient, or only to Students without a Submission? | Every recipient |
| 5 | Do scheduled reminders default to on or off for each Publication? | On |
| 6 | Outbox and delivery-record retention: how long are processed rows kept? | 30 days |

Decided since the first draft (19 October 2026): reopen and extend send the window change notification (§4.3), the teacher's Excel email moves onto the outbox in stage 1 (§3 item 8), there is no RabbitMQ or other broker (§3 non-goals), consent is held by the school's own system, not DriveCal (§4.6), Quartz runs the outbox processor and the scheduled reminder checks (§5.3), messages go out from the school's existing number through Coexistence (§5.7), and a window change notification is sent right away, even during quiet hours (§4.3).

## 9. Prerequisites outside the code

- The client's Meta Business portfolio, WhatsApp Business Account and a payment method (see the client guide shared in conversation).
- The school's existing WhatsApp Business app number connected to the Cloud API through Coexistence, with the manager's phone at hand for the verification code. Coexistence is offered through Meta's Embedded Signup and depends on the region; if Meta's direct onboarding doesn't offer it for Israel, a provider that supports Coexistence is used for onboarding, which changes only the sender's configuration, not the design.
- Three Hebrew Utility templates approved: opening notification, window change notification, and reminder.
- A permanent system-user access token, stored only in the production `.env`, never in git, chat or email.
