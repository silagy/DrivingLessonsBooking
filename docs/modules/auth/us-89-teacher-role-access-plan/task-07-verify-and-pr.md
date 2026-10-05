# Task 7 of 7: Verify in the app and open the PR

> Part of [#89: Teacher-role Users Reach Only Week Schedules and Publications](README.md). Requires tasks 1 to 6 committed. Work on branch `89-teacher-role-navigation`.

**Files:**
- Nothing, unless a step below finds a defect. A defect gets its own fix commit before the PR, with a failing test written first where one can be.

**Interfaces:**
- Consumes: everything from tasks 1 to 6.
- Produces: the PR that closes #89.

**Why:** two of #89's acceptance criteria need the running app:
- AC 8: verified manually in the browser: a Teacher can't reach admin URLs or call admin-only endpoints (403).
- AC 9: an Administrator still sees and can do everything.

This task also covers Review Focus 2, 3 and 4 end to end.

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
docker exec drivinglessonsbooking-postgres-1 psql -U app -d postgres -c "DROP DATABASE IF EXISTS drivinglessons_us89_verify"
docker exec drivinglessonsbooking-postgres-1 psql -U app -d postgres -c "CREATE DATABASE drivinglessons_us89_verify"
```

Start the API in the background (Bash tool, `run_in_background: true`):

```bash
ConnectionStrings__Default='Host=localhost;Port=5432;Database=drivinglessons_us89_verify;Username=app;Password=devpassword' dotnet run --project src/DrivingLessons.Presentation.Web --launch-profile http
```

Seed two Teachers and a Teacher-role User from a scratchpad folder. Write Hebrew payloads to files with `node` and post them by relative `@file` paths, because Hebrew on the Windows `curl` command line turns into `?????` (memory note).

```bash
mkdir -p verify-89 && cd verify-89
API=http://localhost:5080
json() { node -pe "JSON.parse(require('fs').readFileSync(0,'utf8')).$1"; }
printf '{"email":"admin@local.dev","password":"DevAdmin#2026"}' > login.json
ADMIN=$(curl -s -X POST $API/api/auth/login -H "Content-Type: application/json" -d @login.json | json accessToken)
node -e "require('fs').writeFileSync('t1.json', JSON.stringify({name:'יעל כרמי',contactEmail:'yael@school.example'}))"
node -e "require('fs').writeFileSync('t2.json', JSON.stringify({name:'אורן ביטון',contactEmail:'oren@school.example'}))"
YAEL_T=$(curl -s -X POST $API/api/teachers -H "Authorization: Bearer $ADMIN" -H "Content-Type: application/json" -d @t1.json | json id)
curl -s -X POST $API/api/teachers -H "Authorization: Bearer $ADMIN" -H "Content-Type: application/json" -d @t2.json; echo
node -e "require('fs').writeFileSync('u1.json', JSON.stringify({name:'יעל כרמי',signInEmail:'yael.user@school.example',role:'teacher',teacherId:process.argv[1],temporaryPassword:'Temporary#2026'}))" "$YAEL_T"
curl -s -X POST $API/api/users -H "Authorization: Bearer $ADMIN" -H "Content-Type: application/json" -d @u1.json; echo
```

Start the client with `preview_start {name: "client"}`; it proxies `/api` to port 5080. Open `http://localhost:4200`.

Screenshots of this app can come back half-rendered (memory note). Before each screenshot, run `await document.fonts.ready` in `javascript_tool` and wait about 1.5 s. Rely on `read_page`, `get_page_text`, `read_network_requests` and `javascript_tool` for the checks themselves.

- [ ] **Step 3: An Administrator prepares Yael's week (AC 9, part 1)**

Sign in as `admin@local.dev` / `DevAdmin#2026` (Hebrew, RTL).

- [ ] The top bar shows seven items in this order: "לוח בקרה", "רכבים ומורים", "תלמידים", "הכנת שבוע", "פרסומים", "היסטוריה", "משתמשים".
- [ ] The user menu header shows `admin@local.dev` (LTR) and a sky-tinted tag "מנהל מערכת".
- [ ] Weekly prep: the Teacher picker is there with both Teachers. Pick "יעל כרמי": the default week is created on the fly (`POST /api/week-schedules` → `201`), the grid shows, and "פרסום שבוע..." is visible next to the "טיוטה" tag.
- [ ] Mark two Slots Unavailable.
- [ ] Publications: the Teacher picker, "פרסום" and the Administrator draft prompt are there. Users loads (`200`).
- [ ] Sign out.

- [ ] **Step 4: A Teacher lands on Weekly prep and can't reach admin screens (AC 5, 6, 8; Review Focus 2 and 3)**

Sign in as `yael.user@school.example` / `Temporary#2026`.

- [ ] The URL becomes `/week-schedules` and **no** toast appears (signing in is not a refusal).
- [ ] The top bar shows exactly three items: "הכנת שבוע", "פרסומים", "היסטוריה". No "לוח בקרה", "רכבים ומורים", "תלמידים" or "משתמשים" element exists in the DOM (`find` for each label returns nothing in the nav).
- [ ] The user menu header shows `yael.user@school.example` (LTR) and an ocean-tinted tag "מורה". "שינוי הסיסמה שלי" and "התנתקות" are still there.
- [ ] Type each of these into the address bar and press Enter, one at a time: `/users`, `/teachers`, `/roster`. Each time the URL ends on `/week-schedules`, and the info toast "אין לך גישה לדף הזה" / "הועברת למערכת השבועית שלך." appears at the top-left. `read_network_requests` shows **no** request to `/api/users`, `/api/teachers` or `/api/roster-imports`, and no chunk for those features.
- [ ] Open `http://localhost:4200/users` with a **hard reload** (a fresh page load, not in-app navigation). The toast shows the Hebrew copy, **not** `access.refusedTitle` (Review Focus 2).
- [ ] Click the logo (or open `/`): the URL becomes `/week-schedules` with **no** toast.

- [ ] **Step 5: Weekly prep as a Teacher (AC 3, AC 7; Review Focus 4)**

- [ ] No Teacher picker. No "פרסום שבוע..." button even though the week is a Draft (the "טיוטה" tag still shows).
- [ ] The grid is Yael's week, with the two Slots the Administrator marked Unavailable. `read_network_requests`: `GET /api/week-schedules/by-teacher-and-week?teacherId=<Yael's id>...` → `200`; **no** `GET /api/teachers/find`.
- [ ] Click an Open Slot: it turns Unavailable (`POST .../mark-unavailable` → `204`). Click it again to restore it.
- [ ] Pick a later week in the Week picker that nobody prepared. The page shows "מנהל המערכת עדיין לא הכין את השבוע הזה.", not a load error. `read_network_requests` shows the `404` and **no** `POST /api/week-schedules`.

- [ ] **Step 6: Publications as a Teacher, and the server's 403 (AC 2, 3, 7, 8)**

- [ ] Publications: no Teacher picker; the draft notice reads "השבוע הוכן אך טרם פורסם. מנהל המערכת יפרסם אותו ויפתח את חלון ההגשות."; no "פרסום" button.
- [ ] Open `/publications?publish=1`: the publish dialog does **not** open.
- [ ] Pick a week with no Publication: the notice "השבוע הזה עדיין לא הוכן." shows without the "להכנת השבוע" link.
- [ ] History loads (`GET /api/publications/history` → `200`).
- [ ] In `javascript_tool`, call admin-only endpoints with the Teacher's own token:

```javascript
const auth = { Authorization: 'Bearer ' + localStorage.getItem('auth_token') };
const json = { ...auth, 'Content-Type': 'application/json' };
({
  teachers: (await fetch('/api/teachers/find', { headers: auth })).status,
  users: (await fetch('/api/users/find', { headers: auth })).status,
  students: (await fetch('/api/students/find', { headers: auth })).status,
  createWeek: (await fetch('/api/week-schedules', { method: 'POST', headers: json, body: '{}' })).status,
  publish: (await fetch('/api/publications/00000000-0000-0000-0000-000000000001/publish', { method: 'POST', headers: json, body: '{}' })).status,
  history: (await fetch('/api/publications/history', { headers: auth })).status,
})
```

Expected: `teachers`, `users`, `students`, `createWeek` and `publish` are `403`; `history` is `200`. The page is still on its screen (a 403 doesn't sign the User out).

- [ ] **Step 7: English (LTR) as a Teacher**

Switch the language to English:
- `dir` is `ltr`; the nav reads "Weekly prep", "Publications", "History".
- The menu tag reads "Teacher".
- Weekly prep's unprepared week reads "The Administrator hasn't prepared this week yet.".
- Open `/users`: the toast at the top-right reads "You don't have access to that page" / "We've taken you to your Week Schedule.".
- The Publications draft notice reads "This week is prepared but not published yet. The Administrator publishes it to open the submission window.".

Switch back to Hebrew and sign out.

- [ ] **Step 8: The Administrator still does everything (AC 9, part 2)**

Sign in as the Administrator again.
- [ ] Publications for "יעל כרמי": publish the Draft week through "פרסום" (any valid future window). The state becomes "פורסם" and the share link shows.
- [ ] `/users`, `/teachers`, `/roster` and `/` all open with no toast.
- [ ] Sign out, sign in as Yael, open Publications: the Published notice and the share link show; there is still no lifecycle button. Sign out.

Stop the API (`TaskStop`) and the preview (`preview_stop`).

- [ ] **Step 9: Compare against the design**

If the `claude_design` MCP is connected, open frames 2a, 2b, 2c, 2d, 2e, 10a, 10b and 10d of `Users and Roles.html` and compare them with screenshots. Write down every visible difference for the PR. These differences are expected:
- The user menu header shows the email and Role tag, but no name and no linked Teacher (README decision 9). The top-bar avatar button shows the email, as since #88.
- 10a: no locked Teacher chip (lock icon + name) in place of the picker: that is #90.
- 10b: the design shows a per-week table of the Teacher's Publications; #89 keeps today's dashboard layout with the controls hidden. A table view, if wanted, belongs to #90 or later.
- 10c ("(me)" in the picker for an Administrator linked to a Teacher): #90.

- [ ] **Step 10: Push and open the PR**

```bash
git push -u origin 89-teacher-role-navigation
gh pr create --repo silagy/DrivingLessonsBooking --base main --head 89-teacher-role-navigation --title "Teacher-role Users reach only Week Schedules and Publications (#89)" --body-file pr-body.md
```

Write `pr-body.md` in the scratchpad first:

```markdown
Closes #89. Part of #82, slice (2). #90 (Teacher data scoping) follows.

## What changed

- **Backend:** two policies, `Administrator` and `TeacherOrAdministrator`. `Administrator` is now the fallback and default policy, so every endpoint is Administrator-only unless it opts in. Eight actions opt in: get Week Schedule, mark Slot Open / Unavailable, the four Publication queries (by week, dashboard / Submissions, history, Excel download) and change my password. Week Schedule creation and publish / extend / reopen stay Administrator-only. `ControllerAuthorizationTest` pins the matrix.
- **Client auth:** `AuthService` parses `role` and `teacher_id`; `Role` moved to `shared\models`.
- **Guards:** `administratorGuard` (canMatch) on Teachers & Cars, Roster and Users sends a Teacher to Weekly prep with the design's info toast (10d); `homeGuard` sends them there silently from `/`.
- **Shell:** navigation by Role via `navigationFor(role)` (2a / 2b); Role tag in the user menu (2c / 2d).
- **Weekly prep / Publications for a Teacher:** no Teacher picker, no create, no publish / extend / reopen; the Teacher's own `teacher_id` is selected; an unprepared week says so instead of auto-creating.

## Not in this PR (by design)

- Server-side scoping to the Teacher's own data, the locked Teacher chip, "(me)" for a linked Administrator: #90.
- Name and linked Teacher name in the user menu header (not in the token).

## Verification

- Backend and client suites pass; no pending model changes.
- Browser (Hebrew RTL and English): <fill in from steps 3-8>.
- Design differences: <fill in from step 9>.

🤖 Generated with [Claude Code](https://claude.com/claude-code)
```

Fill in the two `<fill in ...>` lines with what steps 3-9 actually showed before creating the PR.

Then call the `ccd_pr` `get_status` tool; if it doesn't report the new PR, bind it with `bind_pr`. Read its CI and offer Auto-fix if a check fails.
