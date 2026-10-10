# Task 4 of 4: Verify in the browser and open the PR

> Part of [#91: Block deleting or unassigning Teachers and Cars while active Students depend on them](README.md). Requires tasks 1 to 3 committed. Work on branch `91-block-removal-with-active-students`. Read README decisions 10 to 12 and the Review Focus first.

**Files:** none changed unless a step finds a defect. A defect found here goes back to the task that owns the code, gets a test there, and its own `fix(teachers): ... (#91)` commit.

**Interfaces:**
- Consumes: everything tasks 1 to 3 produce; `.claude\launch.json` configurations `api` (port 5080) and `client` (port 4200, proxies `/api`).
- Produces: the PR into `82-users-and-roles`.

- [ ] **Step 1: Run every suite**

```bash
dotnet build
dotnet test tests\DrivingLessons.Domain.Test\DrivingLessons.Domain.Test.csproj
dotnet test tests\DrivingLessons.Application.Test\DrivingLessons.Application.Test.csproj
dotnet ef migrations has-pending-model-changes --project src\DrivingLessons.Infrastructure --startup-project src\DrivingLessons.Presentation.Web
```

From `client\`:

```powershell
& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js test --watch=false
& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js build --project client
```

Expected: everything PASS, no pending model changes, the client builds.

- [ ] **Step 2: Start the app on a throwaway database and seed it**

```bash
docker stop dl-postgres
docker compose up -d postgres
docker exec drivinglessonsbooking-postgres-1 psql -U app -d postgres -c "DROP DATABASE IF EXISTS drivinglessons_us91_verify"
docker exec drivinglessonsbooking-postgres-1 psql -U app -d postgres -c "CREATE DATABASE drivinglessons_us91_verify"
```

Start the API in the background (Bash tool, `run_in_background: true`):

```bash
ConnectionStrings__Default='Host=localhost;Port=5432;Database=drivinglessons_us91_verify;Username=app;Password=devpassword' dotnet run --project src/DrivingLessons.Presentation.Web --launch-profile http
```

Seed with task 2 step 8's script (the part up to and including `create omer`), pointed at the same API, plus four more active Students of Ronit on the Corolla so Review Focus 4 is visible: national IDs `000000018`, `000000026`, `000000034`, `000000042` (all pass the check digit), names written by `seed.js` (for example 'אבי כהן', 'דנה ששון', 'ליה חדד', 'רועי אלמוג'). Expected: every create returns 201.

- [ ] **Step 3: Hebrew (RTL) refusals on the Cars & Teachers screen (Review Focus 1, 3, 4, 5)**

Start the client with `preview_start {name: "client"}`. Open `http://localhost:4200`, sign in as `admin@local.dev` / `DevAdmin#2026` (Hebrew) and open "רכבים ומורים". Before any screenshot, `await document.fonts.ready` (memory note); if screenshots hang, verify via `read_page` / `get_page_text`.

1. Delete "קורולה לבנה" and confirm. Expected: an error toast "תלמידים פעילים לומדים על הרכב הזה (5): ...", three names, no trailing dots; the Car is still listed.
2. Open the Corolla's assign popover, uncheck "רונית אברהם", Apply. Expected: an error toast naming "רונית אברהם" and the Students; the popover reopens with Ronit still checked (the Cars reloaded).
3. Open the i20's assign popover, uncheck "רונית אברהם", Apply. Expected: a success toast "שיוכי המורים עודכנו" (Omer is Yael's Student and doesn't block, Review Focus 1). Re-assign Ronit to the i20 for the next steps.
4. On the Users screen, add a Teacher-role User linked to "רונית אברהם". Back on "רכבים ומורים", delete Ronit. Expected: the existing toast "למורה הזה יש משתמש פעיל, ולכן אי אפשר למחוק אותו." (Review Focus 5). Delete that User on the Users screen, then delete Ronit again. Expected: "למורה הזה יש תלמידים פעילים (5): ..."
5. Read the toast's DOM (`get_page_text` or `javascript_tool` on the toast summary): the names sit inside direction-isolation marks, the sentence reads right to left with the count in parentheses, no reordered punctuation.
6. On "תלמידים", mark Noa and the four new Students as inactive. Back on "רכבים ומורים": unassign Ronit from the Corolla (success), delete the Corolla (success), delete Ronit (success). Then delete the i20. Expected: refused, naming Omer.

Take a screenshot of one refusal toast for the PR.

- [ ] **Step 4: English (LTR)**

Switch the language to English and repeat step 3.6's last check (delete the i20). Expected: "Active Students learn on this Car (1): עומר שלו. Change their Car on the Students screen, then delete.", the Hebrew name isolated so the sentence's period and the rest stay in place (Review Focus 3). Take a screenshot.

- [ ] **Step 5: Reset the viewport and stop the servers**

Reset any emulated size with `resize_window {preset: "desktop"}`, stop the client and API (`preview_stop`, `TaskStop`), then drop the database:

```bash
docker exec drivinglessonsbooking-postgres-1 psql -U app -d postgres -c "DROP DATABASE IF EXISTS drivinglessons_us91_verify"
```

- [ ] **Step 6: Reconcile with #95 if it has merged**

Run `git fetch` and `gh pr view 105 --repo silagy/DrivingLessonsBooking --json state`. If #105 is `MERGED`, merge `origin/82-users-and-roles` into this branch (never rebase), then:
- In `tests\DrivingLessons.Domain.Test\Entities\StudentTest.cs`, change each `car.UnassignTeacher(teacher)` / `oldCar.UnassignTeacher(teacher)` (#95 builds flagged Students that way) to pass `[]` as the second argument.
- Resolve conflicts in `client\public\i18n\he.json`, `en.json` and `ApiExceptionFilterTest.cs` by keeping both sides.
- Re-run step 1, then commit the merge.

If #105 is still open, skip this step; README decision 12 tells whoever merges second what to change.

- [ ] **Step 7: Push and open the PR**

```bash
git push -u origin 91-block-removal-with-active-students
```

Write `<scratchpad>/pr-body.md`:

```markdown
Closes #91. Part of #82.

## What changes

- Deleting a Teacher, deleting a Car, and unassigning a Car from a Teacher are refused (409) while active Students depend on them. Inactive Students never block.
- The rule lives in `Teacher.Delete` / `Car.Delete` / `Car.UnassignTeacher`, which now take the resolved Students; the interactors load them with `IStudentRepository.FindByTeacherAsync` / `FindByCarAsync`.
- New `TeacherMustNotHaveActiveStudentsException`, `CarMustNotHaveActiveStudentsException`, `TeacherAssignmentMustNotHaveActiveStudentsException`; the problem carries `code` and `params` (`count`, the first three `names`, and `teacher` for the unassign).
- The Cars & Teachers screen shows each refusal as a translated toast (Hebrew first), names direction-isolated.

## Decisions to challenge

- Three names plus the count, not every name (README decision 7).
- Refusals are toasts, like the existing active-User refusal; there is no Claude Design frame for them.

## Verification

- Domain, application and client suites pass; no pending model changes.
- Postgres smoke of every guard, including a shared Car and inactive-only Students.
- Browser: Hebrew RTL and English toasts (screenshots below).

Plan: `docs/modules/teachers/us-91-block-removal-with-active-students-plan/README.md`

🤖 Generated with [Claude Code](https://claude.com/claude-code)
```

Then:

```bash
gh pr create --repo silagy/DrivingLessonsBooking --base 82-users-and-roles --head 91-block-removal-with-active-students --title "Block deleting or unassigning Teachers and Cars while active Students depend on them (#91)" --body-file "<scratchpad>/pr-body.md"
```

Attach the two screenshots in a PR comment. Then call the `ccd_pr` `get_status` tool; if it doesn't report the new PR, bind it with `bind_pr`. Read its CI and offer Auto-fix if a check fails. `Closes #91` won't auto-close the issue on the non-default base: close it when `82-users-and-roles` merges to `main`.
