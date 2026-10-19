# Notifications Module — Roadmap

Messages the system sends on its own: WhatsApp messages to Students around a Publication's submission window, and the teacher's Excel email moved onto the same delivery path. Each slice ships as its own branch/PR with its own plan folder (`docs\modules\notifications\...-plan\`, same format as prior modules), authored **just-in-time** when the slice starts.

**Start here:** [Student Notifications - Intent](student-notifications-intent.md) (19 October 2026). It records what stage 1 delivers, the architecture it commits to (integration events, a transactional outbox in PostgreSQL, independent consumers, no broker), and the questions planning closed.

## Stage 1 at a glance

| Capability | Trigger |
|------------|---------|
| Opening notification to every recipient | Publication becomes `Open` |
| Window change notification with the new end time | Window extended, or Publication reopened |
| Reminder to recipients without a Submission | A person presses "Send reminder" on the dashboard |
| Reminder to recipients without a Submission | Scheduled checks at configured times while `Open` |
| Teacher's Excel email, now with retries | Publication closes |

## Locked decisions (19 October 2026)

| # | Decision |
|---|----------|
| 1 | **WhatsApp over the Meta Cloud API is the first channel**; other channels are later consumers of the same integration events |
| 2 | **Integration events go through a transactional outbox in PostgreSQL**, processed by a Quartz job; each event may have many consumers, tracked and retried per consumer |
| 3 | **No RabbitMQ, SNS, SQS or other broker.** A broker would still need the outbox and adds a container to run; the processor sits behind one interface so a broker can replace it if the app is ever split |
| 4 | **The teacher's Excel email moves onto the outbox in stage 1**, unchanged in content and versioning (decision #22) |
| 5 | **Extend and reopen send a window change notification**, never the opening notification again |
| 6 | **Consent is not captured in DriveCal.** Students come from the school's own system through the Roster and agree to messages there, so every Student counts as having agreed |
| 7 | **Quartz runs everything on a clock**: the outbox processor (a recurring job) and the scheduled reminder checks (a cron trigger per open Publication, Asia/Jerusalem). Retries live on the outbox row, and startup reconciliation re-registers the triggers |
| 8 | **Messages go out from the school's existing number** (Coexistence): it stays in the WhatsApp Business app on the manager's phone, so replies arrive there and DriveCal never handles them |
| 9 | **A window change notification is sent right away**, even during quiet hours; quiet hours apply only to scheduled reminders |

## Slices

Each slice ships as its own branch and PR. Slice 1 has its plan; the others get theirs **just in time**, when they start, so they absorb what earlier slices taught.

| Slice | Content | Plan |
|-------|---------|------|
| 1. Outbox foundation | Integration events, the transactional outbox, per-consumer deliveries with retries, the Quartz outbox job, and the teacher's Excel email moved onto it as the first consumer. No visible change for Administrators; the email gains retries and never goes out twice | [plan](outbox-foundation-plan/README.md) |
| 2. WhatsApp and the opening notification | Recipients, Israeli numbers to international format, the per-Student message record, the WhatsApp Cloud API sender (Coexistence, log-only when disabled), the opening template; requirements decision #26, glossary terms, ADR 0011 | when the slice starts |
| 3. Window change notification | Extend and reopen send the new end time to every recipient, right away; a newer change supersedes messages still waiting | when the slice starts |
| 4. Manual reminder and what the Administrator sees | "Send reminder" on the dashboard (Administrator only, 202), recipients without a Submission, the shared reminder limits, sent / failed / skipped counts and the per-Student list | when the slice starts |
| 5. Scheduled reminders | A cron trigger per open Publication (Asia/Jerusalem), quiet hours (nights and Shabbat), the last-call reminder, the per-Publication on/off switch (default on) | when the slice starts |

Slice 1 goes first because every later slice writes integration events and consumers, and moving an existing, well-tested email onto the outbox proves it before any Student is messaged. Slices 2 to 5 follow the order a Publication lives through: open, change, remind by hand, remind on a schedule.

## Answered questions (19 October 2026)

Administrator only for manual reminders; scheduled times 10:00 / 14:00 / 19:00 with a 4-hour gap, at most 3 reminders and a last call 3 hours before the end; quiet hours nights 21:00-08:00 and Shabbat; the window change notification goes to every recipient; scheduled reminders default on; succeeded outbox deliveries are kept 30 days. Details in [the intent §8](student-notifications-intent.md#8-decisions-closed-for-planning).
