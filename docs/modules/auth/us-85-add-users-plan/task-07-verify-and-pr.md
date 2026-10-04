# Task 7 of 7: Rules note, in-app verification and the PR

> Part of [#85: Add Users from a New Users Screen](README.md). Requires tasks 1 to 6 committed. Work on branch `85-add-users-screen`.

**Files:**
- Modify: `.claude\rules\api-guidelines.md` (Auth section)
- Nothing else, unless a step below finds a defect. A defect gets its own fix commit, with a failing test first where one can be written, before the PR.

**Interfaces:**
- Consumes: everything from tasks 1 to 6.
- Produces: the PR that closes #85.

**Why:** #85 acceptance criteria 1, 2, 7, 10 and 11 need the running app (list, dialog, translated 409s, Hebrew RTL, nav entry). README Review Focus items 4 and 5.

- [ ] **Step 1: Record the Administrator policy in the API rules**

In `.claude\rules\api-guidelines.md`, section `## Auth`, add this bullet after the first one:

```markdown
- Administrator-only controllers carry `[Authorize(Policy = AuthorizationPolicies.Administrator)]` (`Presentation.Web\Auth\AuthorizationPolicies.cs`). The policy requires the token's `role` claim to be `administrator`; JWT bearer runs with `MapInboundClaims = false` and `RoleClaimType = "role"`. Until #89 makes it the default policy, every new admin-only controller adds the attribute itself (first used by `api/users`, #85)
```

```bash
git add .claude/rules/api-guidelines.md
git commit -m "docs(rules): note the Administrator authorization policy (#85)"
```

End the commit message with the attribution trailer from the session's instructions.

- [ ] **Step 2: Start the app against a throwaway database**

README decision 15. From the repository root:

```bash
docker stop dl-postgres
docker compose up -d postgres
docker exec drivinglessonsbooking-postgres-1 psql -U app -d postgres -c "CREATE DATABASE drivinglessons_us85_verify"
```

Stop anything on port 5080. Start the API in the background (Bash tool, `run_in_background: true`):

```bash
ConnectionStrings__Default='Host=localhost;Port=5432;Database=drivinglessons_us85_verify;Username=app;Password=devpassword' dotnet run --project src/DrivingLessons.Presentation.Web --launch-profile http
```

Wait for `Now listening on: http://localhost:5080`. Seed two Teachers with Hebrew names. Hebrew on a Windows `curl` command line turns into `?????`, so write the payloads to files with bash `printf` and post them with relative `@file` paths, from a folder in the session scratchpad:

```bash
mkdir -p verify-85 && cd verify-85
printf '{"email":"admin@local.dev","password":"DevAdmin#2026"}' > login.json
TOKEN=$(curl -s -X POST http://localhost:5080/api/auth/login -H "Content-Type: application/json" -d @login.json | node -pe "JSON.parse(require('fs').readFileSync(0,'utf8')).accessToken")
printf '%s' '{"name":"דנה לוי","contactEmail":"dana@school.example"}' > t1.json
printf '%s' '{"name":"אבי כהן","contactEmail":"avi@school.example"}' > t2.json
curl -s -X POST http://localhost:5080/api/teachers -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" -d @t1.json
curl -s -X POST http://localhost:5080/api/teachers -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" -d @t2.json
docker exec drivinglessonsbooking-postgres-1 psql -U app -d drivinglessons_us85_verify -At -c "SELECT name FROM teachers ORDER BY name"
```

Expected: the last command prints `אבי כהן` and `דנה לוי`, not question marks.

Start the client with `preview_start {name: "client"}` (it proxies `/api` to port 5080) and open `http://localhost:4200`. Sign in with the dev seed values `admin@local.dev` / `DevAdmin#2026` (`appsettings.Development.json`). Screenshots of this app can come back half-rendered: run `await document.fonts.ready` in `javascript_tool` and wait about 1.5 s before each screenshot, and rely on `read_page`, `get_page_text` and `javascript_tool` for the checks themselves.

- [ ] **Step 3: Check the Users screen in Hebrew**

The app starts in Hebrew. Check each line:

- [ ] `document.documentElement.dir` is `rtl`. The shell nav ends with "משתמשים", after "היסטוריה".
- [ ] Clicking it opens `/users`. `read_network_requests` shows `GET /api/users/find` → `200` and `GET /api/teachers/find` → `200`.
- [ ] Title "משתמשים", the subtitle, and the "הוספת משתמש" button at the end side (left in RTL).
- [ ] One row: name "Administrator", the email `admin@local.dev` reading left to right (`getComputedStyle` of the `bdi` gives `direction: ltr`), Role tag "מנהל", linked teacher "ללא" (muted), status tag "פעיל". The one-line "בינתיים רק אתם יכולים להתחבר..." hint shows under the table.

- [ ] **Step 4: Add Users through the dialog**

Check each line:

- [ ] "הוספת משתמש" opens the dialog with the header "הוספת משתמש". The Role SelectButton has "מורה" selected. "שמירה" is disabled.
- [ ] The teacher Select lists "אבי כהן" then "דנה לוי", both enabled. The hint reads "חובה בתפקיד מורה." plus the fixed-link sentence. Leaving the Select empty after touching it shows "בחרו את המורה שהמשתמש הזה מתחבר בשמו." and "שמירה" stays disabled.
- [ ] The email and password inputs type left to right; the password eye toggles visibility.
- [ ] Fill name `Dana Teacher User`, email `dana.user@school.example`, Role "מורה", teacher "דנה לוי", password `Temporary#2026`, save. A success toast "המשתמש נוסף" appears; `POST /api/users` → `201`; the table now has two rows, the new one with Role "מורה" and linked teacher "דנה לוי"; the "only you" hint is gone.
- [ ] Open the dialog again: "דנה לוי" is disabled and says "כבר יש לו משתמש"; "אבי כהן" is enabled.
- [ ] Switch Role to "מנהל": the hint reads "לא חובה..." and the Select offers a clear button. Save an Administrator with name `Owner Two`, email `owner.two@school.example`, no teacher, password `Temporary#2026`: `201`, a third row with "ללא".
- [ ] Add a User with email ` Dana.User@School.Example ` (other casing, surrounding spaces), Role "מנהל", no teacher: `POST /api/users` → `409`, and the error toast reads "משתמש אחר כבר מתחבר עם האימייל הזה." No new row.
- [ ] There is no control anywhere on the screen that changes a User's linked teacher (#85 criterion 5).

- [ ] **Step 5: Check the Teacher delete guard**

Open Cars & teachers. Check each line:

- [ ] Delete "דנה לוי" and confirm: `DELETE /api/teachers/{id}` → `409`, the error toast reads "למורה הזה יש משתמש פעיל, ולכן אי אפשר למחוק אותו.", and the teacher card stays.
- [ ] Delete "אבי כהן" (no User) and confirm: `204`, success toast, the card disappears. Reload the browser on `/users` (root stores load once per app load, like every other feature's): the Add User teacher Select no longer lists "אבי כהן".
- [ ] Mark Dana's User deleted directly in the database, then delete "דנה לוי" again: it now succeeds (Review Focus 5):

```bash
docker exec drivinglessonsbooking-postgres-1 psql -U app -d drivinglessons_us85_verify -c "UPDATE users SET is_deleted = true WHERE email = 'dana.user@school.example'"
```

  Back on `/users` (reload), Dana's row is last, muted, with status "נמחק", and still shows "דנה לוי" as linked teacher (README decision 8).

- [ ] **Step 6: Check English and narrow widths**

- [ ] Switch the language toggle to English: `dir` is `ltr`, nav "Users", title "Users", columns "Name · Sign-in email · Role · Linked teacher · Status", tags "Administrator" / "Teacher", "Active" / "Deleted". The Add User dialog reads in English, with "Teacher" preselected.
- [ ] `resize_window` to `tablet` (768 px): `document.documentElement.scrollWidth <= window.innerWidth` (no page-level horizontal scroll); the table scrolls inside its card if it needs to. Reset with `resize_window {preset: "desktop"}`.
- [ ] Take one screenshot of the Users page in Hebrew and one of the open Add User dialog, after waiting for fonts, as proof for the PR.
- [ ] If the `claude_design` MCP is connected in this session, compare both against `Users and Roles.html` from the design project ([users-and-roles-design.md](../users-and-roles-design.md)) and list visible differences in the PR. If it isn't, say so in the PR.

- [ ] **Step 7: Check a Teacher-role User**

Both seeded Teachers are deleted by now. As the Administrator, add a Teacher "רון שמש" on Cars & teachers, then on `/users` add a Teacher-role User for him: name `Ron Teacher User`, email `ron.user@school.example`, password `Temporary#2026`. Sign out and sign in as `ron.user@school.example` / `Temporary#2026`.

- [ ] Sign-in succeeds. Opening `/users` shows the load error "טעינת המשתמשים נכשלה", and `GET /api/users/find` → `403`. The other screens still load for this User: hiding them is #89, so note it in the PR rather than fixing it here.

Stop the client (`preview_stop`) and the API, then drop the throwaway database:

```bash
docker exec drivinglessonsbooking-postgres-1 psql -U app -d postgres -c "DROP DATABASE drivinglessons_us85_verify"
```

- [ ] **Step 8: Final checks**

From the repository root:

```bash
dotnet build
dotnet test tests\DrivingLessons.Domain.Test\DrivingLessons.Domain.Test.csproj
dotnet test tests\DrivingLessons.Application.Test\DrivingLessons.Application.Test.csproj
dotnet ef migrations has-pending-model-changes --project src\DrivingLessons.Infrastructure --startup-project src\DrivingLessons.Presentation.Web
git status
```

From `client\` in PowerShell:

```powershell
& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js test --watch=false
& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js build --project client
```

Expected: build clean, every test and spec PASS, `No changes have been made to the model since the last migration.`, the client build succeeds, working tree clean.

- [ ] **Step 9: Push and open the PR**

Ask the user before pushing. Once they confirm:

```bash
git push -u origin 85-add-users-screen
gh pr create --repo silagy/DrivingLessonsBooking --base 84-user-aggregate-sign-in --head 85-add-users-screen --title "Add Users from a new Users screen"
```

The base is `84-user-aggregate-sign-in` because #85 is stacked on [PR #98](https://github.com/silagy/DrivingLessonsBooking/pull/98). If #98 has merged by now, use `--base main` instead (after checking that `git log origin/main..85-add-users-screen` shows only this branch's commits).

The PR body:
- Says what each commit changed (one line each).
- Lists the README decisions a reviewer should challenge, at least 1 (Deleted Users keep their email and Teacher), 3 (no database constraint behind the interactor checks), 5 (blank Temporary Password only, no length rule), 6 (named policy, inbound claim mapping off) and 10 (picker disables linked Teachers).
- Notes that a Teacher-role User can still open every other admin screen and endpoint until #89, and sees a load error on `/users`.
- Includes the checked lists from Steps 3 to 7, the two screenshots, the design comparison (or that the MCP wasn't available), and the results from Step 8.
- Contains `Closes #85` and `Part of #82`, and ends with the attribution line from the session's instructions.

After opening it, follow the session's PR instructions (bind the PR and read its CI status).
