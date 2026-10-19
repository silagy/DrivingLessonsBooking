# Notifications Module — Roadmap

Messages the system sends on its own: WhatsApp messages to Students around a Publication's submission window, and the teacher's Excel email moved onto the same delivery path. Each slice ships as its own branch/PR with its own plan folder (`docs\modules\notifications\...-plan\`, same format as prior modules), authored **just-in-time** when the slice starts.

**Start here:** [Student Notifications - Intent](student-notifications-intent.md) (19 October 2026). It records what stage 1 delivers, the architecture it commits to (integration events, a transactional outbox in PostgreSQL, independent consumers, no broker), and the open questions planning must close.

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

## Slices

To be decided in planning.
