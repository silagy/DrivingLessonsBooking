# Task 6 of 6: Verify in the app and open the PR

> Part of [#88: Change My Own Password](README.md). Requires tasks 1 to 5 committed. Work on branch `88-change-my-password`.

**Files:**
- Nothing, unless a step below finds a defect. A defect gets its own fix commit before the PR, with a failing test written first where one can be.

**Interfaces:**
- Consumes: everything from tasks 1 to 5.
- Produces: the PR that closes #88.

**Why:** three of #88's acceptance criteria need the running app:
- AC 4: the dialog is reachable from the shell for every User, both Roles.
- AC 5: after a change the User keeps working, and the UI says so.
- AC 6: translated, Hebrew first, RTL-correct.

This task also covers Review Focus 1, 2, 3 and 5.

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
docker exec drivinglessonsbooking-postgres-1 psql -U app -d postgres -c "DROP DATABASE IF EXISTS drivinglessons_us88_verify"
docker exec drivinglessonsbooking-postgres-1 psql -U app -d postgres -c "CREATE DATABASE drivinglessons_us88_verify"
```

Start the API in the background (Bash tool, `run_in_background: true`):

```bash
ConnectionStrings__Default='Host=localhost;Port=5432;Database=drivinglessons_us88_verify;Username=app;Password=devpassword' dotnet run --project src/DrivingLessons.Presentation.Web --launch-profile http
```

Seed a Teacher-role User from a scratchpad folder. Write Hebrew payloads to files with `node` and post them by relative `@file` paths, because Hebrew on the Windows `curl` command line turns into `?????` (memory note).

```bash
mkdir -p verify-88 && cd verify-88
API=http://localhost:5080
json() { node -pe "JSON.parse(require('fs').readFileSync(0,'utf8')).$1"; }
printf '{"email":"admin@local.dev","password":"DevAdmin#2026"}' > login.json
ADMIN=$(curl -s -X POST $API/api/auth/login -H "Content-Type: application/json" -d @login.json | json accessToken)
node -e "require('fs').writeFileSync('t1.json', JSON.stringify({name:'יעל כרמי',contactEmail:'yael@school.example'}))"
YAEL_T=$(curl -s -X POST $API/api/teachers -H "Authorization: Bearer $ADMIN" -H "Content-Type: application/json" -d @t1.json | json id)
node -e "require('fs').writeFileSync('u1.json', JSON.stringify({name:'יעל כרמי',signInEmail:'yael.user@school.example',role:'teacher',teacherId:process.argv[1],temporaryPassword:'Temporary#2026'}))" "$YAEL_T"
curl -s -X POST $API/api/users -H "Authorization: Bearer $ADMIN" -H "Content-Type: application/json" -d @u1.json; echo
```

Start the client with `preview_start {name: "client"}`; it proxies `/api` to port 5080. Open `http://localhost:4200` and sign in as `admin@local.dev` / `DevAdmin#2026`.

Screenshots of this app can come back half-rendered (memory note). Before each screenshot, run `await document.fonts.ready` in `javascript_tool` and wait about 1.5 s. Rely on `read_page`, `get_page_text` and `javascript_tool` for the checks themselves.

- [ ] **Step 3: The user menu in Hebrew (design 2c, 2e)**

- [ ] `document.documentElement.dir` is `rtl`.
- [ ] The top bar no longer has a standalone "התנתקות" button. At the inline end (the left, in RTL), after the language toggle, sits the avatar button with initials, the email (LTR) and a chevron. Its accessible name is "תפריט משתמש".
- [ ] Clicking it turns the chevron and opens a popup aligned under the button. The popup holds:
  - a header with a larger avatar and `admin@local.dev` reading left to right
  - "שינוי הסיסמה שלי" (key icon)
  - "התנתקות" (sign-out icon, mirrored: it points to the inline start)
- [ ] The popup isn't clipped and is at least about 300px wide. Clicking outside closes it, and the chevron turns back.

- [ ] **Step 4: Refusals keep you signed in (design 9b, 9c; Review Focus 1 and 5)**

Open "שינוי הסיסמה שלי". The header reads "שינוי הסיסמה שלי", with three empty password fields labelled "סיסמה נוכחית", "סיסמה חדשה" and "אימות הסיסמה החדשה". Each is `dir="ltr"` and has an eye toggle that reveals the text. The buttons are "ביטול" and "שינוי סיסמה", and "שינוי סיסמה" is disabled.

- [ ] **9b:** type `Wrong#2026`, `Fresh#2027` and `Fresh#2026`. "הסיסמאות לא תואמות." appears under the confirmation, its field is outlined as invalid, and "שינוי סיסמה" stays disabled. Fix the confirmation to `Fresh#2027`: the error disappears and the button enables.
- [ ] **9c:** submit. The dialog **stays open**, and its first item is the error "הפעולה לא בוצעה" / "הסיסמה הנוכחית שגויה.". `read_network_requests` shows `PUT /api/me/password` with status `409`. The URL is **not** `/login`.
- [ ] Close the dialog with "ביטול" and switch to the Users screen: it loads (`200`), so you are still signed in.
- [ ] Reopen the dialog: **no** refusal shows, and the fields are empty.
- [ ] Type `DevAdmin#2026`, then three spaces in both new fields, and submit. The dialog stays open with "הזינו סיסמה חדשה." (the server's `passwordMustNotBeEmpty`).

- [ ] **Step 5: Success keeps you signed in (design 9d; AC 5, Review Focus 2)**

1. In `javascript_tool`, note `localStorage.getItem('auth_token')` as the old token.
2. Reopen the dialog and enter `DevAdmin#2026`, `Admin#2027`, `Admin#2027`, then submit.
3. The dialog closes, and the success toast "הסיסמה שונתה" / "נשארת מחובר/ת." appears at the top inline end (top-left in RTL).
4. `localStorage.getItem('auth_token')` is now a different token.
5. Switch to Users, then to Weekly prep. Every request returns `200`, nothing returns `401`, and the URL never becomes `/login`.
6. From bash, the old token is dead: `curl -s -o /dev/null -w "%{http_code}\n" $API/api/users/find -H "Authorization: Bearer <old token>"` → `401`.
7. Sign out from the menu ("התנתקות"). You land on `/login`. Sign in with `Admin#2027`: it works. Signing in with `DevAdmin#2026` shows the login error.

- [ ] **Step 6: Check the English (LTR) version**

Switch the language to English, then check:
- `dir` is `ltr`, and the avatar button sits at the right end.
- The menu reads "Change my password", then "Sign out"; the sign-out icon isn't mirrored.
- The dialog shows "Change my password", "Current password", "New password", "Confirm new password", "Cancel" and "Change password".
- The mismatch line reads "The passwords don't match.", and a wrong current password reads "That didn't go through" / "Current password is incorrect.".
- Change the password back: current `Admin#2027`, new and confirmation `DevAdmin#2026`. The toast reads "Password changed" / "You're still signed in.".

- [ ] **Step 7: A Teacher-role User (AC 4, Review Focus 3)**

1. Sign out. Sign in as `yael.user@school.example` / `Temporary#2026`.
2. The avatar button and the user menu are there with "Change my password". Navigation by Role is slice (2), so the nav items may still show, and some screens may fail with `403`; that's expected here.
3. Change the password: current `Temporary#2026`, new and confirmation `Yael#2027`. The dialog closes with the success toast, and `PUT /api/me/password` returned `200`.
4. Sign out, then sign in with `Yael#2027`: it works.

- [ ] **Step 8: Compare against the design**

If the `claude_design` MCP is connected, open frames 2c, 2d, 2e, 9a, 9b, 9c and 9d of `Users and Roles.html` and compare them with screenshots. Write down every visible difference for the PR. These differences are expected:
- The menu header shows no name, Role tag or linked Teacher (README decision 10).
- The current password field isn't outlined in red on a wrong current password (README decision 11).
- "Sign out" keeps the existing copy instead of "Log out" (README decision 10).
- Button casing and colors come from the app theme (README decision 12).

Stop the API and the client. Drop the throwaway databases:

```bash
docker exec drivinglessonsbooking-postgres-1 psql -U app -d postgres -c "DROP DATABASE IF EXISTS drivinglessons_us88_verify"
docker exec drivinglessonsbooking-postgres-1 psql -U app -d postgres -c "DROP DATABASE IF EXISTS drivinglessons_us88_smoke"
```

- [ ] **Step 9: Push and open the PR**

```bash
git push -u origin 88-change-my-password
gh pr create --base main --title "Change my own password (#88)" --body-file pr-body.md
```

Write `pr-body.md` in the scratchpad, not in the repo. It must contain:
- `Closes #88` and `Part of #82`.
- A summary:
  - `Password` value object and `User.ChangePassword` with `UserPasswordChanged` and stamp rotation
  - `ChangeMyPasswordInteractor`: current-password check, then a fresh token after the commit
  - `PUT api/me/password`, open to every signed-in User
  - the shell's user menu and the Change my password dialog
- **Flagged for review:**
  - Decision 1: the User stays signed in with a fresh token (design 9d, not 9e). Their other sessions end on their next request.
  - Decision 2: a wrong current password is a 409, `userCurrentPasswordMustBeCorrect`, so the 401 interceptor doesn't sign the User out.
  - Decision 10: the user menu header has no name, Role tag or linked Teacher yet; slice (2).
  - No rate limit on wrong current passwords. The caller already holds a valid token, so this is the same exposure as the sign-in endpoint.
- The verification done (suites, API smoke for both Roles, the browser in Hebrew and English, the token swap) and the design differences from step 8.
- The PR attribution line from the session's instructions.
