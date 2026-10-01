# Task 3 of 3: Verification — real emails in Mailpit, a mail server that is down, a bad config, full check, PR

> Part of [US-44: Versioned Excel Email](README.md). Requires tasks 1–2 committed. Work on branch `44-us-44-versioned-excel-email`, commands from the repo root in bash — **one shell for Steps 3–7** (they share variables).

**Files:**
- No source change.
- Local only, never staged: `.claude\launch.json` (adds an `api-smoke` entry, removed in Step 10); smoke files under the git-ignored `.superpowers\sdd\us-44-smoke\`.

**Interfaces:**
- Consumes: tasks 1–2 — the Hebrew subject `בקשות לשבוע {week} - {name} - גרסה {version}`, the attachment named after it, the error line `Excel email for teacher [{TeacherId}] of publication [{PublicationId}] failed.`, `SmtpEmailSender` enabled by `--Email:Enabled=true`, Development's Mailpit settings (`localhost:1025`, `Security: None`, `noreply@local.dev`, `בית הספר לנהיגה (dev)`), the validation message `Email host is required when email is enabled.`. (On `main`): `POST api/teachers`, `PUT api/teachers/{id}/details`, `POST api/week-schedules`, `GET api/publications/by-week`, `POST api/publications/{id}/publish`, `POST api/publications/{id}/reopen` (`{ newEndUtc }`), `GET api/publications/{id}/excel?teacherId=…`, `GET api/submissions/by-link/{token}` (`state`).
- Produces: a verified slice and a PR closing #44.

Mailpit's API (used below): `GET /api/v1/messages` (newest first; `To`, `Subject`, `Attachments` count, `ID`), `GET /api/v1/search?query=to:<address>`, `GET /api/v1/message/{ID}` (`From`, `Subject`, `HTML`, `Attachments[]` with `PartID`, `FileName`, `ContentType`, `Size`), `GET /api/v1/message/{ID}/part/{PartID}` (the attachment bytes), `DELETE /api/v1/messages`.

- [ ] **Step 1: A throwaway database, a throwaway inbox**

The compose Postgres must be running (`docker ps` shows `drivinglessonsbooking-postgres-1`):

```bash
docker exec drivinglessonsbooking-postgres-1 createdb -U app drivinglessons_us44_smoke
docker run -d --name dl-mailpit -p 1025:1025 -p 8025:8025 axllent/mailpit
```

(If either already exists from an earlier run: `docker exec drivinglessonsbooking-postgres-1 dropdb -U app drivinglessons_us44_smoke` / `docker rm -f dl-mailpit`, then retry. Both only ever hold this smoke's data.)

Add a **local-only** entry to `.claude\launch.json` `configurations` (never stage the file):

```json
{
  "name": "api-smoke",
  "runtimeExecutable": "dotnet",
  "runtimeArgs": [
    "run", "--project", "src/DrivingLessons.Presentation.Web", "--launch-profile", "http", "--",
    "--ConnectionStrings:Default=Host=localhost;Port=5432;Database=drivinglessons_us44_smoke;Username=app;Password=devpassword",
    "--Email:Enabled=true"
  ],
  "port": 5080
}
```

- [ ] **Step 2: Start the smoke API**

Stop any running `api` server (same port), then `preview_start {name:"api-smoke"}`. Startup applies every migration to the empty database and seeds the dev admin. `preview_logs` (`level: "error"`) → nothing. The `Development` environment supplies the Mailpit host, port, security and from address; `--Email:Enabled=true` turns sending on.

- [ ] **Step 3: Two teachers, one short window, the first close**

```bash
API=http://localhost:5080
MAILPIT=http://localhost:8025
SMOKE=.superpowers/sdd/us-44-smoke
mkdir -p "$SMOKE"
json() { node -e "let s='';process.stdin.on('data',d=>s+=d).on('end',()=>{const v=process.argv[1].split('.').reduce((o,k)=>o?.[k],JSON.parse(s));console.log(typeof v==='object'?JSON.stringify(v):v)})" "$1"; }
TOKEN=$(curl -s -X POST $API/api/auth/login -H "Content-Type: application/json" \
  -d '{"email":"admin@local.dev","password":"DevAdmin#2026"}' | json accessToken)
AUTH="Authorization: Bearer $TOKEN"
mails() { curl -s "$MAILPIT/api/v1/messages" | node -e "let s='';process.stdin.on('data',d=>s+=d).on('end',()=>{for(const m of JSON.parse(s).messages.reverse())console.log(m.To.map(t=>t.Address).join(',')+' | '+m.Subject+' | attachments '+m.Attachments)})"; }
wait_state() { for i in $(seq 1 36); do [ "$(curl -s $API/api/submissions/by-link/$1 | json state)" = "$2" ] && break; sleep 5; done; echo "link state: $(curl -s $API/api/submissions/by-link/$1 | json state)"; }
new_week() { curl -s -o /dev/null -w "week schedule: %{http_code}\n" -X POST $API/api/week-schedules -H "$AUTH" \
  -H "Content-Type: application/json" -d "{\"teacherId\":\"$1\",\"weekStart\":\"$2\"}"; }

printf '%s' '{"name":"Smoke Cohen","contactEmail":"smoke.cohen@example.com"}' > "$SMOKE/cohen.json"
printf '%s' '{"name":"דנה לוי/בן דוד","contactEmail":"smoke.levi@example.com"}' > "$SMOKE/levi.json"
COHEN_ID=$(curl -s -X POST $API/api/teachers -H "$AUTH" -H "Content-Type: application/json; charset=utf-8" --data-binary @"$SMOKE/cohen.json" | json id)
LEVI_ID=$(curl -s -X POST $API/api/teachers -H "$AUTH" -H "Content-Type: application/json; charset=utf-8" --data-binary @"$SMOKE/levi.json" | json id)
echo "Levi is stored as: $(curl -s $API/api/teachers/$LEVI_ID -H "$AUTH" | json name)"

WEEK=$(date -u -d 'next sunday' +%F)
new_week $COHEN_ID $WEEK
new_week $LEVI_ID $WEEK
PUB=$(curl -s "$API/api/publications/by-week?week=$WEEK" -H "$AUTH")
PUB_ID=$(json id <<< "$PUB")
LINK=$(json linkToken <<< "$PUB")
curl -s -o /dev/null -w "mailpit cleared: %{http_code}\n" -X DELETE $MAILPIT/api/v1/messages
curl -s -o /dev/null -w "publish: %{http_code}\n" -X POST $API/api/publications/$PUB_ID/publish -H "$AUTH" \
  -H "Content-Type: application/json" \
  -d "{\"startUtc\":\"$(date -u -d '-5 minutes' +%Y-%m-%dT%H:%M:%SZ)\",\"endUtc\":\"$(date -u -d '+40 seconds' +%Y-%m-%dT%H:%M:%SZ)\"}"
wait_state $LINK closed
sleep 3
mails
```

Expected:
- `Levi is stored as: דנה לוי/בן דוד` — if it prints `???`, the name went through a shell argument instead of the UTF-8 file (README decision 10); fix that before going on.
- `week schedule: 201` twice, `mailpit cleared: 200`, `publish: 204`, `link state: closed` (within about a minute — Quartz opens the past-start window, then closes it at the end).
- Exactly two messages, with `<n>` the ISO week number of `$WEEK`:
  ```
  smoke.cohen@example.com | בקשות לשבוע <n> - Smoke Cohen - גרסה 1 | attachments 1
  smoke.levi@example.com | בקשות לשבוע <n> - דנה לוי/בן דוד - גרסה 1 | attachments 1
  ```

- [ ] **Step 4: What the teacher actually receives, then a reopen and a second close (same shell)**

```bash
LEVI_MAIL=$(curl -s "$MAILPIT/api/v1/search?query=to:smoke.levi@example.com" | json messages.0.ID)
curl -s $MAILPIT/api/v1/message/$LEVI_MAIL > "$SMOKE/levi-v1.json"
node -e "const m=require('./$SMOKE/levi-v1.json');console.log('from: '+m.From.Name+' <'+m.From.Address+'>');console.log('html: '+m.HTML);for(const a of m.Attachments)console.log('attachment: '+a.FileName+' | '+a.ContentType)"
PART=$(json Attachments.0.PartID < "$SMOKE/levi-v1.json")
curl -s -o "$SMOKE/levi-v1.xlsx" -w "attachment download: %{http_code}\n" "$MAILPIT/api/v1/message/$LEVI_MAIL/part/$PART"
rm -rf "$SMOKE/levi-v1" && mkdir -p "$SMOKE/levi-v1" && /c/Windows/System32/tar.exe -xf "$SMOKE/levi-v1.xlsx" -C "$SMOKE/levi-v1"
grep -o '<sheet [^>]*' "$SMOKE/levi-v1/xl/workbook.xml" | grep -o 'name="[^"]*"'
curl -s -D - -o /dev/null "$API/api/publications/$PUB_ID/excel?teacherId=$LEVI_ID" -H "$AUTH" | grep -i '^content-disposition'

curl -s -o /dev/null -w "reopen: %{http_code}\n" -X POST $API/api/publications/$PUB_ID/reopen -H "$AUTH" \
  -H "Content-Type: application/json" -d "{\"newEndUtc\":\"$(date -u -d '+40 seconds' +%Y-%m-%dT%H:%M:%SZ)\"}"
sleep 3
wait_state $LINK closed
sleep 3
mails
```

Expected, in order:
1. `from: בית הספר לנהיגה (dev) <noreply@local.dev>`.
2. `html: <div dir="rtl" lang="he"><p>שלום דנה לוי/בן דוד,</p><p>מצורף קובץ הבקשות לשבוע <n>, גרסה 1.</p><p>קובץ עם מספר גרסה גבוה יותר מחליף את כל הקבצים הקודמים של אותו שבוע.</p></div>`.
3. `attachment: בקשות לשבוע <n> - דנה לוי-בן דוד - גרסה 1.xlsx | application/vnd.openxmlformats-officedocument.spreadsheetml.sheet` — the `/` became `-` in the file name only (Review Focus 3).
4. `attachment download: 200`, then `name="סיכום"` and `name="פירוט בקשות"` — the attachment is the real two-sheet workbook.
5. The admin download's `content-disposition` still names `week-<yyyy-MM-dd>-<LEVI_ID>.xlsx` (README decision 3).
6. `reopen: 204`, `link state: closed`, and `mails` now lists four messages — the two above, then:
   ```
   smoke.cohen@example.com | בקשות לשבוע <n> - Smoke Cohen - גרסה 2 | attachments 1
   smoke.levi@example.com | בקשות לשבוע <n> - דנה לוי/בן דוד - גרסה 2 | attachments 1
   ```
   (Review Focus 4: the version increments on every close.)

- [ ] **Step 5: The mail server is down when a window closes (same shell)**

```bash
docker stop dl-mailpit
curl -s -o /dev/null -w "reopen: %{http_code}\n" -X POST $API/api/publications/$PUB_ID/reopen -H "$AUTH" \
  -H "Content-Type: application/json" -d "{\"newEndUtc\":\"$(date -u -d '+30 seconds' +%Y-%m-%dT%H:%M:%SZ)\"}"
sleep 3
wait_state $LINK closed
sleep 5
curl -s -o /dev/null -w "api still serving: %{http_code}\n" $API/api/teachers/find -H "$AUTH"
```

Expected: `reopen: 204`, `link state: closed`, `api still serving: 200`. Then `preview_logs` (`search: "failed."`) on `api-smoke` shows **two** `fail: DrivingLessons.Application.EventHandlers.PublicationClosedHandler` entries — `Excel email for teacher [<COHEN_ID>] of publication [<PUB_ID>] failed.` and the same for `<LEVI_ID>` — each followed by a `System.Net.Sockets.SocketException (10061)` stack through `SmtpEmailSender.SendAsync`. Both teachers were tried although the first failed, and neither entry's message line contains an email address or a teacher name (Review Focus 1).

- [ ] **Step 6: The mail server is down when the app restarts after a window ended (same shell)**

```bash
curl -s -o /dev/null -w "reopen: %{http_code}\n" -X POST $API/api/publications/$PUB_ID/reopen -H "$AUTH" \
  -H "Content-Type: application/json" -d "{\"newEndUtc\":\"$(date -u -d '+20 seconds' +%Y-%m-%dT%H:%M:%SZ)\"}"
```

Expected `reopen: 204`. **Immediately** `preview_stop` the `api-smoke` server (before the 20 seconds pass), then wait until the window has ended in the database:

```bash
until [ "$(docker exec drivinglessonsbooking-postgres-1 psql -U app -d drivinglessons_us44_smoke -tAc \
  'select bool_and(window_end_utc < now()) from publications')" = "t" ]; do sleep 2; done
docker exec drivinglessonsbooking-postgres-1 psql -U app -d drivinglessons_us44_smoke -tAc "select state from publications"
```

Expected: `30` (still Open — nobody closed it while the app was down). Now `preview_start {name:"api-smoke"}` with Mailpit still stopped. Expected:
- The server starts (the preview reports the port listening).
- `preview_logs` (new server id, `level: "all"`) shows, **before** `Now listening on: http://localhost:5080` / `Application started.`, the `UPDATE publications SET state` of the startup reconciliation and two `… PublicationClosedHandler … failed.` entries (Review Focus 2 — before this slice, one exception here stopped the host from starting).
- `docker exec drivinglessonsbooking-postgres-1 psql -U app -d drivinglessons_us44_smoke -tAc "select state from publications; select version from publication_teacher_versions"` → `40`, then `4` and `4` (closes 1–2 sent, 3 in Step 5 and 4 here failed but still counted).

Re-login in the shell (the restart does not invalidate the token, but re-run `TOKEN=…` / `AUTH=…` from Step 3 if a call returns 401).

- [ ] **Step 7: A half-configured production refuses to start**

`preview_stop` the `api-smoke` server, then:

```bash
timeout 120 dotnet run --project src/DrivingLessons.Presentation.Web --launch-profile http -- \
  "--ConnectionStrings:Default=Host=localhost;Port=5432;Database=drivinglessons_us44_smoke;Username=app;Password=devpassword" \
  --Email:Enabled=true --Email:Host= 2>&1 | grep -m1 -o "Email host is required when email is enabled."
```

Expected: `Email host is required when email is enabled.` (from the `OptionsValidationException` `ValidateOnStart` throws), and the process exits without listening (Review Focus 5). This one-shot run is not a dev server — it is meant to fail at startup.

- [ ] **Step 8: The email, seen by your human partner**

`docker start dl-mailpit`, `preview_start {name:"api-smoke"}` once more, and in the Step 3 shell (re-login if needed) send one more version so the inbox holds a fresh message:

```bash
curl -s -o /dev/null -w "reopen: %{http_code}\n" -X POST $API/api/publications/$PUB_ID/reopen -H "$AUTH" \
  -H "Content-Type: application/json" -d "{\"newEndUtc\":\"$(date -u -d '+30 seconds' +%Y-%m-%dT%H:%M:%SZ)\"}"
sleep 3
wait_state $LINK closed
sleep 3
mails | tail -2
LEVI_MAIL=$(curl -s "$MAILPIT/api/v1/search?query=to:smoke.levi@example.com" | json messages.0.ID)
PART=$(curl -s $MAILPIT/api/v1/message/$LEVI_MAIL | json Attachments.0.PartID)
curl -s -o "$SMOKE/levi-v5.xlsx" -w "attachment download: %{http_code}\n" "$MAILPIT/api/v1/message/$LEVI_MAIL/part/$PART"
```

Expected: `reopen: 204`, `link state: closed`, the last two `mails` lines are Cohen's and Levi's `גרסה 5`, `attachment download: 200`.

Then `preview_start {url:"http://localhost:8025"}` to open the Mailpit inbox in the browser pane on Levi's `גרסה 5` message, and `SendUserFile` `$SMOKE/levi-v5.xlsx` with `display: "attach"` and caption: *"The email a teacher receives at close — subject בקשות לשבוע <n> - דנה לוי/בן דוד - גרסה 5, Hebrew right-to-left body, this file attached. The Mailpit inbox is open in the browser pane if you want to look at the message itself."* Ask them to reply with anything that reads wrong (wording, direction, the file name); continue with Step 9 while waiting. If they ask for different wording, change `Subject` / `Body` in `PublicationClosedHandler.cs` with the matching test expectations in `PublicationClosedHandlerTest.cs` first, and commit that separately.

- [ ] **Step 9: Full check**

```bash
dotnet build
dotnet test
dotnet ef migrations has-pending-model-changes --project src\DrivingLessons.Infrastructure --startup-project src\DrivingLessons.Presentation.Web
git diff --stat origin/main -- client
grep -rn "LoggingEmailSender" src tests --include=*.cs
```

Expected: build succeeds with no new warnings (only the existing `NU1903` / `MSTEST0001` / `CS8618`); every test PASSES — the Application suite grows by 29 (`PublicationClosedHandlerTest` 14, `EmailMimeMessageTest` 6, `EmailOptionsTest` 9) to 126, the Domain suite is unchanged; `No changes have been made to the model since the last migration.`; the client diff is empty; the grep prints nothing.

- [ ] **Step 10: Clean up the smoke environment**

1. `preview_stop` the `api-smoke` server and close the Mailpit tab.
2. Remove the `api-smoke` entry from `.claude\launch.json`, restoring the file to its pre-task-3 local state. Never stage it.
3. Drop the throwaway database, the inbox and the smoke files — they only hold this slice's smoke data:
   ```bash
   docker exec drivinglessonsbooking-postgres-1 dropdb -U app drivinglessons_us44_smoke
   docker rm -f dl-mailpit
   rm -rf .superpowers/sdd/us-44-smoke
   ```
4. `git status --short` → only ` M .claude/launch.json` and the two US-45 plan files (their unrelated local edits), nothing else.

- [ ] **Step 11: Push and open the PR** (only once your human partner has approved pushing)

```bash
git push -u origin 44-us-44-versioned-excel-email
gh pr create --base main --title "US-44: Teachers receive the versioned Excel email at every close" --body "Closes #44

## Summary
- At every window close each teacher is emailed their two-sheet Excel over SMTP (MailKit → AWS SES, new ADR 0005). Locally a Mailpit container catches it.
- The email is Hebrew: subject \`בקשות לשבוע N - {teacher} - גרסה K\` (K increments on every close), a right-to-left body, and the attachment named after the subject (characters a file name cannot hold become \`-\`). The admin download keeps its file name.
- One teacher's failure never costs another teacher their file: each generate-and-send is isolated and logged with the teacher and publication ids only. A mail outage at startup no longer stops the app from booting.
- \`EmailOptions\` carries the SMTP settings and fails the boot when email is enabled but half-configured. \`LoggingEmailSender\` → \`SmtpEmailSender\`; disabled, it logs the skip as before.

## Notes
- Requirements §6.4 example and new decision #22 record the Hebrew subject.
- New packages: MailKit 4.18.1 (Infrastructure), Microsoft.Extensions.Logging.Abstractions 10.0.9 (Application). No migration, no client change.
- **Before enabling in production:** verify the SES sending domain, leave the SES sandbox, create SES SMTP credentials, and set the \`EMAIL_*\` variables (see \`.env.example\`).

## Test plan
- [x] \`PublicationClosedHandlerTest\` (14): contact email and own file, Hebrew subject, next version after reopen + close, body lines, attachment name, unsafe file-name characters, one file per teacher, deleted teacher skipped, failed send and failed Excel logged while the next teacher still gets their file, missing publication
- [x] \`EmailMimeMessageTest\` (6): from/to, Hebrew subject, plain-text part, RTL HTML part, markup encoded, Hebrew-named attachment with its bytes
- [x] \`EmailOptionsTest\` (9): disabled needs nothing, complete and credential-less configs valid, missing host / from / password rejected, port range
- [x] Mailpit smoke on a throwaway DB: v1 then v2 emails for two teachers (one with a Hebrew name containing \`/\`), RTL HTML, attachment is the two-sheet workbook, admin download name unchanged
- [x] Mail server down at close: both teachers tried, two error lines without PII, API keeps serving
- [x] Mail server down at restart after a window ended: the app boots, closes the publication, logs the failures
- [x] Email enabled without a host: the app refuses to start with the validation message
- [x] \`dotnet build\`, \`dotnet test\`, no pending EF model changes

🤖 Generated with [Claude Code](https://claude.com/claude-code)"
```

---

## Self-Review (done at planning time)

- **Spec coverage:**
  - US-44 "I receive an email … at my contact email" → task 1 `Sends_The_Teachers_File_To_Their_Contact_Email`; task 2 `Is_From_The_School_To_The_Teacher`; task 3 Step 3 (two real messages in Mailpit).
  - "with the two-sheet Excel attached" → task 1 (the generated file is the attachment, bytes compared), task 2 `Attaches_The_Excel_File_Under_Its_Hebrew_Name`, task 3 Step 4 check 4 (`סיכום`, `פירוט בקשות` inside the received file).
  - "a subject like … v1, incrementing on every subsequent close" → task 1 `Subject_Names_…`, `Subject_Carries_The_Next_Version_After_A_Reopen_And_Close`; task 3 Steps 3–4 (`גרסה 1`, `גרסה 2`) and 6 (versions keep counting). Hebrew wording per the human partner's choice → requirements §6.4 + decision #22 (task 1 Step 6).
  - "my week's Publication reached its window end … the close event fires" → both close paths: Quartz job (task 3 Steps 3–5) and startup reconciliation (Step 6).
  - §4 "teachers have no login — email is their only interface" → the email must arrive even when a neighbor's does not (task 1 isolation tests, task 3 Step 5) and the file must be identifiable after download (attachment name, decision 3).
  - §9 Delivery "automatic … at every window close" + "on-demand download" unchanged → task 3 Step 4 check 5.
  - ADR 0002 (SES) → ADR 0005, `.env.example`, compose (task 2 Steps 10–11); open item 2 lists the SES console steps no code can do.
- **Placeholder scan:** every code step has full file contents or an exact anchored replacement; `<n>`, `<COHEN_ID>`, `<LEVI_ID>`, `<PUB_ID>` in expected output are runtime values the steps print. The only conditional instruction is task 3 Step 8's "if they ask for different wording".
- **Type consistency:** `PublicationClosedHandler`'s five-argument constructor is what task 1's test builds and what DI resolves (`ILogger<T>` is registered by the host). `EmailMessage(string ToEmail, string Subject, string Body, ExcelFile Attachment)` and `ExcelFile(string FileName, byte[] Content, string ContentType)` are unchanged and used identically in tasks 1–2. `EmailMimeMessage.Create(EmailMessage, EmailOptions) : MimeMessage` matches between the test, the class and `SmtpEmailSender`. `EmailOptions` property names match `appsettings*.json`, the compose `Email__*` keys and the tests. The error message text in task 1 is the one task 3 Steps 5–6 search for; the validation message in task 2 is the one Step 7 greps.
- **Checked at planning time** (then reverted, nothing committed): tasks 1–2's code compiled and the 29 tests passed (Application suite 97 → 126, Domain 235 unchanged). The smoke ran against a throwaway database and a Mailpit container: v1 and v2 messages for `Smoke Cohen` and `דנה לוי/בן דוד` arrived with the subjects above, week 41 for 4 October 2026; the HTML part was exactly Step 4 check 2; the attachment was `בקשות לשבוע 41 - דנה לוי-בן דוד - גרסה 2.xlsx`, 7,965 bytes, holding `סיכום` and `פירוט בקשות`. With Mailpit stopped, a close logged the two `failed.` entries (GUIDs only, `SocketException 10061`) and the API kept serving; a restart after the window ended booted, closed the publication (versions 4 / 4) and logged the two failures before `Application started.`. Found and fixed while checking: a `…Test.Email` namespace breaks `Email.Of` in sibling tests (→ `Emails`), `MimeMessage.TextBody` returns CRLF, and Git Bash `curl -d` mangles Hebrew (→ UTF-8 files). **Not run at planning time:** Step 7 (the half-configured boot) — it relies on `ValidateOnStart`, the same mechanism that already guards `Jwt` and `Admin`, and on the validator `EmailOptionsTest` exercises directly.
