# Task 8 of 8: Verify in the app and open the PR

> Part of [#90: Teacher-role Users See and Change Only Their Own Teacher's Data](README.md). Requires tasks 1 to 7 committed. Work on branch `90-teacher-data-scoping`.

**Files:**
- Nothing, unless a step below finds a defect. A defect gets its own fix commit before the PR, with a failing test written first where one can be.

**Interfaces:**
- Consumes: everything from tasks 1 to 7 (`ICurrentUser.Role` / `TeacherId`, `CurrentUserExtension.MayReach`, the scoped interactors, `FindHistoryAsync(Guid? teacherId)`, `GET api/me`, `GetUserResponse` (`coresigned-in-userget-user.response.ts`), `SignedInUserStore`, `LockedFieldComponent`, `TeacherOption.isMe`, `weekSchedules.me`, `publications.me`, `publications.dashboard.onlyYours`).
- Produces: the PR that closes #90.

**Why:** some of #90's acceptance criteria need the running app:
- A Teacher sees only their own Week Schedule, Publications, Submissions, share link, Excel download and History, and marks only their own Slots.
- A crafted URL to another Teacher's data is refused as not-found (404), from the browser with the Teacher's real token.
- An Administrator linked to a Teacher defaults to their own Teacher, shown "(me)", and can still switch to any Teacher.
- The locked Teacher chip (10a), "(me)" (10c) and the Teacher subtitle (10b) read correctly in Hebrew (RTL) and English.

This task also covers Review Focus 1, 2, 4 and 5 end to end with three personas: a Teacher-role User linked to Teacher A, an Administrator linked to Teacher B and the unlinked seeded Administrator. Review Focus 3 (a Teacher token without `teacher_id`) can't be produced by the app, since a Teacher-role User always has a linked Teacher; tasks 1 and 4 cover it.

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

Expected: every suite passes, and there are no pending model changes (this story adds no migration).

- [ ] **Step 2: Start the app on a throwaway database with Hebrew data**

Use the compose Postgres, not `dl-postgres` (its migration history is stale, memory note):

```bash
docker stop dl-postgres
docker compose up -d postgres
docker exec drivinglessonsbooking-postgres-1 psql -U app -d postgres -c "DROP DATABASE IF EXISTS drivinglessons_us90_verify"
docker exec drivinglessonsbooking-postgres-1 psql -U app -d postgres -c "CREATE DATABASE drivinglessons_us90_verify"
```

Start the API in the background (Bash tool, `run_in_background: true`). It migrates the database and seeds the first Administrator `admin@local.dev` / `DevAdmin#2026` on start:

```bash
ConnectionStrings__Default='Host=localhost;Port=5432;Database=drivinglessons_us90_verify;Username=app;Password=devpassword' dotnet run --project src/DrivingLessons.Presentation.Web --launch-profile http
```

Seed from a folder in the scratchpad. Two Teachers: A = "יעל כרמי" and B = "אורן ביטון". A Week Schedule for each Teacher for this week and next week (Weekly prep opens next week by default, Publications this week). This week's Publication is published with a window that opens in one minute, so the scheduler opens it and the share link, Submissions stats and Excel download show. A Teacher-role User linked to Teacher A, and an Administrator User linked to Teacher B (a Teacher can be linked to only one User, `TeacherAlreadyLinkedToUserException`, so the linked Administrator takes B).

Hebrew on the Windows `curl` command line turns into `?????` (memory note), so every Hebrew payload is written to a file by `node` and posted by a relative `@file` path.

```bash
mkdir -p verify-90 && cd verify-90
API=http://localhost:5080
json() { node -pe "JSON.parse(require('fs').readFileSync(0,'utf8')).$1"; }
THIS_WEEK=$(node -pe "const d=new Date();d.setDate(d.getDate()-d.getDay());d.toLocaleDateString('sv-SE')")
NEXT_WEEK=$(node -pe "const d=new Date();d.setDate(d.getDate()-d.getDay()+7);d.toLocaleDateString('sv-SE')")

printf '{"email":"admin@local.dev","password":"DevAdmin#2026"}' > login-admin.json
ADMIN=$(curl -s -X POST $API/api/auth/login -H "Content-Type: application/json" -d @login-admin.json | json accessToken)

node -e "require('fs').writeFileSync('teacher-a.json', JSON.stringify({name:'יעל כרמי',contactEmail:'yael@school.example'}))"
node -e "require('fs').writeFileSync('teacher-b.json', JSON.stringify({name:'אורן ביטון',contactEmail:'oren@school.example'}))"
TEACHER_A=$(curl -s -X POST $API/api/teachers -H "Authorization: Bearer $ADMIN" -H "Content-Type: application/json" -d @teacher-a.json | json id)
TEACHER_B=$(curl -s -X POST $API/api/teachers -H "Authorization: Bearer $ADMIN" -H "Content-Type: application/json" -d @teacher-b.json | json id)

for TEACHER in $TEACHER_A $TEACHER_B; do
  for WEEK in $THIS_WEEK $NEXT_WEEK; do
    printf '{"teacherId":"%s","weekStart":"%s"}' "$TEACHER" "$WEEK" > week.json
    curl -s -X POST $API/api/week-schedules -H "Authorization: Bearer $ADMIN" -H "Content-Type: application/json" -d @week.json; echo
  done
done

curl -s "$API/api/week-schedules/by-teacher-and-week?teacherId=$TEACHER_B&week=$NEXT_WEEK" -H "Authorization: Bearer $ADMIN" > week-b.json
WEEK_SCHEDULE_B=$(json id < week-b.json)
SLOT_B=$(json "slots[0].id" < week-b.json)

PUBLICATION=$(curl -s "$API/api/publications/by-week?week=$THIS_WEEK" -H "Authorization: Bearer $ADMIN" | json id)
node -e "const now=Date.now();require('fs').writeFileSync('publish.json', JSON.stringify({startUtc:new Date(now+60000).toISOString(),endUtc:new Date(now+3*86400000).toISOString()}))"
curl -s -o /dev/null -w "publish %{http_code}\n" -X POST $API/api/publications/$PUBLICATION/publish -H "Authorization: Bearer $ADMIN" -H "Content-Type: application/json" -d @publish.json

node -e "require('fs').writeFileSync('user-a.json', JSON.stringify({name:'יעל כרמי',signInEmail:'yael.user@school.example',role:'teacher',teacherId:process.argv[1],temporaryPassword:'Temporary#2026'}))" "$TEACHER_A"
USER_A=$(curl -s -X POST $API/api/users -H "Authorization: Bearer $ADMIN" -H "Content-Type: application/json" -d @user-a.json | json id)

node -e "require('fs').writeFileSync('user-b.json', JSON.stringify({name:'אורן ביטון',signInEmail:'oren.user@school.example',role:'administrator',teacherId:process.argv[1],temporaryPassword:'Temporary#2026'}))" "$TEACHER_B"
curl -s -X POST $API/api/users -H "Authorization: Bearer $ADMIN" -H "Content-Type: application/json" -d @user-b.json; echo

printf 'TEACHER_A=%s\nTEACHER_B=%s\nWEEK_SCHEDULE_B=%s\nSLOT_B=%s\nPUBLICATION=%s\nUSER_A=%s\nTHIS_WEEK=%s\nNEXT_WEEK=%s\n' \
  "$TEACHER_A" "$TEACHER_B" "$WEEK_SCHEDULE_B" "$SLOT_B" "$PUBLICATION" "$USER_A" "$THIS_WEEK" "$NEXT_WEEK" | tee ids.txt
```

Expected: four `{"id":...}` lines for the Week Schedules, `publish 204`, and `ids.txt` with no empty value. About a minute later this week's Publication is Open (`curl -s "$API/api/publications/by-week?week=$THIS_WEEK" -H "Authorization: Bearer $ADMIN" | json state` prints `open`). The values in `ids.txt` are pasted into the `javascript_tool` snippet in step 4.

Start the client with `preview_start {name: "client"}`; it proxies `/api` to port 5080. Open `http://localhost:4200`.

Screenshots of this app can hang or come back half-rendered (memory note). Before each screenshot, run `await document.fonts.ready` in `javascript_tool` and wait about 1.5 s. If a screenshot still hangs, verify through `read_page`, `get_page_text`, `read_network_requests` and `getComputedStyle` in `javascript_tool` instead; the checks below never depend on a screenshot alone.

- [ ] **Step 3: Weekly prep as a Teacher, Hebrew (10a; AC: own Week Schedule, own Slots)**

Sign in as `yael.user@school.example` / `Temporary#2026` (Hebrew, RTL).

- [ ] The URL becomes `/week-schedules`. `read_network_requests`: `GET /api/me` → `200`; `GET /api/week-schedules/by-teacher-and-week?teacherId=<TEACHER_A>&week=<NEXT_WEEK>` → `200`; **no** `GET /api/teachers/find`.
- [ ] No Teacher picker: no element reads "בחרו מורה". In its place, the locked chip: the label "מורה" above, then a chip with a lock icon and "יעל כרמי". Check it in `javascript_tool`:

```javascript
await document.fonts.ready;
const chip = document.querySelector('app-locked-field .locked-field');
const style = chip ? getComputedStyle(chip) : null;
({
  label: document.querySelector('app-locked-field .field__label')?.textContent.trim(),
  value: chip?.querySelector('.locked-field__value')?.textContent.trim(),
  lock: !!chip?.querySelector('.pi-lock'),
  height: chip?.getBoundingClientRect().height,
  background: style?.backgroundColor,
  border: style?.borderColor,
  dir: document.documentElement.dir,
  rawKeys: document.body.innerText.match(/\b(weekSchedules|publications|general|users|access)\.[A-Za-z.]+/g),
})
```

Expected: `label` "מורה", `value` "יעל כרמי", `lock` true, `height` 42 or more, a grey surface and border from the theme, `dir` "rtl", `rawKeys` null.
- [ ] RTL: the lock icon sits at the chip's inline start (its right edge in Hebrew), and the chip lines up with the Week picker the same way the picker did for an Administrator. No "יצירת מערכת שבועית" and no "פרסום שבוע..." (unchanged from #89).
- [ ] Click an Open Slot: it turns Unavailable (`POST /api/week-schedules/<A's id>/slots/<slot id>/mark-unavailable` → `204`). Click it again to restore it (`mark-available` → `204`).

- [ ] **Step 4: Another Teacher's data by crafted request is not-found (Review Focus 1 and 2)**

Still signed in as Yael, paste the values from `ids.txt` and run in `javascript_tool`:

```javascript
const ids = {
  teacherA: '<TEACHER_A>',
  teacherB: '<TEACHER_B>',
  weekScheduleB: '<WEEK_SCHEDULE_B>',
  slotB: '<SLOT_B>',
  publication: '<PUBLICATION>',
  nextWeek: '<NEXT_WEEK>',
};
const auth = { Authorization: 'Bearer ' + localStorage.getItem('auth_token') };
const status = async (url, init = {}) => (await fetch(url, { headers: auth, ...init })).status;
({
  ownWeek: await status(`/api/week-schedules/by-teacher-and-week?teacherId=${ids.teacherA}&week=${ids.nextWeek}`),
  otherWeek: await status(`/api/week-schedules/by-teacher-and-week?teacherId=${ids.teacherB}&week=${ids.nextWeek}`),
  otherSlot: await status(`/api/week-schedules/${ids.weekScheduleB}/slots/${ids.slotB}/mark-unavailable`, { method: 'POST' }),
  ownDashboard: await status(`/api/publications/${ids.publication}/dashboard?teacherId=${ids.teacherA}`),
  otherDashboard: await status(`/api/publications/${ids.publication}/dashboard?teacherId=${ids.teacherB}`),
  ownExcel: await status(`/api/publications/${ids.publication}/excel?teacherId=${ids.teacherA}`),
  otherExcel: await status(`/api/publications/${ids.publication}/excel?teacherId=${ids.teacherB}`),
  me: await (await fetch('/api/me', { headers: auth })).json(),
})
```

Expected: `ownWeek`, `ownDashboard` and `ownExcel` are `200`; `otherWeek`, `otherSlot`, `otherDashboard` and `otherExcel` are `404` (never `403`, never `200`). `me` is `{ id: <USER_A>, name: "יעל כרמי", signInEmail: "yael.user@school.example", role: "teacher", teacherId: <TEACHER_A>, teacherName: "יעל כרמי" }`. The page is still on Weekly prep (a 404 doesn't sign the User out).

Then confirm from the Bash tool that B's Slot is unchanged:

```bash
cd verify-90 && API=http://localhost:5080 && . ./ids.txt
json() { node -pe "JSON.parse(require('fs').readFileSync(0,'utf8')).$1"; }
ADMIN=$(curl -s -X POST $API/api/auth/login -H "Content-Type: application/json" -d @login-admin.json | json accessToken)
curl -s "$API/api/week-schedules/by-teacher-and-week?teacherId=$TEACHER_B&week=$NEXT_WEEK" -H "Authorization: Bearer $ADMIN" | json "slots.find(s => s.id === '$SLOT_B').state"
```

Expected: `open`.

- [ ] **Step 5: Publications and History as a Teacher (10b; AC: own Publications, Submissions, share link, Excel, History)**

- [ ] Publications (this week, Open): no Teacher picker, and the subtitle reads "רק הפרסומים שלך: מה שהתלמידים ביקשו ממך בכל שבוע.". `read_network_requests`: `GET /api/publications/<PUBLICATION>/dashboard?teacherId=<TEACHER_A>` → `200`; no `GET /api/teachers/find`.
- [ ] The stats chips and the grid show (Yael's Submissions, zero so far). No "הארכת מועד" (unchanged from #89).
- [ ] Click "העתקת קישור": the toast "הקישור הועתק" shows.
- [ ] Click "הורדת אקסל": `GET /api/publications/<PUBLICATION>/excel?teacherId=<TEACHER_A>` → `200` and the toast "האקסל הורד" shows.
- [ ] History: exactly two rows, both "יעל כרמי" (this week and next week); no "אורן ביטון" row. `GET /api/publications/history` → `200`.
- [ ] `get_page_text` on both pages shows no raw translation key, and `dir` is `rtl`.

- [ ] **Step 6: English (LTR) as a Teacher**

Switch the language to English:
- [ ] `dir` is `ltr`. Weekly prep: the chip label reads "Teacher" and the value is still "יעל כרמי"; the lock icon is now at the chip's left edge.
- [ ] Publications: the subtitle reads "Only your Publications: what Students requested from you each week.".
- [ ] History: still only Yael's two rows.
- [ ] No raw translation key on any of the three pages.

Switch back to Hebrew and sign out.

- [ ] **Step 7: An Administrator linked to Teacher B (10c; AC 6; Review Focus 4 and 5)**

Sign in as `oren.user@school.example` / `Temporary#2026` (Hebrew), the Administrator User seeded in step 2 and linked to Teacher B.
- [ ] Weekly prep: the Teacher picker shows (no locked chip), already set to "אורן ביטון (אני)", and B's next week loads with no click. `GET /api/me` → `200` with `role` "administrator" and `teacherName` "אורן ביטון". Open the list: both Teachers, and only "אורן ביטון" ends with "(אני)".
- [ ] Pick "יעל כרמי": A's week loads (`200`), the picker shows "יעל כרמי" with no "(אני)", and marking one of A's Slots works (`204`; restore it).
- [ ] Publications: the picker defaults to "אורן ביטון (אני)", and there is **no** "רק הפרסומים שלך" subtitle. Lifecycle controls show ("הארכת מועד" for the Open week). Switch to "יעל כרמי": A's dashboard loads (`200`).
- [ ] History: four rows, both Teachers (a linked Administrator is not narrowed to B). Click "צפייה בלוח" (view dashboard) on a "יעל כרמי" row: Publications opens with `?teacherId=<TEACHER_A>`, and the picker shows "יעל כרמי", not the linked default (the query param wins).
- [ ] English: the picker reads "אורן ביטון (me)" when B is selected, and the open list marks the same option "(me)". Switch back to Hebrew.
- [ ] Sign out.

- [ ] **Step 8: An unlinked Administrator is unchanged**

Sign in as `admin@local.dev` / `DevAdmin#2026`.
- [ ] Weekly prep: the picker starts with no Teacher selected and the prompt "בחרו מורה ושבוע כדי להכין את הרשת." shows; neither option ends with "(אני)".
- [ ] Publications: the picker defaults to the first Teacher by name, "אורן ביטון", with no "(אני)".
- [ ] History: four rows. `GET /api/me` → `200` with `teacherId` null.

If a Teacher chosen by the previous User of the tab is still selected after signing in (store state surviving sign-out), reload and record it in the PR as a finding; it is a defect only if "(אני)" shows for this unlinked Administrator.

Sign out. Stop the API (`TaskStop`) and the preview (`preview_stop`).

- [ ] **Step 9: Compare against the design**

If the `claude_design` MCP is connected, read `auth/screens2.jsx` (`AuPrep` frames 10a and 10c, `AuPubs` frame 10b, `AuLockChip`) and the copy in `auth/strings.jsx` (`tp.me`, `pubs.sub`) from project `6a0ab892-caa4-49f7-baff-bba7ca38c862` with `read_file`, render the frames with `render_preview`, and compare them with the screenshots (or DOM checks) from steps 3 to 7. Check that "(אני)" / "(me)" and the 10b subtitle match `tp.me` and `pubs.sub` word for word, and that the chip's label, height, lock icon, surface and border match `AuLockChip`. Write down every visible difference for the PR. These differences are expected:
- 10b: the design shows a per-week table of the Teacher's Publications; this story keeps the dashboard layout with the subtitle added (deferred, README decision 12).
- The user menu doesn't show the User's name or the "Linked Teacher: {t}" line (2c); `api/me` makes it possible, it's a follow-up (README decision 13).

- [ ] **Step 10: Push and open the PR**

```bash
git push -u origin 90-teacher-data-scoping
```

This branch starts from `89-teacher-role-navigation`, whose PR #100 targets `main` and isn't merged. Until #100 targets (or is merged into) `82-users-and-roles`, this PR's diff also shows every #89 commit. Before creating the PR, **ask the user** whether to retarget PR #100 to `82-users-and-roles` (`gh pr edit 100 --repo silagy/DrivingLessonsBooking --base 82-users-and-roles`). It changes a PR other people see, so run it only on a clear yes.

Write `pr-body.md` in the scratchpad:

```markdown
Closes #90. Follows #89 (PR #100). Part of #82, slice (2).

## What changed

- **Backend scoping:** `ICurrentUser` now carries the Role and the linked Teacher, read from the token; `HttpCurrentUser` refuses a Teacher token without a valid `teacher_id`. One rule, `CurrentUserExtension.MayReach`, guards get Week Schedule, mark Slot Open / Unavailable, the dashboard (Submissions) and the Excel download. A Teacher asking for another Teacher's data gets 404 (not 403), checked before anything loads, and a Slot command on another Teacher's Week Schedule never commits. History filters by the linked Teacher for a Teacher and shows every Teacher for an Administrator, linked or not.
- **`GET api/me`:** returns the signed-in User with the linked Teacher's name (Teacher or Administrator); added to the policy matrix.
- **Client:** a core signed-in User store loads `api/me`. Weekly prep shows a Teacher the locked Teacher chip (10a) instead of the picker, via a shared `LockedFieldComponent` extracted from the edit-user dialog. An Administrator linked to a Teacher defaults to their own Teacher on Weekly prep and Publications, marked "(אני)" / "(me)" (10c), and can still switch; a `?teacherId=` link wins. Publications shows a Teacher the "only your Publications" subtitle (10b).

## Not in this PR (by design)

- The user menu's name and "Linked Teacher: {t}" line (2c). `api/me` makes it possible; follow-up.
- The 10b per-week table of a Teacher's Publications; the dashboard layout stays.

## Verification

- Backend and client suites pass; no pending model changes.
- Browser (Hebrew RTL and English), three personas: <fill in from steps 3-8>.
- Crafted requests as a Teacher (other Teacher's week, Slot, dashboard, Excel): <fill in the statuses from step 4>.
- Design differences: <fill in from step 9>.

🤖 Generated with [Claude Code](https://claude.com/claude-code)
```

Fill in the `<fill in ...>` lines with what steps 3 to 9 actually showed, then:

```bash
gh pr create --repo silagy/DrivingLessonsBooking --base 82-users-and-roles --head 90-teacher-data-scoping --title "Teacher-role Users see and change only their own Teacher's data (#90)" --body-file <scratchpad>/pr-body.md
```

Then call the `ccd_pr` `get_status` tool; if it doesn't report the new PR, bind it with `bind_pr`. Read its CI and offer Auto-fix if a check fails.

- [ ] **Step 11: Drop the throwaway database**

```bash
docker exec drivinglessonsbooking-postgres-1 psql -U app -d postgres -c "DROP DATABASE IF EXISTS drivinglessons_us90_verify"
```

Expected: `DROP DATABASE`. Leave the compose Postgres running and `dl-postgres` stopped unless the user asks otherwise.
