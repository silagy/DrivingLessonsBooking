# Task 6 of 6: Docs, in-app verification and the PR

> Part of [#84: User Aggregate Takes Over Sign-In from the Admin Record](README.md). Requires tasks 1 to 5 committed. Work on branch `84-user-aggregate-sign-in`.

**Files:**
- Modify: `docs\development\running-the-project.md` (lines 54 and 101, seeding wording)
- Modify: `docs\decisions\0004-no-admin-domain-aggregate.md` (status line)
- Modify: `CLAUDE.md` (Domain Snapshot gains **User**)
- Nothing else, unless a step below finds a defect. A defect gets its own fix commit, with a failing test first where one can be written, before the PR.

**Interfaces:**
- Consumes: everything from tasks 1 to 5. The `MoveAdminToUsers` migration must **not** have been applied to the local database yet (task 5 says so). Step 2 checks.
- Produces: the PR against `main` that closes #84.

**Why:** #84 acceptance criteria 7 and 8 ("every existing admin endpoint and screen keeps working", "verified in the app"). The start-up seeding no longer overwrites the password, and the docs still say it does.

- [ ] **Step 1: Update the docs**

`docs\development\running-the-project.md`, line 54. Replace

```markdown
Migrations apply and the admin user is seeded automatically on startup.
```

with

```markdown
Migrations apply on startup. On an empty database, the first Administrator is created from `ADMIN_EMAIL` / `ADMIN_PASSWORD`. Once any User exists, changing those values does nothing: the stored email and password stay as they are.
```

Same file, line 101. Replace

```markdown
apply migrations and seed the dev admin on startup.
```

with

```markdown
apply migrations on startup and, on an empty database, create the dev Administrator from `appsettings.Development.json`.
```

`docs\decisions\0004-no-admin-domain-aggregate.md`, replace the line `**Status:** Accepted` with:

```markdown
**Status:** Superseded by issue [#84](https://github.com/silagy/DrivingLessonsBooking/issues/84): sign-in now uses the `User` aggregate. The replacement ADR comes with the docs slice of spec [#82](https://github.com/silagy/DrivingLessonsBooking/issues/82).
```

`CLAUDE.md`, in **Domain Snapshot**, add this as the first bullet (before **Teacher**):

```markdown
- **User** — a person who can sign in; exactly one Role (`Administrator` | `Teacher`); a Teacher-Role User is linked to exactly one Teacher; sign-in email unique case-insensitively; soft-deleted; replaces the infrastructure admin record (#84)
```

(CLAUDE.md's snapshot bullets already use long dashes. `SourceTextTest` checks code, not docs.)

```bash
git add docs/development/running-the-project.md docs/decisions/0004-no-admin-domain-aggregate.md CLAUDE.md
git commit -m "docs: first-Administrator seeding and the User aggregate (#84)"
```

End the commit message with the attribution trailer from the session's instructions.

- [ ] **Step 2: Record the admin row before the migration runs**

Use the compose Postgres (README). Back up the database inside the container, then print the admin row and the applied migrations:

```bash
docker exec drivinglessonsbooking-postgres-1 pg_dump -U app -d drivinglessons -Fc -f /tmp/before-84.dump
docker exec drivinglessonsbooking-postgres-1 psql -U app -d drivinglessons -At -c 'SELECT "Id", "Email", "PasswordHash" FROM admin_users'
docker exec drivinglessonsbooking-postgres-1 psql -U app -d drivinglessons -At -c 'SELECT "MigrationId" FROM "__EFMigrationsHistory" ORDER BY 1 DESC LIMIT 2'
```

Expected: exactly one admin row (`admin@local.dev`). Keep its id and hash in your notes for Step 3. The newest migration is `..._AddUsers`, and `..._MoveAdminToUsers` is not listed. If `MoveAdminToUsers` is already applied, restore the dump taken before task 5 if one exists. Otherwise tell the user that the hash comparison in Step 3 can't be done, and continue with Step 4.

To undo everything later: `docker exec drivinglessonsbooking-postgres-1 pg_restore -U app -d drivinglessons --clean /tmp/before-84.dump`.

- [ ] **Step 3: Start the API on the branch and check the migrated row**

Start the API with `preview_start {name: "api"}` (it runs the `http` profile on port 5080, applies migrations and seeds), or `dotnet run --project src\DrivingLessons.Presentation.Web --launch-profile http`. Wait for `Now listening on: http://localhost:5080`. Then:

```bash
docker exec drivinglessonsbooking-postgres-1 psql -U app -d drivinglessons -At -c "SELECT id, name, email, password_hash, role, teacher_id IS NULL, is_deleted, length(security_stamp) FROM users"
docker exec drivinglessonsbooking-postgres-1 psql -U app -d drivinglessons -c "\dt admin_users"
```

Check each line:
- [ ] Exactly one row. `id`, `email` and `password_hash` equal the values from Step 2, `name` is `Administrator`, `role` is `10`, `teacher_id IS NULL` is `t`, `is_deleted` is `f`, the stamp length is `32`.
- [ ] `\dt admin_users` prints `Did not find any relation named "admin_users".`
- [ ] The API log shows no exception at start-up.

- [ ] **Step 4: Sign in in the browser and use the existing screens**

Start the client with `preview_start {name: "client"}` and open `http://localhost:4200`. Sign in with `admin@local.dev` / `DevAdmin#2026` (the dev values, as in `appsettings.Development.json`). Check each line:

- [ ] Sign-in succeeds and lands on the admin shell. The header shows the email, as before.
- [ ] Teachers & Cars, Week Schedules, Publications and Roster each load. `read_network_requests` shows their `GET /api/...` calls returning `200`, none `401` or `403`.
- [ ] The token's payload is right. Run this in the page with `javascript_tool`:

```javascript
JSON.parse(atob(localStorage.getItem('auth_token').split('.')[1]))
```

  Expected: `sub` equals the `users.id` from Step 3, `email` is `admin@local.dev`, `role` is `"administrator"`, `security_stamp` is a 32-character string equal to `users.security_stamp`, there is no `teacher_id` key, and no claim has the value `"admin"`.
- [ ] Signing out and signing in with a wrong password shows the existing "invalid credentials" message (`401`). A malformed email can't be tested in the form if the form validates it, so check it with the API directly:

```bash
curl -s -o NUL -w "%{http_code}\n" -X POST http://localhost:5080/api/auth/login -H "Content-Type: application/json" -d "{\"email\":\"not-an-email\",\"password\":\"x\"}"
```

  Expected: `401` (not `409`). On bash, use `-o /dev/null`.

- [ ] **Step 5: Restart with a different configured password**

Stop the API (`preview_stop`, or Ctrl+C). Start it again with an overriding password, from the repository root in PowerShell:

```powershell
$env:Admin__Password = 'Different#2026'; dotnet run --project src\DrivingLessons.Presentation.Web --launch-profile http
```

Check each line:
- [ ] `users` still holds one row, and `password_hash` is unchanged from Step 3 (re-run the `SELECT` from Step 3).
- [ ] In the browser, `admin@local.dev` / `DevAdmin#2026` still signs in.
- [ ] `admin@local.dev` / `Different#2026` is rejected with `401`.

Stop the API and clear the override: `Remove-Item Env:Admin__Password`.

- [ ] **Step 6: Check seeding on a fresh database**

Create a throwaway database and point the API at it:

```bash
docker exec drivinglessonsbooking-postgres-1 psql -U app -d postgres -c "CREATE DATABASE drivinglessons_fresh84"
```

```powershell
$env:ConnectionStrings__Default = 'Host=localhost;Port=5432;Database=drivinglessons_fresh84;Username=app;Password=devpassword'; dotnet run --project src\DrivingLessons.Presentation.Web --launch-profile http
```

Check each line:
- [ ] Start-up applies every migration with no error.
- [ ] `docker exec drivinglessonsbooking-postgres-1 psql -U app -d drivinglessons_fresh84 -At -c "SELECT name, email, role, teacher_id IS NULL FROM users"` prints `Administrator|admin@local.dev|10|t`.
- [ ] `POST /api/auth/login` with `admin@local.dev` / `DevAdmin#2026` returns `200` with an `accessToken`.

Stop the API, clear the override (`Remove-Item Env:ConnectionStrings__Default`), and drop the throwaway database:

```bash
docker exec drivinglessonsbooking-postgres-1 psql -U app -d postgres -c "DROP DATABASE drivinglessons_fresh84"
```

- [ ] **Step 7: Final checks**

```bash
dotnet build
dotnet test tests\DrivingLessons.Domain.Test\DrivingLessons.Domain.Test.csproj
dotnet test tests\DrivingLessons.Application.Test\DrivingLessons.Application.Test.csproj
dotnet ef migrations has-pending-model-changes --project src\DrivingLessons.Infrastructure --startup-project src\DrivingLessons.Presentation.Web
git status
```

Expected: build clean, every test PASS, `No changes have been made to the model since the last migration.`, working tree clean. The client wasn't changed, so its suite doesn't need to run.

- [ ] **Step 8: Push and open the PR against `main`**

Ask the user before pushing. Once they confirm:

```bash
git push -u origin 84-user-aggregate-sign-in
gh pr create --repo silagy/DrivingLessonsBooking --base main --head 84-user-aggregate-sign-in --title "User aggregate takes over sign-in from the admin record"
```

The PR body:
- Says what each of the six commits changed (one line each).
- Lists the decisions from the README's Decisions table that a reviewer should challenge, at least 1 (`Delete` shipped early), 4 (malformed email is 401), 5 (copy and drop in one migration) and 6 (name `Administrator`).
- Calls out the behavior change for deployers: **`ADMIN_EMAIL` / `ADMIN_PASSWORD` no longer reset the stored password on restart.** On production, the existing admin keeps their current password through the deploy.
- Includes the checked lists from Steps 3 to 6 and the results from Step 7.
- Contains `Closes #84` and `Part of #82`, and ends with the attribution line from the session's instructions.

After opening it, follow the session's PR instructions (bind the PR and read its CI status).
