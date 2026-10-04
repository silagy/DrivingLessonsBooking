# Task 6 of 6: Verify in the app and open the PR

> Part of [#87: Edit a User's Details and Role, and Set a Temporary Password](README.md). Requires tasks 1 to 5 committed. Work on branch `87-edit-users`.

**Files:**
- Nothing, unless a step below finds a defect. A defect gets its own fix commit before the PR, with a failing test written first where one can be.

**Interfaces:**
- Consumes: everything from tasks 1 to 5.
- Produces: the PR that closes #87.

**Why:** two of #87's acceptance criteria need the running app:
- AC 6: a demoted User loses Administrator access on their next request, verified manually.
- AC 7 and 8: the screen, translated, Hebrew first and RTL-correct.

This task also covers Review Focus 1, 4 and 5.

- [ ] **Step 1: Run every suite**

```bash
dotnet build
dotnet test tests\DrivingLessons.Domain.Test\DrivingLessons.Domain.Test.csproj
dotnet test tests\DrivingLessons.Application.Test\DrivingLessons.Application.Test.csproj
dotnet ef migrations has-pending-model-changes --project src\DrivingLessons.Infrastructure --startup-project src\DrivingLessons.Presentation.Web
```

From `client\` (PowerShell):

```powershell
& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js test --watch=false
& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js build --project client
```

Expected: every suite passes, and there are no pending model changes.

- [ ] **Step 2: Start the app on a throwaway database with Hebrew data**

```bash
docker stop dl-postgres
docker compose up -d postgres
docker exec drivinglessonsbooking-postgres-1 psql -U app -d postgres -c "DROP DATABASE IF EXISTS drivinglessons_us87_verify"
docker exec drivinglessonsbooking-postgres-1 psql -U app -d postgres -c "CREATE DATABASE drivinglessons_us87_verify"
```

Start the API in the background (Bash tool, `run_in_background: true`):

```bash
ConnectionStrings__Default='Host=localhost;Port=5432;Database=drivinglessons_us87_verify;Username=app;Password=devpassword' dotnet run --project src/DrivingLessons.Presentation.Web --launch-profile http
```

Seed the data from a scratchpad folder. Write Hebrew payloads to files with `node` and post them by relative `@file` paths, because Hebrew on the Windows `curl` command line turns into `?????` (memory note).

```bash
mkdir -p verify-87 && cd verify-87
API=http://localhost:5080
json() { node -pe "JSON.parse(require('fs').readFileSync(0,'utf8')).$1"; }
printf '{"email":"admin@local.dev","password":"DevAdmin#2026"}' > login.json
ADMIN=$(curl -s -X POST $API/api/auth/login -H "Content-Type: application/json" -d @login.json | json accessToken)
node -e "require('fs').writeFileSync('t1.json', JSON.stringify({name:'רונית אברהם',contactEmail:'ronit@school.example'}))"
node -e "require('fs').writeFileSync('t2.json', JSON.stringify({name:'יעל כרמי',contactEmail:'yael@school.example'}))"
RONIT_T=$(curl -s -X POST $API/api/teachers -H "Authorization: Bearer $ADMIN" -H "Content-Type: application/json" -d @t1.json | json id)
YAEL_T=$(curl -s -X POST $API/api/teachers -H "Authorization: Bearer $ADMIN" -H "Content-Type: application/json" -d @t2.json | json id)
node -e "require('fs').writeFileSync('u1.json', JSON.stringify({name:'רונית אברהם',signInEmail:'ronit.user@school.example',role:'administrator',teacherId:process.argv[1],temporaryPassword:'Temporary#2026'}))" "$RONIT_T"
node -e "require('fs').writeFileSync('u2.json', JSON.stringify({name:'עמית רוזן',signInEmail:'amit@school.example',role:'administrator',teacherId:null,temporaryPassword:'Temporary#2026'}))"
node -e "require('fs').writeFileSync('u3.json', JSON.stringify({name:'יעל כרמי',signInEmail:'yael.user@school.example',role:'teacher',teacherId:process.argv[1],temporaryPassword:'Temporary#2026'}))" "$YAEL_T"
for f in u1 u2 u3; do curl -s -X POST $API/api/users -H "Authorization: Bearer $ADMIN" -H "Content-Type: application/json" -d @$f.json; echo; done
docker exec drivinglessonsbooking-postgres-1 psql -U app -d drivinglessons_us87_verify -At -c "SELECT name FROM users ORDER BY name"
```

Expected: the last command prints the Hebrew names, not question marks.

Start the client with `preview_start {name: "client"}`; it proxies `/api` to port 5080. Open `http://localhost:4200` and sign in as `admin@local.dev` / `DevAdmin#2026`.

Screenshots of this app can come back half-rendered (memory note). Before each screenshot, run `await document.fonts.ready` in `javascript_tool` and wait about 1.5 s. Rely on `read_page`, `get_page_text` and `javascript_tool` for the checks themselves.

- [ ] **Step 3: Check the row menu in Hebrew (design 3b)**

- [ ] `document.documentElement.dir` is `rtl`.
- [ ] Opening the kebab on Yael's row shows, in order:
  - "עריכת פרטים" (pencil)
  - "שינוי תפקיד" (shield)
  - "הגדרת סיסמה זמנית" (key)
  - a divider
  - "מחיקה" (trash, in the danger color)
- [ ] The popup isn't clipped by the card, and it opens toward the inline end.

- [ ] **Step 4: Edit details (design 5a, 5b)**

- [ ] On Yael's row, choose "עריכת פרטים". The header reads "עריכת פרטים". Name holds "יעל כרמי". Email holds `yael.user@school.example`, reads left to right, and has `dir="ltr"`. A locked row follows: "מורה מקושר", then a lock icon, "יעל כרמי" and "· אי אפשר לשנות".
- [ ] Change only the name to "יעל כרמי-לוי" and save. The dialog closes, the toast "הפרטים נשמרו" appears, and the row shows the new name. The email wasn't refused (Review Focus 1).
- [ ] Open it again and set the email to `AMIT@school.example`, then save. The dialog **stays open**, and its first item is the error "הפעולה לא בוצעה" / "האימייל הזה כבר משמש משתמש אחר.". `read_network_requests` shows `PUT /api/users/{id}/details` with status `409`.
- [ ] Close the dialog and reopen it: **no** refusal shows (Review Focus 5).
- [ ] On Amit's row, Edit details shows **no** locked Teacher row.

- [ ] **Step 5: Change Role (design 6a-6f)**

- [ ] **6a:** Change Role on Yael. The who-card shows Yael with the "מורה" tag. In "תפקיד חדש", "מנהל מערכת" is selected and "מורה (נוכחי)" is disabled. The info message reads "הקישור למורה יעל כרמי נשמר. מעכשיו תהיה גישה לכל המסכים.", followed by the effect line with an info icon. The buttons are "ביטול" and "שינוי תפקיד". Don't submit yet.
- [ ] **6b:** Change Role on Ronit. "מורה" is preselected, and the demote note names "רונית אברהם".
- [ ] **6c:** Change Role on Amit, then submit. The dialog stays open with "אי אפשר להעביר לתפקיד מורה משתמש שלא מקושר למורה.", and the info note is hidden while the refusal shows.
- [ ] **6d:** Change Role on your own row ("Administrator" with the "את/ה" tag), then submit. The dialog stays open with "אי אפשר לשנות את התפקיד של עצמך.".
- [ ] **6f:** Open Change Role on Yael and leave the dialog open. From bash, promote her outside the UI with `curl -X PUT $API/api/users/<yael id>/role -H "Authorization: Bearer $ADMIN" -H "Content-Type: application/json" -d '{"role":"administrator"}'`. Then submit "מנהל מערכת" in the open dialog. It stays open with "למשתמש כבר יש את התפקיד הזה. רעננו את הדף.".
- [ ] Close the dialog. Change Ronit to "מורה" and submit. The dialog closes, the toast "התפקיד שונה" appears, and Ronit's row shows the "מורה" tag while keeping "רונית אברהם" as her linked Teacher (AC 5).
- [ ] 6e (last active Administrator) can't be reached from the UI, because the caller is always another active Administrator. Task 2's interactor test covers it. Note this in the PR.

- [ ] **Step 6: A demoted User loses access on their next request (AC 6, Review Focus 4)**

1. From bash, change Ronit back to Administrator: `printf '{"role":"administrator"}' > to-admin.json; curl -s -o /dev/null -w "%{http_code}\n" -X PUT $API/api/users/<ronit id>/role -H "Authorization: Bearer $ADMIN" -H "Content-Type: application/json" -d @to-admin.json` → `204`.
2. Sign Ronit in on the API: `printf '{"email":"ronit.user@school.example","password":"Temporary#2026"}' > ronit.json; RONIT=$(curl -s -X POST $API/api/auth/login -H "Content-Type: application/json" -d @ronit.json | json accessToken)`.
3. In the browser, run `localStorage.setItem('auth_token', '<RONIT token>')` in `javascript_tool` and reload. The app now runs as Ronit. Open the Users screen; it loads (`200`).
4. From bash, demote her as the seeded Administrator: `printf '{"role":"teacher"}' > to-teacher.json; curl -s -o /dev/null -w "%{http_code}\n" -X PUT $API/api/users/<ronit id>/role -H "Authorization: Bearer $ADMIN" -H "Content-Type: application/json" -d @to-teacher.json` → `204`.
5. In the browser, click "ניסיון חוזר" (if shown), or switch screens and come back to Users. The next request returns `401` (`read_network_requests`), and the app lands on `/login` straight away.
6. Sign back in as `admin@local.dev`.

- [ ] **Step 7: Set Temporary Password (design 7a)**

- [ ] On Amit's row, choose "הגדרת סיסמה זמנית". The header reads "הגדרת סיסמה זמנית". The dialog shows the who-card for Amit and an empty "סיסמה זמנית" field (`dir="ltr"`) whose eye icon reveals the text. Next comes the info message "כל החיבורים הפעילים שלהם יסתיימו מיד. מסרו להם את הסיסמה החדשה בעצמכם.", and the buttons "ביטול" and "הגדרת סיסמה". "הגדרת סיסמה" stays disabled while the field is empty.
- [ ] Enter `Fresh#2027` and submit. The dialog closes and the toast "הסיסמה הזמנית הוגדרה" appears.
- [ ] From bash: `printf '{"email":"amit@school.example","password":"Fresh#2027"}' > amit.json; curl -s -o /dev/null -w "%{http_code}\n" -X POST $API/api/auth/login -H "Content-Type: application/json" -d @amit.json` → `200`.

- [ ] **Step 8: Check the English (LTR) version**

Switch the language to English, then check:
- `dir` is `ltr`, and the kebab sits at the right end.
- The menu reads "Edit details", "Change Role", "Set Temporary Password", then "Delete".
- Edit details shows "Linked Teacher", the lock icon and "· can't be changed".
- Change Role shows "New Role", "Teacher (current)" (or "Administrator (current)"), the promote or demote note with the Hebrew name isolated, "The change takes effect on their next action, and they'll need to sign in again." and the buttons "Cancel" and "Change Role".
- Set Temporary Password shows "Their current sessions end immediately. Tell them the new password yourself." and "Set password".
- The toasts read "Details saved", "Role changed" and "Temporary Password set".

- [ ] **Step 9: Regression check on Delete and Restore (#86)**

- [ ] Delete still opens its dialog with the who-card, which now comes from the shared component and looks identical: initials, name, LTR email and Role tag. Deleting yourself still shows the in-dialog refusal, and reopening the dialog shows it clean.
- [ ] Add User's Role switch still looks like a pill after `.role-switch` moved to `dialog-form.scss`.

- [ ] **Step 10: Compare against the design**

If the `claude_design` MCP is connected, open frames 3b, 5a, 5b, 6a-6f and 7a of `Users and Roles.html` and compare them with screenshots. Write down every visible difference for the PR. These differences are expected:
- The email field isn't outlined in red on a 409 (README decision 10).
- The danger color and button casing come from the app theme (README decision 12).
- The three success toasts are new (README decision 7).

Stop the API and the client. Drop the throwaway databases:

```bash
docker exec drivinglessonsbooking-postgres-1 psql -U app -d postgres -c "DROP DATABASE IF EXISTS drivinglessons_us87_verify"
docker exec drivinglessonsbooking-postgres-1 psql -U app -d postgres -c "DROP DATABASE IF EXISTS drivinglessons_us87_smoke"
```

- [ ] **Step 11: Push and open the PR**

```bash
git push -u origin 87-edit-users
gh pr create --base main --title "Edit a User's details and Role, and set a Temporary Password (#87)" --body-file pr-body.md
```

Write `pr-body.md` in the scratchpad, not in the repo. It must contain:
- `Closes #87` and `Part of #82`.
- A summary:
  - `User.ChangeDetails`, `ChangeRole` and `SetTemporaryPassword`, with their events and stamp rotation
  - the three interactors, with the self and last-active-Administrator guards
  - `PUT api/users/{id}/details`, `/role` and `/temporary-password`
  - the Users row menu and the three dialogs, with refusals shown in-dialog
  - the shared who-card
- **Flagged for review:**
  - Decision 2: the Teacher-link rule's copy was reworded for both Add User and Change Role.
  - Decision 4: an Administrator may set their own Temporary Password, and is then signed out on their next request.
  - 6e (last active Administrator) can't be reached from the UI; it is covered by a test.
  - Decision 10: no field-level highlight on a 409.
- The verification done (suites, API smoke, the browser in Hebrew and English, the demote lockout) and the design differences from step 10.
- The PR attribution line from the session's instructions.
