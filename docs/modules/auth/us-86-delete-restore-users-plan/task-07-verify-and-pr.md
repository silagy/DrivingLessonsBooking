# Task 7 of 7: Verification in the app and the PR

> Part of [#86: Delete and Restore Users](README.md). Requires tasks 1 to 6 committed. Work on branch `86-delete-restore-users`.

**Files:**
- Nothing, unless a step below finds a defect. A defect gets its own fix commit before the PR, with a failing test first where one can be written.

**Interfaces:**
- Consumes: everything from tasks 1 to 6.
- Produces: the PR that closes #86.

**Why:** #86 AC 5 (a deleted User is signed out on their next request, verified in the browser), AC 6 (the screen) and AC 8 (Hebrew first, RTL-correct) need the running app. Review Focus 1, 4 and 5.

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

Expected: all green. No pending model changes.

- [ ] **Step 2: Start the app on a throwaway database with Hebrew data**

```bash
docker stop dl-postgres
docker compose up -d postgres
docker exec drivinglessonsbooking-postgres-1 psql -U app -d postgres -c "DROP DATABASE IF EXISTS drivinglessons_us86_verify"
docker exec drivinglessonsbooking-postgres-1 psql -U app -d postgres -c "CREATE DATABASE drivinglessons_us86_verify"
```

Start the API in the background (Bash tool, `run_in_background: true`):

```bash
ConnectionStrings__Default='Host=localhost;Port=5432;Database=drivinglessons_us86_verify;Username=app;Password=devpassword' dotnet run --project src/DrivingLessons.Presentation.Web --launch-profile http
```

Seed from a scratchpad folder. Write Hebrew payloads to files with `printf` and post them with relative `@file` paths (memory note: Hebrew on the Windows `curl` command line turns into `?????`).

```bash
mkdir -p verify-86 && cd verify-86
API=http://localhost:5080
json() { node -pe "JSON.parse(require('fs').readFileSync(0,'utf8')).$1"; }
printf '{"email":"admin@local.dev","password":"DevAdmin#2026"}' > login.json
ADMIN=$(curl -s -X POST $API/api/auth/login -H "Content-Type: application/json" -d @login.json | json accessToken)
printf '%s' '{"name":"אורן ביטון","contactEmail":"oren@school.example"}' > t1.json
OREN_TEACHER=$(curl -s -X POST $API/api/teachers -H "Authorization: Bearer $ADMIN" -H "Content-Type: application/json" -d @t1.json | json id)
printf '{"name":"%s","signInEmail":"oren.user@school.example","role":"teacher","teacherId":"%s","temporaryPassword":"Temporary#2026"}' "אורן ביטון" "$OREN_TEACHER" > u1.json
printf '%s' '{"name":"רונית אברהם","signInEmail":"ronit@school.example","role":"administrator","teacherId":null,"temporaryPassword":"Temporary#2026"}' > u2.json
curl -s -X POST $API/api/users -H "Authorization: Bearer $ADMIN" -H "Content-Type: application/json" -d @u1.json
curl -s -X POST $API/api/users -H "Authorization: Bearer $ADMIN" -H "Content-Type: application/json" -d @u2.json
docker exec drivinglessonsbooking-postgres-1 psql -U app -d drivinglessons_us86_verify -At -c "SELECT name FROM users ORDER BY name"
```

Expected: the last command prints the Hebrew names, not question marks. If `printf` mangles `u1.json`, write it with `node -e "require('fs').writeFileSync('u1.json', JSON.stringify({...}))"` instead.

Start the client with `preview_start {name: "client"}` (it proxies `/api` to port 5080). Open `http://localhost:4200` and sign in as `admin@local.dev` / `DevAdmin#2026`. Screenshots of this app can come back half-rendered: run `await document.fonts.ready` in `javascript_tool` and wait about 1.5 s before each screenshot. Rely on `read_page`, `get_page_text` and `javascript_tool` for the checks.

- [ ] **Step 3: The Users screen in Hebrew (design 3a, 3b)**

- [ ] `document.documentElement.dir` is `rtl`. Three rows: "Administrator" (with the "את/ה" tag), "אורן ביטון" and "רונית אברהם". All are active, and each has a kebab at the inline end (left in RTL).
- [ ] The actions column header is empty on screen but has the text "פעולות" (`read_page`).
- [ ] The kebab on Oren's row opens a popup menu below it, not clipped by the card, with one item, "מחיקה", with a trash icon in red.

- [ ] **Step 4: Delete and the in-dialog refusals (design 8a, 8b)**

- [ ] Choose "מחיקה" on Oren's row. The dialog header is "למחוק את אורן ביטון?". The who-card shows initials, the name, `oren.user@school.example` reading left to right, and the Role tag "מורה". The three lines read "הם יוצאו מהמערכת מיד." / "רשומת המורה..." / "אפשר לבטל בכל עת עם "שחזור"."; the sign-out and replay icons are mirrored (`getComputedStyle(...).transform` is not `none`). The buttons are "ביטול" and "מחיקה" (danger).
- [ ] Choose "ביטול": the dialog closes and nothing changes.
- [ ] Open Delete on your own "Administrator" row and confirm. The dialog **stays open** with the message "הפעולה לא בוצעה" / "אי אפשר למחוק את עצמך.". `read_network_requests` shows `DELETE /api/users/{id}` → `409`.
- [ ] Close it and open Delete on the same row again: **no** refusal message (Review Focus 4).
- [ ] Delete Oren. The dialog closes, the toast "המשתמש נמחק" appears, and Oren's row moves to the end, muted, with the status "נמחק" and a "שחזור" text button instead of the kebab. Go to Cars & teachers: the Teacher "אורן ביטון" is still listed (AC 3).

- [ ] **Step 5: Restore (design 3f, 3g)**

- [ ] Choose "שחזור" on Oren's row. A success toast "המשתמש שוחזר" with the detail "אורן ביטון יכול/ה להיכנס שוב." appears. The row is active again with a kebab.
- [ ] 3g, refused: delete Oren again, then in a second request restore him outside the UI (`curl -X POST $API/api/users/<id>/restore -H "Authorization: Bearer $ADMIN"`). Then click "שחזור" on the stale row: an error toast "המשתמש הזה כבר פעיל." appears and the list stays.

- [ ] **Step 6: Immediate lockout in the browser (AC 5, Review Focus 1)**

1. Sign Oren in on the API: `printf '{"email":"oren.user@school.example","password":"Temporary#2026"}' > oren.json`, then `OREN=$(curl -s -X POST $API/api/auth/login -H "Content-Type: application/json" -d @oren.json | json accessToken)`.
2. In the browser, run `localStorage.setItem('auth_token', '<OREN token>')` in `javascript_tool` and reload. The app now runs as Oren; open a screen that loads data (for example Weekly prep).
3. From bash, delete Oren as the Administrator: `curl -s -o /dev/null -w "%{http_code}" -X DELETE $API/api/users/<oren id> -H "Authorization: Bearer $ADMIN"` → `204`.
4. In the browser, click anything that makes an API call (switch week, open another screen). Expected: the request returns `401` (`read_network_requests`) and the app lands on `/login` straight away. Long before the token's 12-hour expiry.
5. Sign back in as `admin@local.dev` for the remaining steps.

- [ ] **Step 7: English (LTR, design 3h)**

Switch the language to English. Check:
- `dir` is `ltr`.
- The kebab is at the right end.
- The "You" tag reads "You".
- The menu item reads "Delete".
- The dialog reads "Delete Oren Biton?" (or the Hebrew name, isolated). Its lines are "They're signed out immediately." etc., and its buttons "Cancel" / "Delete".
- A Restore toast reads "User restored" / "... can sign in again.".

- [ ] **Step 8: Compare against the design**

If the `claude_design` MCP is connected, open `Users and Roles.html` frames 3a, 3b, 3f, 3g and 8a-8d and compare them with screenshots. Write down every visible difference for the PR. Expected differences:
- The danger color comes from the app theme, not the mock's plum (README decision 10).
- The row menu holds only Delete until #87.

- [ ] **Step 9: Link the plan from the design handoff**

In `docs\modules\auth\users-and-roles-design.md`, row "(3) Users admin screen", append: `Delete and restore Users, [#86](https://github.com/silagy/DrivingLessonsBooking/issues/86): [plan](us-86-delete-restore-users-plan/README.md)`. Skip this if the plan commit already did it.

Stop the API and the client. Drop the verify database:

```bash
docker exec drivinglessonsbooking-postgres-1 psql -U app -d postgres -c "DROP DATABASE IF EXISTS drivinglessons_us86_verify"
docker exec drivinglessonsbooking-postgres-1 psql -U app -d postgres -c "DROP DATABASE IF EXISTS drivinglessons_us86_smoke"
```

- [ ] **Step 10: Push and open the PR**

```bash
git push -u origin 86-delete-restore-users
gh pr create --base main --title "Delete and restore Users with immediate lockout (#86)" --body-file pr-body.md
```

Write `pr-body.md` in the scratchpad (not in the repo). It must contain:
- `Closes #86` and `Part of #82`.
- A summary: `User.Restore`, Delete / Restore interactors with the self and last-active-Administrator guards, `api/users` Delete / Restore endpoints, the per-request lockout through `JwtBearerEvents`, and the Users screen row actions, "You" tag, Delete dialog and Restore.
- **Flagged for review:**
  - Decision 4: restoring a User whose linked Teacher is deleted is refused. This is not in #86's text.
  - Decision 3: the last-active-Administrator guard can't be reached from the UI today.
  - Decision 5: the per-request lookup cost.
  - The base64url fix in `AuthService`.
- The verification done (suites, smoke, browser in Hebrew and English, the lockout) and the design differences from step 8.
- The PR attribution line from the session's instructions.
