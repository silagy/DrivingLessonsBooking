# Task 2 of 2: Verification of both plans, then one PR

> Part of [Double Counts Once](README.md). Requires the [excel plan](../../excel/detail-sheet-feedback-plan/README.md)'s tasks and this plan's task 1 committed. No source change.

**Files:**
- No source change. Local only, never staged: `.claude\launch.json`.

**Interfaces:**
- Consumes: the excel plan's task 2 `api-smoke` server and smoke shell (`API`, `AUTH`, `LINK`, `SMOKE`). If the shell is gone, re-run that task's Step 1 to get a fresh open link.
- Produces: a verified branch and a PR against `main`.

- [ ] **Step 1: Start the client against the smoke API**

`preview_start {name:"client"}` (it proxies `api/` to 5080, where `api-smoke` runs). Open `http://localhost:4200/s/$LINK`, then `resize_window` to 360×780.

- [ ] **Step 2: The student form on a phone, Hebrew then English**

1. Identify as Smoke Student B (`000000026`, Levi's student, no submission yet). Continue to the target step.
   Expected: `.target__note` reads `שיעור כפול נספר כשיעור אחד ביעד. רוצים שיעור כפול אחד? בחרו 1.`; `document.documentElement.scrollWidth <= innerWidth`.
2. Raise the target to 2, continue, pick one slot as **כפול**, continue to review.
   Expected: `.review__not-enough` shows the existing body ("היעד שלכם הוא 2, אבל בחרתם 1...") followed by `שיעור כפול נספר כבחירה אחת.`; no horizontal overflow. This is the customer's screenshot, now explained.
3. Lower the target to 1 through **שינוי היעד** and return to review. Expected: no not-enough message, no reminder; submit succeeds (`204` in `read_network_requests`).
4. Toggle to EN and reload, then identify as Smoke Student E (`000000067`, a returning student whose saved list has two Singles). Expected on the target step: the English note `A double lesson counts as one lesson toward your target. Want one double lesson? Choose 1.` Raise the target to 4 and continue to review without adding picks. Expected: the not-enough message and **no** reminder (Singles only). Use **Add slots** to add one Double (3 picks, still short of 4) and return to review. Expected: `A double lesson counts as one pick.` appears.
5. `resize_window` preset `desktop`. Take one screenshot of step 2's review in Hebrew (`await document.fonts.ready` first) and send it with `SendUserFile` (`display: "render"`) captioned "The customer's case, with the new reminder." If screenshots hang, send the `get_page_text` of `.review__not-enough` instead.

- [ ] **Step 3: Full checks**

```bash
dotnet build
dotnet test
dotnet ef migrations has-pending-model-changes --project src\DrivingLessons.Infrastructure --startup-project src\DrivingLessons.Presentation.Web
```

From `client\`: `node.exe node_modules\@angular\cli\bin\ng.js test --watch=false` and `node.exe node_modules\@angular\cli\bin\ng.js build --project client` (the healthy `C:\Program Files\nodejs\node.exe`).

Expected: no new build warnings, every backend and client test passes, `No changes have been made to the model since the last migration.`, and the client build succeeds within its budgets.

- [ ] **Step 4: Clean up**

`preview_stop` both servers. Remove the `api-smoke` entry from `.claude\launch.json`. Run `docker exec drivinglessonsbooking-postgres-1 dropdb -U app drivinglessons_feedback_smoke` and `rm -rf .superpowers/sdd/feedback-smoke`. Then `git status --short`. Expected: clean, apart from any unrelated local edit to `.claude/launch.json`.

- [ ] **Step 5: Push and open the PR** (only once your human partner approves pushing)

```bash
git push -u origin customer-feedback-excel-and-target
gh pr create --base main --title "Customer feedback: leaner Excel detail sheet, and a double lesson counts once" --body "<body>"
```

The body has these parts:
- **Summary.** Detail sheet: National ID, Phone and Transmission removed; Car and 'שיעורים מבוקשים' (`{target} מתוך {picks}`) added. Student form: the target step and the not-enough message say a Double counts as one lesson toward the target; the counting rule is unchanged.
- **Notes.** ADR 0003 amended; requirements §7, §8.1 and §9 updated; no migration, no new packages.
- **Test plan.** The Excel unit tests; the API smoke rows; the 360px Hebrew and English student-form checks; the full backend and client suites.
- The attribution line `🤖 Generated with [Claude Code](https://claude.com/claude-code)`.

Auto-merge is disabled in this repo; your human partner merges by hand with `--merge`.
