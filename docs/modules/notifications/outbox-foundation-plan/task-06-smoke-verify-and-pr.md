# Task 6 of 6: Smoke on Postgres with Mailpit, full suites, and the PR

> Part of [Outbox Foundation](README.md). Work on branch `notifications-outbox-foundation`. Requires tasks 1 to 5 committed. Commands from the repo root in bash, **one shell for Steps 2–7** (they share variables).

**Files:**
- No planned changes. A step that finds a defect fixes it in the owning task's files, with a failing test first where one can be written, as its own commit `fix(notifications): <what>`, before the PR.
- Local only, never staged: smoke files under the git-ignored `.superpowers/sdd/outbox-smoke/`.

**Interfaces:**
- Consumes: everything from tasks 1–5; the US-44 smoke ([task 3](../../excel/us-44-versioned-excel-email-plan/task-03-smoke-verification-and-pr.md)) for the setup it reuses: Mailpit, login, the `json`, `mails`, `wait_state` and `new_week` helpers, two Teachers with Week Schedules, publish with a short window.
- Produces: evidence for README Review Focus 1–7 and the PR.

**What "pass" means everywhere below:** the close's state is right, each Teacher's mailbox in Mailpit holds exactly the expected messages, and the outbox rows tell the same story. Helper for the rows (database name as in Step 1):

```bash
DB=drivinglessons_outbox_smoke
q() { docker exec drivinglessonsbooking-postgres-1 psql -U app -d $DB -tAc "$1"; }
deliveries() { q "select m.event_name, d.consumer, d.state, d.attempts, coalesce(d.last_error,'') from outbox_deliveries d join outbox_messages m on m.id = d.message_id order by m.occurred_at_utc"; }
sent_rows() { q "select teacher_id, version from sent_teacher_excel_emails order by sent_at_utc"; }
```

Use the Postgres container, user and database naming that the US-44 smoke used; if they differ on your machine, change `q()` only.

- [x] **Step 1: A throwaway database and inbox, and the smoke API**

Follow US-44 task 3 Steps 1–2 with the database `drivinglessons_outbox_smoke` instead of `drivinglessons_us44_smoke`, and start the API with `--Email:Enabled=true` against Mailpit. Add `--Outbox:PollSeconds=2` so the smoke waits less. Confirm in the log that both migrations (`AddOutbox`, `AddSentTeacherExcelEmails`) were applied and that `ProcessOutboxJob` logs no error.

- [x] **Step 2: A normal close goes through the outbox (Review Focus 4)**

In one shell, run US-44 task 3 Step 3 up to and including the first close: two Teachers (`smoke.cohen@example.com`, `smoke.levi@example.com`), one short window, `wait_state $LINK closed`. Then:

```bash
sleep 5
deliveries
sent_rows
mails
```

Expected:
- `deliveries`: exactly one row, `publication-closed.v1 | SendTeacherExcelEmailsConsumer | Succeeded | 1 |`.
- `sent_rows`: two rows, both version `1`.
- `mails`: two messages, subjects `… - גרסה 1`, each with 1 attachment, exactly as before this slice.

If the log shows a Postgres syntax error near `UPDATE` from `OutboxProcessor`, EF wrapped the lease SQL: apply the fallback in task 3 Step 2 and commit it as a fix.

- [x] **Step 3: The mail server is down at the close, then comes back (Review Focus 1, 3)**

```bash
docker stop dl-mailpit
curl -s -o /dev/null -w "reopen: %{http_code}\n" -X POST $API/api/publications/$PUB_ID/reopen -H "$AUTH" -H "Content-Type: application/json" -d "{\"newEndUtc\":\"$(date -u -d '+40 seconds' +%Y-%m-%dT%H:%M:%SZ)\"}"
wait_state $LINK closed
sleep 10
deliveries
curl -s -o /dev/null -w "api still serving: %{http_code}\n" $API/api/teachers/find -H "$AUTH"
```

Expected: the close stands; the second `publication-closed.v1` delivery is `Pending` with `attempts` 1 and `last_error` starting with `TeacherExcelEmailFailedException`; the log has two Error lines "Excel email for teacher [...] failed." and one Warning "Outbox delivery [...] failed on attempt [1] ..."; the API still answers 200.

Now bring the server back and pull the retry forward instead of waiting a minute:

```bash
docker start dl-mailpit
q "update outbox_deliveries set next_attempt_at_utc = now() where state = 'Pending'"
sleep 5
deliveries
sent_rows
mails
```

Expected: that delivery is `Succeeded` with `attempts` 2; `sent_rows` has two more rows with version `2`; Mailpit (cleared or not, count the `גרסה 2` messages) holds exactly **one** `גרסה 2` message per Teacher.

To prove "only the missing Teacher is sent" (Review Focus 3), repeat this step once more with `smoke.levi@example.com` made to fail and Cohen succeeding. The simplest way: stop Mailpit right after Cohen's send appears in the log, or temporarily give Levi an address Mailpit rejects. If neither is practical on your machine, the unit test `Skips_A_Teacher_Already_Sent_For_This_Version` covers it; say so in the PR.

- [x] **Step 4: The app stops after the close commits, before the email (Review Focus 2)**

```bash
curl -s -o /dev/null -w "reopen: %{http_code}\n" -X POST $API/api/publications/$PUB_ID/reopen -H "$AUTH" -H "Content-Type: application/json" -d "{\"newEndUtc\":\"$(date -u -d '+40 seconds' +%Y-%m-%dT%H:%M:%SZ)\"}"
```

Stop the API, then start it again with `--Outbox:PollSeconds=3600` so the outbox job can't run for an hour after the restart. The outbox job runs once at startup, before the window ends, and then not for an hour. Wait until the Publication is closed (by its Quartz close job, re-registered at startup) with `q "select state from publications"`. Expected: a third `publication-closed.v1` delivery is `Pending` with `attempts` 0, and no `גרסה 3` email exists. Stop the API, start it again with `--Outbox:PollSeconds=2`. Expected within seconds: the delivery is `Succeeded`, and each Teacher has exactly one `גרסה 3` email.

- [x] **Step 5: Reopen and close again before the first email went out (Review Focus 5)**

With the API running on `--Outbox:PollSeconds=3600` again: reopen with a 30-second window, wait for `Closed`, reopen again with 30 seconds, wait for `Closed`. Expected: two `Pending` deliveries. Restart with `--Outbox:PollSeconds=2`. Expected: both `Succeeded`; each Teacher has one `גרסה 4` and one `גרסה 5` email; `sent_rows` shows versions 4 and 5 once per Teacher.

- [x] **Step 6: Two instances during a deploy (Review Focus 6)**

Start a second API process on another port against the same database (`--urls http://localhost:5099`, same `--Outbox:PollSeconds=2`). With both running, reopen and close once more. Expected: one delivery, `Succeeded` with `attempts` 1; exactly one `גרסה 6` email per Teacher; only one of the two logs shows the send. Stop the second process.

- [x] **Step 7: A dead delivery (Review Focus 7)**

Stop Mailpit; start the API with `--Outbox:MaxAttempts=2`; reopen and close once more; after the first failure pull the retry forward (`q "update outbox_deliveries set next_attempt_at_utc = now() where state = 'Pending'"`). Expected: the delivery is `Dead` with `attempts` 2; one Error line "Outbox delivery [...] is dead after [2] attempts."; the row stays. Then run the runbook's re-arm command from task 5 for that delivery, start Mailpit, and confirm one `גרסה 7` email per Teacher and the delivery `Succeeded`. Finally, purge: `q "update outbox_deliveries set completed_at_utc = now() - interval '31 days' where state = 'Succeeded'"`, wait for the next hourly purge or restart the API (the purge trigger starts now), and confirm `q "select count(*) from outbox_deliveries where state = 'Succeeded'"` → `0` and the messages of purged deliveries are gone.

- [x] **Step 8: A delivery for a consumer that no longer exists (Review Focus 8)**

Restart the API with `--Outbox:PollSeconds=3600` (the outbox job runs once at startup, then not for an hour), reopen and close once more, and wait for `Closed`. The new delivery is `Pending` with `attempts` 0. Rename its consumer to one that isn't registered, then restart with `--Outbox:PollSeconds=2`:

```bash
q "update outbox_deliveries set consumer = 'RemovedConsumer' where state = 'Pending'"
```

```bash
sleep 5
deliveries
```

Expected: that delivery is `Dead` with `attempts` 1 and `last_error` starting with `IntegrationEventCatalogException`; one Error line "Outbox delivery [...] is dead after [1] attempts."; no Warning about a retry. The Teachers get no email for this version, which is right for a consumer that no longer exists.

- [x] **Step 9: Full check**

```bash
dotnet build
dotnet test tests/DrivingLessons.Domain.Test/DrivingLessons.Domain.Test.csproj
dotnet test tests/DrivingLessons.Application.Test/DrivingLessons.Application.Test.csproj
dotnet ef migrations has-pending-model-changes --project src/DrivingLessons.Infrastructure --startup-project src/DrivingLessons.Presentation.Web
git status --short
```

Expected: build clean, all tests pass, no pending model changes, and `git status` shows nothing but the git-ignored smoke files. No client change in this slice, so the client suites don't need to run; run them anyway if `client/` changed on `main` since the branch was cut.

- [x] **Step 10: Clean up**

Stop the API processes, `docker rm -f dl-mailpit`, drop `drivinglessons_outbox_smoke`, delete `.superpowers/sdd/outbox-smoke/`.

- [ ] **Step 11: Push and open the PR** (only once your human partner has approved pushing)

```bash
git push -u origin notifications-outbox-foundation
```

PR title: `Notifications slice 1: integration events, the outbox, and the teacher email as its first consumer`. Body: the README Goal; what changed for whom (nothing visible; the email now retries and never repeats a version); the Review Focus list with the step that proved each item; anything a smoke step couldn't prove and which test covers it instead; the migrations (`AddOutbox`, `AddSentTeacherExcelEmails`). Link the intent and ADR 0010.

## Smoke Results (2026-11-06)

Run in a cloud container without Docker: local PostgreSQL 16 instead of the compose container, and aiosmtpd (a Maildir sink, with a handler that rejects one address on demand) instead of Mailpit. Mail counts below are per Teacher, from the Maildir. Version numbers run one ahead of the steps from Step 4 on, because Step 3's Focus 3 check added a close.

| Step | Result | Evidence |
|------|--------|----------|
| 1 | PASS | `AddOutbox` and `AddSentTeacherExcelEmails` applied to an empty database; no error from either outbox job |
| 2 | PASS | one `publication-closed.v1` message; delivery `Succeeded`, attempts 1; `sent_teacher_excel_emails` v1 × 2; one `גרסה 1` email each, attachment `… דנה לוי-בן דוד - גרסה 1.xlsx` |
| 3 | PASS | SMTP down: close stands (`40`), delivery `Pending`/1 with `TeacherExcelEmailFailedException`, two "Excel email … failed." errors (`SocketException 111`), one retry warning, API 200; SMTP back and retry pulled forward: `Succeeded`/2, one `גרסה 2` each. Focus 3: Levi rejected with 550 → Cohen v3 sent on attempt 1, Levi only on attempt 2; one `גרסה 3` each |
| 4 | PASS | restarted with `PollSeconds=3600`: the Quartz close job closed it, delivery `Pending`/0, no `גרסה 4`; restarted with `PollSeconds=2`: `Succeeded`/1, one `גרסה 4` each |
| 5 | PASS | two closes with the job idle: two `Pending`/0 deliveries, payloads carry versions 5 and 6; after restart both `Succeeded`/1, one `גרסה 5` and one `גרסה 6` each |
| 6 | PASS | instances on 5080 and 5099, both polling; 5080 ran the close, only 5099's log has the two `sent_teacher_excel_emails` inserts; delivery `Succeeded`/1, one `גרסה 7` each |
| 7 | PASS | `MaxAttempts=2`: `Dead`/2, one "is dead after [2] attempts." error, row kept; runbook re-arm → `Succeeded`/1, one `גרסה 8` each. Purge on restart: 0 `Succeeded` left, the `Dead` row (backdated too) kept |
| 8 | PASS | consumer renamed to `RemovedConsumer`: `Dead`/1 with `IntegrationEventCatalogException`, one "is dead after [1] attempts." error, no retry warning, no email |
| 9 | PASS | build clean (only the known `NU1902`/`NU1903`), Domain 362 and Application 525 tests pass, no pending model changes, `git status` clean; client changed on `main`, so its suites ran too: 721 tests pass, build succeeds |
| 10 | PASS | processes stopped, database dropped, smoke files deleted |

Differences between the text and what the code correctly does:

- Step 7's purge deletes an orphaned message only when its `occurred_at_utc` is also past retention. Backdating `completed_at_utc` alone purges the deliveries but leaves the messages; backdating `occurred_at_utc` too removed them (8 → 1, the one left belongs to the `Dead` delivery). In real data a message is never younger than its delivery's completion, so this is the smoke's shortcut, not a defect.
- A delivery that succeeds after a failure keeps the earlier `last_error` (`Succeed` doesn't clear it). Harmless history; the state is what counts.
- Both outbox jobs run twice within the first ~150 ms after startup, then on schedule. A minimal host with only Quartz 3.18.2 and one `ScheduleJob` (or `AddJob` + `AddTrigger`, with or without `StartNow`) does the same: Quartz schedules each configured trigger twice at startup and the second replaces the first after it was already acquired. Both jobs are `[DisallowConcurrentExecution]` and idempotent, so it costs one extra empty run. `QuartzOptions.Scheduling.IgnoreDuplicates = true` with `OverWriteExistingData = false` stops it in the minimal host if it ever matters.
