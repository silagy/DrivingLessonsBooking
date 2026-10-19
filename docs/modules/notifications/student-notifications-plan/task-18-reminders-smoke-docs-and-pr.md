# Task 18 of 18: Milestone C smoke, the runbook, then the PR

> Part of [Student Notifications (stage 1)](README.md), milestone C. Requires tasks 15–17 committed on `notifications-reminders`.

**Files:**
- Modify: `docs\operations\runbook.md` (new §1.9 WhatsApp, §4 day-to-day "Reading the outbox")
- Modify: `docs\modules\notifications\student-notifications-intent.md` (Status: "Stage 1 planned and built, see the plan"; open questions → the decisions that answered them)
- Modify: `docs\requirements.md` (final pass against intent §6), `README.md` (feature list), if needed
- Scratch only: `<scratchpad>/notifications-c/`

**Interfaces:**
- Consumes: everything; task 14's fake Cloud API.
- Produces: PR C. Stage 1 of the intent is done.

- [ ] **Step 1: Run every suite**

As task 14 Step 1.

- [ ] **Step 2: Start and seed**

As task 14 Step 3 (database `drivinglessons_notifications_c`), with quiet hours away from now and **check times set to the next two 15-minute ticks** (`Notifications__Reminders__CheckTimes__0=HH:MM`, `__1=HH:MM`). Set `Notifications__Reminders__FirstReminderDelayHours=0` and `MinimumGapHours=0` for Steps 3–4, so the limits don't hide what is being tested. Step 5 sets them back.

- [ ] **Step 3: Manual reminder (Review Focus 9, 12)**

1. Open the window, wait for the openings. Let one Student submit.
2. `POST …/reminders` → 202 at once (time the call: no WhatsApp request happens inside it). Expected: `Reminder` rows only for the Students without a Submission, sent.
3. With quiet hours set to contain now (restart), request again, then let a Student submit **before** the hold ends. Release the hold (SQL `next_attempt_at_utc = now()`) after restoring quiet hours. Expected: that Student is `Skipped AlreadySubmitted`, and nothing is sent to them.
4. Close the publication, then `POST …/reminders` → 409 with the `PublicationMustBeOpenException` problem details; no outbox row.

- [ ] **Step 4: Scheduled reminders**

Expected at each configured tick: one `submission-reminder-requested` message with `source = 20` and the tick's `checkUtc`, and `scheduled-reminder:{tick}` rows. Turn the switch off from the dashboard before the second tick: no message at that tick. Turn it on again.

- [ ] **Step 5: Limits and two instances (Review Focus 10)**

Restore the default limits (4 h / 4 h / 3). Start a second API on 5081, same database. At the next tick, expected: **two** outbox messages (one per instance) with the same `checkUtc`, **one** set of `scheduled-reminder:{tick}` rows (the unique index held; the second consumer's plan saw them, or its commit failed on the index and the retry saw them), and one WhatsApp request per eligible Student in `requests.log`. Students reminded in Step 3 get nothing (gap).

- [ ] **Step 6: The client**

Hebrew, English, 768px: the Send reminder dialog with the count, the zero state, the toast, the counts moving after the reload, the switch, and the 409 path (close the window in another tab, then confirm).

- [ ] **Step 7: Runbook**

`§1.9 WhatsApp (Meta Cloud API)`: the onboarding checklist from intent §9 and Open Item 2; the three template names, which must match `WHATSAPP_TEMPLATE_*`, their exact Hebrew text and parameters as approved (Utility, `he`; the URL button base `https://{DOMAIN}/s/`); the permanent system-user token, `.env` only, and rotation in §7; turning it on (`WHATSAPP_ENABLED=true`, redeploy) and the dry run with it off.

`§4 Reading the outbox`: `SELECT consumer, state, attempts, next_attempt_at_utc, left(last_error, 200) FROM outbox_deliveries WHERE state <> 20 ORDER BY next_attempt_at_utc;`. What a dead delivery means for each consumer. How to retry one by hand: `UPDATE outbox_deliveries SET state = 10, attempts = 0, next_attempt_at_utc = now() WHERE id = '…';`. Every consumer is safe to re-run, so this never sends a Teacher or a Student a duplicate of something already recorded.

- [ ] **Step 8: Docs final pass, clean up, PR**

Walk intent §6 line by line and tick each document. Update the intent's status and its §8 table (each question → "Decided: README decision N"). Drop the database. Open PR C, `feat(notifications): manual and scheduled submission reminders`, with the evidence of Steps 3–5 and screenshots of Step 6. In the description, map intent §7's eight success criteria to the evidence across the three PRs.
