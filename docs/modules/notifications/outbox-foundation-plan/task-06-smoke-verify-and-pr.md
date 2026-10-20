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

- [ ] **Step 1: A throwaway database and inbox, and the smoke API**

Follow US-44 task 3 Steps 1–2 with the database `drivinglessons_outbox_smoke` instead of `drivinglessons_us44_smoke`, and start the API with `--Email:Enabled=true` against Mailpit. Add `--Outbox:PollSeconds=2` so the smoke waits less. Confirm in the log that both migrations (`AddOutbox`, `AddSentTeacherExcelEmails`) were applied and that `ProcessOutboxJob` logs no error.

- [ ] **Step 2: A normal close goes through the outbox (Review Focus 4)**

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

- [ ] **Step 3: The mail server is down at the close, then comes back (Review Focus 1, 3)**

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

- [ ] **Step 4: The app stops after the close commits, before the email (Review Focus 2)**

```bash
curl -s -o /dev/null -w "reopen: %{http_code}\n" -X POST $API/api/publications/$PUB_ID/reopen -H "$AUTH" -H "Content-Type: application/json" -d "{\"newEndUtc\":\"$(date -u -d '+40 seconds' +%Y-%m-%dT%H:%M:%SZ)\"}"
```

Stop the API, then start it again with `--Outbox:PollSeconds=3600` so the outbox job can't run for an hour after the restart. The outbox job runs once at startup, before the window ends, and then not for an hour. Wait until the Publication is closed (by its Quartz close job, re-registered at startup) with `q "select state from publications"`. Expected: a third `publication-closed.v1` delivery is `Pending` with `attempts` 0, and no `גרסה 3` email exists. Stop the API, start it again with `--Outbox:PollSeconds=2`. Expected within seconds: the delivery is `Succeeded`, and each Teacher has exactly one `גרסה 3` email.

- [ ] **Step 5: Reopen and close again before the first email went out (Review Focus 5)**

With the API running on `--Outbox:PollSeconds=3600` again: reopen with a 30-second window, wait for `Closed`, reopen again with 30 seconds, wait for `Closed`. Expected: two `Pending` deliveries. Restart with `--Outbox:PollSeconds=2`. Expected: both `Succeeded`; each Teacher has one `גרסה 4` and one `גרסה 5` email; `sent_rows` shows versions 4 and 5 once per Teacher.

- [ ] **Step 6: Two instances during a deploy (Review Focus 6)**

Start a second API process on another port against the same database (`--urls http://localhost:5099`, same `--Outbox:PollSeconds=2`). With both running, reopen and close once more. Expected: one delivery, `Succeeded` with `attempts` 1; exactly one `גרסה 6` email per Teacher; only one of the two logs shows the send. Stop the second process.

- [ ] **Step 7: A dead delivery (Review Focus 7)**

Stop Mailpit; start the API with `--Outbox:MaxAttempts=2`; reopen and close once more; after the first failure pull the retry forward (`q "update outbox_deliveries set next_attempt_at_utc = now() where state = 'Pending'"`). Expected: the delivery is `Dead` with `attempts` 2; one Error line "Outbox delivery [...] is dead after [2] attempts."; the row stays. Then run the runbook's re-arm command from task 5 for that delivery, start Mailpit, and confirm one `גרסה 7` email per Teacher and the delivery `Succeeded`. Finally, purge: `q "update outbox_deliveries set completed_at_utc = now() - interval '31 days' where state = 'Succeeded'"`, wait for the next hourly purge or restart the API (the purge trigger starts now), and confirm `q "select count(*) from outbox_deliveries where state = 'Succeeded'"` → `0` and the messages of purged deliveries are gone.

- [ ] **Step 8: A delivery for a consumer that no longer exists (Review Focus 8)**

Restart the API with `--Outbox:PollSeconds=3600` (the outbox job runs once at startup, then not for an hour), reopen and close once more, and wait for `Closed`. The new delivery is `Pending` with `attempts` 0. Rename its consumer to one that isn't registered, then restart with `--Outbox:PollSeconds=2`:

```bash
q "update outbox_deliveries set consumer = 'RemovedConsumer' where state = 'Pending'"
```

```bash
sleep 5
deliveries
```

Expected: that delivery is `Dead` with `attempts` 1 and `last_error` starting with `IntegrationEventCatalogException`; one Error line "Outbox delivery [...] is dead after [1] attempts."; no Warning about a retry. The Teachers get no email for this version, which is right for a consumer that no longer exists.

- [ ] **Step 9: Full check**

```bash
dotnet build
dotnet test tests/DrivingLessons.Domain.Test/DrivingLessons.Domain.Test.csproj
dotnet test tests/DrivingLessons.Application.Test/DrivingLessons.Application.Test.csproj
dotnet ef migrations has-pending-model-changes --project src/DrivingLessons.Infrastructure --startup-project src/DrivingLessons.Presentation.Web
git status --short
```

Expected: build clean, all tests pass, no pending model changes, and `git status` shows nothing but the git-ignored smoke files. No client change in this slice, so the client suites don't need to run; run them anyway if `client/` changed on `main` since the branch was cut.

- [ ] **Step 10: Clean up**

Stop the API processes, `docker rm -f dl-mailpit`, drop `drivinglessons_outbox_smoke`, delete `.superpowers/sdd/outbox-smoke/`.

- [ ] **Step 11: Push and open the PR** (only once your human partner has approved pushing)

```bash
git push -u origin notifications-outbox-foundation
```

PR title: `Notifications slice 1: integration events, the outbox, and the teacher email as its first consumer`. Body: the README Goal; what changed for whom (nothing visible; the email now retries and never repeats a version); the Review Focus list with the step that proved each item; anything a smoke step couldn't prove and which test covers it instead; the migrations (`AddOutbox`, `AddSentTeacherExcelEmails`). Link the intent and ADR 0010.
