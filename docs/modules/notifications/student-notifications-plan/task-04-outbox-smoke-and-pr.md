# Task 4 of 18: Milestone A smoke on a throwaway database, then the PR

> Part of [Student Notifications (stage 1)](README.md), milestone A. Requires tasks 1–3 committed on `notifications-outbox`.

**Files:**
- Nothing, unless a step below finds a defect. A defect gets its own fix commit, with a failing test written first where one can be.
- Scratch only: `<scratchpad>/notifications-a/` (payloads, SQL, logs). Never in the repo.

**Interfaces:**
- Consumes: everything from tasks 1–3, plus the US-44 smoke recipe (compose Postgres, a throwaway database, a throwaway Mailpit `dl-mailpit` on 1025/8025, UTF-8 payload files).
- Produces: PR A, closing the milestone A issue (Open Item 1).

**Why:** the transaction, the held lock, `SKIP LOCKED`, the restart and the retry timing can't be unit-tested in this repo. This task covers Review Focus 1–5 against a real Postgres.

- [ ] **Step 1: Run every suite**

```bash
dotnet build
dotnet test tests/DrivingLessons.Domain.Test/DrivingLessons.Domain.Test.csproj
dotnet test tests/DrivingLessons.Application.Test/DrivingLessons.Application.Test.csproj
dotnet ef migrations has-pending-model-changes --project src/DrivingLessons.Infrastructure --startup-project src/DrivingLessons.Presentation.Web
```

- [ ] **Step 2: Start the app on a throwaway database, with Mailpit**

Create `drivinglessons_notifications_a`, run Mailpit, and start the API in the background with `Email__Enabled=true`, the Development Mailpit settings, and `Logging__LogLevel__DrivingLessons.Infrastructure.Outbox=Debug` plus `Logging__LogLevel__Microsoft.EntityFrameworkCore.Database.Transaction=Debug`. Seed two Teachers (one with a Hebrew name), one Car each, one Student each, a Week Schedule each for next week, and publish that week with a window that opened a minute ago and **ends in 3 minutes**. Use the US-95 seeding helpers: `login-admin.json`, `create()`, `@file` payloads.

- [ ] **Step 3: The aggregate and its outbox row commit together, or not at all (Review Focus 1)**

1. Before the window ends, make every outbox insert fail:
   ```sql
   CREATE FUNCTION fail_outbox() RETURNS trigger AS $$ BEGIN RAISE EXCEPTION 'smoke: outbox insert refused'; END $$ LANGUAGE plpgsql;
   CREATE TRIGGER fail_outbox BEFORE INSERT ON outbox_messages FOR EACH ROW EXECUTE FUNCTION fail_outbox();
   ```
2. Wait for the close. Expected: the log shows `ClosePublicationJob` failing with `smoke: outbox insert refused`, and `SELECT state FROM publications` still shows **30 (Open)**. No row in `publication_teacher_versions`, none in `outbox_messages`, no email in Mailpit.
3. `DROP TRIGGER fail_outbox ON outbox_messages;`, then restart the API. Expected: the reconciliation closes the publication (state 40). One `outbox_messages` row `publication-closed` exists with one `outbox_deliveries` row `TeacherExcelEmailConsumer`, `Processed`, `attempts = 1`. Mailpit holds two emails with the US-44 subjects, `גרסה 1`.
4. In the transaction log of that close, the `UPDATE publications`, the `INSERT INTO publication_teacher_versions` and the `INSERT INTO outbox_messages` / `outbox_deliveries` all sit between one `Beginning transaction` and its `Committed transaction`.

- [ ] **Step 4: SMTP is down at the close and comes back (Review Focus 4)**

1. `docker stop dl-mailpit`. Reopen the publication with an end 2 minutes away (`POST …/reopen`).
2. After the close, expected: the publication is Closed (versions 2) and the reopen and close both returned or ran without error. The delivery is `Pending` with `attempts` rising (30 s, then 1 min), and `last_error` holds the connection error. The Warning logs carry IDs only.
3. `docker start dl-mailpit` after the second failure. Expected, within the next backoff (5 min): exactly **two** new emails, `גרסה 2`, one per Teacher; the delivery is `Processed`; both versions have `emailed_at_utc` set.

- [ ] **Step 5: The app dies in mid-send (Review Focus 2)**

1. Add three more Teachers to the week (five in all). Reopen with an end 1 minute away.
2. Watch Mailpit's API (`curl -s localhost:8025/api/v1/messages | node -pe "…total"`). As soon as the first `גרסה 3` arrives, kill the API (`kill -9` on the `dotnet` PID).
3. Restart the API. Expected: the killed claim transaction rolled back, so the delivery is still `Pending`. It is delivered again. Mailpit ends with **five or six** `גרסה 3` emails (one duplicate at most, for the Teacher whose send had been accepted but not yet recorded), and every version has `emailed_at_utc` set. Record the count in the PR description.

- [ ] **Step 6: Two instances never claim the same delivery (Review Focus 3)**

1. Stop the API. Insert 50 synthetic messages for a publication that doesn't exist, each with one `TeacherExcelEmailConsumer` delivery due now. The consumer returns `Done` at once for a missing publication. Build the SQL with `generate_series`, the payload `{"publicationId":"<random uuid>","weekStart":"2026-10-25","teachers":[],"closedAtUtc":"…"}`.
2. Start two API processes on the same database (`--urls http://localhost:5080` and `http://localhost:5081`, the second with `Outbox__PollIntervalSeconds=1`).
3. Expected: all 50 deliveries end `Processed` with `attempts = 1`. The two logs' Debug lines `Outbox delivery [...] processed.` add up to exactly 50, and no delivery ID appears in both.

- [ ] **Step 7: Clean up and open the PR**

Stop both APIs and `dl-mailpit`, drop `drivinglessons_notifications_a`. Push `notifications-outbox` and open the PR against `main` (title `feat(outbox): integration events, transactional outbox, teacher email retried`). The body summarizes tasks 1–3 and the evidence of Steps 3–6 (the duplicate count of Step 5 included), and links ADR 0010, the ADR 0005 amendment and this plan.
